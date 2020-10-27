using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Agents;
using ContractConfigurator.ExpressionParser;
using ContractConfigurator.Util;

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
        static MethodInfo methodParseExpand = typeof(ContractType).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).
            Where(m => m.Name == "ParseDataExpandString").Single();

        public class DataValueInfo
        {
            public bool required;
            public bool hidden;
            public string title;
            public Type type;

            public DataValueInfo(string title, bool required, bool hidden, Type type)
            {
                this.title = title;
                this.required = required;
                this.hidden = hidden;
                this.type = type;
            }

            public bool IsIgnoredType()
            {
                return type.IsValueType;
            }
        }

        private static Dictionary<string, ContractType> contractTypes = new Dictionary<string,ContractType>();
        public static IEnumerable<ContractType> AllContractTypes { get { return contractTypes.Values; } }
        public static IEnumerable<ContractType> AllValidContractTypes
        {
            get
            {
                foreach (KeyValuePair<string, ContractType> pair in contractTypes)
                {
                    if (pair.Value.enabled)
                    {
                        yield return pair.Value;
                    }
                }
            }
        }
        public static IEnumerable<string> AllValidContractTypeNames
        {
            get
            {
                foreach (KeyValuePair<string, ContractType> pair in contractTypes)
                {
                    if (pair.Value.enabled)
                    {
                        yield return pair.Key;
                    }
                }
            }
        }
        
        public static ContractType GetContractType(string name)
        {
            if (name != null && contractTypes.ContainsKey(name))
            {
                return contractTypes[name];
            }
            return null;
        }

        public static void ClearContractTypes()
        {
            contractTypes.Clear();
        }

        public string FullName
        {
            get
            {
                return (group != null ? (group.Root.name + ".") : "") + name;
            }
        }

        public System.Version minVersion
        {
            get
            {
                for (ContractGroup currentGroup = group; currentGroup != null; currentGroup = currentGroup.parent)
                {
                    if (!string.IsNullOrEmpty(currentGroup.minVersionStr))
                    {
                        return Util.Version.ParseVersion(currentGroup.minVersionStr);
                    }
                }
                return new System.Version(1, 0, 0, 0);
            }
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
        public string title = "";
        public string genericTitle = "";
        public string tag;
        public string notes;
        public string description;
        public string genericDescription = "";
        public string topic;
        public string subject;
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
        public bool trace = false;
        public bool loaded = false;
        public int maxConsecutiveGenerationFailures = 1;
        public int failedGenerationAttempts;
        public double lastGenerationFailure = -100;
        public string sortKey;

        public Dictionary<string, DataValueInfo> dataValues = new Dictionary<string, DataValueInfo>();
        public Dictionary<string, DataNode.UniquenessCheck> uniquenessChecks = new Dictionary<string, DataNode.UniquenessCheck>();

        public HashSet<CelestialBody> contractBodies = new HashSet<CelestialBody>();

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
                LoggingUtil.LogInfo(this, "Loading CONTRACT_TYPE: '{0}'", name);

                // Clear the config node cache
                ConfigNodeUtil.ClearCache(true);

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
                    LoggingUtil.LogWarning(this, "Tracing enabled for contract type {0}", name);
                }

                // Load contract text details
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "title", x => title = x, this);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "tag", x => tag = x, this, "");
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "description", x => description = x, this, (string)null);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "topic", x => topic = x, this, "");
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "subject", x => subject = x, this, "");
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "notes", x => notes = x, this, (string)null);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "synopsis", x => synopsis = x, this);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "completedMessage", x => completedMessage = x, this);

                if (configNode.HasValue("motivation)"))
                {
                    string motivation;
                    valid &= ConfigNodeUtil.ParseValue<string>(configNode, "motivation", x => motivation = x, this, "");
                    LoggingUtil.LogWarning(this, "The 'motivation' attribute is no longer supported as of Contract Configurator 1.23.0");
                }

                // Load optional attributes
                valid &= ConfigNodeUtil.ParseValue<Agent>(configNode, "agent", x => agent = x, this, group != null ? group.agent : null);
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "minExpiry", x => minExpiry = x, this, 5.0f, x => Validation.GE(x, 0.0f));
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "maxExpiry", x => maxExpiry = x, this, 100.0f, x => Validation.GE(x, minExpiry));
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "deadline", x => deadline = x, this, 0.0f, x => Validation.GE(x, 0.0f));
                valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "cancellable", x => cancellable = x, this, true);
                valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "declinable", x => declinable = x, this, true);
                valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "autoAccept", x => autoAccept = x, this, false);
                valid &= ConfigNodeUtil.ParseValue<Contract.ContractPrestige>(configNode, "prestige", x => prestige = new List<Contract.ContractPrestige>() { x }, this, Contract.ContractPrestige.Trivial);
                valid &= ConfigNodeUtil.ParseValue<CelestialBody>(configNode, "targetBody", x => targetBody = x, this, (CelestialBody)null);

                valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCompletions", x => maxCompletions = x, this, 0, x => Validation.GE(x, 0));
                valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxSimultaneous", x => maxSimultaneous = x, this, (maxCompletions == 0 ? 4 : 0), x => Validation.GE(x, 0));

                // Load rewards
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "rewardFunds", x => rewardFunds = x, this, 0.0f);
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "rewardReputation", x => rewardReputation = x, this, 0.0f);
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "rewardScience", x => rewardScience = x, this, 0.0f);
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "failureFunds", x => failureFunds = x, this, 0.0f);
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "failureReputation", x => failureReputation = x, this, 0.0f);
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "advanceFunds", x => advanceFunds = x, this, 0.0f);

                // Load other values
                if (configNode.HasValue("weight"))
                {
                    double weight;
                    valid &= ConfigNodeUtil.ParseValue<double>(configNode, "weight", x => weight = x, this);
                    LoggingUtil.LogWarning(this, "{0}: The weight attribute is deprecated as of Contract Configurator 1.15.0.  Contracts are no longer generated using a weighted system.", ErrorPrefix());
                }

                // Merge in data from the parent contract group
                for (ContractGroup currentGroup = group; currentGroup != null; currentGroup = currentGroup.parent)
                {
                    // Merge dataValues - this is a flag saying what values need to be unique at the contract level
                    foreach (KeyValuePair<string, DataValueInfo> pair in currentGroup.dataValues)
                    {
                        dataValues[currentGroup.name + ":" + pair.Key] = pair.Value;
                    }

                    // Merge uniquenessChecks
                    foreach (KeyValuePair<string, DataNode.UniquenessCheck> pair in currentGroup.uniquenessChecks)
                    {
                        uniquenessChecks[currentGroup.name + ":" + pair.Key] = pair.Value;
                    }
                }

                // Load DATA nodes
                valid &= dataNode.ParseDataNodes(configNode, this, dataValues, uniquenessChecks);

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
                    LoggingUtil.LogError(this, "{0}: Need at least one parameter for a contract!", ErrorPrefix());
                    valid = false;
                }

                ConfigNodeUtil.SetCurrentDataNode(dataNode);

                //
                // Process the DATA_EXPAND nodes - this could cause a restart to the load process
                //
                ConfigNode dataExpandNode = configNode.GetNodes("DATA_EXPAND").FirstOrDefault();
                if (dataExpandNode != null)
                {
                    Type type = null;
                    valid &= ConfigNodeUtil.ParseValue<Type>(dataExpandNode, "type", x => type = x, this);

                    if (type != null)
                    {
                        foreach (ConfigNode.Value pair in dataExpandNode.values)
                        {
                            string key = pair.name;
                            if (key != "type")
                            {
                                object value = null;

                                // Create the setter function
                                Type actionType = typeof(Action<>).MakeGenericType(type);
                                Delegate del = Delegate.CreateDelegate(actionType, value, typeof(DataNode).GetMethod("NullAction"));

                                // Set the ParseDataExpandString method generic value
                                MethodInfo method = methodParseExpand.MakeGenericMethod(new Type[] { type });

                                // Invoke the ParseDataExpandString method
                                List<string> values = (List<string>)method.Invoke(this, new object[] { dataExpandNode, key });

                                // Stop at this point if we're invalid
                                if (values == null || !valid)
                                {
                                    if (values == null)
                                    {
                                        LoggingUtil.LogWarning(this, "{0}: Received an empty list of values when trying to do a DATA_EXPAND", ErrorPrefix());
                                    }
                                    else
                                    {
                                        LoggingUtil.LogWarning(this, "{0}: Not expanding DATA_EXPAND node as the contract had validation errors.", ErrorPrefix());
                                    }
                                    valid = false;
                                    break;
                                }

                                // Expand
                                configNode.RemoveNode(dataExpandNode);
                                foreach (string val in values)
                                {
                                    // Set up for expansion
                                    ConfigNode copy = configNode.CreateCopy();
                                    string newName = name + "." + val;
                                    copy.SetValue("name", newName);

                                    // Set up the data node in the copy
                                    ConfigNode dataNode = new ConfigNode("DATA");
                                    copy.AddNode(dataNode);
                                    dataNode.AddValue("type", dataExpandNode.GetValue("type"));
                                    dataNode.AddValue(key, val);
                                    dataNode.AddValue("isLiteral", true);

                                    ContractType contractTypeCopy = new ContractType(newName);
                                    contractTypeCopy.Load(copy);
                                }

                                // Remove the original
                                contractTypes.Remove(name);

                                // Don't do any more loading for this one
                                LoggingUtil.LogInfo(this, "Successfully expanded CONTRACT_TYPE '{0}'", name);
                                return valid;
                            }
                        }
                    }
                }

                //
                // Do the deferred loads
                //
                valid &= ConfigNodeUtil.ExecuteDeferredLoads();

                //
                // Do generic fields that need to happen after deferred loads
                //
                ConfigNodeUtil.SetCurrentDataNode(dataNode);

                // Generic title
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "genericTitle", x => genericTitle = x, this, title);
                if (!configNode.HasValue("genericTitle") && !dataNode.IsDeterministic("title"))
                {
                    LoggingUtil.Log(minVersion >= ContractConfigurator.ENHANCED_UI_VERSION ? LoggingUtil.LogLevel.ERROR : LoggingUtil.LogLevel.WARNING, this,
                        "{0}: The field 'genericTitle' is required when the title is not determistic (ie. when expressions are used).", ErrorPrefix());

                    // Error on newer versions of contract packs
                    if (minVersion >= ContractConfigurator.ENHANCED_UI_VERSION)
                    {
                        valid = false;
                    }
                }
                else if (!dataNode.IsDeterministic("genericTitle"))
                {
                    valid = false;
                    LoggingUtil.LogError(this, "{0}: The field 'genericTitle' must be deterministic.", ErrorPrefix());
                }

                // Generic description
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "genericDescription", x => genericDescription = x, this, description);
                if (!configNode.HasValue("genericDescription") && !dataNode.IsDeterministic("description"))
                {
                    LoggingUtil.Log(minVersion >= ContractConfigurator.ENHANCED_UI_VERSION ? LoggingUtil.LogLevel.ERROR : LoggingUtil.LogLevel.WARNING, this,
                        "{0}: The field 'genericDescription' is required when the description is not determistic (ie. when expressions are used).", ErrorPrefix());

                    // Error on newer versions of contract packs
                    if (minVersion >= ContractConfigurator.ENHANCED_UI_VERSION)
                    {
                        valid = false;
                    }
                }
                else if (!dataNode.IsDeterministic("genericDescription"))
                {
                    valid = false;
                    LoggingUtil.LogError(this, "{0}: The field 'genericDescription' must be deterministic.", ErrorPrefix());
                }

                // Sorting key
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "sortKey", x => sortKey = x, this, genericTitle);

                // Check for unexpected values - always do this last
                valid &= ConfigNodeUtil.ValidateUnexpectedValues(configNode, this);

                if (valid)
                {
                    LoggingUtil.LogInfo(this, "Successfully loaded CONTRACT_TYPE '{0}'", name);
                }
                else
                {
                    LoggingUtil.LogWarning(this, "Errors encountered while trying to load CONTRACT_TYPE '{0}'", name);
                }
                config = configNode.ToString();
                hash = config.GetHashCode();
                enabled = valid;
                log += LoggingUtil.capturedLog;

                if (LoggingUtil.logLevel >= LoggingUtil.LogLevel.DEBUG)
                {
                    // Get the contract configurator log file
                    string[] dirComponents = new string[] { KSPUtil.ApplicationRootPath, "GameData", "ContractConfigurator", "log", (group == null ? "!NO_GROUP" : group.Root.name) };
                    string[] pathComponents = dirComponents.Union(new string[] { name + ".log" }).ToArray();
                    string dir = string.Join(Path.DirectorySeparatorChar.ToString(), dirComponents);
                    string path = string.Join(Path.DirectorySeparatorChar.ToString(), pathComponents);

                    // Delete the file if it exists
                    if (File.Exists(path))
                    {
                        try
                        {
                            File.Delete(path);
                        }
                        catch (Exception e)
                        {
                            LoggingUtil.LogException(new Exception(StringBuilderCache.Format("Exception while attempting to delete the file: {0}", path), e));
                        }
                    }

                    // Create the directory if it doesn't exist
                    Directory.CreateDirectory(dir);

                    // Now write the config and the cleaned up log to it
                    try
                    {
                        using (StreamWriter sw = File.AppendText(path))
                        {
                            sw.Write("Debug information for CONTRACT_TYPE '" + name + "':\n");
                            sw.Write("\nConfiguration:\n");
                            sw.Write(config);
                            sw.Write("\nData Nodes:\n");
                            sw.Write(DataNodeDebug(dataNode));
                            sw.Write("\nOutput log:\n");
                            sw.Write(log);
                        }
                    }
                    catch
                    {
                        LoggingUtil.LogError(this, "Exception while attempting to write to the file: {0}", path);
                    }
                }

                return valid;
            }
            catch (Exception e)
            {
                enabled = false;
                throw new Exception("Error loading CONTRACT_TYPE '" + name + "'", e);
            }
            finally
            {
                LoggingUtil.CaptureLog = false;
                LoggingUtil.logLevel = origLogLevel;
                loaded = true;
            }
        }

        private List<string> ParseDataExpandString<T>(ConfigNode dataExpandNode, string key)
        {
            List<T> values = null;
            bool valid = ConfigNodeUtil.ParseValue<List<T>>(dataExpandNode, key, x => values = x, this);
            if (!valid || values == null)
            {
                return null;
            }

            // Check that we actually got a deterministic value
            if (!dataNode.IsDeterministic(key))
            {
                LoggingUtil.LogError(this, "{0}: Values captured in a DATA_EXPAND node must be deterministic (the value needs to be fixed when loaded on game startup.", ErrorPrefix());
                return null;
            }

            List<string> results = new List<string>();
            foreach (T t in values)
            {
                Type type;
                results.Add(PersistentDataStore.OutputValue(t, out type));
            }
            return results;
        }

        static string DataNodeDebug(DataNode node, int indent = 0)
        {
            if (node == null)
            {
                return "";
            }

            string indentStr = new string('\t', indent);
            string result = indentStr + node.DebugString(false).Replace("\n", "\n" + indentStr) + "\n";
            foreach (DataNode child in node.Children)
            {
                result += DataNodeDebug(child, indent + 1);
            }

            return result;
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
            return ParameterFactory.GenerateParameters(contract, contract, paramFactories) &&
                contract.ParameterCount > 0; // Check that at least one parameter was generated
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
                    LoggingUtil.LogWarning(this, "Tracing enabled for contract type {0}", name);
                }

                // Check funding
                if (advanceFunds < 0)
                {
                    CurrencyModifierQuery q = new CurrencyModifierQuery(TransactionReasons.ContractAdvance, -advanceFunds, 0.0f, 0.0f);
                    GameEvents.Modifiers.OnCurrencyModifierQuery.Fire(q);
                    float fundsRequired = advanceFunds + q.GetEffectDelta(Currency.Funds);

                    if (!Funding.CanAfford(fundsRequired))
                    {
                        throw new ContractRequirementException("Can't afford contract advance cost.");
                    }
                }

                // Check expiry
                if ((contract.ContractState == Contract.State.Offered || contract.ContractState == Contract.State.Withdrawn) &&
                    Planetarium.fetch != null && contract.DateExpire < Planetarium.fetch.time)
                {
                    throw new ContractRequirementException("Expired contract.");
                }

                // Checks for maxSimultaneous/maxCompletions
                if (maxSimultaneous != 0 || maxCompletions != 0)
                {
                    IEnumerable<ConfiguredContract> contractList = ConfiguredContract.CurrentContracts.
                        Where(c => c.contractType != null && c.contractType.name == name && c != contract);

                    // Check if we're breaching the active limit
                    int activeContracts = contractList.Count();
                    if (maxSimultaneous != 0 && activeContracts >= maxSimultaneous)
                    {
                        throw new ContractRequirementException("Too many active contracts.");
                    }

                    // Check if we're breaching the completed limit
                    if (maxCompletions != 0)
                    {
                        if (ActualCompletions() + activeContracts >= maxCompletions)
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
                LoggingUtil.LogLevel level = contract.ContractState == Contract.State.Active ? LoggingUtil.LogLevel.INFO : contract.contractType != null ? LoggingUtil.LogLevel.DEBUG : LoggingUtil.LogLevel.VERBOSE;
                if (contract.contractType != null)
                {
                    LoggingUtil.Log(level, this.GetType(), "Cancelling contract of type {0} ({1}): {2}", name, contract.Title, e.Message);
                }
                else
                {
                    LoggingUtil.Log(level, this.GetType(), "Didn't generate contract of type {0}: {1}", name, e.Message);
                }
                return false;
            }
            catch
            {
                LoggingUtil.LogError(this, "Exception while attempting to check requirements of contract type {0}", name);
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
                    LoggingUtil.LogWarning(this, "Tracing enabled for contract type {0}", name);
                }

                // Hash check
                if (contract.ContractState == Contract.State.Offered && contract.hash != hash)
                {
                    throw new ContractRequirementException("Contract definition changed.");
                }

                // Check prestige
                if (prestige.Count > 0 && !prestige.Contains(contract.Prestige))
                {
                    throw new ContractRequirementException("Wrong prestige level.");
                }

                // Do a Research Bodies check, if applicable
                ResearchBodiesCheck(contract);

                // Check special values are not null
                if (contract.contractType == null)
                {
                    foreach (KeyValuePair<string, DataValueInfo> pair in dataValues)
                    {
                        // Only check if it is a required value
                        if (pair.Value.required)
                        {
                            CheckRequiredValue(pair.Key);
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

                        LoggingUtil.LogVerbose(this, "Doing unique value check for {0}", key);

                        // Get the active/offered contract lists
                        IEnumerable<ConfiguredContract> contractList = ConfiguredContract.CurrentContracts.
                            Where(c => c != null && c.contractType != null && c != contract);

                        // Add in finished contracts
                        if (uniquenessCheck == DataNode.UniquenessCheck.CONTRACT_ALL || uniquenessCheck == DataNode.UniquenessCheck.GROUP_ALL)
                        {
                            contractList = contractList.Union(ContractSystem.Instance.ContractsFinished.OfType<ConfiguredContract>().
                                Where(c => c != null && c.contractType != null && c != contract));
                        }

                        // Filter anything that doesn't have our key
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
                if (!ContractRequirement.RequirementsMet(contract, this, contract.requirements != null ? contract.requirements : requirements))
                {
                    throw new ContractRequirementException("Failed on contract requirement check.");
                }

                return true;
            }
            catch (ContractRequirementException e)
            {
                LoggingUtil.LogLevel level = contract.ContractState == Contract.State.Active ? LoggingUtil.LogLevel.INFO : contract.contractType != null ? LoggingUtil.LogLevel.DEBUG : LoggingUtil.LogLevel.VERBOSE;
                if (contract.contractType != null)
                {
                    LoggingUtil.Log(level, this.GetType(), "Cancelling contract of type {0} ({1}): {2}", name, contract.Title, e.Message);
                }
                else
                {
                    LoggingUtil.Log(level, this.GetType(), "Didn't generate contract of type {0}: {1}", name, e.Message);
                }
                return false;
            }
            catch
            {
                LoggingUtil.LogError(this, "Exception while attempting to check requirements of contract type {0}", name);
                throw;
            }
            finally
            {
                LoggingUtil.logLevel = origLogLevel;
                loaded = true;
            }
        }

        public void ResearchBodiesCheck(ConfiguredContract contract)
        {
            if (Util.Version.VerifyResearchBodiesVersion())
            {
                LoggingUtil.LogVerbose(this, "ResearchBodies check for contract type {0}", name);

                // Check each body that the contract references
                Dictionary<CelestialBody, RBWrapper.CelestialBodyInfo> bodyInfoDict = RBWrapper.RBactualAPI.CelestialBodies;
                foreach (CelestialBody body in contract.ContractBodies)
                {
                    if (bodyInfoDict.ContainsKey(body) && !body.isHomeWorld)
                    {
                        RBWrapper.CelestialBodyInfo bodyInfo = bodyInfoDict[body];
                        if (!bodyInfo.isResearched)
                        {
                            throw new ContractRequirementException(StringBuilderCache.Format("Research Bodies: {0} has not yet been researched.", body.name));
                        }
                    }
                }
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
                if (!((ContractGroupParametersTemplate)HighLogic.CurrentGame.Parameters.CustomParams(SettingsBuilder.GroupParametersType)).IsEnabled(group.Root.name))
                {
                    throw new ContractRequirementException("Contract group " + group.name + " is not enabled.");
                }

                IEnumerable<ConfiguredContract> contractList = ConfiguredContract.CurrentContracts.
                    Where(c => c.contractType != null && c != contract);

                // Check the group active limit
                int activeContracts = contractList.Count(c => c.contractType != null && group.BelongsToGroup(c.contractType));
                if (group.maxSimultaneous != 0 && activeContracts >= group.maxSimultaneous)
                {
                    throw new ContractRequirementException("Too many active contracts in group (" + group.name + ").");
                }

                // Check the group completed limit
                if (group.maxCompletions != 0)
                {
                    int finishedContracts = ConfiguredContract.CompletedContracts.Count(c => c.contractType != null && group.BelongsToGroup(c.contractType));
                    if (finishedContracts + activeContracts >= maxCompletions)
                    {
                        throw new ContractRequirementException("Too many completed contracts in group (" + group.name + ").");
                    }
                }

                return CheckContractGroup(contract, group.parent);
            }

            return true;
        }

        public void CheckRequiredValue(string name)
        {
            if (!dataNode.IsInitialized(name))
            {
                throw new ContractRequirementException("'" + name + "' was not initialized.");
            }

            object o = dataNode[name];
            if (o == null)
            {
                throw new ContractRequirementException("'" + name + "' was null.");
            }
            else if (o.GetType().GetGenericArguments().Any() && o.GetType().GetGenericTypeDefinition() == typeof(List<>))
            {
                PropertyInfo prop = o.GetType().GetProperty("Count");
                int count = (int)prop.GetValue(o, null);
                if (count == 0)
                {
                    throw new ContractRequirementException("'" + name + "' had zero count.");
                }
            }
            else if (o.GetType() == typeof(Vessel))
            {
                Vessel v = (Vessel)o;

                if (v.state == Vessel.State.DEAD)
                {
                    throw new ContractRequirementException("Vessel '" + v.vesselName + "' is dead.");
                }
            }
        }

        public int ActualCompletions()
        {
            int count = 0;
            foreach (ConfiguredContract c in ConfiguredContract.CompletedContracts)
            {
                if (c.contractType != null && c.contractType.name == name)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Gets the identifier for the contract type.
        /// </summary>
        /// <returns>String for the contract type.</returns>
        public override string ToString()
        {
            return StringBuilderCache.Format("CONTRACT_TYPE [{0}]", name);
        }
        
        public string ErrorPrefix()
        {
            return StringBuilderCache.Format("CONTRACT_TYPE '{0}'", name);
        }

        public string ErrorPrefix(ConfigNode configNode)
        {
            return ErrorPrefix();
        }
    }
}
