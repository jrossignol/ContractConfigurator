using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP;
using KSPAchievements;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator
{
    public class ConfiguredContract : Contract
    {
        public ContractType contractType { get; private set; }
        protected List<ContractBehaviour> behaviours { get; set; }

        protected override bool Generate()
        {
            // Build of list of ContractTypes to choose from
            List<ContractType> validContractTypes = new List<ContractType>();
            foreach (ContractType ct in ContractType.contractTypes.Values)
            {
                // Only add contract types that have their requirements met
                if (ct.MeetRequirements(this))
                {
                    validContractTypes.Add(ct);
                }
            }

            // No contract to generate!
            if (validContractTypes.Count == 0)
            {
                Debug.Log("ContractConfigurator: No currently valid contract types to generate.");
                return false;
            }

            // Pick one of the contract types
            System.Random generator = new System.Random(this.MissionSeed);
            contractType = validContractTypes[generator.Next(0, validContractTypes.Count())];

            // Set the agent
            if (contractType.agent != null)
            {
                agent = contractType.agent;
            }

            // Set the contract expiry
            if (contractType.minExpiry == 0.0f)
            {
                SetExpiry();
            }
            else
            {
                SetExpiry(contractType.minExpiry, contractType.maxExpiry);
            }

            // Set the contract deadline
            if (contractType.deadline == 0.0f)
            {
                deadlineType = Contract.DeadlineType.None;
            }
            else
            {
                SetDeadlineDays(contractType.deadline, contractType.targetBody);
            }

            // Set rewards
            SetScience(contractType.rewardScience, contractType.targetBody);
            SetReputation(contractType.rewardReputation, contractType.failureReputation, contractType.targetBody);
            SetFunds(contractType.advanceFunds, contractType.rewardFunds, contractType.failureFunds, contractType.targetBody);

            // Generate behaviours
            behaviours = new List<ContractBehaviour>();
            contractType.GenerateBehaviours(this);

            // Generate parameters
            contractType.GenerateParameters(this);

            Debug.Log("ContractConfigurator: Generated a contract: " + contractType);
            return true;
        }

        /*
         * Adds a new behaviour to our list.
         */
        public void AddBehaviour(ContractBehaviour behaviour)
        {
            behaviours.Add(behaviour);
            behaviour.contract = this;
        }

        public override bool CanBeCancelled()
        {
            return contractType.cancellable;
        }

        public override bool CanBeDeclined()
        {
            return contractType.declinable;
        }

        protected override string GetHashString()
        {
            return (this.contractType.name + this.MissionSeed.ToString() + this.DateAccepted.ToString());
        }

        protected override string GetTitle()
        {
            return contractType.title;
        }

        protected override string GetDescription()
        {
            if (contractType.description == null || contractType.description == "")
            {
                // Generate the contract description
                return TextGen.GenerateBackStories(agent.Name, agent.GetMindsetString(),
                    contractType.topic, contractType.subject, contractType.motivation, MissionSeed);
            }
            else
            {
                return contractType.description;
            }
        }

        protected override string GetSynopsys()
        {
            return contractType.synopsis;
        }

        protected override string MessageCompleted()
        {
            return contractType.completedMessage;
        }

        protected override string GetNotes()
        {
            return contractType.notes + "\n";
        }
        
        protected override void OnLoad(ConfigNode node)
        {
            contractType = ContractType.contractTypes[node.GetValue("subtype")];
            foreach (ConfigNode child in node.GetNodes("BEHAVIOUR"))
            {
                ContractBehaviour behaviour = ContractBehaviour.LoadBehaviour(child);
                behaviours.Add(behaviour);
            }
        }

        protected override void OnSave(ConfigNode node)
        {
            node.AddValue("subtype", contractType.name);
            foreach (ContractBehaviour behaviour in behaviours)
            {
                ConfigNode child = new ConfigNode("BEHAVIOUR");
                behaviour.Save(child);
            }
        }

        public override bool MeetRequirements()
        {
            // Uninitialized contract - always meets requirement
            if (contractType == null)
            {
                return true;
            }
            // Initialized contract - may no longer meet the requirement
            else
            {
                return contractType.MeetRequirements(this);
            }
        }

        //
        // These methods all fall through to the various ContractBehaviour objects.
        //

        protected override void OnAccepted()
        {
            base.OnAccepted();
            foreach (ContractBehaviour behaviour in behaviours)
            {
                behaviour.Accept();
            }
        }

        protected override void OnCancelled()
        {
            base.OnCancelled();
            foreach (ContractBehaviour behaviour in behaviours)
            {
                behaviour.Cancel();
            }
        }

        protected override void OnCompleted()
        {
            base.OnCompleted();
            foreach (ContractBehaviour behaviour in behaviours)
            {
                behaviour.Complete();
            }
        }

        protected override void OnDeadlineExpired()
        {
            base.OnDeadlineExpired();
            foreach (ContractBehaviour behaviour in behaviours)
            {
                behaviour.ExpireDeadline();
            }
        }

        protected override void OnDeclined()
        {
            base.OnDeclined();
            foreach (ContractBehaviour behaviour in behaviours)
            {
                behaviour.Decline();
            }
        }

        protected override void OnFailed()
        {
            base.OnFailed();
            foreach (ContractBehaviour behaviour in behaviours)
            {
                behaviour.Fail();
            }
        }

        protected override void OnFinished()
        {
            base.OnFinished();
            foreach (ContractBehaviour behaviour in behaviours)
            {
                behaviour.Finish();
            }
        }

        protected override void OnGenerateFailed()
        {
            base.OnGenerateFailed();
            foreach (ContractBehaviour behaviour in behaviours)
            {
                behaviour.FailGeneration();
            }
        }

        protected override void OnOffered()
        {
            base.OnOffered();
            foreach (ContractBehaviour behaviour in behaviours)
            {
                behaviour.Offer();
            }
        }

        protected override void OnOfferExpired()
        {
            base.OnOfferExpired();
            foreach (ContractBehaviour behaviour in behaviours)
            {
                behaviour.ExpireOffer();
            }
        }

        protected override void OnParameterStateChange(ContractParameter param)
        {
            base.OnParameterStateChange(param);
            foreach (ContractBehaviour behaviour in behaviours)
            {
                behaviour.ParameterStateChange(param);
            }
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            foreach (ContractBehaviour behaviour in behaviours)
            {
                behaviour.Register();
            }
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            foreach (ContractBehaviour behaviour in behaviours)
            {
                behaviour.Unregister();
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            foreach (ContractBehaviour behaviour in behaviours)
            {
                behaviour.Update();
            }
        }

        protected override void OnWithdrawn()
        {
            base.OnWithdrawn();
            foreach (ContractBehaviour behaviour in behaviours)
            {
                behaviour.Withdraw();
            }
        }
    }
}