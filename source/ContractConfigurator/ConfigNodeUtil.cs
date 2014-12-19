using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    public class ConfigNodeUtil
    {
        /*
         * Checks whether the mandatory field exists, and if not logs and error.  Returns true
         * only if the validation succeeded.
         */
        public static bool ValidateMandatoryField(ConfigNode configNode, string field, IContractConfiguratorFactory obj)
        {
            if (!configNode.HasValue(field))
            {
                LoggingUtil.LogError(typeof(ConfigNodeUtil), obj.ErrorPrefix(configNode) +
                    ": missing required value '" + field + "'.");
                return false;
            }

            return true;
        }

        /*
         * Parses the CelestialBody from the given ConfigNode and key.
         */
        public static CelestialBody ParseCelestialBody(ConfigNode configNode, string key)
        {
            CelestialBody targetBody = null;
            if (configNode.HasValue(key))
            {
                string celestialName = configNode.GetValue(key);
                bool found = false;
                foreach (CelestialBody body in FlightGlobals.Bodies)
                {
                    if (body.name.Equals(celestialName))
                    {
                        found = true;
                        targetBody = body;
                        break;
                    }
                }

                if (!found)
                {
                    LoggingUtil.LogError(typeof(ConfigNodeUtil), celestialName + "' is not a valid CelestialBody.");
                }
            }

            return targetBody;
        }

        /*
         * Parses the AvailablePart from the given ConfigNode and key.
         */
        public static AvailablePart ParsePart(ConfigNode configNode, string key)
        {
            AvailablePart part = null;
            if (configNode.HasValue(key))
            {
                string partName = configNode.GetValue(key);

                // Underscores in part names get replaced with spaces.  Nobody knows why.
                partName = partName.Replace('_', '.');  
                part = PartLoader.getPartInfoByName(partName);

                if (part == null)
                {
                    LoggingUtil.LogError(typeof(ConfigNodeUtil), partName + "' is not a valid Part.");
                }
            }

            return part;
        }

        /*
         * Parses the PartModule from the given ConfigNode and key.  Returns true if valid
         */
        public static bool ValidatePartModule(string name)
        {
            bool valid = true;
            Type classType = AssemblyLoader.GetClassByName(typeof(PartModule), name);
            if (classType == null)
            {
                LoggingUtil.LogError(typeof(ConfigNodeUtil), "No PartModule class for '" + name + "'.");
                valid = false;
            }
            else
            {
                // One would think there's a better way than this to get a PartModule instance,
                // but this is the best I've come up with
                GameObject go = new GameObject();
                PartModule partModule = (PartModule)go.AddComponent(classType);
                if (partModule == null)
                {
                    valid = false;
                    LoggingUtil.LogError(typeof(ConfigNodeUtil), "Unable to instantiate PartModule '" + name + "'.");
                }
            }

            return valid;
        }

        /*
         * Parses the PartResource from the given ConfigNode and key.
         */
        public static PartResourceDefinition ParseResource(ConfigNode configNode, string key)
        {
            PartResourceDefinition resource = null;
            if (configNode.HasValue(key))
            {
                string name = configNode.GetValue(key);
                resource = PartResourceLibrary.Instance.resourceDefinitions.Where(prd => prd.name == name).First();
                if (resource == null)
                {
                    LoggingUtil.LogError(typeof(ConfigNodeUtil), name + "' is not a valid resource.");
                }
            }

            return resource;
        }
    }
}
