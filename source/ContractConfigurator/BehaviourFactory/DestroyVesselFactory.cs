using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;
using ContractConfigurator.ExpressionParser;
namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// BehaviourFactory wrapper for DestroyVessel ContractBehaviour.
    /// </summary>
    public class DestroyVesselFactory : BehaviourFactory
    {
        protected List<string> vessels;
        protected TriggeredBehaviour.State onState;
        protected List<string> parameter = new List<string>();

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<TriggeredBehaviour.State>(configNode, "onState", x => onState = x, this, TriggeredBehaviour.State.CONTRACT_SUCCESS);
            if (onState == TriggeredBehaviour.State.PARAMETER_COMPLETED || onState == TriggeredBehaviour.State.PARAMETER_FAILED)
            {
                valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "parameter", x => parameter = x, this);
            }
            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "vessel", x => vessels = x, this);

            return valid;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new DestroyVessel(onState, vessels, parameter);
        }
    }
}
