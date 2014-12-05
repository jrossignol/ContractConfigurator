using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using Contracts;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /*
     * ParameterFactory wrapper for ReturnFrom ContractParameter.
     */
    public class ReturnFromFactory : ParameterFactory
    {
        protected KSPAchievements.ReturnFrom situation { get; set; }
        protected string title { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Validate target body
            if (targetBody == null)
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": targetBody for ReachDestination must be specified.");
            }

            // Get returnFrom
            if (!configNode.HasValue("situation"))
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": missing required value 'situation'.");
            }
            try
            {
                string situationStr = configNode.GetValue("situation");
                situation = (KSPAchievements.ReturnFrom)Enum.Parse(typeof(KSPAchievements.ReturnFrom), situationStr);
            }
            catch (Exception e)
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": error parsing situation: " + e.Message);
            }

            // Get title
            title = configNode.HasValue("title") ? configNode.GetValue("title") : null;

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new Parameters.ReturnFrom(targetBody, situation, title);
        }
    }
}
