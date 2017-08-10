using DataExchangeCommon;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataExchangeService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
            portMap = new Dictionary<ClientSocket, SocketMap>();
            portRef = new Dictionary<int, List<ClientSocket>>();
        }

        SocketServer server;
        Dictionary<ClientSocket, SocketMap> portMap;
        Dictionary<int, List<ClientSocket>> portRef;

        protected override void OnStart(string[] args)
        {
            Logs.Create("正在启动服务...");
            try
            {
                IPAddress LocalIP = IPAddress.Any;
                int LocalPort = 9999;
                string ip = ConfigurationManager.AppSettings["ip"];
                string port = ConfigurationManager.AppSettings["port"];
                if (!string.IsNullOrEmpty(ip))
                    LocalIP = IPAddress.Parse(ip);
                if (!string.IsNullOrEmpty(port))
                    LocalPort = int.Parse(port);
                server = new SocketServer(LocalIP, LocalPort);
                server.OnClientConnectEvent += server_OnClientConnectEvent;
                server.OnClientDisconnectEvent += server_OnClientDisconnectEvent;
                server.OnMessageEvent += server_OnMessageEvent;
                server.OnReceiveClientDataEvent += server_OnReceiveClientDataEvent;
                server.Start();
                Logs.Create("服务启动成功！");
            }
            catch (Exception ex)
            {
                Logs.Create("服务启动失败：" + ex.Message);
            }
        }

        void SetClientProcessId(ClientSocket client, int mId)
        {
            //foreach (ClientSocket item in portMap.Keys)
            //{
            //    if (item.Equals(client))
            //    {
            client.ProcessId = mId;
                    return;
            //    }
            //}
        }

        void server_OnReceiveClientDataEvent(object sender, ReceiveDataEventArgs args)
        {
            ExchangeObject mObject = new ExchangeObject();
            while (args.client.mBuffers.Count >= 12)
            {
                if (mObject.Format(args.client.mBuffers))
                {
                    switch ((Common.ECommand)mObject.PackType)
                    {
                        case Common.ECommand.Data:
                            Data data = new Data();
                            if (mObject.GetStruct<Data>(ref data))
                            {
                                if (data.Buffer != null)
                                {
                                    byte[] dat = mObject.ToBuffer<Data>(data, Common.ECommand.Data);
                                    //(sender as SocketServer).SendData(args.client.SocketID, dat);//转发回给源客户端
                                    long id = DataManager.AddData(args.client.sRemoteIPAndPort, data.Buffer);
                                    if (id == 0)
                                        Logs.Create("记录中转数据到数据库失败！数据来源[" + args.client.sRemoteIPAndPort + "]，数据为[" + Encoding.UTF8.GetString(data.Buffer) + "]");
                                }
                            }
                            else
                            {
                                Console.WriteLine("解析Data对象错误");
                                Logs.Create("解析Data对象错误");
                                //(sender as SocketServer).CloseClient(args.client.SocketID);
                            }
                            break;
                        case Common.ECommand.Login:
                            int mProcessId = 0;
                            if (mObject.GetStruct<int>(ref mProcessId))
                            {
                                SetClientProcessId(args.client, mProcessId);
                                //int port = Convert.ToInt32(args.client.sRemoteIPAndPort.Split(':')[1]);
                                foreach (ClientSocket f in portMap.Keys)
                                {
                                    foreach (ClientSocket t in portMap[f].Clients)
                                    {
                                        ExchangeTransfer(f, t);
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("解析ProcessId对象错误");
                                Logs.Create("解析ProcessId对象错误");
                                //(sender as SocketServer).CloseClient(args.client.SocketID);
                            }
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("对象错误" + args.client.mBuffers.Count.ToString());
                    args.client.mBuffers.RemoveAt(0);
                }
            }

        }

        void server_OnClientConnectEvent(object sender, ConnectEventArgs args)
        {
            if (args.client != null)
            {
                ClientSocket from = args.client;
                string[] ap = from.sRemoteIPAndPort.Split(':');
                string addr = ap[0];
                int port = Convert.ToInt32(ap[1]);
                if (!portRef.ContainsKey(port))
                {
                    portRef.Add(port, new List<ClientSocket>());
                    //Logs.Create("有新Socket连接：" + from.sRemoteIPAndPort + "，加入动态映射端口列表中");
                }
                try
                {
                    MapManager.LoadMap();
                    List<int> froms = MapManager.Map.GetFromPort(port);
                    if (froms.Count > 0)
                    {
                        foreach (int p in portRef.Keys)
                        {
                            if (froms.Contains(p))
                            {
                                portRef[p].Add(from);
                                //Logs.Create("有新Socket连接：" + from.sRemoteIPAndPort + "，加入动态映射引用列表中");
                            }
                            else
                            {
                                //非法连接，自动忽略
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logs.Create("查询数据库中的映射配置时出错：" + ex.Message);
                }
                finally
                {
                    MapManager.Dispose();
                }
                if (!portMap.ContainsKey(from))
                {
                    SocketMap map = new SocketMap(new AddressAndPort() { Address = addr, Port = port });
                    portMap.Add(from, map);
                    //Logs.Create("有新Socket连接：" + from.sRemoteIPAndPort + "，加入动态映射起点列表中");
                }
                foreach (int p in portRef.Keys)
                {
                    foreach (ClientSocket c in portRef[p])
                    {
                        foreach (ClientSocket f in portMap.Keys)
                        {
                            int po = Convert.ToInt32(f.sRemoteIPAndPort.Split(':')[1]);
                            if (po == p && !portMap[from].Clients.Contains(c))
                            {
                                portMap[f].Clients.Add(c);
                                //Logs.Create("有合法的映射终点Socket连接：" + from.sRemoteIPAndPort + "，加入动态映射终点列表中");
                            }
                        }
                    }
                }
                //Console.WriteLine(String.Format("{0} 连接.", args.client.sRemoteIPAddress));
                //SocketServer server = (SocketServer)sender;
                //if (server.ClientCount > 1)
                //{
                //    foreach (var item in mClients)
                //    {
                //        System.Net.Sockets.SocketInformation mInfomation = args.client.cSocket.DuplicateAndClose(item.ProcessId);
                //        ChangeObject mObject = new ChangeObject
                //        {
                //            PackType = (int)Common.ECommand.SocketInfo,
                //            SerialNumber = 0,
                //            mData = Common.SerializeSocketInfo(mInfomation)
                //        };
                //        mObject.PackLength = 12 + mObject.mData.Length;
                //        byte[] mBytes = mObject.ToBuffer();
                //        server.SendData(item.cSocket.SocketID, mBytes);
                //        break;
                //    }

                //}
            }
        }

        void server_OnMessageEvent(object sender, MessageEventArgs args)
        {
            Logs.Create(args.mMessage);
        }

        void server_OnClientDisconnectEvent(object sender, DisconenctEventArgs args)
        {            
            portMap.Remove(args.client);
            foreach (SocketMap sm in portMap.Values)
            {
                if (sm.Clients.Contains(args.client))
                {
                    sm.Clients.Remove(args.client);
                }
            }
            foreach (int p in portRef.Keys)
            {
                if (portRef[p].Contains(args.client))
                {
                    portRef[p].Remove(args.client);
                }
            }
        }

        int GetProcessId(ClientSocket cSock)
        {
            foreach (ClientSocket f in portMap.Keys)
            {
                if (f.Equals(cSock))
                {
                    return f.ProcessId;
                }
                foreach (ClientSocket t in portMap[f].Clients)
                {
                    if (t.Equals(cSock))
                    {
                        return t.ProcessId;
                    }
                }
            }
            return -1;
        }

        public void Broadcast(ClientSocket srcSock)
        {
            Parallel.ForEach<ClientSocket>(portMap[srcSock].Clients, (dstSock, loopState) =>
            {
                if (dstSock.SocketID == srcSock.SocketID)
                    loopState.Break();

                int mProcessId = GetProcessId(dstSock);
                if (mProcessId == -1) loopState.Break();

                System.Net.Sockets.SocketInformation mInfomation = srcSock.cSocket.DuplicateAndClose(mProcessId);
                ExchangeObject mObject = new ExchangeObject
                {
                    PackType = (int)Common.ECommand.SocketInfo,
                    SerialNumber = 0,
                    mData = Common.SerializeSocketInfo(mInfomation)
                };
                mObject.PackLength = 12 + mObject.mData.Length;
                byte[] mBytes = mObject.ToBuffer();
                server.SendData(dstSock.SocketID, mBytes);
            });
            Logs.Create("根据映射配置，将" + srcSock.sRemoteIPAndPort.Replace(":", "_") + "的数据群发Broadcast到：" + portMap[srcSock].Clients.Count + "个客户端");
        }

        public void ExchangeTransfer(ClientSocket srcSock, ClientSocket dstSock)
        {
            if (dstSock.SocketID == srcSock.SocketID)
                return;

            int mProcessId = GetProcessId(dstSock);
            if (mProcessId == -1) return;

            System.Net.Sockets.SocketInformation mInfomation = srcSock.cSocket.DuplicateAndClose(mProcessId);
            ExchangeObject mObject = new ExchangeObject
            {
                PackType = (int)Common.ECommand.SocketInfo,
                SerialNumber = 0,
                mData = Common.SerializeSocketInfo(mInfomation)
            };
            mObject.PackLength = 12 + mObject.mData.Length;
            byte[] mBytes = mObject.ToBuffer();
            server.SendData(dstSock.SocketID, mBytes);
            //Thread.Sleep(100);
            //ExchangeObject mObject2 = new ExchangeObject();
            //byte[] mBytes2 = mObject2.ToBuffer<Data>(new Data
            //{
            //    DataGuid = Guid.NewGuid(),
            //    Buffer = null
            //}, Common.ECommand.Data);
            //byte[] mBytes2 = mObject2.ToBuffer<int>(mProcessId, Common.ECommand.Login);
            //server.SendData(dstSock.SocketID, mBytes2);
            Logs.Create("根据映射配置，将" + srcSock.sRemoteIPAndPort.Replace(":", "_") + "的数据转发ExchangeTransfer到："+ dstSock.sRemoteIPAndPort);
        }

        protected override void OnStop()
        {
            Logs.Create("正在停止服务...");
            if (server != null)
            {
                try
                {
                    portMap.Clear();
                    portMap = null;
                    portRef.Clear();
                    portRef = null;
                    server.Dispose();
                    Logs.Create("停止服务成功！");
                }
                catch (Exception ex)
                {
                    Logs.Create("停止服务失败：" + ex.Message);
                }
            }
        }

        protected override void OnShutdown()
        {
            Logs.Create("系统正在关机或重启...");
            OnStop();
            base.OnShutdown();
        }
    }
}
