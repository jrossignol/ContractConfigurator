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
     * Parameter for checking whether a vessel has a part.
     */
    public class HasResource : VesselParameter
    {
        protected string title { get; set; }
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
            : base()
        {
            // Vessels should fail if they don't meet the part conditions
            failWhenUnmet = true;

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

        protected override string GetTitle()
        {
            return title;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
            node.AddValue("minQuantity", minQuantity);
            if (maxQuantity != double.MaxValue)
            {
                node.AddValue("maxQuantity", maxQuantity);
            }
            node.AddValue("resource", resource.name);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            minQuantity = Convert.ToDouble(node.GetValue("minQuantity"));
            maxQuantity = node.HasValue("maxQuantity") ? Convert.ToDouble(node.GetValue("maxQuantity")) : double.MaxValue;
            resource = ConfigNodeUtil.ParseResource(node, "resource");
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
            double quantity = 0.0;
            foreach (Part part in vessel.Parts)
            {
                PartResource pr = part.Resources[resource.name];
                if (pr != null)
                {
                    quantity += pr.amount;
                }
            }
            return quantity >= minQuantity && quantity <= maxQuantity;
        }
    }
}
