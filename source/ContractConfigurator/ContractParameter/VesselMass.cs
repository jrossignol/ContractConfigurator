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
     * Parameter for checking whether the Mass of a vessel
     */
    public class VesselMass : VesselParameter
    {
        protected string title { get; set; }
        protected float minMass { get; set; }
        protected float maxMass { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        public VesselMass()
            : this(0.0f)
        {
        }

        public VesselMass(float minMass = 0.0f, float maxMass = float.MaxValue, string title = null)
            : base()
        {
            // Show as failed when mass incorrect
            failWhenUnmet = true;

            this.minMass = minMass;
            this.maxMass = maxMass;
            if (title == null)
            {
                this.title = "Mass: ";

                if (maxMass == float.MaxValue)
                {
                    this.title += "At least " + minMass + " tons";
                }
                else if (minMass == 0.0)
                {
                    this.title += "At most " + maxMass + " tons";
                }
                else
                {
                    this.title += "Between " + minMass + " and " + maxMass + " tons";
                }
            }
            else
            {
                this.title = title;
            }
        }

        protected override string GetTitle()
        {
            return title;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
            node.AddValue("minMass", minMass);
            if (maxMass != double.MaxValue)
            {
                node.AddValue("maxMass", maxMass);
            }
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            minMass = (float)Convert.ToDouble(node.GetValue("minMass"));
            maxMass = node.HasValue("maxMass") ? (float)Convert.ToDouble(node.GetValue("maxMass")) : float.MaxValue;
        }

        protected override void OnRegister()
        {
            base.OnRegister();
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
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
            float mass = vessel.GetTotalMass();
            return mass >= minMass && mass <= maxMass;
        }
    }
}
