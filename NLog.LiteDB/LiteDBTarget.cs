using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using LiteDB;
using NLog.Common;
using NLog.Config;
using NLog.Targets;

namespace NLog.LiteDB
{
    /// <summary>
    /// NLog message target for LiteDB.
    /// </summary>
    [Target("LiteDBTarget")]
    public class LiteDBTarget : Target
    {
        private static readonly ConcurrentDictionary<string, LiteCollection<BsonDocument>> _collectionCache = new ConcurrentDictionary<string, LiteCollection<BsonDocument>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDBTarget"/> class.
        /// </summary>
        public LiteDBTarget()
        {
            Fields = new List<LiteDBField>();
            Properties = new List<LiteDBField>();
            IncludeDefaults = true;
        }

        /// <summary>
        /// Gets the fields collection.
        /// </summary>
        /// <value>
        /// The fields.
        /// </value>
        [ArrayParameter(typeof(LiteDBField), "field")]
        public IList<LiteDBField> Fields { get; private set; }

        /// <summary>
        /// Gets the properties collection.
        /// </summary>
        /// <value>
        /// The properties.
        /// </value>
        [ArrayParameter(typeof(LiteDBField), "property")]
        public IList<LiteDBField> Properties { get; private set; }

        /// <summary>
        /// Gets or sets the connection string name string.
        /// </summary>
        /// <value>
        /// The connection name string.
        /// </value>
        public string ConnectionString
        {
            get { return _connectionString ?? "NLog.db"; }
            set { _connectionString = value; }
        }
        private string _connectionString;

        /// <summary>
        /// Gets or sets the name of the connection.
        /// </summary>
        /// <value>
        /// The name of the connection.
        /// </value>
        public string ConnectionName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use the default document format.
        /// </summary>
        /// <value>
        ///   <c>true</c> to use the default document format; otherwise, <c>false</c>.
        /// </value>
        public bool IncludeDefaults { get; set; }


        /// <summary>
        /// Gets or sets the name of the collection.
        /// </summary>
        /// <value>
        /// The name of the collection.
        /// </value>
        public string CollectionName
        {
            get { return _connectionName ?? "log"; }
            set { _connectionName = value; }
        }
        private string _connectionName;

        /// <summary>
        /// Gets or sets the name of the User.
        /// </summary>
        /// <value>
        /// The name of the User.
        /// </value>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the Password.
        /// </summary>
        /// <value>
        /// The User's Password.
        /// </value>
        public string Password { get; set; }

        /// <summary>
        /// Initializes the target. Can be used by inheriting classes
        /// to initialize logging.
        /// </summary>
        /// <exception cref="NLog.NLogConfigurationException">Can not resolve LiteDB ConnectionString. Please make sure the ConnectionString property is set.</exception>
        protected override void InitializeTarget()
        {
            base.InitializeTarget();

            if (!string.IsNullOrEmpty(ConnectionName))
                ConnectionString = GetConnectionString(ConnectionName);

            if (string.IsNullOrEmpty(ConnectionString))
                throw new NLogConfigurationException("Can not resolve LiteDB ConnectionString. Please make sure the ConnectionString property is set.");

        }

        /// <summary>
        /// Writes an array of logging events to the log target. By default it iterates on all
        /// events and passes them to "Write" method. Inheriting classes can use this method to
        /// optimize batch writes.
        /// </summary>
        /// <param name="logEvents">Logging events to be written out.</param>
        protected override void Write(AsyncLogEventInfo[] logEvents)
        {
            if (logEvents.Length == 0)
                return;

            try
            {
                var documents = logEvents.Select(e => CreateDocument(e.LogEvent));

                var collection = GetCollection();
                collection.Insert(documents);

                foreach (var ev in logEvents)
                    ev.Continuation(null);

            }
            catch (Exception ex)
            {
                if (ex is StackOverflowException || ex is ThreadAbortException || ex is OutOfMemoryException || ex is NLogConfigurationException)
                    throw;

                InternalLogger.Error("Error when writing to LiteDB {0}", ex);

                foreach (var ev in logEvents)
                    ev.Continuation(ex);

            }
        }

        /// <summary>
        /// Writes logging event to the log target.
        /// classes.
        /// </summary>
        /// <param name="logEvent">Logging event to be written out.</param>
        protected override void Write(LogEventInfo logEvent)
        {
            try
            {
                var document = CreateDocument(logEvent);
                var collection = GetCollection();
                collection.Insert(document);
            }
            catch (Exception ex)
            {
                if (ex is StackOverflowException || ex is ThreadAbortException || ex is OutOfMemoryException || ex is NLogConfigurationException)
                    throw;

                InternalLogger.Error("Error when writing to LiteDB {0}", ex);
            }
        }


        private BsonDocument CreateDocument(LogEventInfo logEvent)
        {
            var document = new BsonDocument();
            if (IncludeDefaults || Fields.Count == 0)
                AddDefaults(document, logEvent);

            // extra fields
            foreach (var field in Fields)
            {
                var value = GetValue(field, logEvent);
                if (value != null)
                    document[field.Name] = value;
            }

            AddProperties(document, logEvent);

            return document;
        }

        private void AddDefaults(BsonDocument document, LogEventInfo logEvent)
        {
            document.Add("Date", new BsonValue(logEvent.TimeStamp));

            if (logEvent.Level != null)
                document.Add("Level", new BsonValue(logEvent.Level.Name));

            if (logEvent.LoggerName != null)
                document.Add("Logger", new BsonValue(logEvent.LoggerName));

            if (logEvent.FormattedMessage != null)
                document.Add("Message", new BsonValue(logEvent.FormattedMessage));

            if (logEvent.Exception != null)
                document.Add("Exception", CreateException(logEvent.Exception));


        }

        private void AddProperties(BsonDocument document, LogEventInfo logEvent)
        {
            var propertiesDocument = new BsonDocument();
            foreach (var field in Properties)
            {
                string key = field.Name;
                var value = GetValue(field, logEvent);

                if (value != null)
                    propertiesDocument[key] = value;
            }

            var properties = logEvent.Properties ?? Enumerable.Empty<KeyValuePair<object, object>>();
            foreach (var property in properties)
            {
                if (property.Key == null || property.Value == null)
                    continue;

                string key = Convert.ToString(property.Key, CultureInfo.InvariantCulture);
                string value = Convert.ToString(property.Value, CultureInfo.InvariantCulture);

                if (!string.IsNullOrEmpty(value))
                    propertiesDocument[key] = new BsonValue(value);
            }

            if (propertiesDocument.Count > 0)
                document.Add("Properties", propertiesDocument);

        }

        private BsonValue CreateException(Exception exception)
        {
            if (exception == null)
                return new BsonValue();

            var document = new BsonDocument();
            document.Add("Message", new BsonValue(exception.Message));
            document.Add("BaseMessage", new BsonValue(exception.GetBaseException().Message));
            document.Add("Text", new BsonValue(exception.ToString()));
            document.Add("Type", new BsonValue(exception.GetType().ToString()));

            var external = exception as ExternalException;
            if (external != null)
                document.Add("ErrorCode", new BsonValue(external.ErrorCode));

            document.Add("Source", new BsonValue(exception.Source));

            MethodBase method = exception.TargetSite;
            if (method != null)
            {
                document.Add("MethodName", new BsonValue(method.Name));

                AssemblyName assembly = method.Module.Assembly.GetName();
                document.Add("ModuleName", new BsonValue(assembly.Name));
                document.Add("ModuleVersion", new BsonValue(assembly.Version.ToString()));
            }

            return document;
        }


        private BsonValue GetValue(LiteDBField field, LogEventInfo logEvent)
        {
            var value = field.Layout.Render(logEvent);
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = value.Trim();

            if (string.IsNullOrEmpty(field.BsonType)
                || string.Equals(field.BsonType, "String", StringComparison.OrdinalIgnoreCase))
                return new BsonValue(value);



            return new BsonValue(value);
        }

        private LiteCollection<BsonDocument> GetCollection()
        {
            // cache lite collection based on target name.
            string key = string.Format("k|{0}|{1}|{2}",
                ConnectionName ?? string.Empty,
                ConnectionString ?? string.Empty,
                CollectionName ?? string.Empty);

            return _collectionCache.GetOrAdd(key, k =>
            {
                // create collection
                var db = new LiteDatabase(ConnectionString);


                string collectionName = CollectionName ?? "Log";

                return db.GetCollection<BsonDocument>(collectionName);
            });
        }


        private static string GetConnectionString(string connectionName)
        {
            if (connectionName == null)
                throw new ArgumentNullException(nameof(connectionName));

            var settings = ConfigurationManager.ConnectionStrings[connectionName];
            if (settings == null)
                throw new NLogConfigurationException($"No connection string named '{connectionName}' could be found in the application configuration file.");

            string connectionString = settings.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
                throw new NLogConfigurationException($"The connection string '{connectionName}' in the application's configuration file does not contain the required connectionString attribute.");

            return settings.ConnectionString;
        }

    }
}
