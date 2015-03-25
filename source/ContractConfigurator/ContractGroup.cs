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
                yield return null;
            }
        }

        // Group attributes
        public string name;
        public string minVersion;
        public int maxCompletions;
        public int maxSimultaneous;

        public bool expandInDebug = false;
        public bool enabled { get; private set; }
        public string config { get; private set; }
        public string log { get; private set; }
        public DataNode dataNode { get; private set; }

        public ContractGroup(string name)
        {
            this.name = name;
            contractGroups.Add(name, this);
        }

        ~ContractGroup()
        {
            contractGroups.Remove(name);
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
                enabled = valid;
                log += LoggingUtil.capturedLog;
                LoggingUtil.CaptureLog = false;

                return valid;
            }
            catch
            {
                enabled = false;
                throw;
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
