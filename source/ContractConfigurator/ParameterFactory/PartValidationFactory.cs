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
        protected int minCount;
        protected int maxCount;
        protected AvailablePart part;
        protected List<string> partModules;
        protected PartCategories? category;
        protected PartCategories? notCategory;
        protected string manufacturer;
        protected string notManufacturer;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "minCount", ref minCount, this, 1, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCount", ref maxCount, this, int.MaxValue, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<AvailablePart>(configNode, "part", ref part, this, (AvailablePart)null);
            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "partModule", ref partModules, this, null, x => x == null || x.All(Validation.ValidatePartModule));
            valid &= ConfigNodeUtil.ParseValue<PartCategories?>(configNode, "category", ref category, this, (PartCategories?)null);
            valid &= ConfigNodeUtil.ParseValue<PartCategories?>(configNode, "notCategory", ref notCategory, this, (PartCategories?)null);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "manufacturer", ref manufacturer, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "notManufacturer", ref notManufacturer, this, (string)null);
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "part", "partModule", "category", "notCategory", "manufacturer", "notManufacturer" }, this);
            valid &= ConfigNodeUtil.MutuallyExclusive(configNode, new string[] { "category" }, new string[] { "notCategory" }, this);
            valid &= ConfigNodeUtil.MutuallyExclusive(configNode, new string[] { "manufacturer" }, new string[] { "notManufacturer" }, this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new PartValidation(part, partModules, category, notCategory, manufacturer, notManufacturer, minCount, maxCount, title);
        }
    }
}
