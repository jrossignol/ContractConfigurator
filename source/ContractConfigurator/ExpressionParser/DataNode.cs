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
        static MethodInfo methodParseValue = typeof(ConfigNodeUtil).GetMethods(BindingFlags.Static | BindingFlags.Public).
            Where(m => m.Name == "ParseValue" && m.GetParameters().Count() == 4).Single();

        public enum UniquenessCheck
        {
            NONE,
            CONTRACT_ACTIVE,
            CONTRACT_ALL,
            GROUP_ACTIVE,
            GROUP_ALL,
        }

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

        protected static MethodInfo parseMethodGeneric = typeof(ConfigNodeUtil).GetMethods(BindingFlags.Static | BindingFlags.Public).
            Where(m => m.Name == "ParseValue" && m.GetParameters().Count() == 4).Single();

        public static int IteratorCurrentIndex = 0;

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

        public DataNode GetChild(string name)
        {
            foreach (DataNode child in children)
            {
                if (child.name == name)
                {
                    return child;
                }
            }
            return null;
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
            else if (factory != null && factory.GetType() != typeof(ContractGroup))
            {
                root = this;
            }
            else
            {
                root = null;
            }
        }

        public string DebugString(bool applyFormatting = true)
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
                        nodeResults += (applyFormatting ? "<color=lime>" : "") + "    " + prefix + pair.Key + (applyFormatting ? "</color>" : "") + " = " + OutputValue(pair.Value.value) +
                            ", deterministic = " + pair.Value.deterministic + "\n";
                    }
                }
                result = nodeResults + result;
            }
            return (applyFormatting ? "<i>" : "") + Name + (applyFormatting ? "</i>\n" : "\n") + result;
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

        public bool IsChildOf(DataNode node)
        {
            for (DataNode p = this; p != null; p = p.parent)
            {
                if (node == p)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Parses the child DATA nodes out of the given config node, and returns the parsed values back in dataValues.
        /// </summary>
        /// <param name="configNode">The ConfigNode to load child DATA nodes from.</param>
        /// <param name="obj">The ContractConfigurator object to load from.</param>
        /// <param name="dataValues"></param>
        /// <param name="uniquenessChecks"></param>
        /// <returns></returns>
        public bool ParseDataNodes(ConfigNode configNode, IContractConfiguratorFactory obj,
            Dictionary<string, ContractType.DataValueInfo> dataValues, Dictionary<string, UniquenessCheck> uniquenessChecks)
        {
            bool valid = true;

            foreach (ConfigNode data in ConfigNodeUtil.GetChildNodes(configNode, "DATA"))
            {
                Type type = null;
                bool requiredValue = true;
                bool hidden = true;
                string title = "";

                ConfigNodeUtil.SetCurrentDataNode(null);
                valid &= ConfigNodeUtil.ParseValue<Type>(data, "type", x => type = x, obj);
                valid &= ConfigNodeUtil.ParseValue<bool>(data, "requiredValue", x => requiredValue = x, obj, true);
                valid &= ConfigNodeUtil.ParseValue<string>(data, "title", x => title = x, obj, "");
                valid &= ConfigNodeUtil.ParseValue<bool>(data, "hidden", x => hidden = x, obj, false);

                bool doneTitleWarning = false;

                UniquenessCheck uniquenessCheck = UniquenessCheck.NONE;
                // Backwards compatibility for Contract Configurator 1.8.3
                if (data.HasValue("uniqueValue") || data.HasValue("activeUniqueValue"))
                {
                    LoggingUtil.LogWarning(this, "The use of uniqueValue and activeUniqueValue is obsolete since Contract Configurator 1.9.0, use uniquenessCheck instead.");
                    
                    bool uniqueValue = false;
                    bool activeUniqueValue = false;
                    valid &= ConfigNodeUtil.ParseValue<bool>(data, "uniqueValue", x => uniqueValue = x, obj, false);
                    valid &= ConfigNodeUtil.ParseValue<bool>(data, "activeUniqueValue", x => activeUniqueValue = x, obj, false);

                    uniquenessCheck = activeUniqueValue ? UniquenessCheck.CONTRACT_ACTIVE : uniqueValue ? UniquenessCheck.CONTRACT_ALL : UniquenessCheck.NONE;
                }
                else
                {
                    valid &= ConfigNodeUtil.ParseValue<UniquenessCheck>(data, "uniquenessCheck", x => uniquenessCheck = x, obj, UniquenessCheck.NONE);
                }

                ConfigNodeUtil.SetCurrentDataNode(this);

                if (type != null)
                {
                    foreach (ConfigNode.Value pair in data.values)
                    {
                        string name = pair.name;
                        if (name != "type" && name != "title" && name != "hidden" && name != "requiredValue" && name != "uniqueValue" && name != "activeUniqueValue" && name != "uniquenessCheck")
                        {
                            if (uniquenessCheck != UniquenessCheck.NONE)
                            {
                                uniquenessChecks[name] = uniquenessCheck;
                            }

                            object value = null;

                            // Create the setter function
                            Type actionType = typeof(Action<>).MakeGenericType(type);
                            Delegate del = Delegate.CreateDelegate(actionType, value, typeof(DataNode).GetMethod("NullAction"));

                            // Set the ParseValue method generic
                            MethodInfo method = methodParseValue.MakeGenericMethod(new Type[] { type });

                            // Invoke the ParseValue method
                            valid &= (bool)method.Invoke(null, new object[] { data, name, del, obj });

                            dataValues[name] = new ContractType.DataValueInfo(title, requiredValue, hidden, type);

                            // Recommend a title
                            if (!data.HasValue("title") && requiredValue && !IsDeterministic(name) && !hidden && !doneTitleWarning && !dataValues[name].IsIgnoredType())
                            {
                                doneTitleWarning = true;

                                LoggingUtil.Log(obj.minVersion >= ContractConfigurator.ENHANCED_UI_VERSION ? LoggingUtil.LogLevel.ERROR : LoggingUtil.LogLevel.WARNING, this,
                                    obj.ErrorPrefix() + ": " + name + ": The field 'title' is required in for data node values where 'requiredValue' is true.  Alternatively, the attribute 'hidden' can be set to true (but be careful - this can cause player confusion if all lines for the contract type show as 'Met' and the contract isn't generating).");

                                // Error on newer versions of contract packs
                                if (obj.minVersion >= ContractConfigurator.ENHANCED_UI_VERSION)
                                {
                                    valid = false;
                                }
                            }

                        }
                    }
                }
            }

            return valid;
        }


        /// <summary>
        /// Loads the ITERATOR nodes.
        /// </summary>
        /// <param name="node"></param>
        /// <returns>Whether the load was successful</returns>
        public static bool LoadIteratorNodes(ConfigNode node, IContractConfiguratorFactory obj)
        {
            bool valid = true;

            IEnumerable<ConfigNode> iteratorNodes = ConfigNodeUtil.GetChildNodes(node, "ITERATOR");
            if (!iteratorNodes.Any())
            {
                return true;
            }
            else if (iteratorNodes.Count() > 1)
            {
                LoggingUtil.LogError(obj, "Multiple ITERATOR nodes found - only one iterator node allowed.");
                return false;
            }

            ConfigNode iteratorNode = iteratorNodes.First();
            DataNode iteratorDataNode = new DataNode("ITERATOR", obj.dataNode, obj);
            try
            {
                ConfigNodeUtil.SetCurrentDataNode(iteratorDataNode);

                valid &= ConfigNodeUtil.ParseValue<Type>(iteratorNode, "type", x => obj.iteratorType = x, obj);
                if (obj.iteratorType != null)
                {
                    foreach (ConfigNode.Value pair in iteratorNode.values)
                    {
                        string name = pair.name;
                        if (name != "type")
                        {
                            if (!string.IsNullOrEmpty(obj.iteratorKey))
                            {
                                LoggingUtil.LogError(obj, "Multiple key values found in ITERATOR node - only one key allowed.");
                                return false;
                            }

                            // Create the list type
                            Type listType = typeof(List<>);
                            listType = listType.MakeGenericType(new Type[] { obj.iteratorType });

                            // Create the setter function
                            object value = null;
                            Type listActionType = typeof(Action<>).MakeGenericType(listType);
                            Delegate listDelegate = Delegate.CreateDelegate(listActionType, value, typeof(DataNode).GetMethod("NullAction"));

                            // Get the ParseValue method
                            MethodInfo parseListMethod = parseMethodGeneric.MakeGenericMethod(new Type[] { listType });

                            // Invoke the ParseValue method
                            valid &= (bool)parseListMethod.Invoke(null, new object[] { iteratorNode, name, listDelegate, obj });

                            // Store the iterator key for later
                            obj.iteratorKey = name;
                        }
                    }
                }

                // Load didn't get us a key
                if (string.IsNullOrEmpty(obj.iteratorKey))
                {
                    LoggingUtil.LogError(obj, "No key field was defined for the ITERATOR!.");
                    return false;
                }
            }
            finally
            {
                ConfigNodeUtil.SetCurrentDataNode(obj.dataNode);
            }

            // Add a dummy value to the parent data node
            node.AddValue(obj.iteratorKey, "@ITERATOR/" + obj.iteratorKey + ".ElementAt(IteratorCurrentIndex())");
            node.AddValue("iteratorCount", "@ITERATOR/" + obj.iteratorKey + ".Count()");

            return valid;
        }

        public static bool InitializeIteratorKey(ConfigNode node, IContractConfiguratorFactory obj)
        {
            bool valid = true;

            // Nothing to do
            if (obj.iteratorType == null)
            {
                return true;
            }

            // Set the correct data node
            ConfigNodeUtil.SetCurrentDataNode(obj.dataNode);

            // First initalize our iterator count variable (no reflection needed)
            valid &= ConfigNodeUtil.ParseValue<int>(node, "iteratorCount", x => { }, obj);

            // Check if it already exists (happens when using the an existing value for a key)
            if (obj.dataNode.data.ContainsKey(obj.iteratorKey)) 
            {
                return valid;
            }

            // Create the setter function
            object value = null;
            Type actionType = typeof(Action<>).MakeGenericType(obj.iteratorType);
            Delegate del = Delegate.CreateDelegate(actionType, value, typeof(DataNode).GetMethod("NullAction"));

            // Invoke the ParseValue method
            MethodInfo parseMethod = parseMethodGeneric.MakeGenericMethod(new Type[] { obj.iteratorType });
            valid &= (bool)parseMethod.Invoke(null, new object[] { node, obj.iteratorKey, del, obj });

            return valid;
        }

        private DataNode NodeForKey(ref string key)
        {
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

        public static void NullAction(object o)
        {
        }
    }
}
