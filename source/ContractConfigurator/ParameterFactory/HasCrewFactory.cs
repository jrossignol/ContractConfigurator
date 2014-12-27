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
    /*
     * ParameterFactory wrapper for HasCrew ContractParameter.
     */
    public class HasCrewFactory : ParameterFactory
    {
        protected string trait;
        protected int minExperience;
        protected int maxExperience;
        protected int minCrew;
        protected int maxCrew;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "trait", ref trait, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "minExperience", ref minExperience, this, 0, x => Validation.Between(x, 0, 5));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxExperience", ref maxExperience, this, 5, x => Validation.Between(x, 0, 5));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "minCrew", ref minCrew, this, 1, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCrew", ref maxCrew, this, int.MaxValue, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minExperience", "maxExperience", "minCrew", "maxCrew" }, this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new HasCrew(title, trait, minCrew, maxCrew, minExperience, maxExperience);
        }
    }
}
