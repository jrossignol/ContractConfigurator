using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for PartValidation ContractParameter.
    /// </summary>
    public class PartValidationFactory : ParameterFactory
    {
        protected AvailablePart part;
        protected List<string> partModules;
        protected PartCategories? category;
        protected string manufacturer;
        protected bool allParts;
        protected int minCount;
        protected int maxCount;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<AvailablePart>(configNode, "part", ref part, this, (AvailablePart)null);
            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "partModule", ref partModules, this, null, x => x == null || x.All(Validation.ValidatePartModule));
            valid &= ConfigNodeUtil.ParseValue<PartCategories?>(configNode, "category", ref category, this, (PartCategories?)null);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "manufacturer", ref manufacturer, this, (string)null);
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "part", "partModule", "category", "manufacturer"}, this);
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "allParts", ref allParts, this, false);
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "minCount", ref minCount, this, 1, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCount", ref maxCount, this, int.MaxValue, x => Validation.GE(x, 0));
            if (allParts)
            {
                valid &= ConfigNodeUtil.MutuallyExclusive(configNode, new string[] { "allParts" }, new string[] { "minCount", "maxCount" }, this);
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new PartValidation(part, partModules, category, manufacturer, allParts, minCount, maxCount, title);
        }
    }
}
