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
        protected PartCategories? notCategory { get; set; }
        protected string manufacturer { get; set; }
        protected string notManufacturer { get; set; }
        protected int minCount { get; set; }
        protected int maxCount { get; set; }

        public PartValidation()
            : this(null, new List<string>())
        {
        }

        public PartValidation(AvailablePart part, List<string> partModules, PartCategories? category = null, PartCategories? notCategory = null,
            string manufacturer = null, string notManufacturer = null, int minCount = 1, int maxCount = int.MaxValue, string title = null)
            : base()
        {
            // Vessels should fail if they don't meet the part conditions
            failWhenUnmet = true;

            this.part = part;
            this.partModules = partModules != null ? partModules : new List<string>();
            this.category = category;
            this.notCategory = notCategory;
            this.manufacturer = manufacturer;
            this.notManufacturer = notManufacturer;
            this.minCount = minCount;
            this.maxCount = maxCount;
            if (title == null)
            {
                bool needsComma = false;
                this.title += "Part: ";
                
                // Add specific part
                if (part != null)
                {
                    this.title += part.title;
                    needsComma = true;
                }

                // Add modules
                if (partModules != null && partModules.Count > 0)
                {
                    this.title += needsComma ? ", " : "";
                    this.title += "With module" + (partModules.Count > 1 ? "s" : "") + ": ";
                    needsComma = false;

                    foreach (string partModule in partModules)
                    {
                        this.title += needsComma ? ", " : "";
                        string moduleName = partModule.Replace("Module", "");
                        moduleName = Regex.Replace(moduleName, "(\\B[A-Z])", " $1");
                        this.title += moduleName;
                        needsComma = true;
                    }
                }

                // Add category
                if (category != null)
                {
                    this.title += needsComma ? ", " : "";
                    this.title += "Category: " + category;
                    needsComma = true;
                }

                // Add not category
                if (notCategory != null)
                {
                    this.title += needsComma ? ", " : "";
                    this.title += "Not category: " + notCategory;
                    needsComma = true;
                }

                // Add manufacturer
                if (manufacturer != null)
                {
                    this.title += needsComma ? ", " : "";
                    this.title += "Manufacturer: " + manufacturer;
                    needsComma = true;
                }

                // Add not manufacturer
                if (notManufacturer != null)
                {
                    this.title += needsComma ? ", " : "";
                    this.title += "Not manufacturer: " + notManufacturer;
                    needsComma = true;
                }

                this.title += ": ";
                if (maxCount == 0)
                {
                    this.title += "None";
                }
                else if (maxCount == int.MaxValue)
                {
                    this.title += "At least " + minCount;
                }
                else if (minCount == 0)
                {
                    this.title += "At most " + maxCount;
                }
                else if (minCount == maxCount)
                {
                    this.title += "Exactly " + minCount;
                }
                else
                {
                    this.title += "Between " + minCount + " and " + maxCount;
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
            if (notCategory != null)
            {
                node.AddValue("notCategory", notCategory);
            }
            if (manufacturer != null)
            {
                node.AddValue("manufacturer", manufacturer);
            }
            if (notManufacturer != null)
            {
                node.AddValue("notManufacturer", notManufacturer);
            }
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
            notCategory = node.HasValue("notCategory") ? ConfigNodeUtil.ParseValue<PartCategories?>(node, "notCategory") : null;
            manufacturer = node.HasValue("manufacturer") ? ConfigNodeUtil.ParseValue<string>(node, "manufacturer") : null;
            notManufacturer = node.HasValue("notManufacturer") ? ConfigNodeUtil.ParseValue<string>(node, "notManufacturer") : null;
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

        /*
         * Whether this vessel meets the parameter condition.
         */
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);

            IEnumerable<Part> parts = vessel.parts;

            // Filter by part name
            if (part != null)
            {
                parts = parts.Where<Part>(p => p.partInfo.name == part.name);
            }

            // Filter by part modules
            foreach (string partModule in partModules)
            {
                parts = PartsWithModule(parts, partModule);
            }

            // Filter by category
            if (category != null)
            {
                parts = parts.Where<Part>(p => p.partInfo.category == (PartCategories)category);
            }

            // Filter by category inverse
            if (notCategory != null)
            {
                parts = parts.Where<Part>(p => p.partInfo.category != (PartCategories)notCategory);
            }

            // Filter by manufacturer
            if (manufacturer != null)
            {
                parts = parts.Where<Part>(p => p.partInfo.manufacturer == manufacturer);
            }

            // Filter by manufacturer inverse
            if (notManufacturer != null)
            {
                parts = parts.Where<Part>(p => p.partInfo.manufacturer != notManufacturer);
            }

            // Validate count
            int count = parts.Count();
            return count >= minCount && count <= maxCount;
        }

        private IEnumerable<Part> PartsWithModule(IEnumerable<Part> parts, string partModule)
        {
            foreach (Part p in parts)
            {
                foreach (PartModule pm in p.Modules)
                {
                    if (pm.moduleName == partModule)
                    {
                        yield return p;
                        break;
                    }
                }
            }
        }
    }
}
