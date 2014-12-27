using System;
using System.Collections.Generic;
using System.Linq;
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
        /*
         * Checks whether the mandatory field exists, and if not logs and error.  Returns true
         * only if the validation succeeded.
         */
        [Obsolete("Use ParseValue<T> which will throw an error if the expected value is not there.")]
        public static bool ValidateMandatoryField(ConfigNode configNode, string field, IContractConfiguratorFactory obj)
        {
            if (!configNode.HasValue(field))
            {
                LoggingUtil.LogError(obj.GetType(), obj.ErrorPrefix() + ": Missing required value '" + field + "'.");
                return false;
            }

            return true;
        }

        /*
         * Checks whether the mandatory field exists, and if not logs and error.  Returns true
         * only if the validation succeeded.
         */
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

        /*
         * Attempts to parse a value from the config node.
         */
        public static T ParseValue<T>(ConfigNode configNode, string key)
        {
            // Check for requried value
            if (!configNode.HasValue(key))
            {
                throw new ArgumentException("Missing required value '" + key + "'.");
            }

            // Special cases
            if (typeof(T) == typeof(CelestialBody))
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
            
            // Get string value
            string stringValue = configNode.GetValue(key);

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

            // Try a basic type
            return (T)Convert.ChangeType(stringValue, typeof(T));
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
                LoggingUtil.LogError(obj, obj.ErrorPrefix(configNode) + ": Error parsing " + key + ": " + configNode.id + e.Message);
                LoggingUtil.LogDebug(obj, e.StackTrace);
                return false;
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
                    LoggingUtil.LogError(obj, obj.ErrorPrefix(configNode) + ": The value supplied for " + key + " (" + value + ") is invalid: " + e.Message);
                    LoggingUtil.LogDebug(obj, e.StackTrace);
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
                    LoggingUtil.LogError(obj, obj.ErrorPrefix(configNode) + ": The value supplied for " + key + " (" + value + ") is invalid: " + e.Message);
                    LoggingUtil.LogDebug(obj, e.StackTrace);
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

        [Obsolete("ParseCelestialBody has been replaced by ParseValue<CelestialBody>")]
        public static CelestialBody ParseCelestialBody(ConfigNode configNode, string key)
        {
            return ParseCelestialBodyValue(configNode, key);
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

        [Obsolete("ParsePart has been replaced by ParseValue<AvailablePart>")]
        public static AvailablePart ParsePart(ConfigNode configNode, string key)
        {
            return ParsePartValue(configNode, key);
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

        [Obsolete("ParseResource has been replaced by ParseValue<PartResourceDefinition>")]
        public static PartResourceDefinition ParseResource(ConfigNode configNode, string key)
        {
            return ParseResourceValue(configNode, key);
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
    }
}
