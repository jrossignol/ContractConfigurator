using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Contracts.Agents;
using ContractConfigurator.ExpressionParser;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// Utility class for dealing with ConfigNode objects.
    /// </summary>
    public static class ConfigNodeUtil
    {
        static MethodInfo methodParseSingleValue = typeof(ConfigNodeUtil).GetMethods().Where(m => m.Name == "ParseSingleValue").Single();
        static MethodInfo methodExecuteLoad = typeof(DeferredLoadUtil).GetMethods().Where(m => m.Name == "ExecuteLoad").First();
        static MethodInfo methodGetDependencies = typeof(DeferredLoadUtil).GetMethods().Where(m => m.Name == "GetDependencies").First();
        static MethodInfo methodLogCircularDependencyError = typeof(DeferredLoadUtil).GetMethods().Where(m => m.Name == "LogCircularDependencyError").First();

        public class DeferredLoadBase
        {
            public string key;
            public ConfigNode configNode;
            public IContractConfiguratorFactory obj;
            public List<string> dependencies = new List<string>();
            public DataNode dataNode;
        }

        public class DeferredLoadObject<T> : DeferredLoadBase
        {
            public Action<T> setter;
            public Func<T, bool> validation;

            public DeferredLoadObject(ConfigNode configNode, string key, Action<T> setter, IContractConfiguratorFactory obj, Func<T, bool> validation,
                DataNode dataNode)
            {
                this.key = key;
                this.configNode = configNode;
                this.setter = setter;
                this.obj = obj;
                this.validation = validation;
                this.dataNode = dataNode;
            }
        }

        private static class DeferredLoadUtil
        {
            public static bool ExecuteLoad<T>(DeferredLoadObject<T> loadObj)
            {
                SetCurrentDataNode(loadObj.dataNode);
                return ParseValue<T>(loadObj.configNode, loadObj.key, loadObj.setter, loadObj.obj, loadObj.validation);
            }

            public static IEnumerable<string> GetDependencies<T>(DeferredLoadObject<T> loadObj)
            {
                return loadObj.dependencies;
            }

            public static void LogCircularDependencyError<T>(DeferredLoadObject<T> loadObj)
            {
                LoggingUtil.LogError(loadObj.obj, "{0}: Error parsing {1}: Circular dependency detected while parsing an expression (possible culprit(s): {2}).",
                    loadObj.obj.ErrorPrefix(loadObj.configNode), loadObj.key,
                    string.Join(", ", loadObj.dependencies.ToArray()));
            }
        }

        private static Dictionary<string, DeferredLoadBase> deferredLoads = new Dictionary<string, DeferredLoadBase>();

        private static Dictionary<ConfigNode, Dictionary<string, int>> keysFound = new Dictionary<ConfigNode, Dictionary<string, int>>();
        private static Dictionary<ConfigNode, ConfigNode> storedValues = new Dictionary<ConfigNode, ConfigNode>();
        public static DataNode currentDataNode;
        private static bool initialLoad = true;

        private static Dictionary<string, Type> typeMap = new Dictionary<string, Type>();
        
        static ConfigNodeUtil()
        {
            // Initialize the hardcoded mappings in the type map
            typeMap["bool"] = typeof(bool);
            typeMap["byte"] = typeof(byte);
            typeMap["sbyte"] = typeof(sbyte);
            typeMap["char"] = typeof(char);
            typeMap["short"] = typeof(short);
            typeMap["int"] = typeof(int);
            typeMap["long"] = typeof(long);
            typeMap["ushort"] = typeof(ushort);
            typeMap["uint"] = typeof(uint);
            typeMap["ulong"] = typeof(ulong);
            typeMap["float"] = typeof(float);
            typeMap["double"] = typeof(double);
            typeMap["string"] = typeof(string);
        }

        /// <summary>
        /// Checks whether the mandatory field exists, and if not logs and error.  Returns true
        /// only if the validation succeeded.
        /// </summary>
        /// <param name="configNode">The ConfigNode to check.</param>
        /// <param name="field">The child that is expected</param>
        /// <param name="obj">IContractConfiguratorFactory object for error reporting</param>
        /// <returns>Whether the validation succeeded, additionally logs an error on failure.</returns>
        public static bool ValidateMandatoryChild(ConfigNode configNode, string field, IContractConfiguratorFactory obj)
        {
            if (!configNode.HasNode(field))
            {
                LoggingUtil.LogError(obj.GetType(), "{0}: missing required child node '{1}'.", obj.ErrorPrefix(), field);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that the given config node does NOT contain the given value.
        /// </summary>
        /// <param name="configNode">The ConfigNode to check.</param>
        /// <param name="field">The field to exclude</param>
        /// <param name="obj">IContractConfiguratorFactory object for error reporting</param>
        /// <returns>Always true, but logs a warning for an unexpected value.</returns>
        public static bool ValidateExcludedValue(ConfigNode configNode, string field, IContractConfiguratorFactory obj)
        {
            if (configNode.HasNode(field) || configNode.HasValue(field))
            {
                LoggingUtil.LogWarning(obj.GetType(), "{0}: unexpected entry '{1}' found, ignored.", obj.ErrorPrefix(), field);
            }

            return true;
        }

        /// <summary>
        /// Parses a value from a config node.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="configNode">The ConfigNode to read from</param>
        /// <param name="key">The key to examine.</param>
        /// <param name="allowExpression">Whether the read value can be an expression.</param>
        /// <returns>The parsed value</returns>
        public static T ParseValue<T>(ConfigNode configNode, string key, bool allowExpression = false)
        {
            // Check for required value
            if (!configNode.HasValue(key))
            {
                if (configNode.HasNode(key))
                {
                    return ParseNode<T>(configNode, key, allowExpression);
                }
                else
                {
                    throw new ArgumentException("Missing required value '" + key + "'.");
                }
            }

            // Special cases
            if (typeof(T).Name == "List`1")
            {
                int count = configNode.GetValues(key).Count();

                // Special handling to try and load the list all at once
                if (count == 1 && allowExpression)
                {
                    try
                    {
                        return ParseSingleValue<T>(key, configNode.GetValue(key), allowExpression);
                    }
                    catch (Exception e)
                    {
                        Exception handled = e;
                        while (handled != null && handled.GetType() == typeof(Exception))
                        {
                            handled = handled.InnerException;
                        }

                        // Exceptions we explicitly ignore
                        if (handled == null ||
                            handled.GetType() != typeof(DataStoreCastException) &&
                            handled.GetType() != typeof(NotSupportedException) &&
                            handled.GetType() != typeof(ArgumentNullException) &&
                            handled.GetType() != typeof(InvalidCastException))
                        {
                            // Exceptions we explicitly rethrow
                            if (handled != null && handled.GetType() == typeof(DataNode.ValueNotInitialized))
                            {
                                throw;
                            }

                            // The rest gets logged
                            LoggingUtil.LogWarning(typeof(ConfigNodeUtil), "Got an unexpected exception trying to load '{0}' as a list:", key);
                            LoggingUtil.LogException(e);
                        }

                        // And continue on to load the standard way
                    }
                }

                // Create the list instance
                T list = (T)Activator.CreateInstance(typeof(T));

                // Create the generic methods
                MethodInfo parseValueMethod = methodParseSingleValue.MakeGenericMethod(typeof(T).GetGenericArguments());
                MethodInfo addMethod = typeof(T).GetMethod("Add");

                // Populate the list
                for (int i = 0; i < count; i++)
                {
                    string strVal = configNode.GetValue(key, i);
                    try
                    {
                        addMethod.Invoke(list, new object[] { parseValueMethod.Invoke(null, new object[] { key, strVal, allowExpression }) });
                    }
                    catch (TargetInvocationException tie)
                    {
                        Exception e = ExceptionUtil.UnwrapTargetInvokationException(tie);
                        if (e != null)
                        {
                            throw e;
                        }
                        throw;
                    }
                }

                return list;
            }
            else if (typeof(T).Name == "Nullable`1")
            {
                // Create the generic method
                MethodInfo parseValueMethod = typeof(ConfigNodeUtil).GetMethod("ParseValue",
                    BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(ConfigNode), typeof(string), typeof(bool) }, null);
                parseValueMethod = parseValueMethod.MakeGenericMethod(typeof(T).GetGenericArguments());

                // Call it
                try
                {
                    return (T)parseValueMethod.Invoke(null, new object[] { configNode, key, allowExpression });
                }
                catch (TargetInvocationException tie)
                {
                    Exception e = ExceptionUtil.UnwrapTargetInvokationException(tie);
                    if (e != null)
                    {
                        throw e;
                    }
                    throw;
                }
            }

            // Get string value, pass to parse single value function
            string stringValue = configNode.GetValue(key);
            return ParseSingleValue<T>(key, stringValue, allowExpression);
        }

        public static T ParseSingleValue<T>(string key, string stringValue, bool allowExpression)
        {
            ExpressionParser<T> parser;
            T value;

            // Handle nullable
            if (typeof(T).Name == "Nullable`1")
            {
                if (typeof(T).GetGenericArguments()[0].IsEnum)
                {
                    value = (T)Enum.Parse(typeof(T).GetGenericArguments()[0], stringValue, true);
                }
                else
                {
                    value = (T)Convert.ChangeType(stringValue, typeof(T).GetGenericArguments()[0]);
                }
            }
            else if (allowExpression && (parser = BaseParser.GetParser<T>()) != null)
            {
                if (initialLoad)
                {
                    value = parser.ParseExpression(key, stringValue, currentDataNode);
                }
                else
                {
                    value = parser.ExecuteExpression(key, stringValue, currentDataNode);
                }
            }
            // Enum parsing logic
            else if (typeof(T).IsEnum)
            {
                value = (T)Enum.Parse(typeof(T), stringValue, true);
            }
            else if (typeof(T) == typeof(AvailablePart))
            {
                value = (T)(object)ParsePartValue(stringValue);
            }
            else if (typeof(T) == typeof(ContractGroup))
            {
                if (!ContractGroup.contractGroups.ContainsKey(stringValue))
                {
                    throw new ArgumentException("No contract group with name '" + stringValue + "'");
                }
                value = (T)(object)ContractGroup.contractGroups[stringValue];
            }
            else if (typeof(T) == typeof(CelestialBody))
            {
                value = (T)(object)ParseCelestialBodyValue(stringValue);
            }
            else if (typeof(T) == typeof(PartResourceDefinition))
            {
                value = (T)(object)ParseResourceValue(stringValue);
            }
            else if (typeof(T) == typeof(Resource))
            {
                value = (T)(object)new Resource(ParseResourceValue(stringValue));
            }
            else if (typeof(T) == typeof(Agent))
            {
                value = (T)(object)ParseAgentValue(stringValue);
            }
            else if (typeof(T) == typeof(Duration))
            {
                value = (T)(object)new Duration(DurationUtil.ParseDuration(stringValue));
            }
            else if (typeof(T) == typeof(ProtoCrewMember))
            {
                value = (T)(object)ParseProtoCrewMemberValue(stringValue);
            }
            else if (typeof(T) == typeof(Kerbal))
            {
                value = (T)(object)new Kerbal(stringValue);
            }
            else if (typeof(T) == typeof(Guid))
            {
                value = (T)(object)new Guid(stringValue);
            }
            else if (typeof(T) == typeof(Vessel))
            {
                value = (T)(object)ParseVesselValue(stringValue);
            }
            else if (typeof(T) == typeof(VesselIdentifier))
            {
                value = (T)(object)new VesselIdentifier(stringValue);
            }
            else if (typeof(T) == typeof(Vector3))
            {
                string[] vals = stringValue.Split(new char[] { ',' });
                float x = (float)Convert.ChangeType(vals[0], typeof(float));
                float y = (float)Convert.ChangeType(vals[1], typeof(float));
                float z = (float)Convert.ChangeType(vals[2], typeof(float));
                value = (T)(object)new Vector3(x, y, z);
            }
            else if (typeof(T) == typeof(Vector3d))
            {
                string[] vals = stringValue.Split(new char[] { ',' });
                double x = (double)Convert.ChangeType(vals[0], typeof(double));
                double y = (double)Convert.ChangeType(vals[1], typeof(double));
                double z = (double)Convert.ChangeType(vals[2], typeof(double));
                value = (T)(object)new Vector3d(x, y, z);
            }
            else if (typeof(T) == typeof(Type))
            {
                value = (T)(object)ParseTypeValue(stringValue);
            }
            else if (typeof(T) == typeof(ScienceSubject))
            {
                value = (T)(object)(ResearchAndDevelopment.Instance != null ? ResearchAndDevelopment.GetSubjectByID(stringValue) : null);
            }
            else if (typeof(T) == typeof(ScienceExperiment))
            {
                value = (T)(object)(ResearchAndDevelopment.Instance != null ? ResearchAndDevelopment.GetExperiment(stringValue) : null);
            }
            else if (typeof(T) == typeof(Color))
            {
                if ((stringValue.Length != 7 && stringValue.Length != 9) || stringValue[0] != '#')
                {
                    throw new ArgumentException("Invalid color code '" + stringValue + "': Must be # followed by 6 or 8 hex digits (ARGB or RGB).");
                }
                stringValue = stringValue.Replace("#", "");
                int a  = 255;
                if (stringValue.Length == 8)
                {
                    a = byte.Parse(stringValue.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    stringValue = stringValue.Substring(2, 6);
                }
                int r = byte.Parse(stringValue.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                int g = byte.Parse(stringValue.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                int b = byte.Parse(stringValue.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

                value = (T)(object)(new Color(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f));
            }
            else if (typeof(T) == typeof(Biome))
            {
                string[] biomeData = stringValue.Split(new char[] {';'});
                CelestialBody cb = ParseCelestialBodyValue(biomeData[0]);
                value = (T)(object)(new Biome(cb, biomeData[1]));
            }
            else if (typeof(T) == typeof(LaunchSite))
            {
                value = (T)(object)ParseLaunchSiteValue(stringValue);
            }
            // Do newline conversions
            else if (typeof(T) == typeof(string))
            {
                value = (T)(object)stringValue.Replace("&br;", "\n").Replace("\\n", "\n");
            }
            // Try a basic type
            else
            {
                value = (T)Convert.ChangeType(stringValue, typeof(T));
            }

            return value;
        }

        /// <summary>
        /// Parses a value from a child config node.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="configNode">The ConfigNode to read from</param>
        /// <param name="key">The key to examine.</param>
        /// <param name="allowExpression">Whether the read value can be an expression.</param>
        /// <returns>The parsed value</returns>
        public static T ParseNode<T>(ConfigNode configNode, string key, bool allowExpression = false)
        {
            T value;

            if (typeof(T) == typeof(Orbit))
            {
                // Get the orbit node
                ConfigNode orbitNode = configNode.GetNode(key);

                // Get our child values
                DataNode oldNode = currentDataNode;
                try
                {
                    currentDataNode = oldNode.GetChild(key);
                    if (currentDataNode == null)
                    {
                        currentDataNode = new DataNode(key, oldNode, oldNode.Factory);
                    }

                    foreach (string orbitKey in new string[] { "SMA", "ECC", "INC", "LPE", "LAN", "MNA", "EPH", "REF" })
                    {
                        object orbitVal;
                        if (orbitKey == "REF")
                        {
                            ParseValue<int>(orbitNode, orbitKey, x => orbitVal = x, oldNode.Factory, 1);
                        }
                        else
                        {
                            ParseValue<double>(orbitNode, orbitKey, x => orbitVal = x, oldNode.Factory, 0.0);
                        }
                    }
                }
                finally
                {
                    currentDataNode = oldNode;
                }

                // Get the orbit parser
                ExpressionParser<T> parser = BaseParser.GetParser<T>();
                if (parser == null)
                {
                    throw new Exception("Couldn't instantiate orbit parser!");
                }

                // Parse the special expression
                string expression = "CreateOrbit([@" + key + "/SMA, @" + key + "/ECC, @" + key +
                    "/INC, @" + key + "/LPE, @" + key + "/LAN, @" + key + "/MNA, @" + key +
                    "/EPH ], @" + key + "/REF)";
                if (initialLoad)
                {
                    value = parser.ParseExpression(key, expression, currentDataNode);
                }
                else
                {
                    value = parser.ExecuteExpression(key, expression, currentDataNode);
                }
            }
            else
            {
                throw new Exception("Unhandled type for child node parsing: " + typeof(T));
            }

            return value;
        }


        /// <summary>
        /// Attempts to parse a value from the config node.  Returns a default value if not found.
        /// </summary>
        /// <typeparam name="T">The type of value to convert to.</typeparam>
        /// <param name="configNode">The ConfigNode to read from.</param>
        /// <param name="key">The key to examine.</param>
        /// <param name="defaultValue">The default value to return.</param>
        /// <returns>The parsed value (or default value if not found)</returns>
        public static T ParseValue<T>(ConfigNode configNode, string key, T defaultValue)
        {
            if (configNode.HasValue(key) || configNode.HasNode(key))
            {
                return ParseValue<T>(configNode, key);
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Attempts to parse a value from the config node.  Returns a default value if not found.
        /// </summary>
        /// <typeparam name="T">The type of value to convert to.</typeparam>
        /// <param name="configNode">The ConfigNode to read from.</param>
        /// <param name="key">The key to examine.</param>
        /// <param name="setter">Function used to set the output value</param>
        /// <param name="obj">Factory object for error messages.</param>
        /// <returns>The parsed value (or default value if not found)</returns>
        public static bool ParseValue<T>(ConfigNode configNode, string key, Action<T> setter, IContractConfiguratorFactory obj)
        {
            // Check for required value
            if (!configNode.HasValue(key) && !configNode.HasNode(key))
            {
                LoggingUtil.LogError(obj, "{0}: Missing required value '{1}'.", obj.ErrorPrefix(configNode), key);
                return false;
            }

            return ParseValue<T>(configNode, key, setter, obj, default(T), x => true);
        }

        /// <summary>
        /// Attempts to parse a value from the config node.  Returns a default value if not found.
        /// </summary>
        /// <typeparam name="T">The type of value to convert to.</typeparam>
        /// <param name="configNode">The ConfigNode to read from.</param>
        /// <param name="key">The key to examine.</param>
        /// <param name="setter">Function used to set the output value</param>
        /// <param name="obj">Factory object for error messages.</param>
        /// <param name="defaultValue">Default value to use if there is no key in the config node</param>
        /// <returns>The parsed value (or default value if not found)</returns>
        public static bool ParseValue<T>(ConfigNode configNode, string key, Action<T> setter, IContractConfiguratorFactory obj, T defaultValue)
        {
            return ParseValue<T>(configNode, key, setter, obj, defaultValue, x => true);
        }

        /// <summary>
        /// Attempts to parse a value from the config node.  Validates return values using the
        /// given function.
        /// </summary>
        /// <typeparam name="T">The type of value to convert to.</typeparam>
        /// <param name="configNode">The ConfigNode to read from.</param>
        /// <param name="key">The key to examine.</param>
        /// <param name="setter">Function used to set the output value</param>
        /// <param name="obj">Factory object for error messages.</param>
        /// <param name="validation">Validation function to run against the returned value</param>
        /// <returns>The parsed value (or default value if not found)</returns>
        public static bool ParseValue<T>(ConfigNode configNode, string key, Action<T> setter, IContractConfiguratorFactory obj, Func<T, bool> validation)
        {
            // Check for required value
            if (!configNode.HasValue(key) && !configNode.HasNode(key))
            {
                LoggingUtil.LogError(obj, "{0}: Missing required value '{1}'.", obj.ErrorPrefix(configNode), key);
                return false;
            }

            return ParseValue<T>(configNode, key, setter, obj, default(T), validation);
        }

        /// <summary>
        /// Attempts to parse a value from the config node.  Returns a default value if not found.
        /// Validates return values using the given function.
        /// </summary>
        /// <typeparam name="T">The type of value to convert to.</typeparam>
        /// <param name="configNode">The ConfigNode to read from.</param>
        /// <param name="key">The key to examine.</param>
        /// <param name="setter">Function used to set the output value</param>
        /// <param name="obj">Factory object for error messages.</param>
        /// <param name="defaultValue">Default value to use if there is no key in the config node</param>
        /// <param name="validation">Validation function to run against the returned value</param>
        /// <returns>The parsed value (or default value if not found)</returns>
        public static bool ParseValue<T>(ConfigNode configNode, string key, Action<T> setter, IContractConfiguratorFactory obj, T defaultValue, Func<T, bool> validation)
        {
            // Initialize the data type of the expression
            if (currentDataNode != null && !currentDataNode.IsInitialized(key))
            {
                currentDataNode.BlankInit(key, typeof(T));
            }

            bool valid = true;
            T value = defaultValue;
            if (configNode.HasValue(key) || configNode.HasNode(key))
            {
                try
                {
                    // Check whether there's a value
                    if (configNode.HasValue(key) && string.IsNullOrEmpty(configNode.GetValue(key)))
                    {
                        LoggingUtil.LogError(obj, "{0}: Required value '{1}' is empty.", obj.ErrorPrefix(configNode), key);
                        valid = false;
                    }
                    else
                    {
                        // Load value
                        value = ParseValue<T>(configNode, key, true);
                    }

                    // If value was non-null, run validation
                    if (value != null && (typeof(T) != typeof(string) || ((string)(object)value) != ""))
                    {
                        try
                        {
                            valid = validation.Invoke(value);
                            if (!valid)
                            {
                                // In general, the validation function should throw an exception and give a much better message
                                LoggingUtil.LogError(obj, "{0}: A validation error occured while loading the key '{1}' with value '{2}'.", obj.ErrorPrefix(configNode), key, value);
                            }
                        }
                        catch (Exception e)
                        {
                            if (e is DataNode.ValueNotInitialized)
                            {
                                throw;
                            }

                            LoggingUtil.LogError(obj, "{0}: A validation error occured while loading the key '{1}' with value '{2}'.", obj.ErrorPrefix(configNode), key, value);
                            LoggingUtil.LogException(e);
                            valid = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e.GetType() == typeof(DataNode.ValueNotInitialized))
                    {
                        string dependency = ((DataNode.ValueNotInitialized)e).key;
                        string path = currentDataNode.Path() + key;

                        LoggingUtil.LogVerbose(typeof(ConfigNodeUtil), "Trying to load {0}, but {1} is uninitialized.", path, dependency);

                        // Defer loading this value
                        DeferredLoadObject<T> loadObj = null;
                        if (!deferredLoads.ContainsKey(path) || deferredLoads[path].GetType().GetGenericArguments().First() != typeof(T))
                        {
                            deferredLoads[path] = new DeferredLoadObject<T>(configNode, key, setter, obj, validation, currentDataNode);
                        }
                        loadObj = (DeferredLoadObject<T>)deferredLoads[path];

                        // New dependency - try again
                        if (!loadObj.dependencies.Contains(dependency))
                        {
                            LoggingUtil.LogVerbose(typeof(ConfigNodeUtil), "    New dependency, will re-attempt to load later.");
                            loadObj.dependencies.Add(dependency);
                            return true;
                        }
                    }

                    LoggingUtil.LogError(obj, "{0}: Error parsing {1}", obj.ErrorPrefix(configNode), key);

                    // Return immediately on deferred load error
                    if (e.GetType() == typeof(DataNode.ValueNotInitialized))
                    {
                        DataNode.ValueNotInitialized vni = e as DataNode.ValueNotInitialized;
                        LoggingUtil.LogException(new Exception(StringBuilderCache.Format("Unknown identifier '@{0}'.", vni.key)));
                        return false;
                    }
                    LoggingUtil.LogException(e);

                    valid = false;
                }
                finally
                {
                    AddFoundKey(configNode, key);
                }
            }

            // Store the value
            if (currentDataNode != null)
            {
                currentDataNode[key] = value;

                if (!currentDataNode.IsDeterministic(key) && initialLoad)
                {
                    currentDataNode.DeferredLoads.Add(new DeferredLoadObject<T>(configNode, key, setter, obj, validation, currentDataNode));
                }
            }

            // Invoke the setter function
            if (valid)
            {
                setter.Invoke(value);
            }

            return valid;
        }

        /// <summary>
        /// Gets the child node of the given name.
        /// </summary>
        /// <param name="configNode">Config node to get a child from.</param>
        /// <param name="name">Name of the child node to fetch.</param>
        /// <returns>The child node.</returns>
        public static ConfigNode GetChildNode(ConfigNode configNode, string name)
        {
            try
            {
                return configNode.GetNode(name);
            }
            finally
            {
                AddFoundKey(configNode, name);
            }
        }

        /// <summary>
        /// Gets all child nodes of the given node.
        /// </summary>
        /// <param name="configNode">Config node to get children for.</param>
        /// <returns>The child nodes.</returns>
        public static ConfigNode[] GetChildNodes(ConfigNode configNode)
        {
            ConfigNode[] nodes = configNode.GetNodes();
            foreach (ConfigNode child in nodes)
            {
                AddFoundKey(configNode, child.name);
            }
            return nodes;
        }

        /// <summary>
        /// Gets all child nodes of the given name.
        /// </summary>
        /// <param name="configNode">Config node to get a child from.</param>
        /// <param name="name">Name of the child node to fetch.</param>
        /// <returns>The child nodes.</returns>
        public static ConfigNode[] GetChildNodes(ConfigNode configNode, string name)
        {
            try
            {
                return configNode.GetNodes(name);
            }
            finally
            {
                AddFoundKey(configNode, name);
            }
        }

        /// <summary>
        /// Ensures the given config node has at least one of the given values.
        /// </summary>
        /// <param name="configNode"></param>
        /// <param name="values"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool AtLeastOne(ConfigNode configNode, string[] values, IContractConfiguratorFactory obj)
        {
            string output = "";
            foreach (string value in values)
            {
                if (configNode.HasValue(value))
                {
                    return true;
                }

                if (value == values.First())
                {
                    output = value;
                }
                else if (value == values.Last())
                {
                    output += " or " + value;
                }
                else
                {
                    output += ", " + value;
                }
            }

            if (values.Count() == 2)
            {
                LoggingUtil.LogError(obj, "{0}: Either {1} is required.", obj.ErrorPrefix(configNode), output);
            }
            else 
            {
                LoggingUtil.LogError(obj, "{0}: One of {1} is required.", obj.ErrorPrefix(configNode), output);
            }
            return false;
        }

        /// <summary>
        /// Ensures the given config node has at exactly  one of the given values.
        /// </summary>
        /// <param name="configNode"></param>
        /// <param name="values"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool OnlyOne(ConfigNode configNode, string[] values, IContractConfiguratorFactory obj)
        {
            int count = 0;
            string output = "";
            foreach (string value in values)
            {
                if (configNode.HasValue(value))
                {
                    count++;
                }

                if (value == values.First())
                {
                    output = value;
                }
                else if (value == values.Last())
                {
                    output += " or " + value;
                }
                else
                {
                    output += ", " + value;
                }
            }

            if (count != 1)
            {
                LoggingUtil.LogError(obj, "{0}: Exactly one of the following types is allowed: {1}", obj.ErrorPrefix(configNode), output);
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Ensures that the config node does not have items from the two mutually exclusive groups.
        /// </summary>
        /// <param name="configNode">The configNode to verify.</param>
        /// <param name="group1">The first group of keys.</param>
        /// <param name="group2">The second group of keys</param>
        /// <param name="obj">IContractConfiguratorFactory for logging</param>
        /// <returns>Whether the condition is satisfied</returns>
        public static bool MutuallyExclusive(ConfigNode configNode, string[] group1, string[] group2, IContractConfiguratorFactory obj)
        {
            string group1String = "";
            string group2String = "";
            bool group1Value = false;
            bool group2Value = false;
            foreach (string value in group1)
            {
                if (configNode.HasValue(value))
                {
                    group1Value = true;
                }

                if (value == group1.First())
                {
                    group1String = value;
                }
                else
                {
                    group1String += ", " + value;
                }
            }
            foreach (string value in group2)
            {
                if (configNode.HasValue(value))
                {
                    group2Value = true;
                }

                if (value == group2.First())
                {
                    group2String = value;
                }
                else
                {
                    group2String += ", " + value;
                }
            }

            if (group1Value && group2Value)
            {
                LoggingUtil.LogError(obj, "{0}: The values {1} and {2} are mutually exclusive.", obj.ErrorPrefix(configNode), group1String, group2String);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Re-executes all non-deterministic values for the given node, providing new values.
        /// </summary>
        /// <param name="node">The node that should be re-executed</param>
        /// <param name="startWith">The node to use as a root, only nodes under this node will be refreshed</param>
        /// <returns>True if it was successful, false otherwise</returns>
        public static bool UpdateNonDeterministicValues(DataNode node, DataNode startWith = null)
        {
            foreach (string val in UpdateNonDeterministicValuesIterator(node, startWith))
            {
                if (val == null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Re-executes all non-deterministic values for the given node, providing new values.
        /// </summary>
        /// <param name="node">The node that should be re-executed</param>
        /// <returns>True if it was successful, false otherwise</returns>
        public static IEnumerable<string> UpdateNonDeterministicValuesIterator(DataNode node, DataNode startWith = null)
        {
            if (node == null)
            {
                yield break;
            }

            try
            {
                // Execute each deferred load
                foreach (DeferredLoadBase loadObj in node.DeferredLoads.Where(dl => startWith == null || dl.dataNode.IsChildOf(startWith)))
                {
                    initialLoad = false;
                    LoggingUtil.LogVerbose(typeof(ConfigNodeUtil), "Doing non-deterministic load for key '{0}'", loadObj.key);

                    MethodInfo method = methodExecuteLoad.MakeGenericMethod(loadObj.GetType().GetGenericArguments());
                    bool valid = (bool)method.Invoke(null, new object[] { loadObj });

                    initialLoad = true;

                    if (!valid)
                    {
                        yield return null;
                        yield break;
                    }
                    else
                    {
                        initialLoad = true;
                        yield return loadObj.key;
                    }
                }
            }
            finally
            {
                initialLoad = true;
            }
        }

        public static CelestialBody ParseCelestialBodyValue(string celestialName)
        {
            CelestialBody result = FlightGlobals.Bodies.Where(cb => cb.name == celestialName || string.Equals(cb.CleanDisplayName(), celestialName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (result == null)
            {
                throw new ArgumentException("'" + celestialName + "' is not a valid CelestialBody.");
            }
            return result;
        }

        private static AvailablePart ParsePartValue(string partName)
        {
            // Underscores in part names get replaced with spaces.  Nobody knows why.
            partName = partName.Replace('_', '.');

            // Get the part
            AvailablePart part = PartLoader.getPartInfoByName(partName);
            if (part == null)
            {
                throw new ArgumentException("'" + partName + "' is not a valid Part.");
            }

            return part;
        }

        private static PartResourceDefinition ParseResourceValue(string name)
        {
            var enumerator = PartResourceLibrary.Instance.resourceDefinitions.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.name == name)
                    {
                        return enumerator.Current;
                    }
                }
            }
            finally
            {
                enumerator.Dispose();
            }
            throw new ArgumentException("'" + name + "' is not a valid resource.");
        }

        private static Agent ParseAgentValue(string name)
        {
            Agent agent = AgentList.Instance.GetAgent(name);
            if (agent == null)
            {
                throw new ArgumentException("'" + name + "' is not a valid agent.");
            }

            return agent;
        }

        private static Vessel ParseVesselValue(string name)
        {
            Guid id = new Guid(name);
            return FlightGlobals.Vessels.Find(v => v != null && v.id == id);
        }

        public static LaunchSite ParseLaunchSiteValue(string siteName)
        {
            LaunchSite result = PSystemSetup.Instance.LaunchSites.Where(ls => ls.name == siteName).FirstOrDefault();
            if (result == null)
            {
                throw new ArgumentException("'" + siteName + "' is not a valid LaunchSite.");
            }
            return result;
        }

        public static Type ParseTypeValue(string name)
        {
            if (name.StartsWith("List<") && name.EndsWith(">"))
            {
                string innerType = name.Substring("List<".Length, name.Length - "List<>".Length);

                Type listType = typeof(List<>);
                return listType.MakeGenericType(ParseTypeValue(innerType));
            }
            else if (name.Contains('.'))
            {
                return Type.GetType(name);
            }
            else
            {
                if (typeMap.ContainsKey(name))
                {
                    return typeMap[name];
                }

                // Get all assemblies, but look at the ContractConfigurator ones first
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().
                    OrderBy(a => a.FullName.Contains("ContractConfigurator") ? 0 :
                        a.FullName.Contains("Assembly-CSharp") ? 1 : 2))
                {
                    try
                    {
                        Type type = assembly.GetTypes().Where(t => t.Name == name).OrderBy(t => t.FullName.Length).FirstOrDefault();
                        if (type != null)
                        {
                            // Cache it
                            typeMap[name] = type;
                            return type;
                        }
                    }
                    catch
                    {
                        // Ignore exception, as assembly type errors gets logged elsewhere
                    }
                }

                throw new ArgumentException("'" + name + "' is not a valid type.");
            }
        }

        private static ProtoCrewMember ParseProtoCrewMemberValue(string name)
        {
            foreach (ProtoCrewMember pcm in HighLogic.CurrentGame.CrewRoster.AllKerbals())
            {
                if (pcm.name == name)
                {
                    return pcm;
                }
            }

            return null;
        }

        private static void AddFoundKey(ConfigNode configNode, string key)
        {
            // Initialize the list
            if (!keysFound.ContainsKey(configNode))
            {
                keysFound[configNode] = new Dictionary<string,int>();
            }

            // Add the key
            keysFound[configNode][key] = 1;
        }

        /// <summary>
        /// Clears the cache of found keys.
        /// </summary>
        /// <param name="firstNode">Whether we are looking at the root node and should do additional clearing</param>
        public static void ClearCache(bool firstNode = false)
        {
            keysFound.Clear();
            storedValues.Clear();
            if (firstNode)
            {
                deferredLoads.Clear();
            }
        }

        /// <summary>
        /// Sets the currently active data node for expressions.
        /// </summary>
        /// <param name="dataNode">The DataNode to use as the current data node.</param>
        public static void SetCurrentDataNode(DataNode dataNode)
        {
            currentDataNode = dataNode;
        }

        /// <summary>
        /// Execute all deferred loads.
        /// </summary>
        /// <returns>Whether we were successful</returns>
        public static bool ExecuteDeferredLoads()
        {
            bool valid = true;

            Dictionary<string, List<string>> dependencies = new Dictionary<string, List<string>>();

            while (deferredLoads.Any())
            {
                string key = null;
                int count = 0;
                object loadObj = null;

                // Rebuild the dependency tree
                dependencies.Clear();
                foreach (KeyValuePair<string, DeferredLoadBase> pair in deferredLoads)
                {
                    MethodInfo method = methodGetDependencies.MakeGenericMethod(pair.Value.GetType().GetGenericArguments());
                    IEnumerable<string> localDependencies = (IEnumerable<string>)method.Invoke(null, new object[] { pair.Value });
                    dependencies[pair.Key] = new List<string>();

                    foreach (string dep in localDependencies)
                    {
                        // Only add dependencies that exist in the list
                        if (deferredLoads.ContainsKey(dep))
                        {
                            dependencies[pair.Key].Add(dep);
                        }
                    }

                    if (dependencies[pair.Key].Count() == 0)
                    {
                        count = localDependencies.Count();
                        key = pair.Key;
                        loadObj = pair.Value;
                        break;
                    }
                }

                // Didn't find anything valid.  The rest are circular dependencies
                if (loadObj == null)
                {
                    valid = false;
                    foreach (KeyValuePair<string, DeferredLoadBase> pair in deferredLoads)
                    {
                        MethodInfo method = methodLogCircularDependencyError.MakeGenericMethod(pair.Value.GetType().GetGenericArguments());
                        method.Invoke(null, new object[] { pair.Value });
                    }
                    deferredLoads.Clear();
                }
                // Found something we can execute
                else
                {
                    // Try parsing it
                    MethodInfo method = methodExecuteLoad.MakeGenericMethod(loadObj.GetType().GetGenericArguments());
                    valid &= (bool)method.Invoke(null, new object[] { loadObj });

                    // If a dependency was not added, then remove from the list
                    method = methodGetDependencies.MakeGenericMethod(loadObj.GetType().GetGenericArguments());
                    if (count == ((IEnumerable<string>)method.Invoke(null, new object[] { loadObj })).Count())
                    {
                        deferredLoads.Remove(key);
                    }
                }
            }

            return valid;
        }

        /// <summary>
        /// Performs validation to check if the given config node has values that were not expected.
        /// </summary>
        /// <param name="configNode">The ConfigNode to check.</param>
        /// <param name="obj">IContractConfiguratorFactory object for error reporting</param>
        /// <returns>Always true, but logs a warning if unexpected keys were found</returns>
        public static bool ValidateUnexpectedValues(ConfigNode configNode, IContractConfiguratorFactory obj)
        {
            bool valid = true;

            if (!keysFound.ContainsKey(configNode))
            {
                obj.hasWarnings = true;
                LoggingUtil.LogWarning(obj.GetType(), "{0}: did not attempt to load values for ConfigNode!", obj.ErrorPrefix());
                return false;
            }

            Dictionary<string, int> found = keysFound[configNode];
            foreach (ConfigNode.Value pair in configNode.values)
            {
                if (!found.ContainsKey(pair.name))
                {
                    obj.hasWarnings = true;
                    LoggingUtil.LogWarning(obj.GetType(), "{0}: unexpected attribute '{1}' found, ignored.", obj.ErrorPrefix(), pair.name);
                }
            }

            foreach (ConfigNode child in configNode.nodes)
            {
                // Exceptions
                if (child.name == "PARAMETER" && (obj is ContractType || obj is ParameterFactory) ||
                    child.name == "REQUIREMENT" && (obj is ContractType || obj is ParameterFactory || obj is ContractRequirement) ||
                    child.name == "BEHAVIOUR" && (obj is ContractType) ||
                    child.name == "ORBIT" && (obj is Behaviour.OrbitGeneratorFactory || obj is Behaviour.SpawnVesselFactory || obj is Behaviour.SpawnKerbalFactory))
                {
                    continue;
                }

                if (!found.ContainsKey(child.name))
                {
                    obj.hasWarnings = true;
                    LoggingUtil.LogWarning(obj.GetType(), "{0}: unexpected child node '{1}' found, ignored.", obj.ErrorPrefix(), child.name);
                }
            }


            return valid;
        }
    }
}
