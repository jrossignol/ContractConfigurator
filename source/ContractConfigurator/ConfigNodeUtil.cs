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
    }
}
