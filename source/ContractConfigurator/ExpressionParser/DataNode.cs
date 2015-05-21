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
            public bool initialized;
            public Type type;

            public Value(Type type)
            {
                this.type = type;
                deterministic = true;
                initialized = false;
            }

            public Value(object v, bool d = true)
            {
                value = v;
                deterministic = d;
                initialized = true;
            }
        }

        public class ValueNotInitialized : Exception
        {
            public string key;

            public ValueNotInitialized(string key)
                : this(key, null)
            {
            }

            public ValueNotInitialized(string key, Exception e)
                : base("Value @" + key + " has not yet been initialized.", e)
            {
                this.key = key;
            }
        }

        protected Dictionary<string, Value> data = new Dictionary<string, Value>();
        private IContractConfiguratorFactory factory;
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
                    data[s].initialized = true;
                }
            }
        }

        public List<ConfigNodeUtil.DeferredLoadBase> DeferredLoads
        {
            get { return Root.deferredLoads; }
        }

        public IContractConfiguratorFactory Factory
        {
            get
            {
                return factory;
            }
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
                data[s].initialized = false;
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
            return data.ContainsKey(key) && data[key] != null && data[key].initialized;
        }

        public void BlankInit(string key, Type type)
        {
            if (!data.ContainsKey(key))
            {
                data[key] = new Value(type);
            }
        }

        public Type GetType(string key)
        {
            return data.ContainsKey(key) && data[key] != null ? data[key].type : null;
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

        public DataNode(string name, IContractConfiguratorFactory factory)
            : this(name, null, factory)
        {
        }

        public DataNode(string name, DataNode parent, IContractConfiguratorFactory factory)
        {
            this.parent = parent;
            this.factory = factory;
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
            string result = "<i>" + Name + "</i>\n";

            foreach (KeyValuePair<string, Value> pair in data)
            {
                string output;
                Type type = pair.Value.value != null ? pair.Value.value.GetType() : null;
                if (type == typeof(ScienceSubject))
                {
                    output = ((ScienceSubject)(pair.Value.value)).id;
                }
                else
                {
                    output = pair.Value.value != null ? pair.Value.value.ToString() : "null";
                }

                result += "    <color=lime>" + pair.Key + "</color> = " + output + ", deterministic = " + pair.Value.deterministic + "\n";
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
