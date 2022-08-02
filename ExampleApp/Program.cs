using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;
using NLog.LiteDB;


namespace ExampleApp
{
	class Program
	{
        private static LiteDBTarget _specialTarget;
        private static LiteDBTarget _traceTarget;
        private readonly static Logger _logger = LogManager.GetCurrentClassLogger();
		static void Main(string[] args)
		{
			_logger.Info($"Start of application at {DateTime.Now.ToString("HH.mm.ss")}");
			_logger.Trace($"Start of application at {DateTime.Now.ToString("HH.mm.ss")}");
			_logger.Trace("Another Trace Message!");
			_logger.Trace("And a third Trace Message!");

            //wait for flush
            Thread.Sleep(2000);
            DisplayLog();
			Console.ReadLine();

		}
		static void DisplayLog()
		{
            var targets = LogManager.Configuration.AllTargets;
            foreach (var target in targets)
            {
                if (target.Name == "special")
                {
                    _specialTarget = target as LiteDBTarget;
                }
                if (target.Name == "liteDB")
                {
                    _traceTarget = target as LiteDBTarget;
                }
            }

            var db = new LiteDB.LiteDatabase(_specialTarget.ConnectionString);
            var traceDB = new LiteDB.LiteDatabase(_traceTarget.ConnectionString);

            ////clear log collection.
            //db.DropCollection("DefaultLog");

            var collection = db.GetCollection<DefaultLog>("DefaultLog");
            var traceCollection = traceDB.GetCollection<DefaultLog>("DefaultLog");
			Console.WriteLine($"There are {collection.Count()} log entries in special collection");
			Console.WriteLine($"There are {traceCollection.Count()} log entries in trace collection");


			var entries = collection.FindAll().ToList();
			var traceEntries = traceCollection.FindAll().ToList();
            Console.WriteLine("Trace Entries:");
			foreach (var item in traceEntries)
			{
				Console.WriteLine($"{item.Level}  {item.Message}");
			}
            Console.WriteLine("");
            Console.WriteLine("Trace Entries:");
            foreach (var item in entries)
			{
				Console.WriteLine($"{item.Level}  {item.Message}");
			}

		}
	}
}
