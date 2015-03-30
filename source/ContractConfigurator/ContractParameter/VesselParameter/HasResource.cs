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
    /// Parameter for checking whether a vessel has the given resource.
    /// </summary>
    public class HasResource : VesselParameter
    {
        protected PartResourceDefinition resource { get; set; }
        protected double minQuantity { get; set; }
        protected double maxQuantity { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        public HasResource()
            : this(null)
        {
        }

        public HasResource(PartResourceDefinition resource, double minQuantity = 0.01, double maxQuantity = double.MaxValue, string title = null)
            : base(title)
        {
            this.resource = resource;
            this.minQuantity = minQuantity;
            this.maxQuantity = maxQuantity;
            if (title == null && resource != null)
            {
                this.title = "Resource: " + resource.name + ": ";

                if (maxQuantity == 0)
                {
                    this.title += "None";
                }
                else if (maxQuantity == double.MaxValue && (minQuantity > 0.0 && minQuantity <= 0.01))
                {
                    this.title += "Not zero units";
                }
                else if (maxQuantity == double.MaxValue)
                {
                    this.title += "At least " + minQuantity + " units";
                }
                else if (minQuantity == 0)
                {
                    this.title += "At most " + maxQuantity + " units";
                }
                else
                {
                    this.title += "Between " + minQuantity + " and " + maxQuantity + " units";
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
            node.AddValue("minQuantity", minQuantity);
            if (maxQuantity != double.MaxValue)
            {
                node.AddValue("maxQuantity", maxQuantity);
            }
            node.AddValue("resource", resource.name);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            minQuantity = Convert.ToDouble(node.GetValue("minQuantity"));
            maxQuantity = node.HasValue("maxQuantity") ? Convert.ToDouble(node.GetValue("maxQuantity")) : double.MaxValue;
            resource = ConfigNodeUtil.ParseValue<PartResourceDefinition>(node, "resource");
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
            double quantity = vessel.ResourceQuantity(resource);
            return quantity >= minQuantity && quantity <= maxQuantity;
        }
    }
}
