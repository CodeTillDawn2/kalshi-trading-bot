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
using BacklashBotData.Data;
using BacklashDTOs.Configuration;

namespace KalshiBotTasks
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
    /// - Updates the appsettings.json configuration file with the latest schema version
    /// - Validates that the schema can be deserialized and matches expected structure
    ///
    /// This ensures that schema versioning works correctly for snapshot data persistence
    /// and that configuration remains synchronized with database schema versions.
    /// </remarks>
    [TestFixture]
    public class SchemaDeployment
    {
        private BacklashBotContext _context;
        private SchemaDeploymentObj _schemaDeployment;
        private IConfigurationRoot _configuration;
        private string basePath;

        /// <summary>
        /// Initializes the test environment by setting up dependency injection and database context.
        /// Loads configuration from appsettings files and configures services needed for schema deployment testing.
        /// </summary>
        /// <remarks>
        /// This setup method:
        /// - Builds initial configuration to read deployment settings (BasePath, JsonWriteIndented)
        /// - Reads configured base path from Deployment:BasePath with fallback to default
        /// - Validates base path configuration to prevent null reference exceptions
        /// - Builds final configuration from JSON files in the configured BacklashBot directory
        /// - Registers the database context with SQL Server connection
        /// - Adds logging services for test output
        /// - Registers the SchemaDeploymentObj service for testing
        /// - Configures GeneralExecutionConfig from configuration section
        /// - Creates the service provider and initializes database context
        /// - Ensures the database schema is created and accessible
        ///
        /// The test database is isolated and uses the same configuration as the main application
        /// to ensure realistic testing conditions.
        /// </remarks>
        [SetUp]
        public void Setup()
        {
            // Load configuration with default basePath first to read Deployment config
            string tempBasePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
            var tempConfig = new ConfigurationBuilder()
                .SetBasePath(tempBasePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Read configured basePath
            basePath = tempConfig["Deployment:BasePath"] ?? tempBasePath;

            // Validate basePath
            if (string.IsNullOrWhiteSpace(basePath))
            {
                throw new InvalidOperationException("BasePath configuration is required and cannot be null or empty.");
            }

            // Load configuration with configured basePath
            _configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Setup dependency injection
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(_configuration);
            serviceCollection.AddDbContext<BacklashBotContext>(options =>
                options.UseSqlServer(_configuration["DBConnection:DefaultConnection"]));
            serviceCollection.AddLogging(logging => logging.AddConsole());
            serviceCollection.AddScoped<SchemaDeploymentObj>();
            serviceCollection.Configure<GeneralExecutionConfig>(_configuration.GetSection("Central:GeneralExecution"));
            serviceCollection.AddSingleton(resolver => resolver.GetRequiredService<IOptions<GeneralExecutionConfig>>().Value);

            IServiceProvider _serviceProvider = serviceCollection.BuildServiceProvider();

            // Initialize context and schema deployment
            _context = _serviceProvider.GetRequiredService<BacklashBotContext>();
            var logger = _serviceProvider.GetRequiredService<ILogger<SchemaDeployment>>();
            var generalExecutionConfig = _serviceProvider.GetRequiredService<GeneralExecutionConfig>();
            _schemaDeployment = new SchemaDeploymentObj(_context, logger, generalExecutionConfig, _configuration);

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
        /// - Updates application configuration with the latest schema version using configurable paths
        /// - Uses configurable JSON serialization options for configuration file updates
        /// - Provides logging for deployment operations and error conditions
        ///
        /// The service ensures that schema changes are tracked and versioned properly
        /// for data compatibility and migration purposes in the trading system.
        /// Configuration options include base paths and JSON formatting settings.
        /// </remarks>
        public class SchemaDeploymentObj
        {
            private readonly BacklashBotContext _context;
            private readonly ILogger<SchemaDeployment> _logger;
            private readonly GeneralExecutionConfig _generalExecutionConfig;
            private readonly IConfigurationRoot _configuration;

            /// <summary>
            /// Initializes a new instance of the SchemaDeploymentObj with required dependencies.
            /// </summary>
            /// <param name="context">Database context for schema persistence operations.</param>
            /// <param name="logger">Logger for recording deployment operations and errors.</param>
            /// <param name="generalExecutionConfig">Configuration settings for general execution operations.</param>
            /// <param name="configuration">Application configuration root for accessing settings.</param>
            /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
            /// <remarks>
            /// The constructor validates all dependencies to ensure the service is properly initialized.
            /// This prevents runtime errors during schema deployment operations.
            /// </remarks>
            public SchemaDeploymentObj(BacklashBotContext context, ILogger<SchemaDeployment> logger, GeneralExecutionConfig generalExecutionConfig, IConfigurationRoot configuration)
            {
                _context = context ?? throw new ArgumentNullException(nameof(context));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _generalExecutionConfig = generalExecutionConfig ?? throw new ArgumentNullException(nameof(generalExecutionConfig));
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
            /// - Updates the appsettings.json file with the new schema version
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

                    // Update appsettings.json with new SchemaVersion
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
            /// Updates the appsettings.json configuration file with the new snapshot schema version.
            /// Navigates the JSON structure to update the Central:GeneralExecution:SnapshotSchemaVersion setting.
            /// </summary>
            /// <param name="newVersion">The new schema version number to write to the configuration file.</param>
            /// <remarks>
            /// This method performs the following operations:
            /// - Reads the configured base path from Deployment:BasePath configuration with fallback
            /// - Constructs the path to appsettings.json in the configured BacklashBot directory
            /// - Validates that the file exists, logging a warning if not found
            /// - Reads the JSON content from the file
            /// - Parses the JSON into a JsonNode for manipulation
            /// - Navigates to or creates the "Snapshots" section in the JSON
            /// - Updates the "SnapshotSchemaVersion" property with the new version
            /// - Reads configured JSON serialization options from Deployment:JsonWriteIndented
            /// - Serializes the updated JSON back to the file with configured formatting
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
                    // Read configured basePath from configuration
                    string basePath = _configuration["Deployment:BasePath"] ?? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
                    string fullAppSettingsPath = Path.Combine(basePath, "appsettings.json");

                    var filePath = fullAppSettingsPath;
                    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    {
                        _logger.LogWarning("appsettings.json file not found at {FilePath}.", filePath);
                        Assert.Fail("appsettings.json file not found.");
                        return;
                    }

                    // Read and parse the JSON file
                    var jsonContent = await File.ReadAllTextAsync(filePath);
                    var jsonNode = JsonNode.Parse(jsonContent, new JsonNodeOptions { PropertyNameCaseInsensitive = true });

                    if (jsonNode == null)
                    {
                        _logger.LogError("Failed to parse appsettings.json.");
                        Assert.Fail("Failed to parse appsettings.json.");
                        return;
                    }

                    // Navigate to Central:GeneralExecution:SnapshotSchemaVersion
                    var centralNode = jsonNode["Central"] as JsonObject;
                    if (centralNode == null)
                    {
                        centralNode = new JsonObject();
                        jsonNode["Central"] = centralNode;
                    }

                    var generalExecutionNode = centralNode["GeneralExecution"] as JsonObject;
                    if (generalExecutionNode == null)
                    {
                        generalExecutionNode = new JsonObject();
                        centralNode["GeneralExecution"] = generalExecutionNode;
                    }

                    // Update SnapshotSchemaVersion
                    generalExecutionNode["SnapshotSchemaVersion"] = newVersion;

                    // Read configured JSON serialization options
                    var writeIndented = _configuration.GetValue<bool>("Deployment:JsonWriteIndented", true);
                    var options = new JsonSerializerOptions { WriteIndented = writeIndented };
                    var updatedJson = jsonNode.ToJsonString(options);
                    await File.WriteAllTextAsync(filePath, updatedJson);

                    _logger.LogInformation("Updated appsettings.json with SnapshotSchemaVersion {NewVersion}.", newVersion);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update appsettings.json with new SnapshotSchemaVersion {NewVersion}. Exception: {ex}. Stack Trace: {st}", newVersion, ex.Message, ex.StackTrace);
                    Assert.Fail("Failed to update appsettings.json with new SnapshotSchemaVersion.");
                    // Do not throw; allow deployment to succeed even if JSON update fails
                }
            }
        }
    }
}
