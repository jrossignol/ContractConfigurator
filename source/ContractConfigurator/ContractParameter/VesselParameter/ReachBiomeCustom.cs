using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator.Parameters
{
    /*
     * Custom version of the stock ReachBiome parameter.
     */
    [Obsolete("Obsolete, use ReachState")]
    public class ReachBiomeCustom : VesselParameter
    {
        protected string title { get; set; }
        public string biome { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.5f;

        private Vessel.Situations[] landedSituations = new Vessel.Situations[] { Vessel.Situations.LANDED, Vessel.Situations.PRELAUNCH, Vessel.Situations.SPLASHED };

        public ReachBiomeCustom()
            : this(null, null)
        {
        }

        public ReachBiomeCustom(string biome, string title)
            : base()
        {
            this.title = title != null ? title : "Biome: " + biome;
            this.biome = biome;
        }

        protected override string GetTitle()
        {
            return title;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
            node.AddValue("biome", biome);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            biome = node.GetValue("biome");
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (UnityEngine.Time.fixedTime - lastUpdate > UPDATE_FREQUENCY)
            {
                lastUpdate = UnityEngine.Time.fixedTime;
                CheckVessel(FlightGlobals.ActiveVessel);
            }
        }

        /*
         * Whether this vessel meets the parameter condition.
         */
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);

            // Fixes problems with special biomes like KSC buildings (total different naming)
            if (landedSituations.Contains(vessel.situation))
            {
                if (Vessel.GetLandedAtString(vessel.landedAt) == biome)
                {
                    return true;
                }
            }

            return ScienceUtil.GetExperimentBiome(vessel.mainBody, vessel.latitude, vessel.longitude) == biome;
        }
    }
}
