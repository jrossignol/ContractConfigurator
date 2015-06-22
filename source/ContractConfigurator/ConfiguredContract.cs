using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using KSPAchievements;
using Contracts;
using Contracts.Agents;
using Contracts.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// Class used for all Contract Configurator contracts.
    /// </summary>
    public class ConfiguredContract : Contract
    {
        public ContractType contractType { get; set; }
        public string subType { get; set; }
        private List<ContractBehaviour> behaviours = new List<ContractBehaviour>();
        public IEnumerable<ContractBehaviour> Behaviours { get { return behaviours.AsReadOnly(); } }

        public Dictionary<string, object> uniqueData = new Dictionary<string, object>();

        public static System.Random random = new System.Random();

		public static string contractTypeName(Contract c)
		{
            if (c == null || c.GetType() != typeof(ConfiguredContract))
            {
                return "";
            }

            ConfiguredContract cc = (ConfiguredContract)c;
            return cc.contractType != null ? cc.contractType.name : "";
		}

        protected string title;
        protected string description;
        protected string synopsis;
        protected string completedMessage;
        protected string notes;

        public int hash { get; private set; }

        public static ConfiguredContract currentContract = null;

        public ConfiguredContract()
        {
        }

        public ConfiguredContract(Contract.ContractPrestige prestige)
        {
            this.prestige = prestige;
        }

        public bool Initialize(ContractType contractType)
        {
            LoggingUtil.LogLevel origLogLevel = LoggingUtil.logLevel;
            try
            {
                this.contractType = contractType;
                if (contractType.trace)
                {
                    LoggingUtil.logLevel = LoggingUtil.LogLevel.VERBOSE;
                }

                LoggingUtil.LogDebug(this.GetType(), "Initializing contract: " + contractType);

                // Set stuff from contract type
                subType = contractType.name;
                hash = contractType.hash;
                AutoAccept = contractType.autoAccept;

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

                // Set the agent
                if (contractType.agent != null)
                {
                    agent = contractType.agent;
                }
                else
                {
                    agent = AgentList.Instance.GetSuitableAgentForContract(this);
                }

                // Set description
                if (string.IsNullOrEmpty(contractType.description) && agent != null)
                {
                    // Generate the contract description
                    description = TextGen.GenerateBackStories(agent.Name, agent.GetMindsetString(),
                        contractType.topic, contractType.subject, contractType.motivation, random.Next());
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
                if (!contractType.GenerateParameters(this))
                {
                    return false;
                }

                LoggingUtil.LogVerbose(this.GetType(), "Initialized contract: " + contractType);
                return true;
            }
            catch (Exception e)
            {
                LoggingUtil.LogError(this, "Error initializing contract " + contractType);
                LoggingUtil.LogException(e);
                ExceptionLogWindow.DisplayFatalException(ExceptionLogWindow.ExceptionSituation.CONTRACT_GENERATION, e,
                    contractType == null ? "unknown" : contractType.name);

                return false;
            }
            finally
            {
                LoggingUtil.logLevel = origLogLevel;
            }
        }

        protected override bool Generate()
        {
            // Special case for pre-loader
            if (ContractState == State.Withdrawn)
            {
                return true;
            }

            try
            {
                if (contractType != null)
                {
                    return true;
                }
                else
                {
                    LoggingUtil.LogVerbose(this, "Generate()");
                    return ContractPreLoader.Instance.GenerateContract(this);
                }
            }
            catch (Exception e)
            {
                LoggingUtil.LogError(this, "Error generating contract!");
                LoggingUtil.LogException(e);
                ExceptionLogWindow.DisplayFatalException(ExceptionLogWindow.ExceptionSituation.CONTRACT_GENERATION, e,
                    contractType == null ? "unknown" : contractType.name);

                try
                {
                    GenerateFailed();
                }
                catch { }

                return false;
            }
        }

        /// <summary>
        /// Performs a shallow copy of the contract details from the given contract
        /// </summary>
        /// <param name="contract">The contract to copy from.</param>
        public void CopyFrom(ConfiguredContract contract)
        {
            // Copy details from the original
            contractType = contract.contractType;
            subType = contract.subType;
            hash = contract.hash;
            AutoAccept = contract.AutoAccept;
            dateExpire = contract.dateExpire;
            TimeExpiry = contract.TimeExpiry;
            expiryType = contract.expiryType;
            dateDeadline = contract.dateDeadline;
            TimeDeadline = contract.TimeDeadline;
            deadlineType = contract.deadlineType;
            FundsAdvance = contract.FundsAdvance;
            FundsCompletion = contract.FundsCompletion;
            FundsFailure = contract.FundsFailure;
            ReputationCompletion = contract.ReputationCompletion;
            ReputationFailure = contract.ReputationFailure;
            ScienceCompletion = contract.ScienceCompletion;
            title = contract.title;
            synopsis = contract.synopsis;
            completedMessage = contract.completedMessage;
            notes = contract.notes;
            agent = contract.agent;
            description = contract.description;
            uniqueData = contract.uniqueData;

            // Copy behaviours
            behaviours = contract.behaviours;
            contract.behaviours = new List<ContractBehaviour>();

            // Copy parameters
            for (int i = 0; i < contract.ParameterCount; i++)
            {
                // Save the old parameter
                ConfigNode node = new ConfigNode("PARAMETER");
                ContractParameter origParameter = contract.GetParameter(i);
                origParameter.Save(node);

                // Load into a new copy
                ContractParameter parameter = (ContractParameter) Activator.CreateInstance(origParameter.GetType());
                AddParameter(parameter, null);
                parameter.Load(node);
            }

            // Run the OnOffered for behaviours
            OnOffered();

            LoggingUtil.LogInfo(this, "Generated contract: " + contractType);
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
            return contractType != null ? contractType.cancellable : true;
        }

        public override bool CanBeDeclined()
        {
            return contractType != null ? contractType.declinable : true;
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
                subType = node.GetValue("subtype");
                contractType = string.IsNullOrEmpty(subType) ? null : ContractType.GetContractType(subType);
                title = ConfigNodeUtil.ParseValue<string>(node, "title", contractType != null ? contractType.title : subType);
                description = ConfigNodeUtil.ParseValue<string>(node, "description", contractType != null ? contractType.description : "");
                synopsis = ConfigNodeUtil.ParseValue<string>(node, "synopsis", contractType != null ? contractType.synopsis : "");
                completedMessage = ConfigNodeUtil.ParseValue<string>(node, "completedMessage", contractType != null ? contractType.completedMessage : "");
                notes = ConfigNodeUtil.ParseValue<string>(node, "notes", contractType != null ? contractType.notes : "");
                hash = ConfigNodeUtil.ParseValue<int>(node, "hash", contractType != null ? contractType.hash : 0);

                // Load the unique data
                ConfigNode dataNode = node.GetNode("UNIQUE_DATA");
                if (dataNode != null)
                {
                    // Handle individual values
                    foreach (ConfigNode.Value pair in dataNode.values)
                    {
                        string typeName = pair.value.Remove(pair.value.IndexOf(":"));
                        string value = pair.value.Substring(typeName.Length + 1);
                        Type type = ConfigNodeUtil.ParseTypeValue(typeName);

                        if (type == typeof(string))
                        {
                            uniqueData[pair.name] = value;
                        }
                        else
                        {
                            // Get the ParseValue method
                            MethodInfo parseValueMethod = typeof(ConfigNodeUtil).GetMethods().Where(m => m.Name == "ParseSingleValue").Single();
                            parseValueMethod = parseValueMethod.MakeGenericMethod(new Type[] { type });

                            // Invoke the ParseValue method
                            uniqueData[pair.name] = parseValueMethod.Invoke(null, new object[] { pair.name, value, false });
                        }
                    }
                }

                foreach (ConfigNode child in node.GetNodes("BEHAVIOUR"))
                {
                    ContractBehaviour behaviour = ContractBehaviour.LoadBehaviour(child, this);
                    behaviours.Add(behaviour);
                }

                // If the contract type is null, then it likely means that it was uninstalled
                if (contractType == null)
                {
                    LoggingUtil.LogWarning(this, "Error loading contract for contract type '" + subType +
                        "'.  The contract type either failed to load or was uninstalled.");
                    try
                    {
                        if (ContractState == State.Active || ContractState == State.Offered)
                        {
                            SetState(ContractState == State.Active ? State.Failed : State.Withdrawn);
                        }
                    }
                    catch { }
                    return;
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
                node.AddValue("subtype", subType);
                node.AddValue("title", title);
                node.AddValue("description", description);
                node.AddValue("synopsis", synopsis);
                node.AddValue("completedMessage", completedMessage);
                node.AddValue("notes", notes);
                node.AddValue("hash", hash);

                // Store the unique data
                if (uniqueData.Any())
                {
                    ConfigNode dataNode = new ConfigNode("UNIQUE_DATA");
                    node.AddNode(dataNode);
                    foreach (KeyValuePair<string, object> p in uniqueData.Where(p => p.Value != null))
                    {
                        string value;
                        Type type = p.Value.GetType();
                        if (type == typeof(CelestialBody))
                        {
                            value = ((CelestialBody)p.Value).name;
                        }
                        else if (type == typeof(Vessel))
                        {
                            value = ((Vessel)p.Value).id.ToString();
                        }
                        else if (type == typeof(ScienceSubject))
                        {
                            value = ((ScienceSubject)p.Value).id;
                        }
                        else
                        {
                            value = p.Value.ToString();
                        }

                        dataNode.AddValue(p.Key, type.Name + ":" + value);
                    }
                }

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
            // ContractType already chosen, check if still meets requirements.
            if (contractType != null)
            {
                bool meets = contractType.MeetRequirements(this);
                if (ContractState == State.Active && !meets)
                {
                    LoggingUtil.LogWarning(this, "Removed contract '" + title + "', as it no longer meets the requirements.");
                }
                return meets;
            }
            else if (ContractState == State.Withdrawn)
            {
                // Special case for pre-loader
                return true;
            }
            // No ContractType chosen
            LoggingUtil.LogVerbose(this, "MeetRequirements()");
            return ContractPreLoader.Instance.GenerateContract(this);
        }

        public override string MissionControlTextRich()
        {
            // Remove the stuff that's supposed to be hidden from the mission control text
            string str = base.MissionControlTextRich();
            str = Regex.Replace(str, "\r", "");
            str = Regex.Replace(str, @"<b><#......>*:.*?\n\n", "", RegexOptions.Singleline);
            str = Regex.Replace(str, @"<b><#......>\s*:.*?\n", "", RegexOptions.Singleline);
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
            return contractType != null ? contractType.name : "unknown";
        }
    }
}