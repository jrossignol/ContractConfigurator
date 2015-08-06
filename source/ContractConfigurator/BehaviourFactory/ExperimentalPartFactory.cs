using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// BehaviourFactory wrapper for ExperimentalPart ContractBehaviour.
    /// </summary>
    public class ExperimentalPartFactory : BehaviourFactory
    {
        protected List<AvailablePart> parts;
        protected ExperimentalPart.UnlockCriteria unlockCriteria;
        protected string unlockParameter;
        protected ExperimentalPart.LockCriteria lockCriteria;
        protected string lockParameter;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<ExperimentalPart.UnlockCriteria>(configNode, "unlockCriteria", x => unlockCriteria = x, this, ExperimentalPart.UnlockCriteria.CONTRACT_ACCEPTANCE);
            if (unlockCriteria == ExperimentalPart.UnlockCriteria.PARAMETER_COMPLETION)
            {
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "unlockParameter", x => unlockParameter = x, this);
            }
            valid &= ConfigNodeUtil.ParseValue<ExperimentalPart.LockCriteria>(configNode, "lockCriteria", x => lockCriteria = x, this, ExperimentalPart.LockCriteria.CONTRACT_COMPLETION);
            if (lockCriteria == ExperimentalPart.LockCriteria.PARAMETER_COMPLETION)
            {
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "lockParameter", x => lockParameter = x, this);
            }

            valid &= ConfigNodeUtil.ParseValue<List<AvailablePart>>(configNode, "part", x => parts = x, this);

            if (configNode.HasValue("add"))
            {
                LoggingUtil.LogWarning(this, "The 'add' attribute of ExperimentalPartFactory is deprecated.  Use 'unlockCriteria' instead.");
                valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "add", x => unlockCriteria = x ? ExperimentalPart.UnlockCriteria.CONTRACT_ACCEPTANCE : ExperimentalPart.UnlockCriteria.DO_NOT_UNLOCK, this);
            }
            if (configNode.HasValue("remove"))
            {
                LoggingUtil.LogWarning(this, "The 'remove' attribute of ExperimentalPartFactory is deprecated.  Use 'lockCriteria' instead.");
                valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "remove", x => lockCriteria = x ? ExperimentalPart.LockCriteria.CONTRACT_COMPLETION : ExperimentalPart.LockCriteria.DO_NOT_LOCK, this);
            }

            return valid;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new ExperimentalPart(parts, unlockCriteria, unlockParameter, lockCriteria, lockParameter);
        }
    }
}
