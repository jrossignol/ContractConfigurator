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
    /// Parameter for checking whether a vessel has a given crew capacity.
    /// </summary>
    public class HasCrewCapacity : VesselParameter
    {
        protected int minCapacity { get; set; }
        protected int maxCapacity { get; set; }

        public HasCrewCapacity()
            : base(null)
        {
        }

        public HasCrewCapacity(int minCapacity = 1, int maxCapacity = int.MaxValue, string title = null)
            : base(title)
        {
            if (minCapacity > maxCapacity)
            {
                minCapacity = maxCapacity;
            }

            // Vessels should fail if they don't meet the part conditions
            failWhenUnmet = true;
            fakeFailures = true;

            this.minCapacity = minCapacity;
            this.maxCapacity = maxCapacity;

            if (string.IsNullOrEmpty(title))
            {
                this.title = "Crew Capacity: ";

                if (maxCapacity == 0)
                {
                    this.title += "None";
                }
                else if (maxCapacity == int.MaxValue)
                {
                    this.title += "At least " + minCapacity;
                }
                else if (minCapacity == 0)
                {
                    this.title += "At most " + maxCapacity;
                }
                else
                {
                    this.title += "Between " + minCapacity + " and " + maxCapacity;
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
            node.AddValue("minCapacity", minCapacity);
            node.AddValue("maxCapacity", maxCapacity);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            minCapacity = ConfigNodeUtil.ParseValue<int>(node, "minCapacity");
            maxCapacity = ConfigNodeUtil.ParseValue<int>(node, "maxCapacity");
        }

        protected override void OnRegister()
        {
            base.OnRegister();
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
        }

        protected override void OnPartAttach(GameEvents.HostTargetAction<Part, Part> e)
        {
            base.OnPartAttach(e);
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected override void OnPartJointBreak(PartJoint p)
        {
            base.OnPartJointBreak(p);
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            int capacity = 0;
            foreach (Part part in vessel.Parts)
            {
                capacity += part.CrewCapacity;
            }
            return capacity >= minCapacity && capacity <= maxCapacity;
        }
    }
}
