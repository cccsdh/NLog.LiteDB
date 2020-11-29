﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using LiteDB;
using NLog.Common;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using NLog.LiteDB.Extensions;

namespace NLog.LiteDB
{
    /// <summary>
    /// NLog message target for LiteDB.
    /// reworked for buffering changes based on NLog.Targets.Wrappers.BufferingTargetWrapper.cs
    /// </summary>
    [Target("LiteDBTarget")]
    public class LiteDBTarget : Target
    {
        private LogEventInfoBuffer _buffer;
        private Timer _flushTimer;
        private readonly object _lockObject = new object();
        private int _count;

        public const int Default_Buffer_Size = 100;
        public const int Min_Buffer_Size = 1;
        public const int Max_Buffer_Size = 100;


        
        /// The filename as target
        /// </summary>
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

        ///// <summary>
        ///// Gets or sets the connection string name string.
        ///// </summary>
        ///// <value>
        ///// The connection name string.
        ///// </value>
        public string ConnectionString
        {
            get { return _connectionString ?? "NLog.db"; }
            set { _connectionString = ParseConnectionString(value); }
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
        public bool IncludeDefaults
        {
            get { return _journaling ?? true; }
            set { _journaling = value; }
        }

        public bool IsJournaling { get; set; } = false;
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
        /// <summary>
        /// Gets or sets the number of log events to be buffered.
        /// </summary>
        [DefaultValue(100)]
        public int BufferSize { get; set; } = 100;
        /// <summary>
        /// Gets or Sets the timeout (in milliseconds) after which the contents of the buffer will be flushed
        /// if there's no write in the specified period of time.  Use -1 to disable timed flushes.
        /// </summary>
        [DefaultValue(-1)]
        public int FlushTimeout { get; set; } = -1;
        /// <summary>
        /// Gets or sets a value indicating whether to use sliding timeout.
        /// </summary>
        /// <remarks>
        /// This value determines how the inactivity period is determined. If sliding timeout is enabled,
        /// the inactivity timer is reset after each write, if it is disabled - inactivity timer will 
        /// count from the first event written to the buffer. 
        /// </remarks>
        /// <docgen category='Buffering Options' order='100' />
        [DefaultValue(true)]
        public bool SlidingTimeout { get; set; } = true;

        /// <summary>
        /// Gets or sets the action to take if the buffer overflows.
        /// </summary>
        /// <remarks>
        /// Setting to <see cref="BufferingTargetWrapperOverflowAction.Discard"/> will replace the
        /// oldest event with new events without sending events down to the wrapped target, and
        /// setting to <see cref="BufferingTargetWrapperOverflowAction.Flush"/> will flush the
        /// entire buffer to the wrapped target.
        /// </remarks>
        /// <docgen category='Buffering Options' order='100' />
        [DefaultValue("Flush")]
        public BufferingTargetWrapperOverflowAction OverflowAction { get; set; } = BufferingTargetWrapperOverflowAction.Flush;
        private string _connectionName;
        private bool? _journaling;

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
        /// Flushes pending events in the buffer (if any), followed by flushing the WrappedTarget.
        /// </summary>
        /// <param name="asyncContinuation">The asynchronous continuation.</param>
        protected override void FlushAsync(AsyncContinuation asyncContinuation)
        {
            WriteEventsInBuffer("Flush Async");
            base.FlushAsync(asyncContinuation);
        }
        /// <summary>
        /// Closes the target by flushing pending events in the buffer (if any).
        /// </summary>
        protected override void CloseTarget()
        {
            var currentTimer = _flushTimer;
            if (currentTimer != null)
            {
                _flushTimer = null;
                if (currentTimer.WaitForDispose(TimeSpan.FromSeconds(1)))
                {
                    WriteEventsInBuffer("Closing Target");
                }
            }

            base.CloseTarget();
        }
        private void FlushCallback(object state)
        {
            bool lockTaken = false;

            try
            {
                int timeoutMilliseconds = Math.Min(FlushTimeout / 2, 100);
                lockTaken = Monitor.TryEnter(_lockObject, timeoutMilliseconds);
                if (lockTaken)
                {
                    if (_flushTimer == null)
                        return;

                    WriteEventsInBuffer(null);
                }
                else
                {
                    if (_count > 0)
                        _flushTimer?.Change(FlushTimeout, -1);   // Schedule new retry timer
                }
            }
            catch (Exception exception)
            {
                InternalLogger.Error(exception, "BufferingWrapper(Name={0}): Error in flush procedure.", Name);

            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(_lockObject);
                }
            }
        }

        private void WriteEventsInBuffer(string reason)
        {


            lock (_lockObject)
            {
                AsyncLogEventInfo[] logEvents = _buffer.GetEventsAndClear();
                if (logEvents.Length > 0)
                {
                    if (reason != null)
                        InternalLogger.Trace("BufferingWrapper(Name={0}): Writing {1} events ({2})", Name, logEvents.Length, reason);
                    SendBatch(logEvents);
                }
            }
        }
        private void SendBatch(IEnumerable<AsyncLogEventInfo> logEvents)
        {

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
        /// Initializes the target. Can be used by inheriting classes
        /// to initialize logging.
        /// </summary>
        /// <exception cref="NLog.NLogConfigurationException">Can not resolve LiteDB ConnectionString. Please make sure the ConnectionString property is set.</exception>
        protected override void InitializeTarget()
        {
            base.InitializeTarget();

            _buffer = new LogEventInfoBuffer(BufferSize, false, 0);
            InternalLogger.Trace("BufferingWrapper(Name={0}): Create Timer", Name);
            _flushTimer = new Timer(FlushCallback, null, Timeout.Infinite, Timeout.Infinite);

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
        protected override void Write(AsyncLogEventInfo logEvent)
        {
            PrecalculateVolatileLayouts(logEvent.LogEvent);

            _count = _buffer.Append(logEvent);
            if(_count >= BufferSize)
            {
                //if overflow actio set to "Discard", the buffer will automatically 
                //roll over the oldest item.
                if(OverflowAction == BufferingTargetWrapperOverflowAction.Flush)
                {
                    WriteEventsInBuffer("Exceeding BufferSize");
                }
            }
            else
            {
                if(FlushTimeout > 0 && (SlidingTimeout || _count == 1))
                {
                    //reset the timer on tfirst item added to the buffer or whenever Sliding Timeout is true
                    _flushTimer.Change(FlushTimeout, -1);
                }
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
        /// <summary>
        /// Parse the supported connection string 'types' into a proper
        /// LiteDB connection string
        /// </summary>
        /// <param name="connection"></param>
        /// <returns>Formatted Connectionstring</returns>
        private string ParseConnectionString(string connection)
        {
            var connectionString = connection.ToLower();
            var path = "";
            var isPath = false;
             if (connectionString.StartsWith("special="))
            {
                path = GetSpecial(connection.Replace("special=",""));
                isPath = true;
            }
            else if (connectionString.StartsWith("path="))
            {
                path = GetPath(connection.Replace("path=",""));
                isPath = true;
            }
            else if (connectionString.StartsWith("file="))
            {
                path = connection.Replace("file=", "");
            }
            else if (connectionString.StartsWith("filename="))
            {
                //legacy journaling off setting
                if(connectionString.Contains(";journal=false"))
                {
                    connection.Replace(";journal=false", "");
                    IsJournaling = false;
                }
                path = connection.Replace("filename=", "");
            }
            else
            {
                throw new ArgumentException($"{connection} is not properly formatted for the LiteDBTarget!");
            }

            //if this is a path type string.  
            //   Check if the directory exists, and if not create it.
            if (isPath)
            {
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }
            }
            return IsJournaling ? $"filename={path}" : $"filename={path};journal=false";
        }
        private string GetPath(string incoming)
        {
            if (Regex.IsMatch(incoming, @"^(?:[a-zA-Z]\:|\\\\[\w\.]+\\[\w.$]+)\\(?:[\w]+\\)*\w([\w.])+$"))
            {
                return incoming;
            }
            throw new ArgumentException($"Folder structure is not valid!  Please correct configuration.");
        }
        private string GetSpecial(string incoming)
        {
            Regex regex = new Regex(@"(?<=\{)(.*?)(?=\})(?=\})(.*?$)");
            var insideBrackets = "";
            var restOfPath = "";
            Match match = regex.Match(incoming);
            if (match.Success)
            {
                insideBrackets = match.Groups[1].Value;
                restOfPath = match.Groups[2].Value.TrimStart('}');
                try
                {
                    var folder = (Environment.SpecialFolder)Enum.Parse(typeof(Environment.SpecialFolder), insideBrackets);
                    var path = $"{Environment.GetFolderPath(folder)}{restOfPath}";
                    return path;
                }
                catch(Exception ex)
                {
                    throw new ArgumentException($"{insideBrackets} is not a valid SpecialFolder!  Full Message: {ex.Message}");
                }
            }
            throw new ArgumentException($"Special Folder failed to be retrieved!  Is the bracketing correct?");
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
