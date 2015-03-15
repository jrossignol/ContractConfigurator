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
using System.Text.RegularExpressions;

namespace ContractConfigurator
{
    /// <summary>
    /// Class used for all Contract Configurator contracts.
    /// </summary>
    public class ConfiguredContract : Contract
    {
        public ContractType contractType { get; private set; }
        private List<ContractBehaviour> behaviours = new List<ContractBehaviour>();
        public IEnumerable<ContractBehaviour> Behaviours { get { return behaviours.AsReadOnly(); } }

		public static string contractTypeName(Contract c)
		{
			try
			{
				ConfiguredContract CC = (ConfiguredContract)c;
				if (CC.contractType != null)
				{
					return CC.contractType.name;
				}
				else
				{
					return "";
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning("Something something can't cast to type + " e);
				return "";
			}
		}

        protected string title;
        protected string description;
        protected string synopsis;
        protected string completedMessage;
        protected string notes;

        private static int lastGenerationFailure = 0;
        private static Dictionary<ContractPrestige, int> lastSpecificGenerationFailure = new Dictionary<ContractPrestige, int>();

        public static ConfiguredContract currentContract = null;

        protected override bool Generate()
        {
            // MeetsRequirement gets called first and sets the contract type, but check it and
            // select the contract type just in case.
            if (contractType == null)
            {
                if (!SelectContractType())
                {
                    return false;
                }
            }

            LoggingUtil.LogDebug(this.GetType(), "Generating contract: " + contractType);

            // Set the agent
            if (contractType.agent != null)
            {
                agent = contractType.agent;
            }

            // Set the contract expiry
            if (contractType.maxExpiry == 0.0f)
            {
                SetExpiry();
                expiryType = DeadlineType.None;
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

            // Copy text from contract type
            title = contractType.title;
            synopsis = contractType.synopsis;
            completedMessage = contractType.completedMessage;
            notes = contractType.notes;

            // Set description
            if (string.IsNullOrEmpty(contractType.description))
            {
                // Generate the contract description
                description = TextGen.GenerateBackStories(agent.Name, agent.GetMindsetString(),
                    contractType.topic, contractType.subject, contractType.motivation, MissionSeed);
            }
            else
            {
                description = contractType.description;
            }

            // Generate behaviours
            behaviours = new List<ContractBehaviour>();
            if (!contractType.GenerateBehaviours(this))
            {
                return false;
            }

            // Generate parameters
            try
            {
                if (!contractType.GenerateParameters(this))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                LoggingUtil.LogException(new Exception("Failed generating parameters for " + contractType, e));
                return false;
            }

            LoggingUtil.LogInfo(this.GetType(), "Generated contract: " + contractType);
            return true;
        }

        /// <summary>
        /// Adds a new behaviour to our list.
        /// </summary>
        /// <param name="behaviour">The behaviour to add</param>
        public void AddBehaviour(ContractBehaviour behaviour)
        {
            behaviour.contract = this;
            behaviours.Add(behaviour);
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
            return ((contractType != null ? contractType.name : "null") + MissionSeed.ToString() + DateAccepted.ToString());
        }

        protected override string GetTitle()
        {
            return title;
        }

        protected override string GetDescription()
        {
            return description;
        }

        protected override string GetSynopsys()
        {
            return synopsis ?? "";
        }

        protected override string MessageCompleted()
        {
            return completedMessage ?? "";
        }

        protected override string GetNotes()
        {
            return string.IsNullOrEmpty(notes) ? "" : notes + "\n";
        }
        
        protected override void OnLoad(ConfigNode node)
        {
            try
            {
                contractType = ContractType.GetContractType(node.GetValue("subtype"));
                title = ConfigNodeUtil.ParseValue<string>(node, "title", contractType.title);
                description = ConfigNodeUtil.ParseValue<string>(node, "description", contractType.description);
                synopsis = ConfigNodeUtil.ParseValue<string>(node, "synopsis", contractType.synopsis);
                completedMessage = ConfigNodeUtil.ParseValue<string>(node, "completedMessage", contractType.completedMessage);
                notes = ConfigNodeUtil.ParseValue<string>(node, "notes", contractType.notes);

                foreach (ConfigNode child in node.GetNodes("BEHAVIOUR"))
                {
                    ContractBehaviour behaviour = ContractBehaviour.LoadBehaviour(child, this);
                    behaviours.Add(behaviour);
                }
            }
            catch (Exception e)
            {
                LoggingUtil.LogError(this, "Error loading contract from persistance file!");
                LoggingUtil.LogException(e);
                ExceptionLogWindow.DisplayFatalException(ExceptionLogWindow.ExceptionSituation.CONTRACT_LOAD, e, this);

                try
                {
                    SetState(State.Failed);
                }
                catch { }
            }
        }

        protected override void OnSave(ConfigNode node)
        {
            try
            {
                node.AddValue("subtype", contractType.name);
                node.AddValue("title", title);
                node.AddValue("description", description);
                node.AddValue("synopsis", synopsis);
                node.AddValue("completedMessage", completedMessage);
                node.AddValue("notes", notes);

                foreach (ContractBehaviour behaviour in behaviours)
                {
                    ConfigNode child = new ConfigNode("BEHAVIOUR");
                    behaviour.Save(child);
                    node.AddNode(child);
                }
            }
            catch (Exception e)
            {
                LoggingUtil.LogError(this, "Error saving contract to persistance file!");
                LoggingUtil.LogException(e);
                ExceptionLogWindow.DisplayFatalException(ExceptionLogWindow.ExceptionSituation.CONTRACT_SAVE, e, this);

                SetState(State.Failed);
            }
        }

        public override bool MeetRequirements()
        {
            // No ContractType chosen
            if (contractType == null)
            {
                return SelectContractType();
            }
            else 
            {
                // ContractType already chosen, check if still meets requirements.
                bool meets = contractType.MeetRequirements(this);
                if (ContractState == State.Active && !meets)
                {
                    LoggingUtil.LogWarning(this, "Removed contract '" + title + "', as it no longer meets the requirements.");
                }
                return meets;
            }
        }

        private bool SelectContractType()
        {
            if (!lastSpecificGenerationFailure.ContainsKey(prestige))
            {
                lastSpecificGenerationFailure[prestige] = 0;
            }

            // Build a weighted list of ContractTypes to choose from
            Dictionary<ContractType, double> validContractTypes = new Dictionary<ContractType, double>();
            double totalWeight = 0.0;
            foreach (ContractType ct in ContractType.AllValidContractTypes)
            {
                LoggingUtil.LogVerbose(this, "Checking ContractType = " + ct.name);
                // KSP tries to generate new contracts *incessantly*, to the point where this becomes
                // a real performance problem.  So if we run into a situation where we did not
                // generate a contract and we are asked AGAIN after a very short time, then do
                // some logic to prevent re-checking uselessly.

                // If there was any generation failure within the last 100 frames, only look at
                // contracts specific to that prestige level
                if (lastGenerationFailure + 100 > Time.frameCount)
                {
                    // If there was a generation failure for this specific prestige level in
                    // the last 100 frames, then just skip the checks entirely
                    if (lastSpecificGenerationFailure[prestige] + 100 > Time.frameCount)
                    {
                        return false;
                    }

                    if (ct.prestige.Count > 0 && ct.prestige.Contains(prestige))
                    {
                        validContractTypes.Add(ct, ct.weight);
                        totalWeight += ct.weight;
                    }
                }
                else
                {
                    validContractTypes.Add(ct, ct.weight);
                    totalWeight += ct.weight;
                }
            }

            // Loop until we either run out of contracts in our list or make a selection
            System.Random generator = new System.Random(this.MissionSeed);
            while (validContractTypes.Count > 0)
            {
                ContractType selectedContractType = null;
                // Pick one of the contract types based on their weight
                double value = generator.NextDouble() * totalWeight;
                foreach (KeyValuePair<ContractType, double> pair in validContractTypes)
                {
                    value -= pair.Value;
                    if (value <= 0.0)
                    {
                        selectedContractType = pair.Key;
                        break;
                    }
                }

                // Shouldn't happen, but floating point rounding could put us here
                if (selectedContractType == null)
                {
                    selectedContractType = validContractTypes.First().Key;
                }

                // Try to refresh non-deterministic values before we check requirements
                currentContract = this;
                if (!ConfigNodeUtil.UpdateNonDeterministicValues(selectedContractType.dataNode))
                {
                    LoggingUtil.LogVerbose(this, selectedContractType.name + " was not generated: non-deterministic expression failure.");
                    validContractTypes.Remove(selectedContractType);
                    totalWeight -= selectedContractType.weight;
                }
                currentContract = null;

                // Check the requirements for our selection
                if (selectedContractType.MeetRequirements(this))
                {
                    contractType = selectedContractType;
                    return true;
                }
                // Remote the selection, and try again
                else
                {
                    validContractTypes.Remove(selectedContractType);
                    totalWeight -= selectedContractType.weight;
                }
            }

            // Set our failure markers
            lastGenerationFailure = Time.frameCount;
            lastSpecificGenerationFailure[prestige] = Time.frameCount;

            return false;
        }

        public override string MissionControlTextRich()
        {
            // Remove the stuff that's supposed to be hidden from the mission control text
            string str = base.MissionControlTextRich();
            str = Regex.Replace(str, "\r", "");
            str = Regex.Replace(str, "<b><#......>:.*?\n\n", "", RegexOptions.Singleline);
            return str;
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
            
            // Stock seems to have issues with setting this correctly.
            // TODO - check post-0.25 to see if this is still necessary as a workaround
            dateFinished = Planetarium.GetUniversalTime();

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

        protected void OnParameterStateChange(Contract contract, ContractParameter param)
        {
            if (contract == this)
            {
                OnParameterStateChange(param);
            }
        }
            
        protected override void OnParameterStateChange(ContractParameter param)
        {
            base.OnParameterStateChange(param);
            foreach (ContractBehaviour behaviour in behaviours)
            {
                behaviour.ParameterStateChange(param);
            }

            // Check for completion - stock ignores the optional flag
            bool completed = true;
            foreach (ContractParameter child in this.GetChildren())
            {
                if (child.State != ParameterState.Complete && !child.Optional)
                {
                    completed = false;
                }
            }
            if (completed)
            {
                SetState(Contract.State.Completed);
            }
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            ContractConfigurator.OnParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterStateChange));
            foreach (ContractBehaviour behaviour in behaviours)
            {
                behaviour.Register();
            }
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            ContractConfigurator.OnParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterStateChange));
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

        public override string ToString()
        {
            return contractType.name;
        }
    }
}