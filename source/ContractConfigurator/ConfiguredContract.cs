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

            // Set the contract expiry, deadline
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

            // Generate parameters
            contractType.GenerateParameters(this);

            Debug.Log("ContractConfigurator: Generated a contract: " + contractType);
            return true;
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
            return contractType.description;
        }

        protected override string GetSynopsys()
        {
            return contractType.synopsis;
        }

        protected override string MessageCompleted()
        {
            return contractType.completedMessage;
        }

        protected override void OnLoad(ConfigNode node)
        {
            contractType = ContractType.contractTypes[node.GetValue("subtype")];
       }

        protected override void OnSave(ConfigNode node)
        {
            node.AddValue("subtype", contractType.name);
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
    }
}