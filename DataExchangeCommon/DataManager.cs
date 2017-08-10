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
    public static class DataManager
    {
        static bool init;
        static SqlConnection conn;
        static string tablename;

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

        private static bool InitDB(string dbname, string server, string userid, string password, string tablename = "History")
        {
            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder();
            scsb.DataSource = server;
            scsb.InitialCatalog = dbname;
            scsb.UserID = userid;
            scsb.Password = password;
            DataManager.tablename = tablename;
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

        public static List<RawData> GetData(int count = 20)
        {
            //if (!init)
            //    throw new ApplicationException("尚未连接数据库，请先调用InitDB方法来初始化映射管理器！");
            string connString = ConfigurationManager.ConnectionStrings["connString"].ConnectionString;
            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(connString);
            if (!MapManager.Init) DataManager.InitDB(scsb.InitialCatalog, scsb.DataSource, scsb.UserID, scsb.Password);
            List<RawData> record = new List<RawData>();
            try
            {
                ExchangeObject mObject = new ExchangeObject();
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                using (SqlCommand cmd = new SqlCommand("select top " + count + " [ID],[Source],[RawData],[DataTime] from [" + tablename + "] order by [ID] desc", conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            long id = reader.GetInt64(0);
                            string source = reader.GetString(1);
                            byte[] data = (byte[])reader[2];
                            //Data dat = new Data();
                            //if (data.Length >= 12 && mObject.Format(new List<byte>(data)) && (Common.ECommand)mObject.PackType == Common.ECommand.Data && mObject.GetStruct<Data>(ref dat))
                            //{
                            //    if (dat.Buffer != null)
                            //        data = dat.Buffer;// mObject.ToBuffer<Data>(dat, Common.ECommand.Data);
                            //}
                            DateTime time = reader.GetDateTime(3);
                            record.Add(new RawData(id, source, data, time));
                        }
                    }
                }
            }
            finally
            {
                conn.Close();
            }
            return record;
        }

        public static long AddData(string source, byte[] data)
        {
            //if (!init)
            //    throw new ApplicationException("尚未连接数据库，请先调用InitDB方法来初始化映射管理器！");
            string connString = ConfigurationManager.ConnectionStrings["connString"].ConnectionString;
            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(connString);
            if (!MapManager.Init) DataManager.InitDB(scsb.InitialCatalog, scsb.DataSource, scsb.UserID, scsb.Password);
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                using (SqlCommand cmd = new SqlCommand("insert into [" + tablename + "]([Source], [RawData]) values (@Source, @RawData); select SCOPE_IDENTITY()", conn))
                {
                    SqlParameter[] paras = new SqlParameter[2]
                    {
                        new SqlParameter("@Source", SqlDbType.VarChar, 50),
                        new SqlParameter("@RawData", SqlDbType.VarBinary)
                    };
                    paras[0].Value = source;
                    paras[1].Value = data;
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddRange(paras);
                    object obj = cmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value)
                        return Convert.ToInt64(obj);
                    return 0;
                }
            }
            finally
            {
                conn.Close();
            }
        }
    }
}
