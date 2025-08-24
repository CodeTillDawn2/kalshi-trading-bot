using KalshiBotData.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NJsonSchema;
using SmokehouseDTOs;
using SmokehouseDTOs.Data;
using System.Text.Json;
using System.Text.Json.Nodes;
using TradingStrategies.Configuration;

namespace TradingSimulator.Executable
{
    public class SchemaDeployment
    {
        private KalshiBotContext _context;
        private SchemaDeploymentObj _schemaDeployment;
        private IConfigurationRoot _configuration;
        private string basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "SmokehouseBot"));

        [SetUp]
        public void Setup()
        {
            // Load configuration
            _configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
                .Build();

            // Setup dependency injection
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(_configuration);
            serviceCollection.AddDbContext<KalshiBotContext>(options =>
                options.UseSqlServer(_configuration.GetConnectionString("DefaultConnection")));
            serviceCollection.AddLogging(logging => logging.AddConsole());
            serviceCollection.AddScoped<SchemaDeploymentObj>();
            serviceCollection.Configure<SnapshotConfig>(_configuration.GetSection("Snapshots"));
            serviceCollection.AddSingleton(resolver => resolver.GetRequiredService<IOptions<SnapshotConfig>>().Value);

            IServiceProvider _serviceProvider = serviceCollection.BuildServiceProvider();

            // Initialize context and schema deployment
            _context = _serviceProvider.GetRequiredService<KalshiBotContext>();
            var logger = _serviceProvider.GetRequiredService<ILogger<SchemaDeployment>>();
            var snapshotConfig = _serviceProvider.GetRequiredService<SnapshotConfig>();
            _schemaDeployment = new SchemaDeploymentObj(_context, logger, snapshotConfig, _configuration);

            // Ensure database is accessible
            _context.Database.EnsureCreated();
        }

        [TearDown]
        public void TearDown()
        {
            // Dispose resources
            _context?.Dispose();
        }

        [Test]
        public async Task DeploySchema_InsertsSchemaRecord()
        {
            // Act
            await _schemaDeployment.DeploySchemaAsync();

            // Assert
            var savedSchemas = await _context.GetSnapshotSchemas();
            savedSchemas = savedSchemas.OrderByDescending(s => s.SchemaVersion).ToList();
            var savedSchema = savedSchemas.FirstOrDefault();
            Assert.That(savedSchema, Is.Not.Null, "Schema should be saved to database");
            Assert.That(savedSchema.SchemaVersion, Is.GreaterThan(0), "SchemaVersion should be auto-incremented");
            Assert.That(savedSchema.SchemaDefinition, Is.Not.Null, "SchemaDefinition should not be null");

            // Verify schema can be deserialized
            var schema = await JsonSchema.FromJsonAsync(savedSchema.SchemaDefinition);
            Assert.That(schema, Is.Not.Null, "Schema should be deserializable");
            Assert.That(schema.Title, Is.EqualTo(typeof(CacheSnapshot).Name), "Schema title should match CacheSnapshot");
        }

        public class SchemaDeploymentObj
        {
            private readonly KalshiBotContext _context;
            private readonly ILogger<SchemaDeployment> _logger;
            private readonly SnapshotConfig _snapshotConfig;
            private readonly IConfigurationRoot _configuration;

            public SchemaDeploymentObj(KalshiBotContext context, ILogger<SchemaDeployment> logger, SnapshotConfig snapshotConfig, IConfigurationRoot configuration)
            {
                _context = context ?? throw new ArgumentNullException(nameof(context));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _snapshotConfig = snapshotConfig ?? throw new ArgumentNullException(nameof(snapshotConfig));
                _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            }

            public async Task DeploySchemaAsync()
            {
                try
                {
                    // Generate schema from CacheSnapshot
                    var schema = JsonSchema.FromType<CacheSnapshot>();
                    var schemaData = schema.ToJson();

                    // Check for existing schema
                    var latestSnapshots = await _context.GetSnapshotSchemas();
                    var latestSnapshot = latestSnapshots.OrderByDescending(x => x.SchemaVersion)
                        .FirstOrDefault();

                    if (latestSnapshot != null)
                    {
                        if (latestSnapshot.SchemaDefinition == schemaData)
                        {
                            _logger.LogInformation("Schema matches version {SchemaVersion}, skipping deployment.", latestSnapshot.SchemaVersion);
                            Assert.Fail($"Schema already matches version {latestSnapshot.SchemaVersion}, skipping deployment.");
                            return;
                        }
                    }

                    // Create new SnapshotSchema entity
                    var snapshotSchema = new SnapshotSchemaDTO
                    {
                        SchemaDefinition = schemaData
                    };

                    // Add to context and save
                    snapshotSchema = await _context.AddSnapshotSchema(snapshotSchema);

                    // Update appsettings.local.json with new SchemaVersion
                    await UpdateSnapshotSchemaVersionAsync(snapshotSchema.SchemaVersion);

                    _logger.LogInformation("Successfully deployed CacheSnapshot schema with version {SchemaVersion}", snapshotSchema.SchemaVersion);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deploy CacheSnapshot schema");
                    throw;
                }
            }

            private async Task UpdateSnapshotSchemaVersionAsync(int newVersion)
            {
                try
                {
                    string basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "SmokehouseBot"));
                    string fullAppSettingsPath = Path.Combine(basePath, "appsettings.local.json");

                    var filePath = fullAppSettingsPath;
                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    {
                        _logger.LogWarning("appsettings.local.json file not found at {FilePath}.", filePath);
                        Assert.Fail("appsettings.local.json file not found.");
                        return;
                    }

                    // Read and parse the JSON file
                    var jsonContent = await File.ReadAllTextAsync(filePath);
                    var jsonNode = JsonNode.Parse(jsonContent, new JsonNodeOptions { PropertyNameCaseInsensitive = true });

                    if (jsonNode == null)
                    {
                        _logger.LogError("Failed to parse appsettings.local.json.");
                        Assert.Fail("Failed to parse appsettings.local.json.");
                        return;
                    }

                    // Navigate to Snapshots:SnapshotSchemaVersion
                    var snapshotsNode = jsonNode["Snapshots"] as JsonObject;
                    if (snapshotsNode == null)
                    {
                        snapshotsNode = new JsonObject();
                        jsonNode["Snapshots"] = snapshotsNode;
                    }

                    // Update SnapshotSchemaVersion
                    snapshotsNode["SnapshotSchemaVersion"] = newVersion;

                    // Write updated JSON back to file
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var updatedJson = jsonNode.ToJsonString(options);
                    await File.WriteAllTextAsync(filePath, updatedJson);

                    _logger.LogInformation("Updated appsettings.local.json with SnapshotSchemaVersion {NewVersion}.", newVersion);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update appsettings.local.json with new SnapshotSchemaVersion {NewVersion}. Exception: {ex}. Stack Trace: {st}", newVersion, ex.Message, ex.StackTrace);
                    Assert.Fail("Failed to update appsettings.local.json with new SnapshotSchemaVersion.");
                    // Do not throw; allow deployment to succeed even if JSON update fails
                }
            }
        }
    }
}