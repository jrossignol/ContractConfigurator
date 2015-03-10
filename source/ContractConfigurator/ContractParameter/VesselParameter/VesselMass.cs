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
    /// <summary>
    /// Parameter for checking the mass of a vessel
    /// </summary>
    public class VesselMass : VesselParameter
    {
        protected float minMass { get; set; }
        protected float maxMass { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        public VesselMass()
            : this(0.0f)
        {
        }

        public VesselMass(float minMass = 0.0f, float maxMass = float.MaxValue, string title = null)
            : base(title)
        {
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

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);
            node.AddValue("minMass", minMass);
            if (maxMass != double.MaxValue)
            {
                node.AddValue("maxMass", maxMass);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
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

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            float mass = vessel.GetTotalMass();
            return mass >= minMass && mass <= maxMass;
        }
    }
}
