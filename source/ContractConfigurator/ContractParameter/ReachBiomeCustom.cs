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
    public class ReachBiomeCustom : VesselParameter
    {
        protected string title { get; set; }
        public string biome { get; set; }

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
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        /*
         * Whether this vessel meets the parameter condition.
         */
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            return ScienceUtil.GetExperimentBiome(vessel.mainBody, vessel.latitude, vessel.longitude) == biome;
        }
    }
}
