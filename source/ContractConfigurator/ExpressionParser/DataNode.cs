using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Analog to a ConfigNode - contains data after expanding expression, and utility methods
    /// to get parent/children.
    /// </summary>
    public class DataNode
    {
        public class Value
        {
            public object value;
            public bool deterministic;

            public Value(object v, bool d = true)
            {
                value = v;
                deterministic = d;
            }
        }

        public class ValueNotInitialized : Exception
        {
            public string key;

            public ValueNotInitialized(string key)
                : base("Value @" + key + " has not yet been initialized.")
            {
                this.key = key;
            }
        }

        protected Dictionary<string, Value> data = new Dictionary<string, Value>();
        private DataNode root;
        private DataNode parent;
        private List<DataNode> children = new List<DataNode>();
        private string name;
        private List<ConfigNodeUtil.DeferredLoadBase> deferredLoads = new List<ConfigNodeUtil.DeferredLoadBase>();
        public double lastModified = Time.fixedTime;

        public object this[string s]
        {
            get
            {
                return data[s].value;
            }
            set
            {
                lastModified = Time.fixedTime;
                if (!data.ContainsKey(s))
                {
                    data[s] = new Value(value);
                }
                else
                {
                    data[s].value = value;
                }
            }
        }

        public List<ConfigNodeUtil.DeferredLoadBase> DeferredLoads
        {
            get { return Root.deferredLoads; }
        }

        public bool IsDeterministic(string s)
        {
            if (!data.ContainsKey(s))
            {
                return true;
            }

            return data[s].deterministic;
        }

        public void SetDeterministic(string s, bool value)
        {
            if (!data.ContainsKey(s))
            {
                data[s] = new Value(null);
            }
            data[s].deterministic = value;
        }

        /// <summary>
        /// Validates whether the given value is initialized or not.
        /// </summary>
        /// <param name="key">The key to check for</param>
        /// <returns>True if the value has been read</returns>
        public bool IsInitialized(string key)
        {
            return data.ContainsKey(key) && data[key] != null;
        }

        public DataNode Parent
        {
            get { return parent; }
            private set { parent = value; }
        }

        public DataNode Root
        {
            get { return root; }
            private set { root = value; }
        }

        public string Name
        {
            get { return name; }
            private set { name = value; }
        }

        public IEnumerable<DataNode> Children
        {
            get { return children.AsEnumerable(); }
        }

        public DataNode(string name)
            : this(name, null)
        {
        }

        public DataNode(string name, DataNode parent)
        {
            this.parent = parent;
            this.name = name;
            if (parent != null)
            {
                parent.children.Add(this);
                root = parent.root;
            }
            else
            {
                root = this;
            }
        }

        public string DebugString()
        {
            string result = "";
            foreach (KeyValuePair<string, Value> pair in data)
            {
                result += "    <color=lime>" + pair.Key + "</color> = " + pair.Value.value + "\n"; ;
            }
            return result;
        }

        public string Path()
        {
            string path = "";

            DataNode node = this;
            while (node != Root)
            {
                path = node.name + "/" + path;
                node = node.parent;
            }

            return "/" + path;
        }
    }
}
