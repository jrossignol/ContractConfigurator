using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using KSP.Localization;
using Contracts;
using Contracts.Parameters;
using RemoteTech;
using ContractConfigurator.Parameters;

namespace ContractConfigurator.RemoteTech
{
    /// <summary>
    /// Parameter for checking whether the vessel has an antenna that meets the specified criteria.
    /// </summary>
    public class HasAntennaParameter : RemoteTechParameter
    {
        public enum AntennaType
        {
            [Description("#RT_Editor_Dish")] Dish,
            [Description("#RT_Editor_Omni")] Omni
        };

        protected int minCount { get; set; }
        protected int maxCount { get; set; }
        protected bool activeVessel { get; set; }
        protected AntennaType? antennaType { get; set; }
        protected double minRange { get; set; }
        protected double maxRange { get; set; }

        public HasAntennaParameter()
            : base(null)
        {
        }

        public HasAntennaParameter(int minCount = 1, int maxCount = int.MaxValue, CelestialBody targetBody = null,
            bool activeVessel = false, AntennaType? antennaType = null, double minRange = 0.0, double maxRange = double.MaxValue, string title = null)
            : base(title)
        {
            this.minCount = minCount;
            this.maxCount = maxCount;
            this.targetBody = targetBody;
            this.activeVessel = activeVessel;
            this.antennaType = antennaType;
            this.minRange = minRange;
            this.maxRange = maxRange;

            CreateDelegates();
        }

        protected override string GetParameterTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                output = "Antenna";
                if (state == ParameterState.Complete)
                {
                    output += ": " + ParameterDelegate<IAntenna>.GetDelegateText(this);
                }
            }
            else
            {
                output = title;
            }
            return output;
        }

        protected void CreateDelegates()
        {
            ParameterDelegateMatchType matchType = ParameterDelegateMatchType.FILTER;
            if (maxCount == 0)
            {
                matchType = ParameterDelegateMatchType.NONE;
            }

            // Filter by type
            if (antennaType == AntennaType.Dish)
            {
                AddParameter(new ParameterDelegate<IAntenna>(Localizer.Format("#cc.remotetech.param.HasAntenna.type", antennaType.displayDescription()),
                    a => a.CanTarget, matchType));
            }
            else if (antennaType == AntennaType.Omni)
            {
                AddParameter(new ParameterDelegate<IAntenna>(Localizer.Format("#cc.remotetech.param.HasAntenna.type", antennaType.displayDescription()),
                    a => !a.CanTarget, matchType));
            }

            // Filter for active vessel
            if (activeVessel)
            {
                AddParameter(new ParameterDelegate<IAntenna>(Localizer.Format("#cc.remotetech.param.HasAntenna.target", Localizer.GetStringByTag("#RT_ModuleUI_ActiveVessel")),
                    a => a.Target == NetworkManager.ActiveVesselGuid || a.Omni > 0.0, matchType));
            }
            // Filter for celestial bodies
            else if (targetBody != null)
            {
                AddParameter(new ParameterDelegate<IAntenna>(Localizer.Format("#cc.remotetech.param.HasAntenna.target", targetBody.CleanDisplayName()),
                    a => a.Target == targetBody.Guid(), matchType));
            }

            // Activated and powered
            AddParameter(new ParameterDelegate<IAntenna>(Localizer.GetStringByTag("#cc.remotetech.param.HasAntenna.activated"), a => a.Activated, matchType, true));
            AddParameter(new ParameterDelegate<IAntenna>(Localizer.GetStringByTag("#cc.remotetech.param.HasAntenna.powered"), a => a.Powered, matchType, true));

            // Filter for range
            if (minRange != 0.0 || maxRange != double.MaxValue)
            {
                string countStr;
                if (maxRange == double.MaxValue)
                {
                    countStr = Localizer.Format("#cc.param.count.atLeast", RemoteTechAssistant.RangeString(minRange));
                }
                else if (minRange == 0)
                {
                    countStr = Localizer.Format("#cc.param.count.atMost", RemoteTechAssistant.RangeString(maxRange));
                }
                else
                {
                    countStr = Localizer.Format("#cc.param.count.between", RemoteTechAssistant.RangeString(minRange), RemoteTechAssistant.RangeString(maxRange));
                }

                AddParameter(new ParameterDelegate<IAntenna>(Localizer.Format("#cc.remotetech.param.HasAntenna.range", countStr),
                    a => Math.Max(a.Omni, a.Dish) >= minRange && Math.Max(a.Omni, a.Dish) <= maxRange, matchType));
            }

            // Extra filter for celestial bodies
            if (!activeVessel && targetBody != null)
            {
                double distance = (Planetarium.fetch.Home.position - targetBody.position).magnitude;
                AddParameter(new ParameterDelegate<IAntenna>(Localizer.Format("#cc.remotetech.param.HasAntenna.range.body", targetBody.CleanDisplayName(true)),
                    a => Math.Max(a.Omni, a.Dish) >= distance, matchType, true));
            }

            // Validate count
            if (minCount != 0 || maxCount != int.MaxValue && !(minCount == maxCount && maxCount == 0))
            {
                AddParameter(new CountParameterDelegate<IAntenna>(minCount, maxCount));
            }
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);
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

        protected override void OnParameterLoad(ConfigNode node)
        {
            try
            {
                base.OnParameterLoad(node);
                minRange = ConfigNodeUtil.ParseValue<double>(node, "minRange");
                maxRange = ConfigNodeUtil.ParseValue<double>(node, "maxRange", double.MaxValue);
                targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody", (CelestialBody)null);
                activeVessel = ConfigNodeUtil.ParseValue<bool>(node, "activeVessel");
                minCount = ConfigNodeUtil.ParseValue<int>(node, "minCount");
                maxCount = ConfigNodeUtil.ParseValue<int>(node, "maxCount");
                antennaType = ConfigNodeUtil.ParseValue<AntennaType?>(node, "antennaType", (AntennaType?)null);

                CreateDelegates();
            }
            finally
            {
                ParameterDelegate<IAntenna>.OnDelegateContainerLoad(node);
            }
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">Vessel to check</param>
        /// <returns>Whether the vessel meets the parameter condition(s).</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id + " (" + vessel.vesselName + ")");

            // Get all the antennae
            VesselSatellite sat = RTCore.Instance.Satellites[vessel.id];
            IEnumerable<IAntenna> antennas = sat != null ? sat.Antennas : new List<IAntenna>();

            // If we're a VesselParameterGroup child, only do actual state change if we're the tracked vessel
            bool checkOnly = false;
            VesselParameterGroup vpg = GetParameterGroupHost();
            if (vpg != null)
            {
                checkOnly = vpg.TrackedVessel != vessel;
            }

            return ParameterDelegate<IAntenna>.CheckChildConditions(this, antennas, checkOnly);
        }
    }
}
