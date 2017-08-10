using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using ProtoBuf;
using System.Text.RegularExpressions;
using ProtoBuf.Meta;

namespace DataExchangeCommon
{
    public class Common
    {
        #region 序列化和反序列化的方法
        /// <summary>
        /// 使用protobuf把对象序列化为Byte数组
        /// </summary>
        /// <typeparam name="T">需要反序列化的对象类型，必须声明[ProtoContract]特征，且相应属性必须声明[ProtoMember(序号)]特征</typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal static Byte[] ProtobufSerialize<T>(T obj)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                Serializer.Serialize(memory, obj);
                return memory.ToArray();
            }
        }

        /// <summary>
        /// 使用protobuf反序列化二进制数组为对象
        /// </summary>
        /// <typeparam name="T">需要反序列化的对象类型，必须声明[ProtoContract]特征，且相应属性必须声明[ProtoMember(序号)]特征</typeparam>
        /// <param name="data"></param>
        internal static T ProtobufDeserialize<T>(Byte[] data) where T : struct
        {
            using (MemoryStream memory = new MemoryStream(data))
            {
                return Serializer.Deserialize<T>(memory);
            }
        }
        public static byte[] SerializeSocketInfo(System.Net.Sockets.SocketInformation mInfo)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter mFormatter = new BinaryFormatter();
                mFormatter.Serialize(ms, mInfo);
                ms.Position = 0;
                byte[] mBytes = new byte[ms.Length];
                ms.Read(mBytes, 0, mBytes.Length);
                return mBytes;
            }
        }
        public static System.Net.Sockets.SocketInformation DeserializeSocketInfo(byte[] mBytes)
        {
            using (MemoryStream ms = new MemoryStream(mBytes))
            {
                BinaryFormatter mFormatter = new BinaryFormatter();
                System.Net.Sockets.SocketInformation mInfo = (System.Net.Sockets.SocketInformation)mFormatter.Deserialize(ms);
                return mInfo;
            }
        }
        #endregion

        public enum ECommand : int
        {
            Data,
            SocketInfo,
            Login
        }
    }
    public struct ExchangeObject
    {
        static int seedSerialNumber = 0;
        /// <summary>
        /// 包长度
        /// </summary>
        public int PackLength;
        /// <summary>
        /// 包类型
        /// </summary>
        public int PackType;
        /// <summary>
        /// 流水号
        /// </summary>
        public int SerialNumber;
        /// <summary>
        /// 包数据。这是由其它的结构对象序列化的结果
        /// </summary>
        public byte[] mData;

        public byte[] ToBuffer<T>(T obj, Common.ECommand mCmd) where T : struct
        {
            List<byte> mBytes = new List<byte>(1024);
            PackType = (int)mCmd;
            mData = Common.ProtobufSerialize<T>(obj);
            PackLength = mData.Length + 12;
            SerialNumber = System.Threading.Interlocked.Increment(ref seedSerialNumber);

            mBytes.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(PackLength)));
            mBytes.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(PackType)));
            mBytes.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(SerialNumber)));
            mBytes.AddRange(mData);
            return mBytes.ToArray();
        }
        public byte[] ToBuffer()
        {
            List<byte> mBytes = new List<byte>(1024);
            mBytes.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(PackLength)));
            mBytes.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(PackType)));
            mBytes.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(SerialNumber)));
            mBytes.AddRange(mData);
            return mBytes.ToArray();
        }

        public bool Format(List<byte> mBuffes)
        {
            PackLength = 0;
            int mIndex = 0;
            PackLength = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(mBuffes.GetRange(mIndex, 4).ToArray(), 0));
            if (PackLength > 4096 || PackLength < 12)
            {
                PackLength = -1;
                return false;
            }
            if (PackLength > mBuffes.Count)
                return false;

            byte[] mBytes = mBuffes.GetRange(mIndex, PackLength).ToArray();
            mBuffes.RemoveRange(mIndex, PackLength);

            mIndex += 4;
            PackType = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(mBytes, mIndex));
            mIndex += 4;
            SerialNumber = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(mBytes, mIndex));
            mIndex += 4;

            int mCopyLength = PackLength - 12;
            mData = new byte[mCopyLength];
            if (mCopyLength > 0)
            {
                Array.Copy(mBytes, mIndex, mData, 0, mCopyLength);
            }
            mIndex += mCopyLength;
            return true;
        }
        public bool Format(byte[] mBytes, ref int mIndex)
        {
            PackLength = 0;
            PackLength = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(mBytes, mIndex));
            if (PackLength > 4096 || PackLength < 12)
            {
                PackLength = -1;
                return false;
            }
            if (PackLength > mBytes.Length)
                return false;

            mIndex += 4;
            PackType = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(mBytes, mIndex));
            mIndex += 4;
            SerialNumber = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(mBytes, mIndex));
            mIndex += 4;

            int mCopyLength = PackLength - 12;
            mData = new byte[mCopyLength];
            if (mCopyLength > 0)
            {
                Array.Copy(mBytes, mIndex, mData, 0, mCopyLength);
            }
            mIndex += mCopyLength;
            return true;
        }
        public bool GetStruct<T>(ref T mObject) where T : struct
        {
            if (mData != null)
            {
                try
                {
                    mObject = Common.ProtobufDeserialize<T>(mData);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }

    [ProtoContract]
    public struct Data
    {
        /// <summary>
        /// 数据Guid
        /// </summary>
        [ProtoMember(1)]
        public Guid DataGuid;
        /// <summary>
        /// 姓名
        /// </summary>
        [ProtoMember(2)]
        public byte[] Buffer;
    }

    public struct AddressAndPort
    {
        public string Address;
        public int Port;

        public static AddressAndPort Empty
        {
            get
            {
                AddressAndPort ap = new AddressAndPort();
                ap.Address = "0.0.0.0";
                ap.Port = 0;
                return ap;
            }
        }

        public override string ToString()
        {
            return Address + ":" + Port;
        }

        public static bool operator ==(AddressAndPort a, AddressAndPort b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(AddressAndPort a, AddressAndPort b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                AddressAndPort other = (AddressAndPort)obj;
                return this.ToString().Equals(other.ToString(), StringComparison.InvariantCultureIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public static class AddressAndPortExtensions
    {
        public static bool ContainsPort(this List<AddressAndPort> list, AddressAndPort other)
        {
            foreach (AddressAndPort e in list)
            {
                if (e.Port == other.Port)
                    return true;
            }
            return false;
        }
        public static bool ContainsPort(this List<AddressAndPort> list, int port)
        {
            foreach (AddressAndPort e in list)
            {
                if (e.Port == port)
                    return true;
            }
            return false;
        }
    }

    public class SocketMap
    {
        AddressAndPort device;
        List<ClientSocket> clients;

        public AddressAndPort Device
        {
            get
            {
                return device;
            }
        }

        public List<ClientSocket> Clients
        {
            get
            {
                return clients;
            }
        }

        public SocketMap(AddressAndPort device)
        {
            this.device = device;
            this.clients = new List<ClientSocket>();
        }

        public SocketMap(AddressAndPort device, List<ClientSocket> clients)
        {
            this.device = device;
            if (clients != null)
                this.clients = clients;
            else
                this.clients = new List<ClientSocket>();
        }
    }
}
