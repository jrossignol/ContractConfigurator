using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Parameter for checking whether the parts in a vessel meet certain criteria.
    /// </summary>
    public class PartValidation : VesselParameter
    {
        public class Filter
        {
            public ParameterDelegateMatchType type = ParameterDelegateMatchType.FILTER;
            public AvailablePart part = null;
            public List<string> partModules = new List<string>();
            public PartCategories? category = null;
            public string manufacturer = null;
            public int minCount = 1;
            public int maxCount = int.MaxValue;

            public Filter() { }
            public Filter(ParameterDelegateMatchType type) { this.type = type; }
        }

        protected List<Filter> filters { get; set; }
        protected int minCount { get; set; }
        protected int maxCount { get; set; }

        public PartValidation()
            : base(null)
        {
        }

        public PartValidation(List<Filter> filters, int minCount = 1, int maxCount = int.MaxValue, string title = null)
            : base(title)
        {
            this.filters = filters;
            this.minCount = minCount;
            this.maxCount = maxCount;

            CreateDelegates();
        }

        protected override string GetTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                output = "Parts";
                if (state == ParameterState.Complete)
                {
                    output += ": " + ParameterDelegate<Part>.GetDelegateText(this);
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
                // Filter by part name
                if (filter.part != null)
                {
                    if (filter.type == ParameterDelegateMatchType.VALIDATE)
                    {
                        AddParameter(new CountParameterDelegate<Part>(filter.minCount, filter.maxCount, p => p.partInfo.name == filter.part.name,
                            filter.part.title));
                    }
                    else
                    {
                        AddParameter(new ParameterDelegate<Part>(filter.type.Prefix() + "type: " + filter.part.title,
                            p => p.partInfo.name == filter.part.name, filter.type));
                    }
                }

                // Filter by part modules
                foreach (string partModule in filter.partModules)
                {
                    AddParameter(new ParameterDelegate<Part>(filter.type.Prefix() + "module: " + ModuleName(partModule), p => PartHasModule(p, partModule), filter.type));
                }

                // Filter by category
                if (filter.category != null)
                {
                    AddParameter(new ParameterDelegate<Part>(filter.type.Prefix() + "category: " + filter.category,
                        p => p.partInfo.category == filter.category.Value, filter.type));
                }

                // Filter by manufacturer
                if (filter.manufacturer != null)
                {
                    AddParameter(new ParameterDelegate<Part>(filter.type.Prefix() + "manufacturer: " + filter.manufacturer,
                        p => p.partInfo.manufacturer == filter.manufacturer, filter.type));
                }
            }

            // Validate count
            if (minCount != 0 || maxCount != int.MaxValue && !(minCount == maxCount && maxCount == 0))
            {
                AddParameter(new CountParameterDelegate<Part>(minCount, maxCount));
            }
        }

        private string ModuleName(string partModule)
        {
            string output = partModule.Replace("Module", "");

            // Hardcoded special values
            if (output == "SAS")
            {
                return output;
            }
            else if (output == "RTAntenna")
            {
                return "Antenna";
            }

            return Regex.Replace(output, "(\\B[A-Z])", " $1");
        }

        private bool PartHasModule(Part p, string partModule)
        {
            foreach (PartModule pm in p.Modules)
            {
                if (pm.moduleName == partModule)
                {
                    return true;
                }
            }
            return false;
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onVesselWasModified.Add(new EventData<Vessel>.OnEvent(OnVesselWasModified));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onVesselWasModified.Remove(new EventData<Vessel>.OnEvent(OnVesselWasModified));
        }

        protected void OnVesselWasModified(Vessel v)
        {
            CheckVessel(v);
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);
            node.AddValue("minCount", minCount);
            node.AddValue("maxCount", maxCount);

            foreach (Filter filter in filters)
            {
                ConfigNode child = new ConfigNode("FILTER");
                child.AddValue("type", filter.type);

                if (filter.part != null)
                {
                    child.AddValue("part", filter.part.name);
                }
                foreach (string partModule in filter.partModules)
                {
                    child.AddValue("partModule", partModule);
                }
                if (filter.category != null)
                {
                    child.AddValue("category", filter.category);
                }
                if (filter.manufacturer != null)
                {
                    child.AddValue("manufacturer", filter.manufacturer);
                }
                if (filter.type == ParameterDelegateMatchType.VALIDATE)
                {
                    child.AddValue("minCount", filter.minCount);
                    child.AddValue("maxCount", filter.maxCount);
                }

                node.AddNode(child);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            minCount = Convert.ToInt32(node.GetValue("minCount"));
            maxCount = Convert.ToInt32(node.GetValue("maxCount"));

            filters = new List<Filter>();

            foreach (ConfigNode child in node.GetNodes("FILTER"))
            {
                Filter filter = new Filter();
                filter.type = ConfigNodeUtil.ParseValue<ParameterDelegateMatchType>(child, "type");

                filter.part = ConfigNodeUtil.ParseValue<AvailablePart>(child, "part", (AvailablePart)null);
                filter.partModules = child.GetValues("partModule").ToList();
                filter.category = ConfigNodeUtil.ParseValue<PartCategories?>(child, "category", (PartCategories?)null);
                filter.manufacturer = ConfigNodeUtil.ParseValue<string>(child, "manufacturer", (string)null);
                filter.minCount = ConfigNodeUtil.ParseValue<int>(child, "minCount", 1);
                filter.maxCount = ConfigNodeUtil.ParseValue<int>(child, "maxCount", int.MaxValue);

                filters.Add(filter);
            }

            ParameterDelegate<Part>.OnDelegateContainerLoad(node);
            CreateDelegates();
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

            // If we're a VesselParameterGroup child, only do actual state change if we're the tracked vessel
            bool checkOnly = false;
            if (Parent is VesselParameterGroup)
            {
                checkOnly = ((VesselParameterGroup)Parent).TrackedVessel != vessel;
            }

            return ParameterDelegate<Part>.CheckChildConditions(this, vessel.parts, checkOnly);
        }
    }
}
