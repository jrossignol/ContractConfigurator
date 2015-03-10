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
        protected List<PartValidation.Filter> filters = new List<PartValidation.Filter>();
        protected bool failWhenUnmet;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "failWhenUnmet", x => failWhenUnmet = x, this, false, x => true);

            // Read min/max first
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "minCount", x => minCount = x, this,
                configNode.HasNode("VALIDATE") || configNode.HasNode("VALIDATE_ALL") || configNode.HasNode("NONE") ? 0 : 1, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCount", x => maxCount = x, this, int.MaxValue, x => Validation.GE(x, 0));

            // Set the default match type
            ParameterDelegateMatchType defaultMatch = ParameterDelegateMatchType.FILTER;
            if (maxCount == 0)
            {
                defaultMatch = ParameterDelegateMatchType.NONE;
            }

            // Standard definition
            if (configNode.HasValue("part") || configNode.HasValue("partModule") || configNode.HasValue("category") || configNode.HasValue("manufacturer"))
            {
                PartValidation.Filter filter = new PartValidation.Filter(defaultMatch);
                valid &= ConfigNodeUtil.ParseValue<AvailablePart>(configNode, "part", x => filter.part = x, this, (AvailablePart)null);
                valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "partModule", x => filter.partModules = x, this, new List<string>(), x => x.All(Validation.ValidatePartModule));
                valid &= ConfigNodeUtil.ParseValue<PartCategories?>(configNode, "category", x => filter.category = x, this, (PartCategories?)null);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "manufacturer", x => filter.manufacturer = x, this, (string)null);
                filters.Add(filter);
            }

            // Extended definition
            foreach (ConfigNode child in configNode.GetNodes())
            {
                ParameterDelegateMatchType matchType;
                if (child.name == "FILTER")
                {
                    matchType = ParameterDelegateMatchType.FILTER;
                }
                else if (child.name == "VALIDATE")
                {
                    matchType = ParameterDelegateMatchType.VALIDATE;
                }
                else if (child.name == "VALIDATE_ALL")
                {
                    matchType = ParameterDelegateMatchType.VALIDATE_ALL;
                }
                else if (child.name == "NONE")
                {
                    matchType = ParameterDelegateMatchType.NONE;
                }
                else
                {
                    LoggingUtil.LogError(this, ErrorPrefix() + ": unexpected node '" + child.name + "'.");
                    valid = false;
                    continue;
                }

                if (defaultMatch == ParameterDelegateMatchType.NONE)
                {
                    matchType = ParameterDelegateMatchType.NONE;
                }

                PartValidation.Filter filter = new PartValidation.Filter(matchType);
                valid &= ConfigNodeUtil.ParseValue<AvailablePart>(child, "part", x => filter.part = x, this, (AvailablePart)null);
                valid &= ConfigNodeUtil.ParseValue<List<string>>(child, "partModule", x => filter.partModules = x, this, new List<string>(), x => x.All(Validation.ValidatePartModule));
                valid &= ConfigNodeUtil.ParseValue<PartCategories?>(child, "category", x => filter.category = x, this, (PartCategories?)null);
                valid &= ConfigNodeUtil.ParseValue<string>(child, "manufacturer", x => filter.manufacturer = x, this, (string)null);

                if (matchType == ParameterDelegateMatchType.VALIDATE)
                {
                    valid &= ConfigNodeUtil.ParseValue<int>(child, "minCount", x => filter.minCount = x, this, 1, x => Validation.GE(x, 0));
                    valid &= ConfigNodeUtil.ParseValue<int>(child, "maxCount", x => filter.maxCount = x, this, int.MaxValue, x => Validation.GE(x, 0));
                }

                filters.Add(filter);
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new PartValidation(filters, failWhenUnmet, minCount, maxCount, title);
        }
    }
}
