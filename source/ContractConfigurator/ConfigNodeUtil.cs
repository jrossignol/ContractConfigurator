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
                    Debug.LogError("ContractConfigurator: '" + celestialName + "' is not a valid CelestialBody.");
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
                part = PartLoader.getPartInfoByName(partName);
                if (part == null)
                {
                    Debug.LogError("ContractConfigurator: '" + partName + "' is not a valid Part.");
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
                Debug.LogError("ContractConfigurator: No PartModule class for '" + name + "'.");
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
                    Debug.LogError("ContractConfigurator: Unable to instantiate PartModule '" + name + "'.");
                }
            }

            return valid;
        }
    }
}
