using DataExchangeCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataExchangeCommon
{
    public class AddressMap
    {
        Dictionary<AddressAndPort, Tuple<int, List<AddressAndPort>, string>> map;

        public Dictionary<AddressAndPort, Tuple<int, List<AddressAndPort>, string>>.KeyCollection Froms
        {
            get
            {
                return map.Keys;
            }
        }

        public Dictionary<AddressAndPort, Tuple<int, List<AddressAndPort>, string>>.ValueCollection Tos
        {
            get
            {
                return map.Values;
            }
        }

        public Tuple<int, List<AddressAndPort>, string> this[AddressAndPort from]
        {
            get
            {
                return map[from];
            }
            set
            {
                map[from] = value;
            }
        }

        public AddressMap()
        {
            map = new Dictionary<AddressAndPort, Tuple<int, List<AddressAndPort>, string>>();
        }

        public AddressMap(int id, AddressAndPort from, AddressAndPort to, string remark)
        {
            map = new Dictionary<AddressAndPort, Tuple<int, List<AddressAndPort>, string>>();
            List<AddressAndPort> tos = new List<AddressAndPort>();
            tos.Add(to);
            map.Add(from, new Tuple<int, List<AddressAndPort>, string>(id, tos, remark));
        }

        public AddressMap(int id, AddressAndPort from, List<AddressAndPort> to, string remark)
        {
            map = new Dictionary<AddressAndPort, Tuple<int, List<AddressAndPort>, string>>();
            map.Add(from, new Tuple<int, List<AddressAndPort>, string>(id, to, remark));
        }

        public bool Exist(AddressAndPort from)
        {
            return map.ContainsKey(from);
        }

        public void Add(int id, AddressAndPort from, AddressAndPort to, string remark)
        {
            if (!map.ContainsKey(from))
            {
                List<AddressAndPort> tos = new List<AddressAndPort>();
                tos.Add(to);
                map.Add(from, new Tuple<int, List<AddressAndPort>, string>(id, tos, remark));
            }
            else
            {
                if (!map[from].Item2.Contains(to))
                    map[from].Item2.Add(to);
            }
        }

        public void Add(AddressAndPort from, Tuple<int, List<AddressAndPort>, string> to)
        {
            if (!map.ContainsKey(from))
            {
                map.Add(from, to);
            }
            else
            {
                foreach (AddressAndPort ap in to.Item2)
                {
                    if (!map[from].Item2.Contains(ap))
                        map[from].Item2.Add(ap);
                }
            }
        }

        public void RemoveFrom(AddressAndPort from)
        {
            if (map.ContainsKey(from))
                map.Remove(from);
        }

        public void RemoveTo(AddressAndPort from, AddressAndPort to)
        {
            if (map.ContainsKey(from))
            {
                if (map[from].Item2.Contains(to))
                    map[from].Item2.Remove(to);
            }
        }

        public void Update(int id, AddressAndPort from, List<AddressAndPort> to, string remark)
        {
            if (map.ContainsKey(from))
            {
                map.Remove(from);
            }
            map.Add(from, new Tuple<int, List<AddressAndPort>, string>(id, to, remark));
        }

        public List<AddressAndPort> GetTo(AddressAndPort from)
        {
            List<AddressAndPort> tos = new List<AddressAndPort>();
            if (map.ContainsKey(from))
                tos = map[from].Item2;
            return tos;
        }

        public List<AddressAndPort> GetFrom(AddressAndPort to)
        {
            List<AddressAndPort> froms = new List<AddressAndPort>();
            foreach (AddressAndPort ap in map.Keys)
            {
                if (map[ap].Item2.Contains(to))
                    froms.Add(ap);
            }
            return froms;
        }

        public List<int> GetToPort(int fromPort)
        {
            List<int> tos = new List<int>();
            foreach (AddressAndPort from in map.Keys)
            {
                if (from.Port == fromPort)
                {
                    foreach (AddressAndPort to in map[from].Item2)
                    {
                        tos.Add(to.Port);
                    }
                }
            }
            return tos;
        }

        public List<int> GetFromPort(int toPort)
        {
            List<int> froms = new List<int>();
            foreach (AddressAndPort from in map.Keys)
            {
                foreach (AddressAndPort to in map[from].Item2)
                {
                    if (to.Port == toPort)
                       froms.Add(from.Port);
                }
            }
            return froms;
        }

        public string GetRemark(AddressAndPort from)
        {
            return map[from].Item3;
        }
    }
}
