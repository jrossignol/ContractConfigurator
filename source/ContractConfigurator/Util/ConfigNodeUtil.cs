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
        public class DeferredLoadBase
        {
            public string key;
            public ConfigNode configNode;
            public IContractConfiguratorFactory obj;
            public List<string> dependencies = new List<string>();
        }

        public class DeferredLoadObject<T> : DeferredLoadBase
        {
            public Action<T> setter;
            public Func<T, bool> validation;
            public DataNode dataNode;

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

            public static void LogCicularDependencyError<T>(DeferredLoadObject<T> loadObj)
            {
                LoggingUtil.LogError(loadObj.obj, loadObj.obj.ErrorPrefix(loadObj.configNode) + ": Error parsing " + loadObj.key + ": " +
                    "Circular dependency detected while parsing an expression (possible culprit(s): " +
                    string.Join(", ", loadObj.dependencies.ToArray()) + ").");
            }
        }

        private static Dictionary<string, DeferredLoadBase> deferredLoads = new Dictionary<string, DeferredLoadBase>();

        private static Dictionary<ConfigNode, Dictionary<string, int>> keysFound = new Dictionary<ConfigNode, Dictionary<string, int>>();
        private static Dictionary<ConfigNode, ConfigNode> storedValues = new Dictionary<ConfigNode, ConfigNode>();
        private static DataNode currentDataNode;
        private static bool initialLoad = true;

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
                LoggingUtil.LogError(obj.GetType(), obj.ErrorPrefix() +
                    ": missing required child node '" + field + "'.");
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
                LoggingUtil.LogWarning(obj.GetType(), obj.ErrorPrefix() +
                    ": unexpected entry '" + field + "' found, ignored.");
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
                throw new ArgumentException("Missing required value '" + key + "'.");
            }

            // Special cases
            if (typeof(T).Name == "List`1")
            {
                // Create the list instance
                T list = (T)Activator.CreateInstance(typeof(T));
                int count = configNode.GetValues(key).Count();

                // Create the generic methods
                MethodInfo parseValueMethod = typeof(ConfigNodeUtil).GetMethod("ParseSingleValue",
                    BindingFlags.NonPublic | BindingFlags.Static, null,
                    new Type[] { typeof(string), typeof(string), typeof(bool)}, null);
                parseValueMethod = parseValueMethod.MakeGenericMethod(typeof(T).GetGenericArguments());
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
                // Let enum fall through to the ParseSingleValue method
                if (!typeof(T).GetGenericArguments()[0].IsEnum)
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
            }

            // Get string value, pass to parse single value function
            string stringValue = configNode.GetValue(key);
            return ParseSingleValue<T>(key, stringValue, allowExpression);
        }

        private static T ParseSingleValue<T>(string key, string stringValue, bool allowExpression)
        {
            ExpressionParser<T> parser = BaseParser.GetParser<T>();
            T value;

            // Enum parsing logic
            if (typeof(T).IsEnum)
            {
                value = (T)Enum.Parse(typeof(T), stringValue);
            }
            // Handle nullable
            else if (typeof(T).Name == "Nullable`1")
            {
                if (typeof(T).GetGenericArguments()[0].IsEnum)
                {
                    value = (T)Enum.Parse(typeof(T).GetGenericArguments()[0], stringValue);
                }
                else
                {
                    value = (T)Convert.ChangeType(stringValue, typeof(T).GetGenericArguments()[0]);
                }
            }
            else if (allowExpression && parser != null)
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
            else if (typeof(T) == typeof(Agent))
            {
                value = (T)(object)ParseAgentValue(stringValue);
            }
            else if (typeof(T) == typeof(ProtoCrewMember))
            {
                value = (T)(object)ParseProtoCrewMemberValue(stringValue);
            }
            else if (typeof(T) == typeof(Guid))
            {
                value = (T)(object)new Guid(stringValue);
            }
            else if (typeof(T) == typeof(Vessel))
            {
                value = (T)(object)ParseVesselValue(stringValue);
            }
            // Do newline conversions
            else if (typeof(T) == typeof(string))
            {
                value = (T)(object)stringValue.Replace("\\n", "\n");
            }
            // Try a basic type
            else
            {
                value = (T)Convert.ChangeType(stringValue, typeof(T));
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
            if (configNode.HasValue(key))
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
            if (!configNode.HasValue(key))
            {
                LoggingUtil.LogError(obj, obj.ErrorPrefix(configNode) + ": Missing required value '" + key + "'.");
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
            if (!configNode.HasValue(key))
            {
                LoggingUtil.LogError(obj, obj.ErrorPrefix(configNode) + ": Missing required value '" + key + "'.");
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
            if (configNode.HasValue(key))
            {
                try
                {
                    // Load value
                    value = ParseValue<T>(configNode, key, true);

                    // If value was non-null, run validation
                    if (value != null)
                    {
                        try
                        {
                            valid = validation.Invoke(value);
                            if (!valid)
                            {
                                // In general, the validation function should throw an exception and give a much better message
                                LoggingUtil.LogError(obj, obj.ErrorPrefix(configNode) + ": The value supplied for " + key + " (" + value + ") is invalid.");
                            }
                        }
                        catch (Exception e)
                        {
                            LoggingUtil.LogError(obj, obj.ErrorPrefix(configNode) + ": The value supplied for " + key + " (" + value + ") is invalid.");
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

                        // Defer loading this value
                        DeferredLoadObject<T> loadObj = null;
                        if (!deferredLoads.ContainsKey(path))
                        {
                            deferredLoads[path] = new DeferredLoadObject<T>(configNode, key, setter, obj, validation, currentDataNode);
                        }
                        loadObj = (DeferredLoadObject<T>)deferredLoads[path];

                        // New dependency - try again
                        if (!loadObj.dependencies.Contains(dependency))
                        {
                            loadObj.dependencies.Add(dependency);
                            return true;
                        }
                    }

                    LoggingUtil.LogError(obj, obj.ErrorPrefix(configNode) + ": Error parsing " + key);
                    LoggingUtil.LogException(e);

                    // Return immediately on deferred load error
                    if (e.GetType() == typeof(DataNode.ValueNotInitialized))
                    {
                        return false;
                    }

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
                LoggingUtil.LogVerbose(typeof(ConfigNodeUtil), "DataNode[" + currentDataNode.Name + "], storing " + key + " = " + value);
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
                LoggingUtil.LogError(obj, obj.ErrorPrefix(configNode) + ": Either " + output + " is required.");
            }
            else
            {
                LoggingUtil.LogError(obj, obj.ErrorPrefix(configNode) + ": One of " + output + " is required.");
            }
            return false;
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
                LoggingUtil.LogError(obj, obj.ErrorPrefix(configNode) + ": The values " + group1String + " and " + group2String + " are mutually exclusive.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Re-executes all non-deterministic values for the given node, providing new values.
        /// </summary>
        /// <param name="node">The node that should be re-executed</param>
        /// <returns>True if it was successful, false otherwise</returns>
        public static bool UpdateNonDeterministicValues(DataNode node)
        {
            if (node == null)
            {
                return true;
            }

            initialLoad = false;

            try
            {
                // Execute each deferred load
                MethodInfo executeMethod = typeof(DeferredLoadUtil).GetMethods().Where(m => m.Name == "ExecuteLoad").First();
                foreach (DeferredLoadBase loadObj in node.DeferredLoads)
                {
                    LoggingUtil.LogVerbose(typeof(ConfigNodeUtil), "Doing non-deterministic load for key '" + loadObj.key + "'");

                    MethodInfo method = executeMethod.MakeGenericMethod(loadObj.GetType().GetGenericArguments());
                    bool valid = (bool)method.Invoke(null, new object[] { loadObj });

                    if (!valid)
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                initialLoad = true;
            }
        }

        public static CelestialBody ParseCelestialBodyValue(string celestialName)
        {
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body.name.Equals(celestialName))
                {
                    return body;
                }
            }

            throw new ArgumentException("'" + celestialName + "' is not a valid CelestialBody.");
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
            PartResourceDefinition resource = PartResourceLibrary.Instance.resourceDefinitions.Where(prd => prd.name == name).First();
            if (resource == null)
            {
                throw new ArgumentException("'" + name + "' is not a valid resource.");
            }

            return resource;
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
            return FlightGlobals.Vessels.Find(v => v.id == id);
        }

        private static ProtoCrewMember ParseProtoCrewMemberValue(string name)
        {
            return HighLogic.CurrentGame.CrewRoster.AllKerbals().Where(pcm => pcm.name == name).FirstOrDefault();
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

            // Generic methods
            MethodInfo dependenciesMethod = typeof(DeferredLoadUtil).GetMethods().Where(m => m.Name == "GetDependencies").First();
            MethodInfo circularDependendencyMethod = typeof(DeferredLoadUtil).GetMethods().Where(m => m.Name == "LogCicularDependencyError").First();
            MethodInfo executeMethod = typeof(DeferredLoadUtil).GetMethods().Where(m => m.Name == "ExecuteLoad").First();

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
                    MethodInfo method = dependenciesMethod.MakeGenericMethod(pair.Value.GetType().GetGenericArguments());
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
                        MethodInfo method = circularDependendencyMethod.MakeGenericMethod(pair.Value.GetType().GetGenericArguments());
                        method.Invoke(null, new object[] { pair.Value });
                    }
                    deferredLoads.Clear();
                }
                // Found something we can execute
                else
                {
                    // Try parsing it
                    MethodInfo method = executeMethod.MakeGenericMethod(loadObj.GetType().GetGenericArguments());
                    valid &= (bool)method.Invoke(null, new object[] { loadObj });

                    // If a dependency was not added, then remove from the list
                    method = dependenciesMethod.MakeGenericMethod(loadObj.GetType().GetGenericArguments());
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
                LoggingUtil.LogWarning(obj.GetType(), obj.ErrorPrefix() +
                    ": did not attempt to load values for ConfigNode!");
                return false;
            }

            Dictionary<string, int> found = keysFound[configNode];
            foreach (ConfigNode.Value pair in configNode.values)
            {
                if (!found.ContainsKey(pair.name))
                {
                    LoggingUtil.LogWarning(obj.GetType(), obj.ErrorPrefix() +
                        ": unexpected entry '" + pair.name + "' found, ignored.");
                }
            }

            return valid;
        }
    }
}
