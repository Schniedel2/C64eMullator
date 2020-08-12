using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Net;

namespace C64Emulator.C64HttpServer
{    
    public class HttpServer
    {
		static HttpServer s_this = null;
		public string wwwRootFolder;

		Dictionary<string, IWebSocketHandler> WebSocketHandlers = new Dictionary<string, IWebSocketHandler>();
		/*
        TcpListener myListener;
        TcpListener myWSListener;
		*/
		IPAddress myIPAddress = IPAddress.Any;

		List<ClientConnection> Connections = new List<ClientConnection>();

		List<TcpListener> myListeners = new List<TcpListener>();
		static int Port = 80;
		static int WSPort = 81;

		public HttpServer(string _wwwRootFolder, string[] args)
		{
			wwwRootFolder = _wwwRootFolder;
			SetParameters(args);
		}

		public HttpServer(string _wwwRootFolder, string parameters)
		{
			wwwRootFolder = _wwwRootFolder;
			string[] args = parameters.Split(new char[] { ' ' });
			SetParameters(args);
		}

		void SetParameters(string[] args)
        {
			for (int i=0; i<args.Length; i++)
			{
				if (args[i].ToUpper() == "-PORT")
				{
					Port = int.Parse(args[i + 1]);
				}
				if (args[i].ToUpper() == "-WSPORT")
				{
					WSPort = int.Parse(args[i + 1]);
				}
			}
			Console.Out.WriteLine();
			Console.Out.WriteLine("known parameters:");
			Console.Out.WriteLine(" -port n");
			Console.Out.WriteLine("  n = network port for http listener (default: 80)");
			Console.Out.WriteLine(" -wsport n");
			Console.Out.WriteLine("  n = network port for http listener (default: 80)");
			Console.Out.WriteLine();

			IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
			//IPAddress ipAddr = ipHost.AddressList[0];

			foreach (IPAddress ipAddr in ipHost.AddressList)
			{
				if (ipAddr.AddressFamily == AddressFamily.InterNetwork)
				{
					/*
					if (myIPAddress == IPAddress.Any)
					{
						myIPAddress = ipAddr;
					}
					*/
					myListeners.Add(new TcpListener(ipAddr, Port));
					myListeners.Add(new TcpListener(ipAddr, WSPort));
					Console.Out.WriteLine("listening on: {0}:{1}/{2}", ipAddr, Port, WSPort);
					myIPAddress = ipAddr;
				}
			}
			myListeners.Add(new TcpListener(IPAddress.Any, Port));
			myListeners.Add(new TcpListener(IPAddress.Any, WSPort));
			Console.Out.WriteLine("listening on: {0}:{1}/{2}", IPAddress.Any, Port, WSPort);

			/*
			myListener = new TcpListener(myIPAddress, Port);

			//  init websockets
			myWSListener = new TcpListener(myIPAddress, WebSocketPort());
			*/
			RegisterWebSocketHandler(new WebSocketHandler_null(), "null");

			s_this = this;

			Console.Out.Write("listening on: {0}\n", myIPAddress);
			Console.Out.Write("http listener on Port: {0}\n", Port);
			Console.Out.Write("WebSocket listener on Port: {0}\n", WSPort);
			Console.Out.WriteLine();
		}

		public static HttpServer GetInstance()
		{
			return s_this;
		}

		public string WebSocketIP()
        {
			return myIPAddress.ToString();
		}

		public int WebSocketPort()
        {
            return WSPort;
        }

        public void RegisterWebSocketHandler(IWebSocketHandler _handler, string _protocolID)
        {
            string protocol = _protocolID;
            WebSocketHandlers.Add(protocol, _handler);
        }

        public IWebSocketHandler GetWebSocketHandler(string _protocol)
        {
            if (WebSocketHandlers.ContainsKey(_protocol))
            {
                return WebSocketHandlers[_protocol];
            }
            return WebSocketHandlers["null"];
        }

        public void StartListen()
        {
			/*
            myListener.Start();
            myWSListener.Start();
			*/

			foreach (var listener in myListeners)
				listener.Start();
		}

        public void StopListen()
        {
			/*
            myListener.Stop();
            myWSListener.Stop();
			*/

			foreach (var listener in myListeners)
				listener.Stop();
		}

		public void Process()
        {
			foreach (var listener in myListeners)
			{
				if (listener.Pending())
				{
					Connections.Add(new ClientConnection(this, listener));
				}
			}
				
            var activeClients = new List<ClientConnection>();
                
            foreach (var c in Connections)
            {
				if (c.Process())
				{
					activeClients.Add(c);
				}
				else
				{
					System.Console.Out.WriteLine("removed connection: {0}", c.ID);
				}
            }

            Connections = activeClients;
        }

		public string CreateSetupJS(string _templateFilename, ClientConnection _connection)
		{
			StreamReader s = File.OpenText(_templateFilename);
			string js = s.ReadToEnd();

			var localEndpoint = _connection.Tcp.Client.LocalEndPoint as System.Net.IPEndPoint;

			string WebSocketServer = string.Format("ws://{0}:{1}", localEndpoint.Address, WebSocketPort());
			js = js.Replace("%WEBSOCKETSERVER%", WebSocketServer);

			return js;
		}

		public string ProcessHTTPRequest(string request, ClientConnection _connection)
        {
            string path;
            Dictionary<string, string> ParamList;

            if (!HttpHelper.ParseRequest(request, out path, out ParamList))
            {
                Console.Out.Write("Error: bad request");
                return "error";
            }

			//	parse global set-parameters
			Dictionary<string, string> SkippedParams = new Dictionary<string, string>();
			{
				foreach (var p in ParamList)
				{
					bool handled = false;

					if (p.Key.ToUpper() == "EXEC")
					{
						if (Execute(p.Value))
							handled = true;
					}

					if (!handled)
						SkippedParams[p.Key] = p.Value;
				}
				ParamList = SkippedParams;
			}

			//  file-oriented webserver
			if (path == "")
				path = "index.html";

			string wwwFilename = wwwRootFolder + path;

			//	check for .cfg-requests
			string suffix = "";
			if (path.Length > 0)
			{
				int p = path.LastIndexOf('.');
				if (p>=0)
				{
					suffix = path.Substring(p+1);
				}
			}
			
			switch (suffix.ToUpper())
			{
				case "HTML":
					{
						break;
					}
				case "CFG":
					{
						string cfg = CreateCFGFile(path);

						StreamWriter o = File.CreateText(wwwRootFolder + path);
						o.Write(cfg);
						o.Close();

						break;
					}
				case "JS":
					{
						if (path.ToUpper().EndsWith("SERVERSETUP.JS"))
						{
							string SetupJS = CreateSetupJS(wwwRootFolder + "ServerSetup.js.template", _connection);
							return SetupJS;
						}
						break;
					}
			}

			if (path.ToUpper() == "DUMP")
			{
				//ProcessMemoryDump(ParamList["Adr"]);
			}

			if (File.Exists(wwwFilename))
            {
                Console.Out.Write("GET {0}", wwwFilename);

                string html = File.ReadAllText(wwwFilename);
                // parameter injection
                var e = ParamList.GetEnumerator();
                while (e.MoveNext())
                {
                    string key = string.Format("%{0}%", e.Current.Key);
                    html = html.Replace(key, e.Current.Value);
                }
                
                return html;
            }

            Console.Out.Write("GET {0}", path);
			return string.Format("Error: invalid URL {0}", path);
        }

		/*
        public void SaveAll(string _slot)
        {
            foreach (var app in Apps)
            {
                app.Value.Save(_slot);
            }
        }

        public void LoadAll(string _slot)
        {
            foreach (var app in Apps)
            {
                app.Value.Load(_slot);
            }
        }
        */

		public virtual bool Execute(string _cmd)
		{
			return false;
		}

		public virtual string CreateCFGFile(string _filename)
		{
			return "";
		}
	}
}
