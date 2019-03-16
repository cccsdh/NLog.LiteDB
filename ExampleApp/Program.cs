using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace ExampleApp
{
	class Program
	{
		private readonly static Logger _logger = LogManager.GetCurrentClassLogger();
		static void Main(string[] args)
		{
			_logger.Info($"Start of application at {DateTime.Now.ToString("HH.mm.ss")}");

			DisplayLog();
			Console.ReadLine();

		}
		static void DisplayLog()
		{
            var targets = LogManager.Configuration.AllTargets;

            var db = new LiteDB.LiteDatabase("filename=Example_Nlog.db");

			var collection = db.GetCollection<DefaultLog>("DefaultLog");
			Console.WriteLine($"There are {collection.Count()} log entries in collection");


			var entries = collection.FindAll().ToList();

			foreach (var item in entries)
			{
				Console.WriteLine($"{item.Level}  {item.Message}");
			}

		}
	}
}
