using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataExchangeCommon
{
    public class RawData
    {
        long id;
        string source;
        byte[] data;
        DateTime time;

        public long ID
        {
            get { return id; }
            set { id = value; }
        }

        public string Source
        {
            get { return source; }
            set { source = value; }
        }

        public byte[] Data
        {
            get { return data; }
            set { data = value; }
        }

        public DateTime Time
        {
            get { return time; }
            set { time = value; }
        }

        public RawData()
        { }

        public RawData(long id, string source, byte[] data, DateTime time)
        {
            this.id = id;
            this.source = source;
            this.data = data;
            this.time = time;
        }
    }
}
