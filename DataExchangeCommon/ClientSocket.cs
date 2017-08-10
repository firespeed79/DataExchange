using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace DataExchangeCommon
{
    public class ClientSocket : IDisposable
    {
        public Socket cSocket;
        public DateTime m_LastVisit;
        public DateTime m_ConnectTime;
        public string sRemoteIPAndPort;
        public string sLocalIPAndPort;
        public int SocketID;
        public int ProcessId;
        public byte[] mReceiveBytes;
        public List<byte> mBuffers;

        public override string ToString()
        {
            return sRemoteIPAndPort.ToString();
        }

        public void Disconnect()
        {
            if (cSocket != null)
            {
                if (cSocket.Connected && _Conenected)
                {
                    cSocket.Close();
                }
            }
            if (mBuffers != null)
                mBuffers.Clear();
            mReceiveBytes = null;
        }

        public void Dispose()
        {
            lock (this)
            {
                if (cSocket != null)
                {
                    if (cSocket.Connected && _Conenected)
                    {
                        cSocket.Shutdown(SocketShutdown.Both);
                    }
                    cSocket.Close();
                    cSocket = null;
                }
                if (mBuffers != null)
                    mBuffers.Clear();
                mReceiveBytes = null;
            }
        }

        public ClientSocket() { }
        public ClientSocket(Socket s) {
            m_ConnectTime = DateTime.Now;
            cSocket = s;
            m_LastVisit = DateTime.Now;
            mBuffers = new List<byte>(4096);
            mReceiveBytes = new byte[2048];
            SocketID = s.Handle.ToInt32();
            sRemoteIPAndPort = s.RemoteEndPoint.ToString();
            sLocalIPAndPort = s.LocalEndPoint.ToString();
        }

        #region 作为客户端时需要的东西

        public event MessageEventHandle OnMessageEvent;
        public event ClientConnectEventHandle OnConnectEvent;
        public event ClientDisconnectEventHandle OnDisconnectEvent;
        public event ReceiveDataEventHandle OnReceiveEvent;

        private void ShowMessage(String msg)
        {
            if (OnMessageEvent != null)
                OnMessageEvent(this, new MessageEventArgs(msg));
        }
        private void OnConnect()
        {
            this.m_ConnectTime = DateTime.Now;
            this.m_LastVisit = DateTime.Now;
            this.mBuffers = new List<byte>(4096);
            this.mReceiveBytes = new byte[2048];
            this.SocketID = this.cSocket.Handle.ToInt32();
            this.sRemoteIPAndPort = cSocket.RemoteEndPoint.ToString();

            if (OnConnectEvent != null)
            {
                OnConnectEvent(this, new ConnectEventArgs(this));
            }
        }
        private void OnDisconnect()
        {
            _Conenected = false;
            if (OnDisconnectEvent != null)
                OnDisconnectEvent(this, new DisconenctEventArgs(this));
            this.Dispose();
        }
        private void OnReceive()
        {
            if (OnReceiveEvent != null)
                OnReceiveEvent(this, new ReceiveDataEventArgs(this, null));
        }
        #endregion

        private String mServerIPOrDomainName;
        private int mServerPort;
        private bool isIPAddress;
        public ClientSocket(String serverIPOrDomainName, int serverPort)
        {
            _UsedByServer = false;
            mServerPort = serverPort;
            mServerIPOrDomainName = serverIPOrDomainName;
            IPAddress ip;
            if (IPAddress.TryParse(serverIPOrDomainName, out ip))
            {
                isIPAddress = true;
            }
        }
        /// <summary>
        /// 正在连接标志
        /// </summary>
        bool _ConencetFlag = false;
        /// <summary>
        /// 已连接标志
        /// </summary>
        bool _Conenected = false;

        bool _UsedByServer = true;
        public bool StartConnect(int port = 0)
        {
            if (_UsedByServer) return false;
            if (_ConencetFlag || _Conenected) return false;
            _ConencetFlag = true;
            return ConnectToServer(port);
        }

        bool ConnectToServer(int port = 0)
        {
            cSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
            try
            {
                cSocket.Bind(ep);
                if (isIPAddress)
                {
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(mServerIPOrDomainName), mServerPort);
                    cSocket.BeginConnect(endPoint, new AsyncCallback(ConnectCallBack), cSocket);
                }
                else
                {
                    cSocket.BeginConnect(mServerIPOrDomainName, mServerPort, new AsyncCallback(ConnectCallBack), cSocket);
                }
                return true;
            }
            catch (Exception ex)
            {
                ShowMessage(string.Format("绑定到本地{0}失败：{1}", ep.ToString(), ex.Message));
            }
            return false;
        }
        void ConnectCallBack(IAsyncResult ar)
        {
            Socket cSock = (Socket)ar.AsyncState;
            try
            {
                cSock.EndConnect(ar);
                if (cSock.Connected)
                {
                    _Conenected = true;
                    _ConencetFlag = false;
                    OnConnect();
                    ShowMessage(string.Format("已连接到{0}:{1}", mServerIPOrDomainName, mServerPort));
                    StartReceive();
                }
                else
                {
                    System.Threading.Thread.Sleep(1000);
                    cSock.Close();
                    ConnectToServer();
                }
            }
            catch (SocketException ex)
            {
                ShowMessage(string.Format("连接到{0}:{1}失败：{2}", mServerIPOrDomainName, mServerPort, ex.Message));
                System.Threading.Thread.Sleep(1000);
                cSock.Close();
                ConnectToServer();
            }
        }
        public bool Conenected
        {
            get
            {
                return _Conenected;
            }
        }
        public void SendData(byte[] mBytes)
        {
            if (_Conenected)
            {
                try
                {
                    cSocket.BeginSend(mBytes, 0, mBytes.Length, SocketFlags.None, new AsyncCallback(SendCallback), this);
                }
                catch (Exception)
                {
                    CloseSocket();
                }
            }
        }
        void SendCallback(IAsyncResult ar)
        {
            ClientSocket client = (ClientSocket)ar.AsyncState;
            try
            {
                int mSendLength = client.cSocket.EndSend(ar);
                if (mSendLength == 0)
                    CloseSocket();
            }
            catch (SocketException)
            {
                CloseSocket();
            }
        }
        void StartReceive()
        {
            if (_Conenected)
            {
                cSocket.BeginReceive(this.mReceiveBytes, 0, this.mReceiveBytes.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), this);
            }
        }
        
        public void StartReceiveEx()
        {
            if (cSocket.Connected)
            {
                cSocket.BeginReceive(this.mReceiveBytes, 0, this.mReceiveBytes.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), this);
            }
        }
        void ReceiveCallBack(IAsyncResult ar)
        {
            ClientSocket client = (ClientSocket)ar.AsyncState;
            if (client.cSocket != null)
            {
                try
                {
                    int mReceiveLength = client.cSocket.EndReceive(ar);
                    if (mReceiveLength == 0)
                    {
                        //CloseSocket();
                    }
                    else
                    {
                        byte[] mData = new byte[mReceiveLength];
                        Buffer.BlockCopy(this.mReceiveBytes, 0, mData, 0, mReceiveLength);
                        this.mBuffers.AddRange(mData);
                        if (client.cSocket.Available > 0)
                        {
                            mReceiveLength = client.cSocket.Available;
                            mReceiveBytes = new byte[mReceiveLength];
                            StartReceive();
                        }
                        else
                        {
                            OnReceive();
                            mReceiveLength = 2048;
                            mReceiveBytes = new byte[mReceiveLength];
                            StartReceive();
                        }
                    }
                }
                catch (SocketException)
                {
                    CloseSocket();
                    Console.WriteLine("已断开连接");
                }
                catch (Exception)
                {
                    ShowMessage("已断开连接");
                }
            }
        }
        void CloseSocket()
        {
            OnDisconnect();
            System.Threading.Thread.Sleep(1000);
            StartConnect();
        }
    }

    public static class ClientSocketExtensions
    {
        public static bool ContainsSocket(this List<ClientSocket> list, ClientSocket other)
        {
            foreach (ClientSocket e in list)
            {
                if (e.sRemoteIPAndPort.Equals(other.sRemoteIPAndPort, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
