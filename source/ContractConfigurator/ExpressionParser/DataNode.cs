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

                LoggingUtil.LogVerbose(this, "DataNode[" + name + "], storing " + s + " = " + OutputValue(value));
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
            if (parent != null)
            {
                // Check for duplicate name - probably could be more efficient here
                string origName = name;
                int i = 1;
                while (parent.children.Any(dn => dn.name == name))
                {
                    name = origName + "_" + i++;
                }
            }

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
                result += "    <color=lime>" + pair.Key + "</color> = " + OutputValue(pair.Value.value) + ", deterministic = " + pair.Value.deterministic + "\n";
            }
            return result;
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

            return "/" + path;
        }

        /// <summary>
        /// Parses the child DATA nodes out of the given config node, and returns the parsed values back in dataValues
        /// </summary>
        /// <param name="configNode"></param>
        /// <param name="obj"></param>
        /// <param name="dataValues"></param>
        /// <param name="uniqueValues"></param>
        /// <returns></returns>
        public static bool ParseDataNodes(ConfigNode configNode, IContractConfiguratorFactory obj,
            Dictionary<string, bool> dataValues, Dictionary<string, bool> uniqueValues)
        {
            bool valid = true;

            foreach (ConfigNode data in ConfigNodeUtil.GetChildNodes(configNode, "DATA"))
            {
                Type type = null;
                bool requiredValue = true;
                bool uniqueValue = false;
                bool activeUniqueValue = false;
                valid &= ConfigNodeUtil.ParseValue<Type>(data, "type", x => type = x, obj);
                valid &= ConfigNodeUtil.ParseValue<bool>(data, "requiredValue", x => requiredValue = x, obj, true);
                valid &= ConfigNodeUtil.ParseValue<bool>(data, "uniqueValue", x => uniqueValue = x, obj, false);
                valid &= ConfigNodeUtil.ParseValue<bool>(data, "activeUniqueValue", x => activeUniqueValue = x, obj, false);

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
    }
}
