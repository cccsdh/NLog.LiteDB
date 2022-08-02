using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;


namespace NLog.LiteDB.Specs.UnitTests
{
	[TestClass]
	public class LiteDBTargetSpecs
	{

		[TestInitialize]
		public void TestTarget()
		{

		}

		[TestMethod]
		public void TestDefaultSettings()
		{
            new LiteDBTarget().ConnectionString
                .Should().Be("NLog.db");
            new LiteDBTarget().CollectionName
                .Should().Be("log");
		}
        [TestMethod]
        public void TestSettings_with_File_Style()
        {
            const string username = "someUser";
            const string password = "q198743n3d8yh32028##@!";
            const string connectionString = @"file=NLog.db";
            const string resultCS = @"filename=NLog.db;journal=false";
            const string connectionName = "litedb";
            const string collectionName = "loggerName";

            var target = new LiteDBTarget
            {
                UserName = username,
                Password = password,
                ConnectionString = connectionString,
                ConnectionName = connectionName,
                CollectionName = collectionName
            };


            target.UserName
                .Should().Be(username);
            target.Password
                .Should().Be(password);
            target.ConnectionString
                .Should().Be(resultCS);
            target.ConnectionName
                .Should().Be(connectionName);
            target.CollectionName
                .Should().Be(collectionName);
        }

        [TestMethod]
        public void TestSettings_with_path_Style()
        {
            const string username = "someUser";
            const string password = "q198743n3d8yh32028##@!";
            const string connectionString = @"path=c:\temp\NLog.db";
            const string resultCS = @"filename=c:\temp\NLog.db;journal=false";
            const string connectionName = "litedb";
            const string collectionName = "loggerName";

            var target = new LiteDBTarget
            {
                UserName = username,
                Password = password,
                ConnectionString = connectionString,
                ConnectionName = connectionName,
                CollectionName = collectionName
            };


            target.UserName
                .Should().Be(username);
            target.Password
                .Should().Be(password);
            target.ConnectionString
                .Should().Be(resultCS);
            target.ConnectionName
                .Should().Be(connectionName);
            target.CollectionName
                .Should().Be(collectionName);
        }

        [TestMethod]
        [Ignore]
		public void TestSettings_with_SpecialFolder_Style()
		{
			const string username = "someUser";
			const string password = "q198743n3d8yh32028##@!";
			const string connectionString = @"special={ApplicationData}\application\NLog.db";

            //Note - **user** should be updated to your userid before running this test.
            const string resultCS = @"C:\Users\**user**\AppData\Roaming\application\NLog.db";
            const string connectionName = "litedb";
			const string collectionName = "loggerName";

			var target = new LiteDBTarget
			{
				UserName = username,
				Password = password,
				ConnectionString = connectionString,
				ConnectionName = connectionName,
				CollectionName = collectionName
			};


			target.UserName
				.Should().Be(username);
			target.Password
				.Should().Be(password);
			target.ConnectionString
				.Should().Be(resultCS);
			target.ConnectionName
				.Should().Be(connectionName);
			target.CollectionName
				.Should().Be(collectionName);
		}


    }
}
