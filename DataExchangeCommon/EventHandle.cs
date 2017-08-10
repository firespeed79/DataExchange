using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataExchangeCommon
{
    public delegate void ReceiveDataEventHandle(object sender, ReceiveDataEventArgs args);
    public delegate void ClientConnectEventHandle(object sender, ConnectEventArgs args);
    public delegate void ClientDisconnectEventHandle(object sender, DisconenctEventArgs args);
    public delegate void MessageEventHandle(object sender, MessageEventArgs args);

    public class MessageEventArgs : EventArgs
    {
        public DateTime mMessageTime = DateTime.Now;
        public string mMessage;
        public MessageEventArgs(string msg)
        {
            mMessage = msg;
        }
    }
    public class ReceiveDataEventArgs : EventArgs
    {
        public ClientSocket client;
        public byte[] receiveBytes;

        public ReceiveDataEventArgs(ClientSocket _client, byte[] _ReceiveBytes)
        {
            client = _client;
            receiveBytes = _ReceiveBytes;
        }
    }
    public class DisconenctEventArgs : EventArgs
    {
        public ClientSocket client;
        public DisconenctEventArgs(ClientSocket _client)
        {
            client = _client;
        }
    }
    public class ConnectEventArgs : EventArgs
    {
        public ClientSocket client;
        public ConnectEventArgs(ClientSocket _client)
        {
            client = _client;
        }
    }
}
