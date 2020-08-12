using System;

namespace C64Emulator.C64HttpServer
{
	public interface IWebSocketHandler
	{
		string ExecMessage(string _protocol, string _data);
	}

	public abstract class WebSocketHandlerBase : IWebSocketHandler
    {
		public virtual string ExecMessage(string _protocol, string _data)
		{
			if (_data == "PING")
			{
				return "PONG";
			}
			return "";
		}
    }
}
