using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using RemoteTech;

namespace ContractConfigurator.RemoteTech
{
    /// <summary>
    /// Parameter for checking whether the vessel has an antenna that meets the specified criteria.
    /// </summary>
    public class HasAntennaParameter : RemoteTechParameter
    {
        public enum AntennaType
        {
            Dish,
            Omni
        };

        protected string title { get; set; }
        protected int minCount { get; set; }
        protected int maxCount { get; set; }
        public CelestialBody targetBody = null;
        public bool activeVessel = false;
        public AntennaType? antennaType = null;
        public double minRange = 0.0;
        public double maxRange = double.MaxValue;

        public HasAntennaParameter()
            : this(1)
        {
        }

        public HasAntennaParameter(int minCount = 1, int maxCount = int.MaxValue, string title = null)
            : base()
        {
            this.minCount = minCount;
            this.maxCount = maxCount;
            this.title = title;
        }

        protected override string GetTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                output = (antennaType == null ? "Antenna" : antennaType.ToString()) + ":";

                bool needsComma = false;
                if (targetBody != null || activeVessel)
                {
                    output += needsComma ? "; " : " ";
                    output += "Target: ";
                    if (activeVessel)
                    {
                        output += "Active Vessel";
                    }
                    else
                    {
                        output += targetBody.name;
                    }
                    needsComma = true;
                }

                if (minRange != 0.0 || maxRange != double.MaxValue)
                {
                    output += needsComma ? "; " : " ";
                    output += "Range: ";
                    if (maxRange == double.MaxValue)
                    {
                        output += "At least " + RemoteTechAssistant.RangeString(minRange);
                    }
                    else if (minRange == 0)
                    {
                        output += "At most " + RemoteTechAssistant.RangeString(maxRange);
                    }
                    else
                    {
                        output += "Between " + RemoteTechAssistant.RangeString(minRange) + " and " + RemoteTechAssistant.RangeString(maxRange);
                    }
                    needsComma = true;
                }

                if (minCount != 0 && maxCount != int.MaxValue)
                {
                    output += needsComma ? ": " : " ";
                    if (maxCount == 0)
                    {
                        output += "None";
                    }
                    else if (maxCount == int.MaxValue)
                    {
                        output += "At least " + minCount;
                    }
                    else if (minCount == 0)
                    {
                        output += "At most " + maxCount;
                    }
                    else if (minCount == maxCount)
                    {
                        output += "Exactly " + minCount;
                    }
                    else
                    {
                        output += "Between " + minCount + " and " + maxCount;
                    }
                }
            }
            else
            {
                output = title;
            }
            return output;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            if (title != null)
            {
                node.AddValue("title", title);
            }
            node.AddValue("minRange", minRange);
            if (maxRange != double.MaxValue)
            {
                node.AddValue("maxRange", maxRange);
            }
            if (targetBody != null)
            {
                node.AddValue("targetBody", targetBody.name);
            }
            node.AddValue("activeVessel", activeVessel);
            node.AddValue("minCount", minCount);
            node.AddValue("maxCount", maxCount);
            if (antennaType != null)
            {
                node.AddValue("antennaType", antennaType);
            }
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = ConfigNodeUtil.ParseValue<string>(node, "title", (string)null);
            minRange = ConfigNodeUtil.ParseValue<double>(node, "minRange");
            maxRange = ConfigNodeUtil.ParseValue<double>(node, "maxRange", double.MaxValue);
            targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody", (CelestialBody)null);
            activeVessel = ConfigNodeUtil.ParseValue<bool>(node, "activeVessel");
            minCount = ConfigNodeUtil.ParseValue<int>(node, "minCount");
            maxCount = ConfigNodeUtil.ParseValue<int>(node, "maxCount");
            antennaType = ConfigNodeUtil.ParseValue<AntennaType?>(node, "antennaType", (AntennaType?)null);
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">Vessel to check</param>
        /// <returns>Whether the vessel meets the parameter condition(s).</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);

            // Get all the antennae
            VesselSatellite sat = RTCore.Instance.Satellites[vessel.id];
            IEnumerable<IAntenna> antennae = sat.Antennas.Where(a => a.Activated && a.Powered);

            // Filter by type
            if (antennaType == AntennaType.Dish)
            {
                antennae = antennae.Where(a => a.Dish > 0.0);
            }
            else if (antennaType == AntennaType.Omni)
            {
                antennae = antennae.Where(a => a.Omni > 0.0);
            }

            // Filter for active vessel
            if (activeVessel)
            {
                antennae = antennae.Where(a => a.Target == NetworkManager.ActiveVesselGuid || a.Omni > 0.0);
            }

            double minRange = this.minRange;

            // Filter for celestial bodies
            if (targetBody != null)
            {
                double distance = (Planetarium.fetch.Home.position - targetBody.position).magnitude;
                antennae = antennae.Where(a => a.Target == targetBody.Guid() || a.Omni > 0.0);
                minRange = Math.Max(minRange, distance);
            }

            // Filter for range
            antennae = antennae.Where(a => Math.Max(a.Omni, a.Dish) >= minRange && Math.Max(a.Omni, a.Dish) <= maxRange);

            // Validate count
            int count = antennae.Count();
            return count >= minCount && count <= maxCount;
        }
    }
}
