using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

// https://tools.ietf.org/html/draft-ietf-hybi-thewebsocketprotocol-03

namespace C64Emulator.C64HttpServer
{
    public struct WebSocketFrame
    {
        public int OpCode;
        public string Message;
    }

    public static class HttpHelper
    {
        public static string ReadRequest(TcpClient client)
        {
            byte[] data = new byte[65536];

            NetworkStream stream = client.GetStream();
            string request = "";
            while (stream.DataAvailable)
            {
                int size = stream.Read(data, 0, data.Length);
                string msg = System.Text.Encoding.ASCII.GetString(data, 0, size);
                request += msg;
            }

            return request;
        }

        public static byte[] ReadRequestRaw(TcpClient client)
        {
            byte[] data = new byte[65536];

            List<byte> raw = new List<byte>();

            NetworkStream stream = client.GetStream();
            while (stream.DataAvailable)
            {
                int size = stream.Read(data, 0, data.Length);
                for (int i = 0; i < size; i++)
                    raw.Add(data[i]);
            }
            return raw.ToArray();
        }

        public static Dictionary<string, string> GetHTTPParams(string request)
        {
            string[] seps = new string[1];
            seps[0] = "\r\n";
            string[] lines = request.Split(seps, StringSplitOptions.None);

            var paramsList = new Dictionary<string, string>();

            foreach (string str in lines)
            {
                string[] sep = new string[1];
                sep[0] = ": ";
                string[] toks = str.Split(sep, StringSplitOptions.None);
                if (toks.Length >= 2)
                {
                    paramsList.Add(toks[0], toks[1]);
                }
            }

            return paramsList;
        }

        public static bool ParseRequest(string request, out string path, out Dictionary<string, string> ParamList)
        {
            path = "";
            ParamList = null;

            if (request == "")
                return false;

            var httpParams = GetHTTPParams(request);

            string[] seps = new string[1];
            seps[0] = "\r\n";
            string[] lines = request.Split(seps, StringSplitOptions.None);

            if (lines.Length <= 1)
                return false;

            ParamList = new Dictionary<string, string>();

            if (lines[0].Substring(0, 3) == "GET")
            {
                string strGetParam = "";
                string strParamList = "";

                lines[0] = WebUtility.UrlDecode(lines[0]);

                strGetParam = lines[0].Substring(5);
                int spc = strGetParam.IndexOf(' ');
                strGetParam = strGetParam.Substring(0, spc);

                int pos = strGetParam.IndexOf('?');
                if (pos >= 0)
                {
                    strParamList = strGetParam.Substring(pos + 1);
                    strGetParam = strGetParam.Substring(0, pos);
                }
                path = strGetParam;

                string[] Params = strParamList.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string p in Params)
                {
                    string[] vals = p.Split(new char[] { '=' });
                    if (vals.Length == 2)
                    {
                        ParamList.Add(vals[0], vals[1]);
                    }
                    else
                    {
                        ParamList.Add(vals[0], "");
                    }
                }
            }
            else if (lines[0].Substring(0, 4) == "POST")
            {
                lines[0] = WebUtility.UrlDecode(lines[0]);

                string str = lines[0].Substring(6);
                int spc = str.IndexOf(' ');
                path = str.Substring(0, spc);

                bool sepDetected = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (sepDetected)
                    {
                        string strParamList = WebUtility.UrlDecode(lines[i]);

                        string[] Params = strParamList.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string p in Params)
                        {
                            string[] vals = p.Split(new char[] { '=' });
                            if (vals.Length == 2)
                            {
                                ParamList.Add(vals[0], vals[1]);
                            }
                            else
                            {
                                ParamList.Add(vals[0], "");
                            }
                        }
                    }
                    else if (lines[i] == "")
                    {
                        sepDetected = true;
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }

		public static bool SendHttpResponse(TcpClient _tcp, string _data)
        {
            string response = "";
            response += "HTTP/1.1 200 OK\r\n";
            response += string.Format("Content-Length: {0}\r\n", _data.Length);
            response += "\r\n";
            response += _data;

            return SendString(_tcp, response);
        }

        public static bool SendString(TcpClient _tcp, string _data)
        {
			try
			{
				NetworkStream s = _tcp.GetStream();
				byte[] responseRaw = Encoding.UTF8.GetBytes(_data);
				s.Write(responseRaw, 0, responseRaw.Length);
			}
			catch (Exception e)
			{
				return false;
			}
			return true;		
		}

		public static string RespondWebSocketHandshake(string _webSocketKey, string _wsSubProtocol)
        {
            //  handshake
            byte[] rawKey = Encoding.UTF8.GetBytes(_webSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
            var hash = System.Security.Cryptography.SHA1.Create().ComputeHash(rawKey);

            string response = "";
            response += "HTTP/1.1 101 Switching Protocols\r\n";
            response += "Connection: Upgrade\r\n";
            response += "Upgrade: websocket\r\n";
            response += "Sec-WebSocket-Accept: " + Convert.ToBase64String(hash) + "\r\n";
			if (_wsSubProtocol != "")
				response += "Sec-WebSocket-Protocol: " + _wsSubProtocol + "\r\n";
			

			response += "\r\n";

            return response;
        }
		
        public static WebSocketFrame[] ReadWebsocketRequests(TcpClient _tcp)
        {
            List<WebSocketFrame> packets = new List<WebSocketFrame>();

            byte[] data = ReadRequestRaw(_tcp);

            int ofs = 0;
            while (ofs < data.Length)
            {
                bool moreFragments = (data[ofs] >= 128);
                byte opcode = (byte)(data[ofs] & 0x0f);
                ofs++;

                bool masked = ((data[ofs] & 128) > 0);
                int payload = (data[ofs] & 127);
                ofs++;

                byte[] mask = { 0, 0, 0, 0 };
                if (masked)
                {
                    mask[0] = data[ofs];
                    ofs++;
                    mask[1] = data[ofs];
                    ofs++;
                    mask[2] = data[ofs];
                    ofs++;
                    mask[3] = data[ofs];
                    ofs++;
                }

                byte[] decode = new byte[payload];
                for (int i = 0; i < payload; i++)
                {
                    decode[i] = (byte)(data[ofs] ^ mask[i % 4]);
                    ofs++;
                }

                WebSocketFrame packet = new WebSocketFrame();
                packet.OpCode = opcode;
                packet.Message = Encoding.ASCII.GetString(decode, 0, payload);
                packets.Add(packet);
            }
            return packets.ToArray();
        }

		public static bool SendWebSocket_Text(TcpClient _tcp, string _text)
		{
			try
			{
				NetworkStream s = _tcp.GetStream();

				byte[] msgData = Encoding.UTF8.GetBytes(_text);
				List<Byte> data = new List<Byte>();

				byte opcode = 1; // 1 == text frame (?)
				byte len0 = (byte)_text.Length;

				data.Add((byte)(opcode | 128));
				data.Add(len0);
				for (int i = 0; i < msgData.Length; i++)
					data.Add(msgData[i]);

				s.Write(data.ToArray(), 0, data.Count);
			}
			catch(Exception e)
			{
				return false;
			}
			return true;
		}

		public static void SendWebSocket_Blob(TcpClient _tcp, byte[] _data)
		{
			if (!_tcp.Connected)
				return;
			NetworkStream s = _tcp.GetStream();
			if (s == null)
				return;

			List<Byte> data = new List<Byte>();

			byte opcode = (2 | 128); // 2 == blob data
			byte len0 = (byte)_data.Length;

			data.Add(opcode);
			data.Add(len0);
			for (int i = 0; i < _data.Length; i++)
				data.Add(_data[i]);

			s.Write(data.ToArray(), 0, data.Count);
		}

	}
}
