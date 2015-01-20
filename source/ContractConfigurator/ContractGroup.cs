using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Agents;

namespace ContractConfigurator
{
    /// <summary>
    /// Class for grouping contract types.
    /// </summary>
    public class ContractGroup : IContractConfiguratorFactory
    {
        public static Dictionary<string, ContractGroup> contractGroups = new Dictionary<string, ContractGroup>();

        // Group attributes
        public string name;
        public int maxCompletions;
        public int maxSimultaneous;

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
            ConfigNodeUtil.ClearFoundCache();
            bool valid = true;

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "name", ref name, this);
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCompletions", ref maxCompletions, this, 0, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxSimultaneous", ref maxSimultaneous, this, 0, x => Validation.GE(x, 0));
            
            // Check for unexpected values - always do this last
            valid &= ConfigNodeUtil.ValidateUnexpectedValues(configNode, this);

            return valid;
        }

        /// <summary>
        /// Returns the name of the contract group.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "ContractGroup[" + name + "]";
        }
        
        public string ErrorPrefix()
        {
            return "CONTRACT_GROUP '" + name + "'";
        }

        public string ErrorPrefix(ConfigNode configNode)
        {
            return ErrorPrefix();
        }

    }
}
