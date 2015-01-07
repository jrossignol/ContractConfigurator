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
    /*
     * Parameter for checking whether the parts in a vessel meet certain criteria.
     */
    public class PartValidation : VesselParameter
    {
        protected string title { get; set; }
        protected AvailablePart part { get; set; }
        protected List<string> partModules { get; set; }
        protected PartCategories? category { get; set; }
        protected string manufacturer { get; set; }
        protected bool allParts { get; set; }
        protected int minCount { get; set; }
        protected int maxCount { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        public PartValidation()
            : base()
        {
        }

        public PartValidation(AvailablePart part, List<string> partModules, PartCategories? category = null,
            string manufacturer = null, bool allParts = false, int minCount = 1, int maxCount = int.MaxValue, string title = null)
            : base()
        {
            // Vessels should fail if they don't meet the part conditions
            failWhenUnmet = true;

            this.part = part;
            this.partModules = partModules != null ? partModules : new List<string>();
            this.category = category;
            this.manufacturer = manufacturer;
            this.allParts = allParts;
            this.minCount = minCount;
            this.maxCount = maxCount;
            this.title = title;

            CreateDelegates();
        }

        protected override string GetTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                output = allParts ? "All parts" : "Part";
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
            // Filter by part name
            if (part != null)
            {
                AddParameter(new ParameterDelegate<Part>("Part: " + part.title,
                    p => p.partInfo.name == part.name));
            }

            // Filter by part modules
            foreach (string partModule in partModules)
            {
                string moduleName = partModule.Replace("Module", "");
                moduleName = Regex.Replace(moduleName, "(\\B[A-Z])", " $1");
                AddParameter(new ParameterDelegate<Part>("Module: " + moduleName, p => PartHasModule(p, partModule)));
            }
            
            // Filter by category
            if (category != null)
            {
                AddParameter(new ParameterDelegate<Part>("Category: " + category,
                    p => p.partInfo.category == (PartCategories)category));
            }

            // Filter by manufacturer
            if (manufacturer != null)
            {
                AddParameter(new ParameterDelegate<Part>("Manufacturer: " + manufacturer,
                    p => p.partInfo.manufacturer == manufacturer));
            }

            // Validate count
            if (!allParts && (minCount != 0 || maxCount != int.MaxValue))
            {
                AddParameter(new CountParameterDelegate<Part>(minCount, maxCount));
            }
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

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
            node.AddValue("minCount", minCount);
            node.AddValue("maxCount", maxCount);
            if (part != null)
            {
                node.AddValue("part", part.name);
            }
            foreach (string partModule in partModules)
            {
                node.AddValue("partModule", partModule);
            }
            if (category != null)
            {
                node.AddValue("category", category);
            }
            if (manufacturer != null)
            {
                node.AddValue("manufacturer", manufacturer);
            }
            node.AddValue("allParts", allParts);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            minCount = Convert.ToInt32(node.GetValue("minCount"));
            maxCount = Convert.ToInt32(node.GetValue("maxCount"));
            part = node.HasValue("part") ? ConfigNodeUtil.ParseValue<AvailablePart>(node, "part") : null;
            partModules = node.GetValues("partModule").ToList();
            category = node.HasValue("category") ? ConfigNodeUtil.ParseValue<PartCategories?>(node, "category") : null;
            manufacturer = node.HasValue("manufacturer") ? ConfigNodeUtil.ParseValue<string>(node, "manufacturer") : null;
            allParts = ConfigNodeUtil.ParseValue<bool>(node, "allParts");

            ParameterDelegate<Part>.OnDelegateContainerLoad(node);
            CreateDelegates();
        }

/*        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (UnityEngine.Time.fixedTime - lastUpdate > UPDATE_FREQUENCY)
            {
                lastUpdate = UnityEngine.Time.fixedTime;
                CheckVessel(FlightGlobals.ActiveVessel);
            }
        }*/

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

        /*
         * Whether this vessel meets the parameter condition.
         */
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);

            // If we're a VesselParameterGroup child, only do actual state change if we're the tracked vessel
            bool checkOnly = false;
            if (Parent is VesselParameterGroup)
            {
                checkOnly = ((VesselParameterGroup)Parent).TrackedVessel != vessel;
            }

            // Check which method of verification we are using - ALL or SEQUENTIAL
            if (allParts)
            {
                return ParameterDelegate<Part>.CheckChildConditionsForAll(this, vessel.parts, checkOnly);
            }
            else if (minCount == maxCount && minCount == 0)
            {
                return ParameterDelegate<Part>.CheckChildConditionsForNone(this, vessel.parts, checkOnly);
            }
            else
            {
                return ParameterDelegate<Part>.CheckChildConditions(this, vessel.parts, checkOnly);
            }
        }
    }
}
