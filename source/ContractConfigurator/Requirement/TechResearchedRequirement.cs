using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using FinePrint;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement to provide requirement for player having researched a technology.
    /// </summary>
    public class TechResearchedRequirement : ContractRequirement
    {
        protected List<string> techs;
        protected List<string> partModules;
        protected List<string> partModuleTypes;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            // Check on active contracts too
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : true;

            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "tech", x => techs = x, this, new List<string>());

            if (configNode.HasValue("part"))
            {
                List<AvailablePart> parts = new List<AvailablePart>();
                valid &= ConfigNodeUtil.ParseValue<List<AvailablePart>>(configNode, "part", x => parts = x, this);

                foreach (AvailablePart part in parts)
                {
                    techs.AddUnique(part.TechRequired);
                }
            }

            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "partModule", x => partModules = x, this, new List<string>(), x => x.All(Validation.ValidatePartModule));
            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "partModuleType", x => partModuleTypes = x, this, new List<string>(), x => x.All(Validation.ValidatePartModuleType));

            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "tech", "part", "partModule", "partModuleType" }, this);

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            foreach (string tech in techs)
            {
                configNode.AddValue("tech", tech);
            }

            foreach (string partModule in partModules)
            {
                configNode.AddValue("partModule", partModule);
            }

            foreach (string partModuleType in partModuleTypes)
            {
                configNode.AddValue("partModuleType", partModuleType);
            }
        }

        public override void OnLoad(ConfigNode configNode)
        {
            techs = ConfigNodeUtil.ParseValue<List<string>>(configNode, "tech", new List<string>());
            partModules = ConfigNodeUtil.ParseValue<List<string>>(configNode, "partModule", new List<string>());
            partModuleTypes = ConfigNodeUtil.ParseValue<List<string>>(configNode, "partModuleType", new List<string>());
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            foreach (string tech in techs)
            {
                ProtoTechNode techNode = ResearchAndDevelopment.Instance.GetTechState(tech);
                if (techNode == null || techNode.state != RDTech.State.Available)
                {
                    return false;
                }
            }

            foreach (string partModule in partModules)
            {
                bool hasModule = false;
                foreach (AvailablePart part in PartLoader.LoadedPartsList)
                {
                    if (part.partPrefab == null || part.partPrefab.Modules == null)
                    {
                        continue;
                    }

                    if (ResearchAndDevelopment.PartTechAvailable(part))
                    {
                        hasModule = true;
                        break;
                    }
                }

                if (!hasModule)
                {
                    return false;
                }
            }

            foreach (string partModuleType in partModuleTypes)
            {
                List<string> modules = ContractDefs.GetModules(partModuleType);

                bool hasType = false;
                foreach (string module in modules)
                {
                    foreach (AvailablePart part in PartLoader.LoadedPartsList)
                    {
                        if (part.partPrefab == null || part.partPrefab.Modules == null)
                        {
                            continue;
                        }

                        if (ResearchAndDevelopment.PartTechAvailable(part))
                        {
                            hasType = true;
                            break;
                        }
                    }

                    if (hasType)
                    {
                        break;
                    }
                }

                if (!hasType)
                {
                    return false;
                }
            }

            return true;
        }

        protected override string RequirementText()
        {
            // Techs
            string techStr = "";
            for (int i = 0; i < techs.Count; i++)
            {
                if (i != 0)
                {
                    if (i == techs.Count - 1)
                    {
                        techStr += " and ";
                    }
                    else
                    {
                        techStr += ", ";
                    }
                }

                techStr += Tech.GetTech(techs[i]).title;
            }
            if (techs.Count > 0)
            {
                techStr = "have researched " + techStr;
            }

            // Part module 
            string pmStr = "";
            for (int i = 0; i < partModules.Count; i++)
            {
                if (i != 0)
                {
                    if (i == partModules.Count - 1)
                    {
                        pmStr += " and ";
                    }
                    else
                    {
                        pmStr += ", ";
                    }
                }

                pmStr += partModules[i];
            }
            if (partModules.Count > 0)
            {
                if (techs.Count > 0)
                {
                    techStr += " and";
                }
                pmStr = " have researched tech for " + pmStr;
            }

            // Part module type
            string pmtStr = "";
            for (int i = 0; i < partModuleTypes.Count; i++)
            {
                if (i != 0)
                {
                    if (i == partModuleTypes.Count - 1)
                    {
                        pmtStr += " and ";
                    }
                    else
                    {
                        pmtStr += ", ";
                    }
                }

                pmtStr += partModuleTypes[i];
            }
            if (partModuleTypes.Count > 0)
            {
                if (techs.Count > 0 || partModules.Count > 0)
                {
                    pmStr += " and";
                }
                pmtStr = " have researched tech for " + pmtStr;
            }

            return "Must " + (invertRequirement ? "not " : "") + techStr + pmStr + pmtStr;
        }
    }
}
