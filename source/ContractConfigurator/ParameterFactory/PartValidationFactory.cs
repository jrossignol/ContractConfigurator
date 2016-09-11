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
        protected int? minCount = null;
        protected int maxCount;
        protected List<PartValidation.Filter> filters = new List<PartValidation.Filter>();

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Read min/max first
            valid &= ConfigNodeUtil.ParseValue<int?>(configNode, "minCount", x => minCount = x, this,
                configNode.HasNode("VALIDATE") || configNode.HasNode("VALIDATE_ALL") || configNode.HasNode("NONE") ? (int?)null : 1, x => x == null || Validation.GE(x.Value, 0));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCount", x => maxCount = x, this, minCount != null && minCount.Value == 0 ? 0 : int.MaxValue, x => Validation.GE(x, 0));

            // Set the default match type
            ParameterDelegateMatchType defaultMatch = ParameterDelegateMatchType.FILTER;
            if (maxCount == 0)
            {
                defaultMatch = ParameterDelegateMatchType.NONE;
            }

            // Standard definition
            if (configNode.HasValue("part") || configNode.HasValue("partModule") || configNode.HasValue("partModuleType") || configNode.HasValue("category") || configNode.HasValue("manufacturer"))
            {
                PartValidation.Filter filter = new PartValidation.Filter(defaultMatch);
                valid &= ConfigNodeUtil.ParseValue<List<AvailablePart>>(configNode, "part", x => filter.parts = x, this, new List<AvailablePart>());
                valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "partModule", x => filter.partModules = x, this, new List<string>(), x => x.All(Validation.ValidatePartModule));
                valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "partModuleType", x => filter.partModuleTypes = x, this, new List<string>());
                valid &= ConfigNodeUtil.ParseValue<PartCategories?>(configNode, "category", x => filter.category = x, this, (PartCategories?)null);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "manufacturer", x => filter.manufacturer = x, this, (string)null);
                filters.Add(filter);
            }

            // Extended definition
            foreach (ConfigNode child in ConfigNodeUtil.GetChildNodes(configNode))
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
                valid &= ConfigNodeUtil.ParseValue<List<AvailablePart>>(child, "part", x => filter.parts = x, this, new List<AvailablePart>());
                valid &= ConfigNodeUtil.ParseValue<List<string>>(child, "partModule", x => filter.partModules = x, this, new List<string>(), x => x.All(Validation.ValidatePartModule));
                valid &= ConfigNodeUtil.ParseValue<List<string>>(child, "partModuleType", x => filter.partModuleTypes = x, this, new List<string>());
                valid &= ConfigNodeUtil.ParseValue<PartCategories?>(child, "category", x => filter.category = x, this, (PartCategories?)null);
                valid &= ConfigNodeUtil.ParseValue<string>(child, "manufacturer", x => filter.manufacturer = x, this, (string)null);

                foreach (ConfigNode moduleNode in child.GetNodes("MODULE"))
                {
                    ConfigNode.ValueList tmp = new ConfigNode.ValueList();
                    foreach (ConfigNode.Value v in moduleNode.values)
                    {
                        tmp.Add(new ConfigNode.Value(v.name, v.value));
                    }
                    filter.partModuleExtended.Add(tmp);
                }

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
            return new PartValidation(filters, minCount ?? 0, maxCount, title);
        }
    }
}
