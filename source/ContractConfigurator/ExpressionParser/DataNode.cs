using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                DataNode node = NodeForKey(ref s);
                return node.data[s].value;
            }
            set
            {
                DataNode node = NodeForKey(ref s);
                lastModified = Time.fixedTime;
                if (!node.data.ContainsKey(s))
                {
                    node.data[s] = new Value(value);
                }
                else
                {
                    node.data[s].value = value;
                    node.data[s].initialized = true;
                }

                LoggingUtil.LogVerbose(this, "DataNode[" + node.name + "], storing " + s + " = " + OutputValue(value));
            }
        }

        public List<ConfigNodeUtil.DeferredLoadBase> DeferredLoads
        {
            get { return (root != null ? root : this).deferredLoads; }
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
            DataNode node = NodeForKey(ref s);
            if (!node.data.ContainsKey(s))
            {
                return true;
            }

            return node.data[s].deterministic;
        }

        public void SetDeterministic(string s, bool value)
        {
            DataNode node = NodeForKey(ref s);
            if (!node.data.ContainsKey(s))
            {
                node.data[s] = new Value(null);
                node.data[s].initialized = false;
            }
            node.data[s].deterministic = value;
        }

        /// <summary>
        /// Validates whether the given value is initialized or not.
        /// </summary>
        /// <param name="key">The key to check for</param>
        /// <returns>True if the value has been read</returns>
        public bool IsInitialized(string key)
        {
            DataNode node = NodeForKey(ref key);
            return node.data.ContainsKey(key) && node.data[key] != null && node.data[key].initialized;
        }

        public void BlankInit(string key, Type type)
        {
            DataNode node = NodeForKey(ref key);
            if (!node.data.ContainsKey(key))
            {
                node.data[key] = new Value(type);
            }
        }

        public Type GetType(string key)
        {
            DataNode node = NodeForKey(ref key);
            return node.data.ContainsKey(key) && node.data[key] != null ? node.data[key].type : null;
        }

        public DataNode Parent
        {
            get { return parent; }
            set { parent = value; }
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
            
            if (parent != null && parent.factory.GetType() != typeof(ContractGroup))
            {
                // Check for duplicate name - probably could be more efficient here
                int i = 1;
                while (parent.children.Any(dn => dn.name == this.name))
                {
                    this.name = name + "_" + i++;
                }

                parent.children.Add(this);
                root = parent.root;
            }
            else if (factory.GetType() != typeof(ContractGroup))
            {
                root = this;
            }
            else
            {
                root = null;
            }
        }

        public string DebugString()
        {
            string result = "";

            for (DataNode node = this; node != null; node = (this == root ? node.parent : null))
            {
                string nodeResults = "";
                string prefix = node == this ? "" : node.name + ":";
                if (node == this || node.root == null)
                {
                    foreach (KeyValuePair<string, Value> pair in node.data)
                    {
                        nodeResults += "    <color=lime>" + prefix + pair.Key + "</color> = " + OutputValue(pair.Value.value) +
                            ", deterministic = " + pair.Value.deterministic + "\n";
                    }
                }
                result = nodeResults + result;
            }
            return "<i>" + Name + "</i>\n" + result;
        }

        private static string OutputValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            Type type = value.GetType();
            string output;
            if (type == typeof(ScienceSubject))
            {
                output = ((ScienceSubject)(value)).id;
            }
            else if (type == typeof(ScienceExperiment))
            {
                output = ((ScienceExperiment)(value)).id;
            }
            else if (type == typeof(AvailablePart))
            {
                output = ((AvailablePart)(value)).name;
            }
            else if (type == typeof(Vessel))
            {
                output = ((Vessel)(value)).vesselName;
            }
            else if (type.Name == "List`1")
            {
                output = "[ ";
                System.Collections.IEnumerable list = (System.Collections.IEnumerable)value;
                foreach (object o in list)
                {
                    output += OutputValue(o) + ", ";
                }
                output = output.Length == 2 ? "[]" : (output.Remove(output.Length - 2) + " ]");
            }
            else
            {
                output = value.ToString();
            }

            return output;
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

            return (node != null ? "/" : "") + path;
        }

        /// <summary>
        /// Parses the child DATA nodes out of the given config node, and returns the parsed values back in dataValues.
        /// </summary>
        /// <param name="configNode">The ConfigNode to load child DATA nodes from.</param>
        /// <param name="obj">The ContractConfigurator object to load from.</param>
        /// <param name="dataValues"></param>
        /// <param name="uniqueValues"></param>
        /// <returns></returns>
        public bool ParseDataNodes(ConfigNode configNode, IContractConfiguratorFactory obj,
            Dictionary<string, bool> dataValues, Dictionary<string, bool> uniqueValues)
        {
            bool valid = true;

            foreach (ConfigNode data in ConfigNodeUtil.GetChildNodes(configNode, "DATA"))
            {
                Type type = null;
                bool requiredValue = true;
                bool uniqueValue = false;
                bool activeUniqueValue = false;
                ConfigNodeUtil.SetCurrentDataNode(null);
                valid &= ConfigNodeUtil.ParseValue<Type>(data, "type", x => type = x, obj);
                valid &= ConfigNodeUtil.ParseValue<bool>(data, "requiredValue", x => requiredValue = x, obj, true);
                valid &= ConfigNodeUtil.ParseValue<bool>(data, "uniqueValue", x => uniqueValue = x, obj, false);
                valid &= ConfigNodeUtil.ParseValue<bool>(data, "activeUniqueValue", x => activeUniqueValue = x, obj, false);
                ConfigNodeUtil.SetCurrentDataNode(this);

                if (type != null)
                {
                    foreach (ConfigNode.Value pair in data.values)
                    {
                        string name = pair.name;
                        if (name != "type" && name != "requiredValue" && name != "uniqueValue" && name != "activeUniqueValue")
                        {
                            object value = null;

                            // Create the setter function
                            Type actionType = typeof(Action<>).MakeGenericType(type);
                            Delegate del = Delegate.CreateDelegate(actionType, value, typeof(ContractType).GetMethod("NullAction"));

                            // Get the ParseValue method
                            MethodInfo method = typeof(ConfigNodeUtil).GetMethods(BindingFlags.Static | BindingFlags.Public).
                                Where(m => m.Name == "ParseValue" && m.GetParameters().Count() == 4).Single();
                            method = method.MakeGenericMethod(new Type[] { type });

                            // Invoke the ParseValue method
                            valid &= (bool)method.Invoke(null, new object[] { data, name, del, obj });

                            dataValues[name] = requiredValue;

                            if (uniqueValue || activeUniqueValue)
                            {
                                uniqueValues[name] = activeUniqueValue;
                            }
                        }
                    }
                }
            }

            return valid;
        }

        private DataNode NodeForKey(ref string key)
        {
            LoggingUtil.LogVerbose(this, "Node for key: " + key);
            if (!key.Contains(':'))
            {
                return this;
            }

            string[] names = key.Split(':');
            if (names.Count() > 2)
            {
                throw new ArgumentException("Key value '" + key + "' is invalid, can only have one namespace preceeded by a colon (:).");
            }

            string group = names[0];

            for (DataNode node = (root != null ? root : this).parent; node != null; node = node.parent)
            {
                if (node.name == group)
                {
                    key = names[1];
                    return node;
                }
            }

            throw new ArgumentException("Contract group '" + group + "' does not exist, or is not a parent of this contract.");
        }
    }
}
