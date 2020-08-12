using System;

namespace C64Emulator.C64HttpServer
{
    public class WebSocketHandler_null : WebSocketHandlerBase
	{
		public WebSocketHandler_null()
		{
		}

		public override string ExecMessage(string _protocol, string _data)
        {
            Console.Out.WriteLine("[{0}]: {1}", _protocol, _data);
			return "";
        }
    }
}
