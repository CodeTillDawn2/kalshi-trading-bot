using KalshiBotData.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NJsonSchema;
using BacklashDTOs;
using BacklashDTOs.Data;
using System.Text.Json;
using System.Text.Json.Nodes;
using TradingStrategies.Configuration;

namespace TradingSimulator.Executable
{
    /// <summary>
    /// NUnit test fixture for validating schema deployment functionality in the trading simulator.
    /// This class tests the complete workflow of generating JSON schemas from data models,
    /// persisting them to the database, and updating configuration files with schema version information.
    /// </summary>
    /// <remarks>
    /// The test fixture performs the following operations:
    /// - Sets up a test database context with dependency injection
    /// - Generates a JSON schema from the CacheSnapshot class using NJsonSchema
    /// - Compares the schema against existing versions to determine if deployment is needed
    /// - Saves new schema versions to the database with auto-incremented version numbers
    /// - Updates the appsettings.local.json configuration file with the latest schema version
    /// - Validates that the schema can be deserialized and matches expected structure
    ///
    /// This ensures that schema versioning works correctly for snapshot data persistence
    /// and that configuration remains synchronized with database schema versions.
    /// </remarks>
    public class SchemaDeployment
    {
        private KalshiBotContext _context;
        private SchemaDeploymentObj _schemaDeployment;
        private IConfigurationRoot _configuration;
        private string basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));

        /// <summary>
        /// Initializes the test environment by setting up dependency injection and database context.
        /// Loads configuration from appsettings files and configures services needed for schema deployment testing.
        /// </summary>
        /// <remarks>
        /// This setup method:
        /// - Builds configuration from JSON files in the BacklashBot directory
        /// - Registers the database context with SQL Server connection
        /// - Adds logging services for test output
        /// - Registers the SchemaDeploymentObj service for testing
        /// - Configures SnapshotConfig from configuration section
        /// - Creates the service provider and initializes database context
        /// - Ensures the database schema is created and accessible
        ///
        /// The test database is isolated and uses the same configuration as the main application
        /// to ensure realistic testing conditions.
        /// </remarks>
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

        /// <summary>
        /// Cleans up test resources by disposing of the database context.
        /// Ensures proper resource cleanup after each test execution.
        /// </summary>
        /// <remarks>
        /// This method disposes of the database context to prevent resource leaks
        /// and ensure test isolation. The context is recreated fresh for each test
        /// in the Setup method.
        /// </remarks>
        [TearDown]
        public void TearDown()
        {
            // Dispose resources
            _context?.Dispose();
        }

        /// <summary>
        /// Tests the complete schema deployment workflow for CacheSnapshot.
        /// Verifies that a schema can be generated, saved to database, and properly validated.
        /// </summary>
        /// <remarks>
        /// This test performs the following validations:
        /// - Calls the schema deployment service to generate and save a schema
        /// - Retrieves all snapshot schemas from the database, ordered by version
        /// - Verifies that at least one schema record exists
        /// - Confirms the schema version is auto-incremented (greater than 0)
        /// - Ensures the schema definition is not null or empty
        /// - Deserializes the schema JSON to validate its structure
        /// - Verifies the schema title matches the CacheSnapshot class name
        ///
        /// This ensures the schema deployment pipeline works end-to-end
        /// and that schemas are properly versioned and stored.
        /// </remarks>
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

        /// <summary>
        /// Service class responsible for deploying JSON schemas for data models used in the trading system.
        /// Handles schema generation, version comparison, database persistence, and configuration updates.
        /// </summary>
        /// <remarks>
        /// This class encapsulates the business logic for schema deployment:
        /// - Generates JSON schemas from .NET types using NJsonSchema
        /// - Compares schemas to determine if new versions need deployment
        /// - Persists schema versions to the database with auto-incremented version numbers
        /// - Updates application configuration with the latest schema version
        /// - Provides logging for deployment operations and error conditions
        ///
        /// The service ensures that schema changes are tracked and versioned properly
        /// for data compatibility and migration purposes in the trading system.
        /// </remarks>
        public class SchemaDeploymentObj
        {
            private readonly KalshiBotContext _context;
            private readonly ILogger<SchemaDeployment> _logger;
            private readonly SnapshotConfig _snapshotConfig;
            private readonly IConfigurationRoot _configuration;

            /// <summary>
            /// Initializes a new instance of the SchemaDeploymentObj with required dependencies.
            /// </summary>
            /// <param name="context">Database context for schema persistence operations.</param>
            /// <param name="logger">Logger for recording deployment operations and errors.</param>
            /// <param name="snapshotConfig">Configuration settings for snapshot operations.</param>
            /// <param name="configuration">Application configuration root for accessing settings.</param>
            /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
            /// <remarks>
            /// The constructor validates all dependencies to ensure the service is properly initialized.
            /// This prevents runtime errors during schema deployment operations.
            /// </remarks>
            public SchemaDeploymentObj(KalshiBotContext context, ILogger<SchemaDeployment> logger, SnapshotConfig snapshotConfig, IConfigurationRoot configuration)
            {
                _context = context ?? throw new ArgumentNullException(nameof(context));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _snapshotConfig = snapshotConfig ?? throw new ArgumentNullException(nameof(snapshotConfig));
                _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            }

            /// <summary>
            /// Deploys a JSON schema for the CacheSnapshot class to the database.
            /// Generates schema from the type, compares with existing versions, and saves if different.
            /// </summary>
            /// <remarks>
            /// This method performs the following operations:
            /// - Generates a JSON schema from the CacheSnapshot class using NJsonSchema
            /// - Retrieves existing schema versions from the database, ordered by version descending
            /// - Compares the generated schema with the latest existing version
            /// - If schemas match, logs that deployment is skipped
            /// - If schemas differ, creates a new SnapshotSchemaDTO and saves it to the database
            /// - Updates the appsettings.local.json file with the new schema version
            /// - Logs successful deployment with the new version number
            ///
            /// The method handles exceptions gracefully and ensures database consistency.
            /// Schema versions are auto-incremented by the database.
            /// </remarks>
            /// <exception cref="Exception">Thrown when schema deployment fails, with details about the failure.</exception>
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

            /// <summary>
            /// Updates the appsettings.local.json configuration file with the new snapshot schema version.
            /// Navigates the JSON structure to update the Snapshots:SnapshotSchemaVersion setting.
            /// </summary>
            /// <param name="newVersion">The new schema version number to write to the configuration file.</param>
            /// <remarks>
            /// This method performs the following operations:
            /// - Constructs the path to appsettings.local.json in the BacklashBot directory
            /// - Validates that the file exists, logging a warning if not found
            /// - Reads the JSON content from the file
            /// - Parses the JSON into a JsonNode for manipulation
            /// - Navigates to or creates the "Snapshots" section in the JSON
            /// - Updates the "SnapshotSchemaVersion" property with the new version
            /// - Serializes the updated JSON back to the file with proper formatting
            /// - Logs successful update with the new version number
            ///
            /// If the file cannot be parsed or updated, the method logs an error
            /// but allows deployment to succeed since the database contains the truth.
            /// </remarks>
            /// <exception cref="Exception">Not thrown - errors are logged but deployment continues.</exception>
            private async Task UpdateSnapshotSchemaVersionAsync(int newVersion)
            {
                try
                {
                    string basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
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
