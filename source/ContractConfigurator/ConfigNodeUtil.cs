using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using Contracts.Agents;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    public class ConfigNodeUtil
    {
        private static Dictionary<ConfigNode, Dictionary<string, int>> keysFound = new Dictionary<ConfigNode,Dictionary<string,int>>();

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
        /// <returns>The parsed value</returns>
        public static T ParseValue<T>(ConfigNode configNode, string key)
        {
            // Check for requried value
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
                    new Type[] { typeof(string), typeof(string) }, null);
                parseValueMethod = parseValueMethod.MakeGenericMethod(typeof(T).GetGenericArguments());
                MethodInfo addMethod = typeof(T).GetMethod("Add");

                // Populate the list
                for (int i = 0; i < count; i++)
                {
                    string strVal = configNode.GetValue(key, i);
                    addMethod.Invoke(list, new object[] { parseValueMethod.Invoke(null, new object[] { key, strVal }) });
                }

                return list;
            }
            else if (typeof(T) == typeof(CelestialBody))
            {
                return (T)(object)ParseCelestialBodyValue(configNode, key);
            }
            else if (typeof(T) == typeof(AvailablePart))
            {
                return (T)(object)ParsePartValue(configNode, key);
            }
            else if (typeof(T) == typeof(PartResourceDefinition))
            {
                return (T)(object)ParseResourceValue(configNode, key);
            }
            else if (typeof(T) == typeof(Agent))
            {
                return (T)(object)ParseAgentValue(configNode, key);
            }
            else if (typeof(T) == typeof(Guid))
            {
                return (T)(object)new Guid(configNode.GetValue(key));
            }
            else if (typeof(T).Name == "Nullable`1")
            {
                // Let enum fall through to the ParseSingleValue method
                if (!typeof(T).GetGenericArguments()[0].IsEnum)
                {
                    // Create the generic method
                    MethodInfo parseValueMethod = typeof(ConfigNodeUtil).GetMethod("ParseValue",
                        BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(ConfigNode), typeof(string) }, null);
                    parseValueMethod = parseValueMethod.MakeGenericMethod(typeof(T).GetGenericArguments());

                    // Call it
                    return (T)parseValueMethod.Invoke(null, new object[] { configNode, key });
                }
            }

            // Get string value, pass to parse single value function
            string stringValue = configNode.GetValue(key);
            return ParseSingleValue<T>(key, stringValue);
        }

        private static T ParseSingleValue<T>(string key, string stringValue)
        {
            // Enum parsing logic
            if (typeof(T).IsEnum)
            {
                return (T)Enum.Parse(typeof(T), stringValue);
            }

            // Handle nullable
            if (typeof(T).Name == "Nullable`1")
            {
                if (typeof(T).GetGenericArguments()[0].IsEnum)
                {
                    return (T)Enum.Parse(typeof(T).GetGenericArguments()[0], stringValue);
                }
                else
                {
                    return (T)Convert.ChangeType(stringValue, typeof(T).GetGenericArguments()[0]);
                }
            }

            // Do newline conversions
            if (typeof(T) == typeof(string))
            {
                stringValue = stringValue.Replace("\\n", "\n");
            }

            // Try a basic type
            return (T)Convert.ChangeType(stringValue, typeof(T));
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

        /*
         * Attempts to parse a value from the config node.
         */
        public static bool ParseValue<T>(ConfigNode configNode, string key, ref T value, IContractConfiguratorFactory obj)
        {
            try
            {
                value = ParseValue<T>(configNode, key);
                return value != null;
            }
            catch (Exception e)
            {
                LoggingUtil.LogError(obj, obj.ErrorPrefix(configNode) + ": Error parsing " + key);
                Debug.LogException(e);
                return false;
            }
            finally
            {
                AddFoundKey(configNode, key);
            }
        }

        /*
         * Attempts to parse a value from the config node.  Validates return values using the
         * given function.
         */
        public static bool ParseValue<T>(ConfigNode configNode, string key, ref T value, IContractConfiguratorFactory obj, Func<T, bool> validation)
        {
            if (ParseValue<T>(configNode, key, ref value, obj))
            {
                try
                {
                    if (!validation.Invoke(value))
                    {
                        // In general, the validation function should throw an exception and give a much better message
                        LoggingUtil.LogError(obj, obj.ErrorPrefix(configNode) + ": The value supplied for " + key + " (" + value + ") is invalid.");
                        return false;
                    }
                }
                catch (Exception e)
                {
                    LoggingUtil.LogError(obj, obj.ErrorPrefix(configNode) + ": The value supplied for " + key + " (" + value + ") is invalid.");
                    Debug.LogException(e);
                    return false;
                }
                return true;
            }
            return false;
        }

        /*
         * Attempts to parse a value from the config node.  Returns a default value if not found.
         */
        public static bool ParseValue<T>(ConfigNode configNode, string key, ref T value, IContractConfiguratorFactory obj, T defaultValue)
        {
            if (configNode.HasValue(key))
            {
                return ParseValue<T>(configNode, key, ref value, obj);
            }
            else
            {
                value = defaultValue;
                return true;
            }
        }

        /*
         * Attempts to parse a value from the config node.  Returns a default value if not found.
         * Validates return values using the given function.
         */
        public static bool ParseValue<T>(ConfigNode configNode, string key, ref T value, IContractConfiguratorFactory obj, T defaultValue, Func<T, bool> validation)
        {
            if (ParseValue<T>(configNode, key, ref value, obj, defaultValue))
            {
                try
                {
                    if (!validation.Invoke(value))
                    {
                        // In general, the validation function should throw an exception and give a much better message
                        LoggingUtil.LogError(obj, obj.ErrorPrefix(configNode) + ": The value supplied for " + key + " (" + value + ") is invalid.");
                        return false;
                    }
                }
                catch (Exception e)
                {
                    LoggingUtil.LogError(obj, obj.ErrorPrefix(configNode) + ": The value supplied for " + key + " (" + value + ") is invalid.");
                    Debug.LogException(e);
                    return false;
                }
                return true;
            }
            return false;
        }

        /*
         * Ensures the given config node has at least one of the given values.
         */
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

        /*
         * Parses the CelestialBody from the given ConfigNode and key.
         */
        protected static CelestialBody ParseCelestialBodyValue(ConfigNode configNode, string key)
        {
            string celestialName = configNode.GetValue(key);

            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body.name.Equals(celestialName))
                {
                    return body;
                }
            }

            throw new ArgumentException("'" + celestialName + "' is not a valid CelestialBody.");
        }

        /*
         * Parses the AvailablePart from the given ConfigNode and key.
         */
        protected static AvailablePart ParsePartValue(ConfigNode configNode, string key)
        {
            // Underscores in part names get replaced with spaces.  Nobody knows why.
            string partName = configNode.GetValue(key);
            partName = partName.Replace('_', '.');

            // Get the part
            AvailablePart part = PartLoader.getPartInfoByName(partName);
            if (part == null)
            {
                throw new ArgumentException("'" + partName + "' is not a valid Part.");
            }

            return part;
        }

        /*
         * Parses the PartResource from the given ConfigNode and key.
         */
        protected static PartResourceDefinition ParseResourceValue(ConfigNode configNode, string key)
        {
            string name = configNode.GetValue(key);
            PartResourceDefinition resource = PartResourceLibrary.Instance.resourceDefinitions.Where(prd => prd.name == name).First();
            if (resource == null)
            {
                throw new ArgumentException("'" + name + "' is not a valid resource.");
            }

            return resource;
        }

        /*
         * Parses the Agent from the given ConfigNode and key.
         */
        protected static Agent ParseAgentValue(ConfigNode configNode, string key)
        {
            string name = configNode.GetValue(key);
            Agent agent = AgentList.Instance.GetAgent(configNode.GetValue(key));
            if (agent == null)
            {
                throw new ArgumentException("'" + name + "' is not a valid agent.");
            }

            return agent;
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
        public static void ClearFoundCache()
        {
            keysFound = new Dictionary<ConfigNode, Dictionary<string, int>>();
        }

        /// <summary>
        /// Performs validation to check if the given config node has values that were not expected.
        /// </summary>
        /// <param name="configNode">The ConfigNode to check.</param>
        /// <param name="obj">IContractConfiguratorFactory object for error reporting</param>
        /// <returns>Always true, but logs a warning if unexpected keys were found</returns>
        public static bool ValidateUnexpectedValues(ConfigNode configNode, IContractConfiguratorFactory obj)
        {
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

            return true;
        }
    }
}
