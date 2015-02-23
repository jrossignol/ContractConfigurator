using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Analog to a ConfigNode - contains data after expanding expression, and utility methods
    /// to get parent/children.
    /// </summary>
    public class DataNode
    {
        public class ValueNotInitialized : Exception
        {
            public string key;

            public ValueNotInitialized(string key)
                : base("Value @" + key + " has not yet been initialized.")
            {
                this.key = key;
            }
        }

        protected Dictionary<string, object> data = new Dictionary<string, object>();
        private DataNode parent;
        private List<DataNode> children = new List<DataNode>();

        public object this[string s]
        {
            get
            {
                return data[s];
            }
            set
            {
                data[s] = value;
            }
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

        public IEnumerable<DataNode> Children
        {
            get { return children.AsEnumerable(); }
        }

        public DataNode()
            : this(null)
        {
        }

        public DataNode(DataNode parent)
        {
            this.parent = parent;
            if (parent != null)
            {
                parent.children.Add(this);
            }
        }
    }
}
