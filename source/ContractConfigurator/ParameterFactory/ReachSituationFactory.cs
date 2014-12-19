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
     * ParameterFactory wrapper for ReachSituation ContractParameter.
     */
    public class ReachSituationFactory : ParameterFactory
    {
        protected Vessel.Situations situation { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get situation
            valid &= ConfigNodeUtil.ValidateMandatoryField(configNode, "situation", this);
            if (valid)
            {
                try
                {
                    string situationStr = configNode.GetValue("situation");
                    situation = (Vessel.Situations)Enum.Parse(typeof(Vessel.Situations), situationStr);
                }
                catch (Exception e)
                {
                    valid = false;
                    Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                        ": error parsing situation: " + e.Message);
                }
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new ReachSituationCustom(situation, title);
        }
    }
}
