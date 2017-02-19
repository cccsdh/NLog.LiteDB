using System;
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
		public void TestSettings()
		{
			const string username = "someUser";
			const string password = "q198743n3d8yh32028##@!";
			const string connectionString = "some file name";
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
				.Should().Be(connectionString);
			target.ConnectionName
				.Should().Be(connectionName);
			target.CollectionName
				.Should().Be(collectionName);
		}

	}
}
