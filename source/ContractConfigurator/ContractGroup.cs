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

        // Group attributes
        public string name;
        public string minVersion;
        public int maxCompletions;
        public int maxSimultaneous;

        public bool expandInDebug = false;
        public bool hasWarnings { get; set; }
        public bool enabled { get; private set; }
        public string config { get; private set; }
        public string log { get; private set; }
        public DataNode dataNode { get; private set; }

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
                ConfigNodeUtil.ClearCache(true);
                ConfigNodeUtil.SetCurrentDataNode(dataNode);
                bool valid = true;

                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "name", x => name = x, this);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "minVersion", x => minVersion = x, this, "");
                valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCompletions", x => maxCompletions = x, this, 0, x => Validation.GE(x, 0));
                valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxSimultaneous", x => maxSimultaneous = x, this, 0, x => Validation.GE(x, 0));

                if (!string.IsNullOrEmpty(minVersion))
                {
                    if (Util.Version.VerifyAssemblyVersion("ContractConfigurator", minVersion) == null)
                    {
                        valid = false;

                        var ainfoV = Attribute.GetCustomAttribute(typeof(ExceptionLogWindow).Assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
                        string title = "Contract Configurator " + ainfoV.InformationalVersion + " Message";
                        string message = "The contract group '" + name + "' requires at least Contract Configurator " + minVersion +
                            " to work, and you are running version " + ainfoV.InformationalVersion +
                            ".  Please upgrade Contract Configurator to use the contracts in this group.";
                        DialogOption dialogOption = new DialogOption("Okay", new Callback(DoNothing), true);
                        PopupDialog.SpawnPopupDialog(new MultiOptionDialog(message, title, HighLogic.Skin, dialogOption), false, HighLogic.Skin);
                    }
                }

                // Check for unexpected values - always do this last
                valid &= ConfigNodeUtil.ValidateUnexpectedValues(configNode, this);

                // Do the deferred loads
                valid &= ConfigNodeUtil.ExecuteDeferredLoads();

                config = configNode.ToString();
                log += LoggingUtil.capturedLog;
                LoggingUtil.CaptureLog = false;

                // Load child groups
                foreach (ConfigNode childNode in ConfigNodeUtil.GetChildNodes(configNode, "CONTRACT_GROUP"))
                {
                    ContractGroup child = null;
                    string name = childNode.GetValue("name");
                    try
                    {
                        child = new ContractGroup(name);
                    }
                    catch (ArgumentException)
                    {
                        LoggingUtil.LogError(this, "Couldn't load CONTRACT_GROUP '" + name + "' due to a duplicate name.");
                        valid = false;
                        continue;
                    }

                    valid &= child.Load(childNode);
                    child.parent = this;
                    if (child.hasWarnings)
                    {
                        hasWarnings = true;
                    }
                }

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
            foreach (ContractGroup child in AllGroups.Where(g => g.parent == this))
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
