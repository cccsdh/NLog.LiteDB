using System;
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
        private string _connectionString;
        private string _collectionName;


        [TestInitialize]
        public void Init()
        {
            var targets = LogManager.Configuration.AllTargets;

            var target = targets.First() as LiteDBTarget;

            _connectionString = target.ConnectionString;
            _collectionName = target.CollectionName;






            _db = new LiteDatabase(_connectionString);
        }

		[TestCleanup]
		public void CleanUp()
		{

        }

		[TestMethod]
		public void Test_DynamicFields()
		{

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



    }
}