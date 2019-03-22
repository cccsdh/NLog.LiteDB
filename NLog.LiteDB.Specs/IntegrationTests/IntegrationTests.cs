using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using FluentAssertions;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace NLog.LiteDB.Specs.IntegrationTests
{
	[TestClass]
	public class IntegrationTests
	{
		private LiteDatabase _db;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private List<NLog.Targets.Target> _targets;
        private LiteDBTarget _pathTarget;
        private LiteDBTarget _fileTarget;
        private LiteDBTarget _legacyTarget;
        private LiteDBTarget _specialTarget;
        private string _connectionString;
        private string _collectionName;


        [TestInitialize]
        public void Init()
        {
            //get all the targets
            _targets = LogManager.Configuration.AllTargets.ToList();

            foreach (var target in _targets)
            {
                if(target.Name == "path")
                {
                    _pathTarget = target as LiteDBTarget;
                }
                if (target.Name == "special")
                {
                    _specialTarget = target as LiteDBTarget;
                }
                if (target.Name == "file")
                {
                    _fileTarget = target as LiteDBTarget;
                }
                if (target.Name == "legacy")
                {
                    _legacyTarget = target as LiteDBTarget;
                }
            }

        }

		[TestCleanup]
		public void CleanUp()
		{

        }

		[TestMethod]
		public void Test_PathTarget()
		{
            _collectionName = _pathTarget.CollectionName;
            _connectionString = _pathTarget.ConnectionString;

            _db = new LiteDatabase(_connectionString);

            //clear log collection.
            _db.DropCollection(_collectionName);

            logger.Info(new Exception("Test Exception", new Exception("Inner Exception")), "Test Log Message");

			Thread.Sleep(2000);
            var collection = _db.GetCollection(_collectionName);
            collection.Count().Should().Be(1);
            var logEntry = collection.Find(Query.All()).First();


			logEntry["Level"].Should().Be(LogLevel.Info.ToString());
			logEntry["Message"].Should().Be("Test Log Message");

            var logException = logEntry["Exception"].AsDocument;
            logException["Message"].Should().Be("Test Exception");


		}

        [TestMethod]
        public void Test_FileTarget()
        {
            _collectionName = _fileTarget.CollectionName;
            _connectionString = _fileTarget.ConnectionString;

            _db = new LiteDatabase(_connectionString);

            //clear log collection.
            _db.DropCollection(_collectionName);

            logger.Error(new Exception("Test Exception", new Exception("Inner Exception")), "Test Log Message");

            Thread.Sleep(2000);
            var collection = _db.GetCollection(_collectionName);
            collection.Count().Should().Be(1);
            var logEntry = collection.Find(Query.All()).First();


            logEntry["Level"].Should().Be(LogLevel.Error.ToString());
            logEntry["Message"].Should().Be("Test Log Message");

            var logException = logEntry["Exception"].AsDocument;
            logException["Message"].Should().Be("Test Exception");


        }
        [TestMethod]
        public void Test_LegacyTarget()
        {
            _collectionName = _fileTarget.CollectionName;
            _connectionString = _fileTarget.ConnectionString;

            _db = new LiteDatabase(_connectionString);

            //clear log collection.
            _db.DropCollection(_collectionName);

            logger.Error(new Exception("Test Exception", new Exception("Inner Exception")), "Test Log Message");

            Thread.Sleep(2000);
            var collection = _db.GetCollection(_collectionName);
            collection.Count().Should().Be(1);
            var logEntry = collection.Find(Query.All()).First();


            logEntry["Level"].Should().Be(LogLevel.Error.ToString());
            logEntry["Message"].Should().Be("Test Log Message");

            var logException = logEntry["Exception"].AsDocument;
            logException["Message"].Should().Be("Test Exception");


        }


        [TestMethod]
        public void Test_SpecialTarget()
        {
            _collectionName = _specialTarget.CollectionName;
            _connectionString = _specialTarget.ConnectionString;

            _db = new LiteDatabase(_connectionString);

            //clear log collection.
            _db.DropCollection(_collectionName);

            logger.Trace(new Exception("Test Exception", new Exception("Inner Exception")), "Test Log Message");

            Thread.Sleep(2000);
            var collection = _db.GetCollection(_collectionName);
            collection.Count().Should().Be(1);
            var logEntry = collection.Find(Query.All()).First();


            logEntry["Level"].Should().Be(LogLevel.Trace.ToString());
            logEntry["Message"].Should().Be("Test Log Message");

            var logException = logEntry["Exception"].AsDocument;
            logException["Message"].Should().Be("Test Exception");


        }



    }
}