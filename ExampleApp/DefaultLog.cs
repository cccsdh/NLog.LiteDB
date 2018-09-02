using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleApp
{
	public class DefaultLog
	{
		public int ThreadID { get; set; }
		public string ThreadName { get; set; }
		public int ProcessID { get; set; }
		public string ProcessName { get; set; }
		public string UserName { get; set; }
		public string Level { get; set; }
		public string Message { get; set; }
	}
}
