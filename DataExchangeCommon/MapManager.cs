using DataExchangeCommon;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Configuration;

namespace DataExchangeCommon
{
    public static class MapManager
    {
        static AddressMap map;
        static bool init;
        static SqlConnection conn;
        static string tablename;

        public static AddressMap Map
        {
            get
            {
                return map;
            }
        }

        public static bool Init
        {
            get
            {
                return init;
            }
        }

        public static void Dispose()
        {
            if (conn != null)
            {
                try
                {
                    if (conn.State != ConnectionState.Closed)
                        conn.Close();
                }
                catch { }
                finally
                {
                    conn.Dispose();
                }
            }
            init = false;
        }

        private static bool InitDB(string dbname, string server, string userid, string password, string tablename = "Map")
        {
            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder();
            scsb.DataSource = server;
            scsb.InitialCatalog = dbname;
            scsb.UserID = userid;
            scsb.Password = password;
            MapManager.tablename = tablename;
            try
            {
                conn = new SqlConnection(scsb.ConnectionString);
                init = true;
            }
            catch
            {
                init = false;
            }
            return init;
        }

        public static AddressAndPort ParseFrom(string str)
        {
            string[] ss = str.Split(':');
            if (ss.Length == 2)
            {
                return new AddressAndPort() { Address = ss[0], Port = Convert.ToInt32(ss[1]) };
            }
            return AddressAndPort.Empty;
        }

        public static List<AddressAndPort> ParseTo(string str)
        {
            List<AddressAndPort> aps = new List<AddressAndPort>();
            string[] ss = str.Split('|');
            foreach (string s in ss)
            {
                if (s.IndexOf(":") > -1)
                    aps.Add(ParseFrom(s));
            }
            return aps;
        }

        public static string Parse(AddressAndPort ap)
        {
            return ap.ToString();
        }

        public static void LoadMap()
        {
            //if (!init)
            //    throw new ApplicationException("尚未连接数据库，请先调用InitDB方法来初始化映射管理器！");
            string connString = ConfigurationManager.ConnectionStrings["connString"].ConnectionString;
            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(connString);
            if (!MapManager.Init) MapManager.InitDB(scsb.InitialCatalog, scsb.DataSource, scsb.UserID, scsb.Password);
            if (map == null) map = new AddressMap();
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                using (SqlCommand cmd = new SqlCommand("select [From],[To],[ID],[Remark] from [" + tablename + "]", conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            string from = reader.GetString(0);
                            string to = reader.GetString(1);
                            int id = reader.GetInt32(2);
                            string remark = reader.GetString(3);
                            AddressAndPort From = ParseFrom(from);
                            List<AddressAndPort> To = ParseTo(to);
                            Tuple<int, List<AddressAndPort>, string> Tos = new Tuple<int, List<AddressAndPort>, string>(id, To, remark);
                            if (!map.Exist(From))
                                map.Add(From, Tos);
                        }
                    }
                }
            }
            finally
            {
                conn.Close();
            }
        }

        public static int AddMap(KeyValuePair<AddressAndPort, List<AddressAndPort>> map, string remark)
        {
            //if (!init)
            //    throw new ApplicationException("尚未连接数据库，请先调用InitDB方法来初始化映射管理器！");
            string connString = ConfigurationManager.ConnectionStrings["connString"].ConnectionString;
            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(connString);
            if (!MapManager.Init) MapManager.InitDB(scsb.InitialCatalog, scsb.DataSource, scsb.UserID, scsb.Password);
            StringBuilder sb = new StringBuilder();
            foreach (AddressAndPort ap in map.Value)
            {
                if (sb.Length == 0)
                    sb.Append(ap.ToString());
                else
                    sb.Append("|" + ap.ToString());
            }
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                using (SqlCommand cmd = new SqlCommand("if exists (select 1 from [" + tablename + "] where [From]='" + map.Key + "') update [" + tablename + "] set [To]='" + sb.ToString() + "', [Remark]='" + remark + "' where [From]='" + map.Key + "' else insert into [" + tablename + "]([From],[To],[Remark]) values ('" + map.Key + "', '" + sb.ToString() + "', '" + remark + "'); select SCOPE_IDENTITY()", conn))
                {
                    object obj = cmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value)
                        return Convert.ToInt32(obj);
                    return 0;
                }
            }
            finally
            {
                conn.Close();
            }
        }

        public static bool UpdateMap(int id, KeyValuePair<AddressAndPort, List<AddressAndPort>> map, string remark)
        {
            //if (!init)
            //    throw new ApplicationException("尚未连接数据库，请先调用InitDB方法来初始化映射管理器！");
            string connString = ConfigurationManager.ConnectionStrings["connString"].ConnectionString;
            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(connString);
            if (!MapManager.Init) MapManager.InitDB(scsb.InitialCatalog, scsb.DataSource, scsb.UserID, scsb.Password);
            StringBuilder sb = new StringBuilder();
            foreach (AddressAndPort ap in map.Value)
            {
                if (sb.Length == 0)
                    sb.Append(ap.ToString());
                else
                    sb.Append("|" + ap.ToString());
            }
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                using (SqlCommand cmd = new SqlCommand("update [" + tablename + "] set [From]='" + map.Key + "', [To]='" + sb.ToString() + "', [Remark]='" + remark + "' where [ID]=" + id, conn))
                {
                    int i = cmd.ExecuteNonQuery();
                    return i > 0;
                }
            }
            finally
            {
                conn.Close();
            }
        }

        public static bool DeleteMap(int id)
        {
            //if (!init)
            //    throw new ApplicationException("尚未连接数据库，请先调用InitDB方法来初始化映射管理器！");
            string connString = ConfigurationManager.ConnectionStrings["connString"].ConnectionString;
            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(connString);
            if (!MapManager.Init) MapManager.InitDB(scsb.InitialCatalog, scsb.DataSource, scsb.UserID, scsb.Password);
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                using (SqlCommand cmd = new SqlCommand("delete from [" + tablename + "] where [ID]=" + id, conn))
                {
                    int i = cmd.ExecuteNonQuery();
                    return i > 0;
                }
            }
            finally
            {
                conn.Close();
            }
        }
    }
}
