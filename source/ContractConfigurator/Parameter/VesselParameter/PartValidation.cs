using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using FinePrint;
using KSP.Localization;

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
            public List<AvailablePart> parts = new List<AvailablePart>();
            public List<string> partModules = new List<string>();
            public List<string> partModuleTypes = new List<string>();
            public List<List<Tuple<string, string, string>>> partModuleExtended = new List<List<Tuple<string, string, string>>>();
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

        protected override string GetParameterTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                if (state == ParameterState.Complete)
                {
                    if (maxCount == int.MaxValue && minCount != 1)
                    {
                        output = Localizer.Format("#cc.param.PartValidation.atLeast", minCount, ParameterDelegate<Part>.GetDelegateText(this));
                    }
                    else if (maxCount != int.MaxValue && minCount == 1)
                    {
                        output = Localizer.Format("#cc.param.PartValidation.atMost", maxCount, ParameterDelegate<Part>.GetDelegateText(this));
                    }
                    else
                    {
                        output = Localizer.Format("#cc.param.PartValidation.nocount", ParameterDelegate<Part>.GetDelegateText(this));
                    }
                }
                else
                {
                    output = Localizer.GetStringByTag("#cc.param.PartValidation");
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
                if (filter.type == ParameterDelegateMatchType.VALIDATE)
                {
                    foreach (AvailablePart part in filter.parts)
                    {
                        AddParameter(new CountParameterDelegate<Part>(filter.minCount, filter.maxCount, p => p.partInfo.name == part.name,
                            part.title, false));
                    }

                    // Filter by part modules
                    foreach (string partModule in filter.partModules)
                    {
                        AddParameter(new CountParameterDelegate<Part>(filter.minCount, filter.maxCount, p => PartHasModule(p, partModule),
                            Localizer.Format("#cc.param.PartValidation.withModule", ModuleName(partModule)), false));
                    }

                    // Filter by part module types
                    foreach (string partModuleType in filter.partModuleTypes)
                    {
                        AddParameter(new CountParameterDelegate<Part>(filter.minCount, filter.maxCount, p => PartHasObjective(p, partModuleType),
                            Localizer.Format("#cc.param.PartValidation.withModuleType", ModuleTypeName(partModuleType)), false));
                    }
                }
                else
                {
                    // Filter by part
                    if (filter.parts.Any())
                    {
                        AddParameter(new ParameterDelegate<Part>(
                            Localizer.Format("#cc.param.PartValidation.type", filter.type.Prefix(), LocalizationUtil.LocalizeList<AvailablePart>(LocalizationUtil.Conjunction.OR, filter.parts, p => p.title)),
                            p => filter.parts.Any(pp => p.partInfo.name == pp.name), filter.type));
                    }

                    // Filter by part modules
                    foreach (string partModule in filter.partModules)
                    {
                        AddParameter(new ParameterDelegate<Part>(Localizer.Format("#cc.param.PartValidation.module", filter.type.Prefix(), ModuleName(partModule)), p => PartHasModule(p, partModule), filter.type));
                    }

                    // Filter by part modules
                    foreach (string partModuleType in filter.partModuleTypes)
                    {
                        AddParameter(new ParameterDelegate<Part>(Localizer.Format("#cc.param.PartValidation.moduleType", filter.type.Prefix(), ModuleTypeName(partModuleType)), p => PartHasObjective(p, partModuleType), filter.type));
                    }

                    // Filter by part modules - extended mode
                    foreach (List<Tuple<string, string, string>> list in filter.partModuleExtended)
                    {
                        ContractParameter wrapperParam = AddParameter(new AllParameterDelegate<Part>(Localizer.Format("#cc.param.PartValidation.moduleShort", filter.type.Prefix()), filter.type));

                        foreach (Tuple<string, string, string> v in list)
                        {
                            string name = v.Item1;
                            string label;
                            if (string.IsNullOrEmpty(v.Item2))
                            {
                                label = Regex.Replace(name, @"([A-Z]+?(?=[A-Z][^A-Z])|\B[A-Z]+?(?=[^A-Z]))", " $1");
                                label = label.Substring(0, 1).ToUpper() + label.Substring(1);
                            }
                            else
                            {
                                label = v.Item2;
                            }
                            string value = name == "name" ? ModuleName(v.Item3) : v.Item3;

                            ParameterDelegateMatchType childFilter = ParameterDelegateMatchType.FILTER;
                            wrapperParam.AddParameter(new ParameterDelegate<Part>(Localizer.Format("#cc.param.PartValidation.generic", childFilter.Prefix(), label, value), p => PartModuleCheck(p, v), childFilter));
                        }
                    }

                    // Filter by category
                    if (filter.category != null)
                    {
                        AddParameter(new ParameterDelegate<Part>(Localizer.Format("#cc.param.PartValidation.category", filter.type.Prefix(), filter.category),
                            p => p.partInfo.category == filter.category.Value, filter.type));
                    }

                    // Filter by manufacturer
                    if (filter.manufacturer != null)
                    {
                        AddParameter(new ParameterDelegate<Part>(Localizer.Format("#cc.param.PartValidation.manufacturer", filter.type.Prefix(), filter.manufacturer),
                            p => p.partInfo.manufacturer == filter.manufacturer, filter.type));
                    }
                }
            }

            // Validate count
            if (minCount != 0 || maxCount != int.MaxValue && !(minCount == maxCount && maxCount == 0))
            {
                AddParameter(new CountParameterDelegate<Part>(minCount, maxCount));
            }
        }

        public static string ModuleName(string partModule)
        {
            string output = partModule.Replace("Module", "").Replace("FX", "");

            // Hardcoded special values
            if (output == "SAS")
            {
                return output;
            }
            else if (output == "RTAntenna")
            {
                return "Antenna";
            }
            else if (output.Contains("Wheel"))
            {
                return "Wheel";
            }

            return Regex.Replace(output, "(\\B[A-Z])", " $1");
        }

        public static string ModuleTypeName(string partModule)
        {
            // We don't have a reliable way to translate these, so we just do hardcoded mappings
            switch(partModule)
            {
                case "Antenna":
                    return Localizer.GetStringByTag("#autoLOC_234196");
                case "Battery":
                    return Localizer.GetStringByTag("#cc.parts.battery");
                case "Dock":
                    return Localizer.GetStringByTag("#cc.parts.dock");
                case "Generator":
                    return Localizer.GetStringByTag("#autoLOC_235532");
                case "Grapple":
                    return Localizer.GetStringByTag("#autoLOC_8005456");
                case "Wheel":
                    return Localizer.GetStringByTag("#autoLOC_148102");
                default:
                    return partModule;
            }
        }

        private bool PartHasModule(Part p, string partModule)
        {
            foreach (PartModule pm in p.Modules)
            {
                if (pm.moduleName.StartsWith(partModule) || pm.GetType().BaseType.Name.StartsWith(partModule))
                {
                    return true;
                }
            }
            return false;
        }

        private bool PartHasObjective(Part p, string contractObjective)
        {
            return p.HasValidContractObjective(contractObjective);
        }

        private bool PartModuleCheck(Part p, Tuple<string, string, string> v)
        {
            foreach (PartModule pm in p.Modules)
            {
                if (v.Item1 == "name")
                {
                    if (pm.moduleName == v.Item3)
                    {
                        return true;
                    }
                }
                else if (v.Item1 == "EngineType")
                {
                    ModuleEngines me = pm as ModuleEngines;
                    return me != null && me.engineType.ToString() == v.Item3;
                }

                foreach (BaseField field in pm.Fields)
                {
                    if (field != null && field.name == v.Item1 && field.originalValue != null && field.originalValue.ToString() == v.Item3)
                    {
                        return true;
                    }
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

                foreach (AvailablePart part in filter.parts)
                {
                    child.AddValue("part", part.name);
                }
                foreach (string partModule in filter.partModules)
                {
                    child.AddValue("partModule", partModule);
                }
                foreach (string partModuleType in filter.partModuleTypes)
                {
                    child.AddValue("partModuleType", partModuleType);
                }
                foreach (List<Tuple<string, string, string>> list in filter.partModuleExtended)
                {
                    ConfigNode moduleNode = new ConfigNode("MODULE");
                    child.AddNode(moduleNode);

                    foreach (Tuple<string, string, string> v in list)
                    {
                        if (!String.IsNullOrEmpty(v.Item2))
                        {
                            moduleNode.AddValue("label", v.Item2);
                        }
                        moduleNode.AddValue(v.Item1, v.Item3);
                    }
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
            try
            {
                base.OnParameterLoad(node);
                minCount = Convert.ToInt32(node.GetValue("minCount"));
                maxCount = Convert.ToInt32(node.GetValue("maxCount"));

                filters = new List<Filter>();

                foreach (ConfigNode child in node.GetNodes("FILTER"))
                {
                    Filter filter = new Filter();
                    filter.type = ConfigNodeUtil.ParseValue<ParameterDelegateMatchType>(child, "type");

                    filter.parts = ConfigNodeUtil.ParseValue<List<AvailablePart>>(child, "part", new List<AvailablePart>());
                    filter.partModules = child.GetValues("partModule").ToList();
                    filter.partModuleTypes = child.GetValues("partModuleType").ToList();
                    filter.category = ConfigNodeUtil.ParseValue<PartCategories?>(child, "category", (PartCategories?)null);
                    filter.manufacturer = ConfigNodeUtil.ParseValue<string>(child, "manufacturer", (string)null);
                    filter.minCount = ConfigNodeUtil.ParseValue<int>(child, "minCount", 1);
                    filter.maxCount = ConfigNodeUtil.ParseValue<int>(child, "maxCount", int.MaxValue);

                    foreach (ConfigNode moduleNode in child.GetNodes("MODULE"))
                    {
                        List<Tuple<string, string, string>> tmp = new List<Tuple<string, string, string>>();
                        string nextLabel = "";
                        foreach (ConfigNode.Value v in moduleNode.values)
                        {
                            if (v.name == "name")
                            {
                                tmp.Add(new Tuple<string, string, string>(v.name, "", v.value));
                            }
                            else if (v.name == "label")
                            {
                                nextLabel = v.value;
                            }
                            else
                            {
                                tmp.Add(new Tuple<string, string, string>(v.name, nextLabel, v.value));
                                nextLabel = "";
                            }
                        }
                        filter.partModuleExtended.Add(tmp);
                    }

                    filters.Add(filter);
                }

                CreateDelegates();
            }
            finally
            {
                ParameterDelegate<Part>.OnDelegateContainerLoad(node);
            }
        }

        protected override void OnPartAttach(GameEvents.HostTargetAction<Part, Part> e)
        {
            base.OnPartAttach(e);
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected override void OnPartJointBreak(PartJoint p, float breakForce)
        {
            base.OnPartJointBreak(p, breakForce);
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: {0}", vessel.id);

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
