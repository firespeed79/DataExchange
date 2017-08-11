using DataExchangeCommon;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        ClientSocket client = null;
        int ObjectCount;
        bool _Conenected;

        public void ConnectToServer(int port = 0)
        {
            client = new ClientSocket(this.textBox1.Text.Trim(), (int)this.numericUpDown1.Value);
            client.OnConnectEvent += client_OnConnectEvent;
            client.OnDisconnectEvent += client_OnDisconnectEvent;
            client.OnMessageEvent += client_OnMessageEvent;
            client.OnReceiveEvent += client_OnReceiveEvent;
            if (client.StartConnect(port))
            {
                button1.Text = "断开";
                button2.Enabled = true;
                _Conenected = true;
            }
        }
        public void DisconnectServer()
        {
            client.Disconnect();
            client.OnConnectEvent -= client_OnConnectEvent;
            client.OnDisconnectEvent -= client_OnDisconnectEvent;
            //client.OnMessageEvent -= client_OnMessageEvent;
            //client.OnReceiveEvent -= client_OnReceiveEvent;
            _Conenected = false;
            button1.Text = "连接";
            button2.Enabled = false;
        }
        void client_OnReceiveEvent(object sender, ReceiveDataEventArgs args)
        {
            this.Invoke((MethodInvoker)(() =>
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
                                    toolStripStatusLabel1.Text = "收到";
                                    textBox3.Text = Encoding.UTF8.GetString(data.Buffer);
                                    ObjectCount++;
                                    toolStripStatusLabel3.Text = "来自" + client.cSocket.LocalEndPoint.ToString() + "第" + ObjectCount + "个数据[" + data.DataGuid.ToString("N") + "]";
                                }
                                else
                                {
                                    Console.WriteLine("解析Data对象错误");
                                }
                                break;
                            case Common.ECommand.SocketInfo:
                                SocketInformation mInfomation = Common.DeserializeSocketInfo(mObject.mData);
                                CreateSocket(mInfomation);
                                break;
                        }
                    }
                    else
                    {
                        args.client.mBuffers.RemoveAt(0);
                    }
                }
            }));
        }
        void CreateSocket(SocketInformation mInfomation)
        {
            Socket s = new Socket(mInfomation);
            if (s.Connected)
            {
                ClientSocket cSock = new ClientSocket();
                cSock.m_ConnectTime = DateTime.Now;
                cSock.cSocket = s;
                cSock.m_LastVisit = DateTime.Now;
                cSock.mBuffers = new List<byte>(4096);
                cSock.mReceiveBytes = new byte[2048];
                cSock.SocketID = cSock.cSocket.Handle.ToInt32();
                cSock.sRemoteIPAndPort = s.RemoteEndPoint.ToString();
                cSock.sLocalIPAndPort = s.LocalEndPoint.ToString();
                s.BeginReceive(cSock.mReceiveBytes, 0, 2048, SocketFlags.None, new AsyncCallback(ReceiveCallBack), cSock);
                //Console.WriteLine("接受服务器端传来的连接:" + s.RemoteEndPoint.ToString());
                //AddClientToList(cSock);
            }
        }
        void CloseClientSocket(ClientSocket _client)
        {
            _client.cSocket.Close();
            _Conenected = false;
            this.Invoke((MethodInvoker)(() =>
            {
                button1.Text = "连接";
                button2.Enabled = false;
            }
            ));
            //ClientDisconencet(_client);
        }
        void ReceiveCallBack(IAsyncResult ar)
        {
            ClientSocket _client = (ClientSocket)ar.AsyncState;
            if (client.cSocket != null)
            {
                try
                {
                    int mReceiveLength = _client.cSocket.EndReceive(ar);
                    if (mReceiveLength == 0)
                    {
                        //CloseClientSocket(_client);
                    }
                    else
                    {
                        byte[] mData = new byte[mReceiveLength];
                        Buffer.BlockCopy(_client.mReceiveBytes, 0, mData, 0, mReceiveLength);
                        _client.mBuffers.AddRange(mData);
                        if (_client.cSocket.Available > 0)
                        {
                            mReceiveLength = _client.cSocket.Available;
                            _client.mReceiveBytes = new byte[mReceiveLength];
                            if (_Conenected)
                            {
                                _client.cSocket.BeginReceive(_client.mReceiveBytes, 0, mReceiveLength, SocketFlags.None, new AsyncCallback(ReceiveCallBack), _client);
                            }
                        }
                        else
                        {
                            //OnReceive();
                            DoExitReceiveData(_client, _client.mBuffers);
                            mReceiveLength = 2048;
                            _client.mReceiveBytes = new byte[mReceiveLength];
                            if (_Conenected)
                            {
                                _client.cSocket.BeginReceive(_client.mReceiveBytes, 0, 2048, SocketFlags.None, new AsyncCallback(ReceiveCallBack), _client);
                            }
                        }
                    }
                }
                catch (SocketException)
                {
                    CloseClientSocket(_client);
                    Console.WriteLine("已断开连接");
                }
                catch (Exception)
                {
                    Console.WriteLine("已断开连接");
                }
            }
        }
        //void ClientDisconencet(ClientSocket _client)
        //{
        //    this.Invoke((MethodInvoker)(() =>
        //    {
        //        foreach (ListViewItem item in this.listView1.Items)
        //        {
        //            if (item.Tag.Equals(_client))
        //            {
        //                item.ForeColor = Color.Red;
        //                item.SubItems[0].Text = "×";
        //                return;
        //            }
        //        }
        //    }));
        //}
        //void AddClientToList(ClientSocket _client)
        //{
        //    this.Invoke((MethodInvoker)(() =>
        //    {
        //        ListViewItem li = new ListViewItem();
        //        li.SubItems[0].Text = _client.SocketID.ToString();
        //        li.SubItems.Add(_client.sRemoteIPAndPort);
        //        li.SubItems.Add("0");
        //        li.SubItems.Add("-");
        //        li.Tag = _client;
        //        this.listView1.Items.Add(li);
        //    }));

        //}

        void AddClientReceivePack(ClientSocket client, Data data)
        {
            this.Invoke((MethodInvoker)(() =>
            {
                toolStripStatusLabel1.Text = "收到";
                ObjectCount++;
                toolStripStatusLabel3.Text = "来自" + client.cSocket.LocalEndPoint.ToString() + "第" + ObjectCount + "个数据[" + data.DataGuid.ToString("N") + "]";
                textBox3.Text = Encoding.UTF8.GetString(data.Buffer);
            }));
        }
        void DoExitReceiveData(ClientSocket _client, List<byte> mBytesBuffer)
        {
            ExchangeObject mObject = new ExchangeObject();
            while (mBytesBuffer.Count >= 12)
            {
                if (mObject.Format(mBytesBuffer))
                {
                    switch ((Common.ECommand)mObject.PackType)
                    {
                        case Common.ECommand.Data:
                            Data data = new Data();
                            if (mObject.GetStruct<Data>(ref data))
                            {
                                AddClientReceivePack(_client, data);
                                //_client.SendData(mObject.ToBuffer<Data>(data, Common.ECommand.Data));
                            }
                            else
                            {
                                Console.WriteLine("解析Data对象错误");
                            }
                            break;
                    }
                }
                else
                {
                    mBytesBuffer.RemoveAt(0);
                }
            }
        }
        void client_OnMessageEvent(object sender, MessageEventArgs args)
        {
            SetStatus(args.mMessage);
        }
        void client_OnDisconnectEvent(object sender, DisconenctEventArgs args)
        {
            SetStatus("和服务器断开");
            SetConencetStatus(false);
        }
        void SetConencetStatus(bool mConnected)
        {
            this.Invoke((MethodInvoker)(() =>
            {
                if (mConnected)
                {
                    this.toolStripStatusLabel1.Text = "连接";
                    this.pictureBox1.Image = global::ClientTest.Properties.Resources.check;
                    this.pictureBox1.Enabled = true;
                    toolStripStatusLabel3.Text = client.cSocket.LocalEndPoint.ToString();
                }
                else
                {
                    this.pictureBox1.Enabled = false;
                    this.toolStripStatusLabel1.Text = "断开";
                    this.pictureBox1.Image = global::ClientTest.Properties.Resources.busy;
                }
            }));
        }

        void SetStatus(String msg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    SetStatus(msg);
                }));
            }
            else
            {
                this.toolStripStatusLabel1.Text = msg;
            }
        }
        void client_OnConnectEvent(object sender, ConnectEventArgs args)
        {
            SetStatus("连接服务器成功." + client.cSocket.RemoteEndPoint.ToString());
            SetConencetStatus(true);
            //Task.Factory.StartNew(() =>
            //{
                int mProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
                ExchangeObject mObject = new ExchangeObject();
                byte[] mBytes = mObject.ToBuffer<int>(mProcessId, Common.ECommand.Login);
                ((ClientSocket)sender).SendData(mBytes);
                Thread.Sleep(100);
                Send(null);
            //});
        }

        public void Send(byte[] data)
        {
            Task.Factory.StartNew(() =>
            {
                int mSleepCount = (new Random((int)DateTime.Now.Ticks)).Next(10);
                ExchangeObject mObject = new ExchangeObject();
                byte[] mBytes = mObject.ToBuffer<Data>(new Data
                {
                    DataGuid = Guid.NewGuid(),
                    Buffer = data
                }, Common.ECommand.Data);
                client.SendData(mBytes);
                Thread.Sleep(mSleepCount);
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "连接")
            {
                ConnectToServer((int)numericUpDown2.Value);
            }
            else
            {
                DisconnectServer();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string msg = textBox2.Text.Trim();
            if (!string.IsNullOrEmpty(msg))
                Send(Encoding.UTF8.GetBytes(msg));
            else
                MessageBox.Show("数据不能为空！");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox3.Clear();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            List<RawData> record = DataManager.GetData();
            foreach (RawData data in record)
            {
                listBox1.Items.Add("[" + data.Time.ToString("yy-MM-dd HH:mm:ss") + "]{ID:" + data.ID + ",From:" + data.Source + "}" + Encoding.UTF8.GetString(data.Data));
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                toolTip1.SetToolTip(listBox1, listBox1.SelectedItem.ToString());
            }
        }
    }
}
