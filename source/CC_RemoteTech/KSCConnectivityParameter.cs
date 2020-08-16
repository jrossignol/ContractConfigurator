using ContractConfigurator.Parameters;
using Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using UnityEngine;
using KSP.Localization;
using RemoteTech;
using RemoteTech.API;
using ContractConfigurator;

namespace ContractConfigurator.RemoteTech
{
    public class KSCConnectivityParameter : RemoteTechParameter
    {
        public bool hasConnectivity { get; set; }

        const int CHECK_COUNT = 10;
        bool[] checks = new bool[CHECK_COUNT];
        int currentCheck = 0;
        bool initialized = false;

        public KSCConnectivityParameter()
            : this(true, "")
        {
        }

        public KSCConnectivityParameter(bool hasConnectivity, string title)
            : base(title)
        {
            this.title = string.IsNullOrEmpty(title) ? Localizer.GetStringByTag(hasConnectivity ? "#cc.remotetech.param.KSCConnectivity" : "#cc.remotetech.param.KSCConnectivity.x") : title;
            this.hasConnectivity = hasConnectivity;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);

            node.AddValue("hasConnectivity", hasConnectivity);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);

            hasConnectivity = ConfigNodeUtil.ParseValue<bool>(node, "hasConnectivity");
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            var satellite = RTCore.Instance.Satellites[vessel.id];
            foreach (var v in RTCore.Instance.Network[satellite])
            {
                LoggingUtil.LogVerbose(this, "    Goal = " + v.Goal.Name);
                LoggingUtil.LogVerbose(this, "    Links.Count = " + v.Links.Count);
            }

            // Do a single check
            bool result = API.HasConnectionToKSC(vessel.id) ^ !hasConnectivity;

            // Store the result
            if (!initialized)
            {
                for (int i = 0; i < CHECK_COUNT; i++)
                {
                    checks[i] = result;
                    initialized = true;
                }
            }
            else
            {
                currentCheck = (currentCheck + 1) % CHECK_COUNT;
                checks[currentCheck] = result;
            }

            // Only need one check to have passed
            for (int i = 0; i < CHECK_COUNT; i++)
            {
                if (checks[i])
                {
                    return true;
                }
            }

            return false;
        }
    }
}
