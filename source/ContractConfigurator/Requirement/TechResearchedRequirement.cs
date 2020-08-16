using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using FinePrint;
using KSP.Localization;

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

            valid &= ConfigNodeUtil.OnlyOne(configNode, new string[] { "tech", "part", "partModule", "partModuleType" }, this);

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
                bool hasType = false;
                foreach (AvailablePart part in PartLoader.LoadedPartsList)
                {
                    if (part.partPrefab == null || part.partPrefab.Modules == null)
                    {
                        continue;
                    }

                    if (part.partPrefab.HasValidContractObjective(partModuleType) && ResearchAndDevelopment.PartTechAvailable(part))
                    {
                        hasType = true;
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
            if (techs.Count > 0)
            {
                return Localizer.Format(invertRequirement ? "#cc.req.TechResearched.tech.x" : "#cc.req.TechResearched.tech",
                    LocalizationUtil.LocalizeList<string>(invertRequirement ? LocalizationUtil.Conjunction.AND : LocalizationUtil.Conjunction.OR, techs, x => { Tech t = Tech.GetTech(x); return t != null ? t.title : x; } ));
            }

            // Part module 
            if (partModules.Count > 0)
            {
                return Localizer.Format(invertRequirement ? "#cc.req.TechResearched.part.x" : "#cc.req.TechResearched.part",
                    LocalizationUtil.LocalizeList<string>(invertRequirement ? LocalizationUtil.Conjunction.AND : LocalizationUtil.Conjunction.OR, partModules, x => Parameters.PartValidation.ModuleName(x)));
            }

            // Part module type
            return Localizer.Format(invertRequirement ? "#cc.req.TechResearched.part.x" : "#cc.req.TechResearched.part",
                LocalizationUtil.LocalizeList<string>(invertRequirement ? LocalizationUtil.Conjunction.AND : LocalizationUtil.Conjunction.OR, partModuleTypes, x => Parameters.PartValidation.ModuleTypeName(x)));
        }
    }
}
