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

namespace ContractConfigurator
{
    /// <summary>
    /// Class for grouping contract types.
    /// </summary>
    public class ContractGroup : IContractConfiguratorFactory
    {
        public static Dictionary<string, ContractGroup> contractGroups = new Dictionary<string, ContractGroup>();
        public static IEnumerable<ContractGroup> AllGroups
        {
            get
            {
                foreach (ContractGroup group in contractGroups.Values)
                {
                    yield return group;
                }

                // This is not supposed to be a yield break - returning a null group is intentional
                yield return null;
            }
        }

        /// <summary>
        /// Static method (used by other mods via reflection) to get the contract group display name.
        /// </summary>
        public static string GroupDisplayName(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                return "";
            }

            ContractGroup group = contractGroups.ContainsKey(groupName) ? contractGroups[groupName] : null;
            return group == null ? "" : group.displayName;
        }

        public ContractGroup Root
        {
            get
            {
                return parent == null ? this : parent.Root;
            }
        }

        // Group attributes
        public string name;
        public string displayName;
        public string minVersionStr;
        public int maxCompletions;
        public int maxSimultaneous;
        public List<string> disabledContractType;
        public Agent agent;
        public string sortKey;

        public bool expandInDebug = false;
        public bool hasWarnings { get; set; }
        public Type iteratorType { get; set; }
        public string iteratorKey { get; set; }
        public bool enabled { get; private set; }
        public string config { get; private set; }
        public string log { get; private set; }
        public DataNode dataNode { get; private set; }
        public Version minVersion
        {
            get
            {
                return string.IsNullOrEmpty(minVersionStr) ? new Version(1, 0) : Util.Version.ParseVersion(minVersionStr);
            }
        }

        public Dictionary<string, ContractType.DataValueInfo> dataValues = new Dictionary<string, ContractType.DataValueInfo>();
        public Dictionary<string, DataNode.UniquenessCheck> uniquenessChecks = new Dictionary<string, DataNode.UniquenessCheck>();

        public ContractGroup parent = null;

        public ContractGroup(string name)
        {
            this.name = name;
            contractGroups.Add(name, this);
        }

        /// <summary>
        /// Loads the contract group details from the given config node.
        /// </summary>
        /// <param name="configNode">The config node to load from</param>
        /// <returns>Whether we were successful.</returns>
        public bool Load(ConfigNode configNode)
        {
            try
            {
                dataNode = new DataNode(configNode.GetValue("name"), this);

                LoggingUtil.CaptureLog = true;
                ConfigNodeUtil.SetCurrentDataNode(dataNode);
                bool valid = true;
                string unused;

                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "name", x => name = x, this);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "displayName", x => displayName = x, this, name);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "minVersion", x => minVersionStr = x, this, "");
                valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCompletions", x => maxCompletions = x, this, 0, x => Validation.GE(x, 0));
                valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxSimultaneous", x => maxSimultaneous = x, this, 0, x => Validation.GE(x, 0));
                valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "disabledContractType", x => disabledContractType = x, this, new List<string>());
                valid &= ConfigNodeUtil.ParseValue<Agent>(configNode, "agent", x => agent = x, this, (Agent)null);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "sortKey", x => sortKey = x, this, displayName);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "tip", x => unused = x, this, "");

                if (configNode.HasValue("sortKey") && parent == null)
                {
                    sortKey = displayName;
                    LoggingUtil.LogWarning(this, ErrorPrefix() + ": Using the sortKey field is only applicable on child CONTRACT_GROUP elements");
                }

                if (!string.IsNullOrEmpty(minVersionStr))
                {
                    if (Util.Version.VerifyAssemblyVersion("ContractConfigurator", minVersionStr) == null)
                    {
                        valid = false;

                        var ainfoV = Attribute.GetCustomAttribute(typeof(ExceptionLogWindow).Assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
                        string title = "Contract Configurator " + ainfoV.InformationalVersion + " Message";
                        string message = "The contract group '" + name + "' requires at least Contract Configurator " + minVersionStr +
                            " to work, and you are running version " + ainfoV.InformationalVersion +
                            ".  Please upgrade Contract Configurator to use the contracts in this group.";
                        DialogGUIButton dialogOption = new DialogGUIButton("Okay", new Callback(DoNothing), true);
                        PopupDialog.SpawnPopupDialog(new MultiOptionDialog(message, title, UISkinManager.GetSkin("default"), dialogOption), false, UISkinManager.GetSkin("default"));
                    }
                }

                // Load DATA nodes
                valid &= dataNode.ParseDataNodes(configNode, this, dataValues, uniquenessChecks);

                // Do the deferred loads
                valid &= ConfigNodeUtil.ExecuteDeferredLoads();

                // Do post-deferred load warnings
                if (agent == null)
                {
                    LoggingUtil.LogWarning(this, ErrorPrefix() + ": Providing the agent field for all CONTRACT_GROUP nodes is highly recommended, as the agent is used to group contracts in Mission Control.");
                }
                if (string.IsNullOrEmpty(minVersionStr) || minVersion < ContractConfigurator.ENHANCED_UI_VERSION)
                {
                    LoggingUtil.LogWarning(this, ErrorPrefix() + ": No minVersion or older minVersion provided.  It is recommended that the minVersion is set to at least 1.15.0 to turn important warnings for deprecated functionality into errors.");
                }
                if (!configNode.HasValue("displayName"))
                {
                    LoggingUtil.LogWarning(this, ErrorPrefix() + ": No display name provided.  A display name is recommended, as it is used in the Mission Control UI.");
                }

                config = configNode.ToString();
                log += LoggingUtil.capturedLog;
                LoggingUtil.CaptureLog = false;

                // Load child groups
                foreach (ConfigNode childNode in ConfigNodeUtil.GetChildNodes(configNode, "CONTRACT_GROUP"))
                {
                    ContractGroup child = null;
                    string childName = childNode.GetValue("name");
                    try
                    {
                        child = new ContractGroup(childName);
                    }
                    catch (ArgumentException)
                    {
                        LoggingUtil.LogError(this, "Couldn't load CONTRACT_GROUP '" + childName + "' due to a duplicate name.");
                        valid = false;
                        continue;
                    }

                    child.parent = this;
                    valid &= child.Load(childNode);
                    child.dataNode.Parent = dataNode;
                    if (child.hasWarnings)
                    {
                        hasWarnings = true;
                    }
                }

                // Check for unexpected values - always do this last
                valid &= ConfigNodeUtil.ValidateUnexpectedValues(configNode, this);

                // Invalidate children
                if (!valid)
                {
                    Invalidate();
                }

                enabled = valid;
                return valid;
            }
            catch
            {
                enabled = false;
                throw;
            }
        }

        private void Invalidate()
        {
            enabled = false;
            foreach (ContractGroup child in AllGroups.Where(g => g != null && g.parent == this))
            {
                child.Invalidate();
            }
        }

        /// <summary>
        /// Checks if the given contract type belongs to the given group
        /// </summary>
        /// <param name="contractType">The contract type to check</param>
        /// <returns>True if the contract type is a part of this group</returns>
        public bool BelongsToGroup(ContractType contractType)
        {
            if (contractType == null)
            {
                return false;
            }

            ContractGroup group = contractType.group;
            while (group != null)
            {
                if (group.name == name)
                {
                    return true;
                }
                group = group.parent;
            }

            return false;
        }

        /// <summary>
        /// Verifies that the contract group is not empty.
        /// </summary>
        public void CheckEmpty()
        {
            bool atLeastOne = false;
            foreach (ContractType contractType in ContractType.AllContractTypes.Where(ct => ct.group == this))
            {
                atLeastOne = true;
                break;
            }

            // Need at least one contract in the group
            if (!atLeastOne)
            {
                // Try for a child group
                if (!ContractGroup.AllGroups.Where(g => g != null && g.parent != null && g.parent.name == name).Any())
                {
                    LoggingUtil.CaptureLog = true;
                    LoggingUtil.LogWarning(this, "Contract group '" + name + "' contains no contract types or child groups!");
                    log += LoggingUtil.capturedLog;
                    LoggingUtil.CaptureLog = false;
                    hasWarnings = true;
                }
            }
        }

        private void DoNothing() { }

        /// <summary>
        /// Returns the name of the contract group.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "CONTRACT_GROUP[" + name + "]";
        }
        
        public string ErrorPrefix()
        {
            return "CONTRACT_GROUP '" + name + "'";
        }

        public string ErrorPrefix(ConfigNode configNode)
        {
            return ErrorPrefix();
        }

        /// <summary>
        /// Outputs the debugging info for the debug window.
        /// </summary>
        /// <returns>Debug info for the debug window</returns>
        public string DebugInfo()
        {
            return "";
        }
    }
}
