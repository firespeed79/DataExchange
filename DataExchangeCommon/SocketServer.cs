using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace DataExchangeCommon
{
    public class SocketServer : IDisposable
    {
        Socket sSocket;
        Dictionary<int, ClientSocket> mClients;
        IPAddress mListenIPAddress = IPAddress.Any;
        int mListenPort = 9999;
        bool mRunFlag = false;

        public int ClientCount
        {
            get
            {
                return mClients == null ? 0 : mClients.Count;
            }
        }
        public ClientSocket[] ClientArray
        {
            get
            {
                return mClients == null ? null : mClients.Values.ToArray();
            }
        }
        #region MyRegion

        public event ReceiveDataEventHandle OnReceiveClientDataEvent;
        public event ClientConnectEventHandle OnClientConnectEvent;
        public event ClientDisconnectEventHandle OnClientDisconnectEvent;
        public event MessageEventHandle OnMessageEvent;

        private void ShowMessage(string msg)
        {
            if (OnMessageEvent != null)
                OnMessageEvent(this, new MessageEventArgs(msg));
        }
        private void ClientConnected(ClientSocket client)
        {
            if (client != null)
            {
                ShowMessage("ClientConnected{" + client.SocketID + "}[" + client.sRemoteIPAndPort + "]");
                if (OnClientConnectEvent != null)
                {
                    OnClientConnectEvent(this, new ConnectEventArgs(client));
                }
                lock (mClients)
                {
                    if (client.cSocket.Connected && !mClients.ContainsKey(client.SocketID))
                        mClients.Add(client.SocketID, client);
                }
            }
        }
        private void ReceiveData(ClientSocket client, byte[] mBytes)
        {
            ShowMessage("ReceiveData{" + client.SocketID + "}[" + client.sRemoteIPAndPort + "]");
            if (OnReceiveClientDataEvent != null)
            {
                OnReceiveClientDataEvent(this, new ReceiveDataEventArgs(client, mBytes));
                client.m_LastVisit = DateTime.Now;
            }
        }
        private void ClientDisconnect(ClientSocket client)
        {
            ShowMessage("ClientDisconnect{" + client.SocketID + "}[" + client.sRemoteIPAndPort + "]");
            bool mHasValue = false;
            lock (mClients)
            {
                if (mClients.ContainsKey(client.SocketID))
                {
                    mClients.Remove(client.SocketID);
                    mHasValue = true;
                }
            }
            if (mHasValue && OnClientDisconnectEvent != null)
            {
                OnClientDisconnectEvent(this, new DisconenctEventArgs(client));
            }
            client.Dispose();
        }
        public void Dispose()
        {
            lock (this)
            {
                if (sSocket != null)
                {
                    ShowMessage("释放服务端Socket[" + sSocket.LocalEndPoint.ToString() + "]");
                    sSocket.Close();
                    sSocket = null;
                }
                if (mClients != null)
                {
                    foreach (ClientSocket _Client in mClients.Values)
                    {
                        ShowMessage("释放客户端Socket{"+ _Client.SocketID + "}[" + _Client.sRemoteIPAndPort + "]");
                        _Client.Dispose();
                    }
                    mClients.Clear();
                    mClients = null;
                }
            }
        }
        #endregion
        
        /// <summary>
        /// 默认为监听所有网卡的9999端口
        /// </summary>
        public SocketServer()
        {
            mClients = new Dictionary<int, ClientSocket>();
        }

        public SocketServer(IPAddress m_ListenIPAddress, int m_ListenPort)
        {
            mClients = new Dictionary<int, ClientSocket>();
            mListenIPAddress = m_ListenIPAddress;
            mListenPort = m_ListenPort;
        }

        #region Public Function
        public void Start()
        {
            if (mRunFlag) return;

            mRunFlag = true;
            mClients = new Dictionary<int, ClientSocket>(8);

            sSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sSocket.Bind(new IPEndPoint(mListenIPAddress, mListenPort));
            sSocket.Listen(0);
            sSocket.BeginAccept(new AsyncCallback(AcceptCallBack), sSocket);

            ShowMessage("启动Server监听[" + mListenIPAddress + ":" + mListenPort.ToString() + "]");
        }
        public void CloseClient(int mSocketID)
        {
            ClientSocket client = null;
            if (mClients.TryGetValue(mSocketID, out client))
            {
                CloseSocket(client);
            }
        }
        public void SendData(int mSocketID, byte[] mBytes)
        {
            if (mSocketID == -1)
            {
                //广播
                ShowMessage("SendData广播");
                int[] mKeys = null;
                lock (mClients)
                {
                    mKeys = mClients.Keys.ToArray();
                }
                foreach (var item in mKeys)
                {
                    SendData(item, mBytes);
                }
            }
            else
            {
                ClientSocket client = null;
                if (mClients.TryGetValue(mSocketID, out client))
                {
                    ShowMessage("SendDataTo{"+ client.SocketID + "}["+ client.sRemoteIPAndPort.ToString() + "]");
                    SendData(client, mBytes);
                }
            }
        }
        #endregion

        #region Private Function
        void AcceptCallBack(IAsyncResult ar)
        {
            Socket s = (Socket)ar.AsyncState;
            ClientSocket client = null;
            try
            {
                Socket cSock = s.EndAccept(ar);
                cSock.UseOnlyOverlappedIO = true;
                client = new ClientSocket
                {
                    cSocket = cSock,
                    m_ConnectTime = DateTime.Now,
                    m_LastVisit = DateTime.Now,
                    SocketID = cSock.Handle.ToInt32(),
                    sRemoteIPAndPort = cSock.RemoteEndPoint.ToString(),
                    sLocalIPAndPort = cSock.LocalEndPoint.ToString(),
                    mReceiveBytes = new byte[2048],
                    mBuffers = new List<byte>(4096)
                };
                //ShowMessage("AcceptCallBack[" + client.sRemoteIPAndPort.ToString() + "]");
                ClientConnected(client);
                cSock.BeginReceive(client.mReceiveBytes, 0, 2048, SocketFlags.None, new AsyncCallback(ReceiveCallBack), client);
                s.BeginAccept(new AsyncCallback(AcceptCallBack), s);
            }
            catch (SocketException)
            {
                ShowMessage("客户端连接" + client.sRemoteIPAndPort + "已关闭");
                CloseSocket(client);
            }
            catch (ObjectDisposedException)
            {
                ShowMessage("ObjectDisposedException:客户端连接" + client.sRemoteIPAndPort + "已关闭");
                CloseSocket(client);
            }
            catch (Exception ex)
            {
                ShowMessage("接受连接时出错:" + ex.ToString());
            }
            finally
            {
            }
        }
        void ReceiveCallBack(IAsyncResult ar)
        {
            ClientSocket client = (ClientSocket)ar.AsyncState;
            try
            {
                int mReceiveLength = client.cSocket.EndReceive(ar);
                if (mReceiveLength == 0)
                {
                    //CloseSocket(client);
                    return;
                }
                byte[] mData = new byte[mReceiveLength];
                Buffer.BlockCopy(client.mReceiveBytes, 0, mData, 0, mReceiveLength);
                client.mBuffers.AddRange(mData);
                if (client.cSocket.Available > 0)
                {
                    mReceiveLength = client.cSocket.Available;
                    client.mReceiveBytes = new byte[mReceiveLength];
                }
                else
                {
                    ReceiveData(client, null);
                    mReceiveLength = 2048;
                    client.mReceiveBytes = new byte[mReceiveLength];
                }
                if (client != null && client.cSocket != null && client.cSocket.Connected)
                    client.cSocket.BeginReceive(client.mReceiveBytes, 0, mReceiveLength, SocketFlags.None, new AsyncCallback(ReceiveCallBack), client);
                //ShowMessage("ReceiveCallBack[" + client.sRemoteIPAndPort.ToString() + "]");
            }
            catch (SocketException)
            {
                ShowMessage("客户端连接" + client.sRemoteIPAndPort + "已关闭");
                CloseSocket(client);
            }
            catch (ObjectDisposedException)
            {
                //ShowMessage("ObjectDisposedException:客户端连接" + client.sRemoteIPAndPort + "已关闭");
                //CloseSocket(client);
            }
            catch (Exception ex)
            {
                ShowMessage("接收数据时出错:" + ex.ToString());
            }
        }
        void CloseSocket(ClientSocket client)
        {
            if (client != null)
                ClientDisconnect(client);
        }
        void SendData(ClientSocket client, byte[] mSendData)
        {
            try
            {
                client.cSocket.BeginSend(mSendData, 0, mSendData.Length, SocketFlags.None, new AsyncCallback(SendCallBack), client);
            }
            catch (SocketException)
            {
                ClientDisconnect(client);
            }
            catch (Exception ex)
            {
                ShowMessage("准备发送数据时出错:" + ex.ToString());
            }
        }
        void SendCallBack(IAsyncResult ar)
        {
            ClientSocket client = (ClientSocket)ar.AsyncState;
            try
            {
                int mSendLength = client.cSocket.EndSend(ar);
                //if (mSendLength == 0)
                //{
                //    ClientDisconnect(client);
                //}
            }
            catch (SocketException)
            {
                ClientDisconnect(client);
            }
            catch (Exception ex)
            {
                ShowMessage("正在发送数据时出错:" + ex.ToString());
            }
        }
        #endregion
    }
   
}
