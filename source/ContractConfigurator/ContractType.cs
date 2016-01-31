using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Agents;
using ContractConfigurator.ExpressionParser;

// ++++++++++++++++++++++++++++++++++
using System.IO;
using System.Text.RegularExpressions;
// ++++++++++++++++++++++++++++++++++

namespace ContractConfigurator
{
    public class ContractRequirementException : Exception
    {
        public ContractRequirementException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Class for capturing all contract type details.
    /// </summary>
    public class ContractType : IContractConfiguratorFactory
    {
		// ++++++++++++++++++++++++++++++++++
		// Following lines added by LinuxGuruGamer
		private const string ccLogFile = @"cc_output.log";
		private static bool deleteCCLogfile = true;
		// ++++++++++++++++++++++++++++++++++

        private static Dictionary<string, ContractType> contractTypes = new Dictionary<string,ContractType>();
        public static IEnumerable<ContractType> AllContractTypes { get { return contractTypes.Values; } }
        public static IEnumerable<ContractType> AllValidContractTypes
        {
            get
            {
                return contractTypes.Values.Where(ct => ct.enabled);
            }
        }
        public static IEnumerable<string> AllValidContractTypeNames
        {
            get
            {
                return AllValidContractTypes.Select<ContractType, string>(ct => ct.name);
            }
        }
        
        public static ContractType GetContractType(string name)
        {
            if (contractTypes.ContainsKey(name))
            {
                return contractTypes[name];
            }
            return null;
        }

        public static void ClearContractTypes()
        {
            contractTypes.Clear();
        }

        protected List<ParameterFactory> paramFactories = new List<ParameterFactory>();
        protected List<BehaviourFactory> behaviourFactories = new List<BehaviourFactory>();
        protected List<ContractRequirement> requirements = new List<ContractRequirement>();

        public IEnumerable<ParameterFactory> ParamFactories { get { return paramFactories; } }
        public IEnumerable<BehaviourFactory> BehaviourFactories { get { return behaviourFactories; } }
        public IEnumerable<ContractRequirement> Requirements { get { return requirements; } }

        public bool expandInDebug = false;
        public bool hasWarnings { get; set; }
        public Type iteratorType { get; set; }
        public string iteratorKey { get; set; }
        public bool enabled { get; private set; }
        public string config { get; private set; }
        public int hash { get; private set; }
        public string log { get; private set; }
        public DataNode dataNode { get; private set; }

        // Contract attributes
        public string name;
        public ContractGroup group;
        public string title;
        public string tag;
        public string notes;
        public string description;
        public string topic;
        public string subject;
        public string motivation;
        public string synopsis;
        public string completedMessage;
        public Agent agent;
        public float minExpiry;
        public float maxExpiry;
        public float deadline;
        public bool cancellable;
        public bool declinable;
        public bool autoAccept;
        public List<Contract.ContractPrestige> prestige;
        public CelestialBody targetBody;
        protected List<CelestialBody> targetBodies;
        protected Vessel targetVessel;
        protected List<Vessel> targetVessels;
        protected Kerbal targetKerbal;
        protected List<Kerbal> targetKerbals;
        public int maxCompletions;
        public int maxSimultaneous;
        public float rewardScience;
        public float rewardReputation;
        public float rewardFunds;
        public float failureReputation;
        public float failureFunds;
        public float advanceFunds;
        public double weight;
        public bool trace = false;
        public bool loaded = false;

        private Dictionary<string, bool> dataValues = new Dictionary<string, bool>();
        public Dictionary<string, DataNode.UniquenessCheck> uniquenessChecks = new Dictionary<string, DataNode.UniquenessCheck>();

        public ContractType(string name)
        {
            this.name = name;
            contractTypes.Add(name, this);

            // Member defaults
            group = null;
            agent = null;
            minExpiry = 0;
            maxExpiry = 0;
            deadline = 0;
            cancellable = true;
            declinable = true;
            autoAccept = false;
            prestige = new List<Contract.ContractPrestige>();
            maxCompletions = 0;
            maxSimultaneous = 0;
            rewardScience = 0.0f;
            rewardReputation = 0.0f;
            rewardFunds = 0.0f;
            failureReputation = 0.0f;
            failureFunds = 0.0f;
            advanceFunds = 0.0f;
            weight = 1.0;
            enabled = true;
        }
			
        /// <summary>
        /// Loads the contract type details from the given config node.
        /// </summary>
        /// <param name="configNode">The config node to load from.</param>
        /// <returns>Whether the load was successful.</returns>
        public bool Load(ConfigNode configNode)
        {
            LoggingUtil.LogLevel origLogLevel = LoggingUtil.logLevel;

            try
            {
                // Logging on
                LoggingUtil.CaptureLog = true;
                ConfigNodeUtil.SetCurrentDataNode(null);

                // Load values that are immediately required
                bool valid = true;
                valid &= ConfigNodeUtil.ParseValue<ContractGroup>(configNode, "group", x => group = x, this, (ContractGroup)null);

                // Set up the data node
                dataNode = new DataNode(configNode.GetValue("name"), group != null ? group.dataNode : null, this);
                ConfigNodeUtil.SetCurrentDataNode(dataNode);

                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "name", x => name = x, this);

                // Try to turn on trace mode
                valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "trace", x => trace = x, this, false);
                if (trace)
                {
                    LoggingUtil.logLevel = LoggingUtil.LogLevel.VERBOSE;
                    LoggingUtil.LogWarning(this, "Tracing enabled for contract type " + name);
                }

                // Load contract text details
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "title", x => title = x, this);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "tag", x => tag = x, this, "");
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "description", x => description = x, this, (string)null);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "topic", x => topic = x, this, "");
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "subject", x => subject = x, this, "");
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "motivation", x => motivation = x, this, "");
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "notes", x => notes = x, this, (string)null);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "synopsis", x => synopsis = x, this);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "completedMessage", x => completedMessage = x, this);

                // Load optional attributes
                valid &= ConfigNodeUtil.ParseValue<Agent>(configNode, "agent", x => agent = x, this, (Agent)null);
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "minExpiry", x => minExpiry = x, this, 1.0f, x => Validation.GE(x, 0.0f));
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "maxExpiry", x => maxExpiry = x, this, 7.0f, x => Validation.GE(x, minExpiry));
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "deadline", x => deadline = x, this, 0.0f, x => Validation.GE(x, 0.0f));
                valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "cancellable", x => cancellable = x, this, true);
                valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "declinable", x => declinable = x, this, true);
                valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "autoAccept", x => autoAccept = x, this, false);
                valid &= ConfigNodeUtil.ParseValue<List<Contract.ContractPrestige>>(configNode, "prestige", x => prestige = x, this, new List<Contract.ContractPrestige>());
                valid &= ConfigNodeUtil.ParseValue<CelestialBody>(configNode, "targetBody", x => targetBody = x, this, (CelestialBody)null);
            
                valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCompletions", x => maxCompletions = x, this, 0, x => Validation.GE(x, 0));
                valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxSimultaneous", x => maxSimultaneous = x, this, 0, x => Validation.GE(x, 0));

                // Load rewards
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "rewardFunds", x => rewardFunds = x, this, 0.0f, x => Validation.GE(x, 0.0f));
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "rewardReputation", x => rewardReputation = x, this, 0.0f, x => Validation.GE(x, 0.0f));
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "rewardScience", x => rewardScience = x, this, 0.0f, x => Validation.GE(x, 0.0f));
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "failureFunds", x => failureFunds = x, this, 0.0f, x => Validation.GE(x, 0.0f));
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "failureReputation", x => failureReputation = x, this, 0.0f, x => Validation.GE(x, 0.0f));
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "advanceFunds", x => advanceFunds = x, this, 0.0f, x => Validation.GE(x, 0.0f));

                // Load other values
                valid &= ConfigNodeUtil.ParseValue<double>(configNode, "weight", x => weight = x, this, 1.0, x => Validation.GE(x, 0.0f));

                // Merge in data from the parent contract group
                for (ContractGroup currentGroup = group; currentGroup != null; currentGroup = currentGroup.parent)
                {
                    // Merge dataValues - this is a flag saying what values need to be unique at the contract level
                    foreach (KeyValuePair<string, bool> pair in currentGroup.dataValues)
                    {
                        dataValues[group.name + ":" + pair.Key] = pair.Value;
                    }

                    // Merge uniquenessChecks
                    foreach (KeyValuePair<string, DataNode.UniquenessCheck> pair in currentGroup.uniquenessChecks)
                    {
                        uniquenessChecks[group.name + ":" + pair.Key] = pair.Value;
                    }
                }

                // Load DATA nodes
                valid &= dataNode.ParseDataNodes(configNode, this, dataValues, uniquenessChecks);

                // Check for unexpected values - always do this last
                valid &= ConfigNodeUtil.ValidateUnexpectedValues(configNode, this);

                log = LoggingUtil.capturedLog;
                LoggingUtil.CaptureLog = false;

                // Load parameters
                foreach (ConfigNode contractParameter in ConfigNodeUtil.GetChildNodes(configNode, "PARAMETER"))
                {
                    ParameterFactory paramFactory = null;
                    valid &= ParameterFactory.GenerateParameterFactory(contractParameter, this, out paramFactory);
                    if (paramFactory != null)
                    {
                        paramFactories.Add(paramFactory);
                        if (paramFactory.hasWarnings)
                        {
                            hasWarnings = true;
                        }
                    }
                }

                // Load behaviours
                foreach (ConfigNode requirementNode in ConfigNodeUtil.GetChildNodes(configNode, "BEHAVIOUR"))
                {
                    BehaviourFactory behaviourFactory = null;
                    valid &= BehaviourFactory.GenerateBehaviourFactory(requirementNode, this, out behaviourFactory);
                    if (behaviourFactory != null)
                    {
                        behaviourFactories.Add(behaviourFactory);
                        if (behaviourFactory.hasWarnings)
                        {
                            hasWarnings = true;
                        }
                    }
                }

                // Load requirements
                foreach (ConfigNode requirementNode in ConfigNodeUtil.GetChildNodes(configNode, "REQUIREMENT"))
                {
                    ContractRequirement requirement = null;
                    valid &= ContractRequirement.GenerateRequirement(requirementNode, this, out requirement);
                    if (requirement != null)
                    {
                        requirements.Add(requirement);
                        if (requirement.hasWarnings)
                        {
                            hasWarnings = true;
                        }
                    }
                }

                // Logging on
                LoggingUtil.CaptureLog = true;

                // Check we have at least one valid parameter
                if (paramFactories.Count() == 0)
                {
                    LoggingUtil.LogError(this.GetType(), ErrorPrefix() + ": Need at least one parameter for a contract!");
                    valid = false;
                }

                // Do the deferred loads
                valid &= ConfigNodeUtil.ExecuteDeferredLoads();

                config = configNode.ToString();
                hash = config.GetHashCode();
                enabled = valid;
                log += LoggingUtil.capturedLog;

				if (LoggingUtil.logLevel == LoggingUtil.LogLevel.VERBOSE || LoggingUtil.logLevel == LoggingUtil.LogLevel.DEBUG)
				{
					//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
					// to write the complete config and log for contract to both the system log and
					// a seperate CC log file
					//
					string pattern = @"\[(VERBOSE|DEBUG|INFO|WARNING|ERROR)\] ContractConfigurator\.ExpressionParser.BaseParser.+\n";
					string replacement = "";
					Regex rgx = new Regex(pattern);

					// First, write to the standard log file.  the regex strips out the leading text of the line, but 
					// if you want the text there, then just remove the rgx.Replace
					LoggingUtil.LogDebug(this, config);
					// Write the log out, ignoring the BaseParser lines
					LoggingUtil.LogDebug(this, rgx.Replace(log, replacement));

					// Now write to the CC log file, in a much cleaner manner 
					string path = ccLogFile;

					// Delete the file first time through, ignore any errors
					if (deleteCCLogfile)
					{
						deleteCCLogfile = false;
						try {
							if (File.Exists(path))
								File.Delete(path);
						} catch { 
							LoggingUtil.LogError(this, "Exception while attempting to delete the file: " + path);
						}
					}
					// Create the file if it doesn't exist
					if (!File.Exists(path)) 
					{
						// Create a file to write to.
						try {
							using (StreamWriter sw = File.CreateText(path)) {}
						} catch {
							LoggingUtil.LogError(this, "Exception while attempting to create the file: " + path);
						}
					}

					// This regex also strips out the second part of the line, so that the output is very clean 

					string pattern2 = @"\[(VERBOSE|DEBUG|INFO|WARNING|ERROR)\] ContractConfigurator\.ExpressionParser\.DataNode: ";
					Regex rgx2 = new Regex(pattern2);

					// Now write the config and the cleaned up log to it
					try {
						using (StreamWriter sw = File.AppendText (path)) {
							sw.WriteLine (config);
							sw.WriteLine("++++++++++\n");
							sw.WriteLine(rgx2.Replace(rgx.Replace(log, replacement), replacement));
							sw.WriteLine("==================================================================================");
	        		    }
					} catch {
						LoggingUtil.LogError(this, "Exception while attempting to write to the file: " + path);
					}
					//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
				}
				LoggingUtil.CaptureLog = false;

				return valid;
			}
            catch
            {
                enabled = false;
                throw;
            }
            finally
            {
                LoggingUtil.logLevel = origLogLevel;
                loaded = true;
            }
        }

        /// <summary>
        /// Generates and loads all the parameters required for the given contract.
        /// </summary>
        /// <param name="contract"></param>
        /// <returns>Whether the generation was successful.</returns>
        public bool GenerateBehaviours(ConfiguredContract contract)
        {
            return BehaviourFactory.GenerateBehaviours(contract, behaviourFactories);
        }

        /// <summary>
        /// Generates and loads all the parameters required for the given contract.
        /// </summary>
        /// <param name="contract">Contract to load parameters for</param>
        /// <returns>Whether the generation was successful.</returns>
        public bool GenerateParameters(ConfiguredContract contract)
        {
            return ParameterFactory.GenerateParameters(contract, contract, paramFactories);
        }

        /// <summary>
        /// Checks if the "basic" requirements that shouldn't change due to expressions are met.
        /// </summary>
        /// <param name="contract">The contract</param>
        /// <returns>Whether the contract can be offered.</returns>
        public bool MeetBasicRequirements(ConfiguredContract contract)
        {
            LoggingUtil.LogLevel origLogLevel = LoggingUtil.logLevel;
            try
            {
                // Turn tracing on
                if (trace)
                {
                    LoggingUtil.logLevel = LoggingUtil.LogLevel.VERBOSE;
                    LoggingUtil.LogWarning(this, "Tracing enabled for contract type " + name);
                }

                // Check expiry
                if (contract.ContractState == Contract.State.Withdrawn && Planetarium.fetch != null &&
                    contract.DateExpire < Planetarium.fetch.time)
                {
                    throw new ContractRequirementException("Expired contract.");
                }

                // Check prestige
                if (prestige.Count > 0 && !prestige.Contains(contract.Prestige))
                {
                    throw new ContractRequirementException("Wrong prestige level.");
                }

                // Checks for maxSimultaneous/maxCompletions
                if (maxSimultaneous != 0 || maxCompletions != 0)
                {
                    IEnumerable<ConfiguredContract> contractList = ContractSystem.Instance.GetCurrentContracts<ConfiguredContract>().
                        Where(c => c.contractType != null && c.contractType.name == name);

                    // Special case for pre-loader contracts
                    if (contract.ContractState == Contract.State.Withdrawn)
                    {
                        contractList = contractList.Union(ContractPreLoader.Instance.PendingContracts(this, contract.Prestige));
                        contractList = contractList.Where(c => c != contract);
                    }

                    // Get the count of active contracts - excluding ours
                    int activeContracts = contractList.Count();
                    if (contract.ContractState == Contract.State.Offered ||
                        contract.ContractState == Contract.State.Active ||
                        contractList.Contains(contract))
                    {
                        activeContracts--;
                    }

                    // Check if we're breaching the active limit
                    if (maxSimultaneous != 0 && activeContracts >= maxSimultaneous)
                    {
                        throw new ContractRequirementException("Too many active contracts.");
                    }

                    // Check if we're breaching the completed limit
                    if (maxCompletions != 0)
                    {
                        int finishedContracts = ContractSystem.Instance.GetCompletedContracts<ConfiguredContract>().
                            Count(c => c.contractType != null && c.contractType.name == name);
                        if (finishedContracts + activeContracts >= maxCompletions)
                        {
                            throw new ContractRequirementException("Too many completed/active/offered contracts.");
                        }
                    }
                }

                // Check the group values
                if (group != null)
                {
                    CheckContractGroup(contract, group);
                }

                return true;
            }
            catch (ContractRequirementException e)
            {
                LoggingUtil.LogLevel level = contract.ContractState == Contract.State.Active ? LoggingUtil.LogLevel.DEBUG : LoggingUtil.LogLevel.VERBOSE;
                string prefix = contract.contractType != null ? "Cancelling contract of type " + name + " (" + contract.Title + "): " :
                    "Didn't generate contract type " + name + ": ";
                LoggingUtil.Log(level, this.GetType(), prefix + e.Message);
                return false;
            }
            catch
            {
                LoggingUtil.LogError(this, "Exception while attempting to check requirements of contract type " + name);
                throw;
            }
            finally
            {
                LoggingUtil.logLevel = origLogLevel;
                loaded = true;
            }
        }
        
        /// <summary>
        /// Checks if the "extended" requirements that change due to expressions.
        /// </summary>
        /// <param name="contract">The contract</param>
        /// <returns>Whether the contract can be offered.</returns>
        public bool MeetExtendedRequirements(ConfiguredContract contract, ContractType contractType)
        {
            LoggingUtil.LogLevel origLogLevel = LoggingUtil.logLevel;
            try
            {
                // Turn tracing on
                if (trace)
                {
                    LoggingUtil.logLevel = LoggingUtil.LogLevel.VERBOSE;
                    LoggingUtil.LogWarning(this, "Tracing enabled for contract type " + name);
                }

                // Hash check
                if (contract.ContractState == Contract.State.Offered && contract.hash != hash)
                {
                    throw new ContractRequirementException("Contract definition changed.");
                }

                // Check special values are not null
                if (contract.contractType == null)
                {
                    foreach (KeyValuePair<string, bool> pair in dataValues)
                    {
                        // Only check if it is a required value
                        if (pair.Value)
                        {
                            string name = pair.Key;

                            if (!dataNode.IsInitialized(name))
                            {
                                throw new ContractRequirementException("'" + name + "' was not initialized.");
                            }

                            object o = dataNode[name];
                            if (o == null)
                            {
                                throw new ContractRequirementException("'" + name + "' was null.");
                            }
                            else if (o == typeof(List<>))
                            {
                                PropertyInfo prop = o.GetType().GetProperty("Count");
                                int count = (int)prop.GetValue(o, null);
                                if (count == 0)
                                {
                                    throw new ContractRequirementException("'" + name + "' had zero count.");
                                }
                            }
                            else if (o == typeof(Vessel))
                            {
                                Vessel v = (Vessel)o;

                                if (v.state == Vessel.State.DEAD)
                                {
                                    throw new ContractRequirementException("Vessel '" + v.vesselName + "' is dead.");
                                }
                            }
                        }
                    }
                }

                if (contract.contractType == null || contract.ContractState == Contract.State.Generated || contract.ContractState == Contract.State.Withdrawn)
                {
                    // Check for unique values against other contracts of the same type
                    foreach (KeyValuePair<string, DataNode.UniquenessCheck> pair in uniquenessChecks.Where(p => contract.uniqueData.ContainsKey(p.Key)))
                    {
                        string key = pair.Key;
                        DataNode.UniquenessCheck uniquenessCheck = pair.Value;

                        LoggingUtil.LogVerbose(this, "Doing unique value check for " + key);

                        // Get the active/offered contract lists
                        IEnumerable<ConfiguredContract> contractList = ContractSystem.Instance.Contracts.OfType<ConfiguredContract>().
                            Where(c => c != null && c.contractType != null && c != contract);

                        // Add in finished contracts
                        if (uniquenessCheck == DataNode.UniquenessCheck.CONTRACT_ALL || uniquenessCheck == DataNode.UniquenessCheck.GROUP_ALL)
                        {
                            contractList = contractList.Union(ContractSystem.Instance.ContractsFinished.OfType<ConfiguredContract>().
                            Where(c => c != null && c.contractType != null && c != contract));
                        }

                        // Special case for pre-loader contracts
                        if (contract.ContractState == Contract.State.Withdrawn)
                        {
                            contractList = contractList.Union(ContractPreLoader.Instance.PendingContracts(this, contract.Prestige).
                                Where(c => c != null && c != contract && c.contractType != null));
                        }

                        // Fileter anything that doesn't have our key
                        contractList = contractList.Where(c => c.uniqueData.ContainsKey(key));

                        // Check for contracts of the same type
                        if (uniquenessCheck == DataNode.UniquenessCheck.CONTRACT_ALL || uniquenessCheck == DataNode.UniquenessCheck.CONTRACT_ACTIVE)
                        {
                            contractList = contractList.Where(c => c.contractType.name == name);
                        }
                        // Check for a shared group
                        else if (contractType.group != null)
                        {
                            contractList = contractList.Where(c => c.contractType.group != null && c.contractType.group.name == contractType.group.name);
                        }
                        // Shared lack of group
                        else
                        {
                            contractList = contractList.Where(c => c.contractType.group == null);
                        }

                        object val = contract.uniqueData[key];
                        if (val != null)
                        {
                            // Special case for vessels - convert to the Guid
                            Vessel vessel = val as Vessel;
                            if (vessel != null)
                            {
                                val = vessel.id;
                            }

                            foreach (ConfiguredContract otherContract in contractList)
                            {
                                if (val.Equals(otherContract.uniqueData[key]))
                                {
                                    throw new ContractRequirementException("Failed on unique value check for key '" + key + "'.");
                                }
                            }
                        }
                    }
                }

                // Check the captured requirements
                if (!ContractRequirement.RequirementsMet(contract, this, requirements))
                {
                    throw new ContractRequirementException("Failed on contract requirement check.");
                }

                return true;
            }
            catch (ContractRequirementException e)
            {
                LoggingUtil.LogLevel level = contract.ContractState == Contract.State.Active ? LoggingUtil.LogLevel.INFO : LoggingUtil.LogLevel.VERBOSE;
                string prefix = contract.contractType != null ? "Cancelling contract of type " + name + " (" + contract.Title + "): " :
                    "Didn't generate contract type " + name + ": ";
                LoggingUtil.Log(level, this.GetType(), prefix + e.Message);
                return false;
            }
            catch
            {
                LoggingUtil.LogError(this, "Exception while attempting to check requirements of contract type " + name);
                throw;
            }
            finally
            {
                LoggingUtil.logLevel = origLogLevel;
                loaded = true;
            }
        }

        /// <summary>
        /// Tests whether a contract can be offered.
        /// </summary>
        /// <param name="contract">The contract</param>
        /// <returns>Whether the contract can be offered.</returns>
        public bool MeetRequirements(ConfiguredContract contract, ContractType contractType)
        {
            return MeetBasicRequirements(contract) && MeetExtendedRequirements(contract, contractType);
        }

        protected bool CheckContractGroup(ConfiguredContract contract, ContractGroup group)
        {
            if (group != null)
            {
                // Check the group is enabled
                if (!ContractConfiguratorSettings.IsEnabled(group))
                {
                    throw new ContractRequirementException("Contract group " + group.name + " is not enabled.");
                }

                IEnumerable<ConfiguredContract> contractList = ContractSystem.Instance.GetCurrentContracts<ConfiguredContract>().
                    Where(c => c.contractType != null);

                // Special case for pre-loader contracts
                if (contract.ContractState == Contract.State.Withdrawn)
                {
                    contractList = contractList.Union(ContractPreLoader.Instance.PendingContracts(contract.Prestige));
                    contractList = contractList.Where(c => c != contract);
                }

                // Check the group active limit
                int activeContracts = contractList.Count(c => c.contractType != null && group.BelongsToGroup(c.contractType));
                if (contract.ContractState == Contract.State.Offered || contract.ContractState == Contract.State.Active)
                {
                    activeContracts--;
                }

                if (group.maxSimultaneous != 0 && activeContracts >= group.maxSimultaneous)
                {
                    throw new ContractRequirementException("Too many active contracts in group (" + group.name + ").");
                }

                // Check the group completed limit
                if (group.maxCompletions != 0)
                {
                    int finishedContracts = ContractSystem.Instance.GetCompletedContracts<ConfiguredContract>().Count(c => c.contractType != null && group.BelongsToGroup(c.contractType));
                    if (finishedContracts + activeContracts >= maxCompletions)
                    {
                        throw new ContractRequirementException("Too many completed contracts in group (" + group.name + ").");
                    }
                }

                return CheckContractGroup(contract, group.parent);
            }

            return true;
        }

        /// <summary>
        /// Gets the identifier for the contract type.
        /// </summary>
        /// <returns>String for the contract type.</returns>
        public override string ToString()
        {
            return "CONTRACT_TYPE [" + name + "]";
        }
        
        public string ErrorPrefix()
        {
            return "CONTRACT_TYPE '" + name + "'";
        }

        public string ErrorPrefix(ConfigNode configNode)
        {
            return ErrorPrefix();
        }
    }
}
