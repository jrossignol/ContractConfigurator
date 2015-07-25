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
        public class Filter
        {
            public PartResourceDefinition resource { get; set; }
            public double minQuantity { get; set; }
            public double maxQuantity { get; set; }

            public Filter() { }
        }

        protected List<Filter> filters = new List<Filter>();

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        public HasResource()
            : base(null)
        {
        }

        public HasResource(List<Filter> filters, string title = null)
            : base(title)
        {
            this.filters = filters;

            CreateDelegates();
        }

        protected override string GetParameterTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                output = "Resources";
                if (state == ParameterState.Complete)
                {
                    output += ": " + ParameterDelegate<Vessel>.GetDelegateText(this);
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
            foreach (Filter filter in filters)
            {
                string output = "Resource: " + filter.resource.name + ": ";
                if (filter.maxQuantity == 0)
                {
                    output += "None";
                }
                else if (filter.maxQuantity == double.MaxValue && (filter.minQuantity > 0.0 && filter.minQuantity <= 0.01))
                {
                    output += "Not zero units";
                }
                else if (filter.maxQuantity == double.MaxValue)
                {
                    output += "At least " + filter.minQuantity + " units";
                }
                else if (filter.minQuantity == 0)
                {
                    output += "At most " + filter.maxQuantity + " units";
                }
                else
                {
                    output += "Between " + filter.minQuantity + " and " + filter.maxQuantity + " units";
                }


                AddParameter(new ParameterDelegate<Vessel>(output, v => VesselHasResource(v, filter.resource, filter.minQuantity, filter.maxQuantity),
                    ParameterDelegateMatchType.VALIDATE));
            }

            if (this.GetChildren().Count() == 1 && string.IsNullOrEmpty(title))
            {
                this.hideChildren = true;
                this.title = ParameterDelegate<Vessel>.GetDelegateText(this);
            }
        }

        protected static bool VesselHasResource(Vessel vessel, PartResourceDefinition resource, double minQuantity, double maxQuantity)
        {
            double quantity = vessel.ResourceQuantity(resource);
            return quantity >= minQuantity && quantity <= maxQuantity;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);

            foreach (Filter filter in filters)
            {
                ConfigNode childNode = new ConfigNode("RESOURCE");
                node.AddNode(childNode);

                childNode.AddValue("resource", filter.resource.name);
                childNode.AddValue("minQuantity", filter.minQuantity);
                if (filter.maxQuantity != double.MaxValue)
                {
                    childNode.AddValue("maxQuantity", filter.maxQuantity);
                }
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);

            foreach (ConfigNode childNode in node.GetNodes("RESOURCE"))
            {
                Filter filter = new Filter();

                filter.resource = ConfigNodeUtil.ParseValue<PartResourceDefinition>(childNode, "resource");
                filter.minQuantity = ConfigNodeUtil.ParseValue<double>(childNode, "minQuantity");
                filter.maxQuantity = ConfigNodeUtil.ParseValue<double>(childNode, "maxQuantity", double.MaxValue);

                filters.Add(filter);
            }

            // Legacy
            if (node.HasValue("resource"))
            {
                Filter filter = new Filter();

                filter.resource = ConfigNodeUtil.ParseValue<PartResourceDefinition>(node, "resource");
                filter.minQuantity = ConfigNodeUtil.ParseValue<double>(node, "minQuantity");
                filter.maxQuantity = ConfigNodeUtil.ParseValue<double>(node, "maxQuantity", double.MaxValue);

                filters.Add(filter);
            }

            ParameterDelegate<Part>.OnDelegateContainerLoad(node);
            CreateDelegates();
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

            return ParameterDelegate<Vessel>.CheckChildConditions(this, vessel);
        }
    }
}
