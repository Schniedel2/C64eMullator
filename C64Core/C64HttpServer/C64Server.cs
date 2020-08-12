using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C64Emulator.C64HttpServer
{
	public class C64Server : HttpServer
	{
		C64 myC64;

		public C64Server(string www, C64 _c64, string parameters) : base(www, parameters)
		{
			myC64 = _c64;
		}

		public override bool Execute(string _cmd)
		{
			_cmd = _cmd.ToUpper();

			switch (_cmd)
			{
				case "CPU.START":
					{
						myC64.StartClock();
						return true;
					}
				case "CPU.STOP":
					{
						myC64.StopClock();
						return true;
					}
			}

			return false;
		}

		public override string CreateCFGFile(string _filename)
		{
			_filename = _filename.ToUpper();

			switch (_filename)
			{
				case "CPU.CFG":
					{						
						return GetCPUState();
					}
			}

			return "";
		}

		public string GetCPUState()
		{			
			var stateDict =  myC64.CPU.GetStateDict();
			string cfg = DictToJSON(stateDict);
			cfg = "cpu = " + cfg;
			return cfg;
		}

		public string DictToJSON(Dictionary<string, object> _dict)
		{
			string json = "";
			foreach (var e in _dict)
			{
				string val = "\"" + e.Key + "\": ";
				if (e.Value is string)
				{
					val += "\"" + e.Value + "\"";
				}
				else if (e.Value is Boolean)
				{
					val += "\"" + e.Value.ToString().ToLower() + "\"";
				}
				else
				{
					val += "\"" + e.Value + "\"";
				}

				if (val != "")
				{
					if (json != "")
						json += ",\n";
					json += "  " + val;
				}
			}
			json = "{\n" + json + "\n};";

			return json;
		}
	}
}
