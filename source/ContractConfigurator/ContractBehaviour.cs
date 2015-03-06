using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;

namespace ContractConfigurator
{
    /// <summary>
    /// Class for assigning a particular behaviour to a contract.
    /// </summary>
    public class ContractBehaviour
    {
        public ConfiguredContract contract { get; set; }

        /// <summary>
        /// Loads a behaviour from a ConfigNode.
        /// </summary>
        /// <param name="configNode"></param>
        /// <param name="contract"></param>
        /// <returns></returns>
        public static ContractBehaviour LoadBehaviour(ConfigNode configNode, ConfiguredContract contract)
        {
            // Determine the type
            string typeName = configNode.GetValue("type");
            Type type = Type.GetType(typeName);
            if (type == null)
            {
                LoggingUtil.LogError(typeof(ContractBehaviour), "No ContractBehaviour with type = '" + typeName + "'.");
                return null;
            }

            // Instantiate and load
            ContractBehaviour behaviour = (ContractBehaviour)Activator.CreateInstance(type);
            behaviour.contract = contract;
            behaviour.Load(configNode);
            return behaviour;
        }

        //
        // Lots of methods that can be overriden!
        //

        public void Accept() { OnAccepted(); }
        protected virtual void OnAccepted() { }

        public void Cancel() { OnCancelled(); }
        protected virtual void OnCancelled() { }

        public void Complete() { OnCompleted(); }
        protected virtual void OnCompleted() { }

        public void ExpireDeadline() { OnDeadlineExpired(); }
        protected virtual void OnDeadlineExpired() { }

        public void Decline() { OnDeclined(); }
        protected virtual void OnDeclined() { }

        public void Fail() { OnFailed(); }
        protected virtual void OnFailed() { }

        public void Finish() { OnFinished(); }
        protected virtual void OnFinished() { }

        public void FailGeneration() { OnGenerateFailed(); }
        protected virtual void OnGenerateFailed() { }

        public void Offer() { OnOffered(); }
        protected virtual void OnOffered() { }

        public void ExpireOffer() { OnOfferExpired(); }
        protected virtual void OnOfferExpired() { }

        public void ParameterStateChange(ContractParameter param) { OnParameterStateChange(param); }
        protected virtual void OnParameterStateChange(ContractParameter param) { }

        public void Register() { OnRegister(); }
        protected virtual void OnRegister() { }

        public void Unregister() { OnUnregister(); }
        protected virtual void OnUnregister() { }

        public void Update() { OnUpdate(); }
        protected virtual void OnUpdate() { }

        public void Withdraw() { OnWithdrawn(); }
        protected virtual void OnWithdrawn() { }

        public void Load(ConfigNode configNode)
        {
            OnLoad(configNode);
        }
        protected virtual void OnLoad(ConfigNode configNode) { }

        public void Save(ConfigNode configNode)
        {
            configNode.AddValue("type", GetType());
            OnSave(configNode);
        }
        protected virtual void OnSave(ConfigNode configNode) { }
    }
}
