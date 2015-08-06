using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Behaviour for adding/removing an experimental part.
    /// </summary>
    public class ExperimentalPart : ContractBehaviour
    {
        public enum UnlockCriteria
        {
            DO_NOT_UNLOCK,
            CONTRACT_ACCEPTANCE,
            CONTRACT_COMPLETION,
            PARAMETER_COMPLETION
        }

        public enum LockCriteria
        {
            DO_NOT_LOCK,
            CONTRACT_ACCEPTANCE,
            CONTRACT_COMPLETION,
            PARAMETER_COMPLETION
        }

        protected List<AvailablePart> parts;
        protected ExperimentalPart.UnlockCriteria unlockCriteria;
        protected string unlockParameter;
        protected ExperimentalPart.LockCriteria lockCriteria;
        protected string lockParameter;

        public ExperimentalPart()
            : base()
        {
        }

        public ExperimentalPart(List<AvailablePart> parts, UnlockCriteria unlockCriteria, string unlockParameter, LockCriteria lockCriteria, string lockParameter)
        {
            this.parts = parts;
            this.unlockCriteria = unlockCriteria;
            this.unlockParameter = unlockParameter;
            this.lockCriteria = lockCriteria;
            this.lockParameter = lockParameter;
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.Contract.onParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
            ContractConfigurator.OnParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.Contract.onParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
            ContractConfigurator.OnParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }
        
        protected override void OnAccepted()
        {
            if (unlockCriteria == UnlockCriteria.CONTRACT_ACCEPTANCE)
            {
                UnlockParts();
            }
            if (lockCriteria == LockCriteria.CONTRACT_ACCEPTANCE)
            {
                LockParts();
            }
        }

        protected override void OnCancelled()
        {
            if (unlockCriteria != UnlockCriteria.DO_NOT_UNLOCK)
            {
                LockParts();
            }
        }

        protected override void OnDeadlineExpired()
        {
            if (unlockCriteria != UnlockCriteria.DO_NOT_UNLOCK)
            {
                LockParts();
            }
        }

        protected override void OnFailed()
        {
            if (unlockCriteria != UnlockCriteria.DO_NOT_UNLOCK)
            {
                LockParts();
            }
        }

        protected override void OnCompleted()
        {
            if (unlockCriteria == UnlockCriteria.CONTRACT_COMPLETION)
            {
                UnlockParts();
            }

            if (lockCriteria == LockCriteria.CONTRACT_COMPLETION)
            {
                LockParts();
            }
        }

        protected void OnParameterChange(Contract c, ContractParameter p)
        {
            if (c != contract)
            {
                return;
            }

            if (p.ID == unlockParameter && unlockCriteria == UnlockCriteria.PARAMETER_COMPLETION)
            {
                UnlockParts();
            }
            if (p.ID == lockParameter && lockCriteria == LockCriteria.PARAMETER_COMPLETION)
            {
                LockParts();
            }
        }

        protected void UnlockParts()
        {
            foreach (AvailablePart part in parts)
            {
                ResearchAndDevelopment.AddExperimentalPart(part);
            }
        }

        protected void LockParts()
        {
            foreach (AvailablePart part in parts)
            {
                ResearchAndDevelopment.RemoveExperimentalPart(part);
            }
        }

        protected override void OnSave(ConfigNode configNode)
        {
            foreach (AvailablePart part in parts)
            {
                configNode.AddValue("part", part.name);
            }
            configNode.AddValue("unlockCriteria", unlockCriteria);
            if (!string.IsNullOrEmpty(unlockParameter))
            {
                configNode.AddValue("unlockParameter", unlockParameter);
            }
            configNode.AddValue("lockCriteria", lockCriteria);
            if (!string.IsNullOrEmpty(lockParameter))
            {
                configNode.AddValue("lockParameter", lockParameter);
            }
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            parts = ConfigNodeUtil.ParseValue<List<AvailablePart>>(configNode, "part");

            if (configNode.HasValue("remove"))
            {
                bool remove = ConfigNodeUtil.ParseValue<bool>(configNode, "remove");
                lockCriteria = remove ? ExperimentalPart.LockCriteria.CONTRACT_ACCEPTANCE : ExperimentalPart.LockCriteria.DO_NOT_LOCK;
            }
            else
            {
                lockCriteria = ConfigNodeUtil.ParseValue<LockCriteria>(configNode, "lockCriteria");
                lockParameter = ConfigNodeUtil.ParseValue<string>(configNode, "lockParameter", "");
            }

            if (configNode.HasValue("add"))
            {
                bool add = ConfigNodeUtil.ParseValue<bool>(configNode, "add");
                unlockCriteria = add ? ExperimentalPart.UnlockCriteria.CONTRACT_ACCEPTANCE : ExperimentalPart.UnlockCriteria.DO_NOT_UNLOCK;
            }
            else
            {
                unlockCriteria = ConfigNodeUtil.ParseValue<UnlockCriteria>(configNode, "unlockCriteria");
                unlockParameter = ConfigNodeUtil.ParseValue<string>(configNode, "unlockParameter", "");
            }
        }
    }
}
