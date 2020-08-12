using System;
using System.Net.Sockets;

namespace C64Emulator.C64HttpServer
{
	public class ClientConnection
	{
		static int s_nextID = 1;

        public TcpClient Tcp;
        public DateTime CreationTimestamp;
        public DateTime LastActivity;
		public DateTime NextPing;
        public string Request = "";
        public bool IsWebSocket = false;
        public string WebSocketProtocol = "";
        public IWebSocketHandler WebSocketHandler = null;
        HttpServer Server;
		public int ID;

        public ClientConnection(HttpServer _server, TcpListener _listener)
        {
			ID = s_nextID;
			s_nextID++;

			Server = _server;
            Tcp = _listener.AcceptTcpClient();
            LastActivity = DateTime.Now;
            CreationTimestamp = DateTime.Now;
			NextPing = LastActivity + TimeSpan.FromSeconds(5);
			Request = "";
            IsWebSocket = false;

            Console.Out.WriteLine("[{0}]: connected ID:{1}", Tcp.Client.RemoteEndPoint, ID);
        }

        public bool IsConnected()
        {
            if (Tcp == null)
                return false;
            if (Tcp.Client == null)
                return false;
            return (Tcp.Client.Connected);
        }

        public bool Process()
        {
            if (!IsConnected())
                return false;

            TimeSpan idle = DateTime.Now - LastActivity;

            if ((idle.TotalSeconds > 60) && (!IsWebSocket))
            {
                Console.Out.WriteLine("[{0}]: disconnected ID:{1}", Tcp.Client.RemoteEndPoint, ID);
                Tcp.Close();
                return false;
            }

			if (IsWebSocket)
			{
				if (DateTime.Now > NextPing)
				{
					NextPing = DateTime.Now + TimeSpan.FromSeconds(5);
					return HttpHelper.SendWebSocket_Text(Tcp, "PING");
				}
			}

			int a = Tcp.Available;
            if (a == 0)
                return true;

            LastActivity = DateTime.Now;

            if (IsWebSocket)
            {
                var Requests = HttpHelper.ReadWebsocketRequests(Tcp);
                for (int i = 0; i < Requests.Length; i++)
                {
					int opcode = Requests[i].OpCode;
					switch (opcode)
					{
						case 1: // text data
							{
								string response = WebSocketHandler.ExecMessage("?", Requests[i].Message);
								if (response != "")
								{
									if (!HttpHelper.SendWebSocket_Text(Tcp, response))
										return false;
								}
								break;
							}
						default:
							{
								break;
							}
					}
                }
                Request = "";
            }
            else
            {
                Request += HttpHelper.ReadRequest(Tcp);
                ProcessHttpRequest(Request);
                Request = "";
            }

			return true;
        }

        public void ProcessHttpRequest(string _request)
        {
            Console.Out.Write("[{0}]: ", Tcp.Client.RemoteEndPoint);

            var httpParams = HttpHelper.GetHTTPParams(_request);
            if (httpParams.ContainsKey("Sec-WebSocket-Key"))
            {
				string wsProtocol = "";

				if (httpParams.ContainsKey("Sec-WebSocket-Protocol"))
                {
                    WebSocketProtocol = httpParams["Sec-WebSocket-Protocol"];
                    WebSocketHandler = Server.GetWebSocketHandler(WebSocketProtocol);
                    Console.Out.WriteLine("upgradet to WebSocketL: [{0}]", WebSocketProtocol);

					if (WebSocketHandler != null)
						wsProtocol = this.WebSocketProtocol;
				}				
				string response = HttpHelper.RespondWebSocketHandshake(httpParams["Sec-WebSocket-Key"], wsProtocol);
				IsWebSocket = true;
                HttpHelper.SendString(Tcp, response);
            }
            else
            {
                string response = Server.ProcessHTTPRequest(_request, this);
                HttpHelper.SendHttpResponse(Tcp, response);
                Console.Out.WriteLine("");
            }
        }
    }
}
