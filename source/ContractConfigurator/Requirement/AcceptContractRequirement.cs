using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator.Util;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement to provide requirement for player having accepted other contracts.
    /// </summary>
    public class AcceptContractRequirement : ContractCheckRequirement
    {
        public override bool RequirementMet(ConfiguredContract contract)
        {
            // Get the count of accepted contracts
            int accepted = 0;

            // Finished contracts - Contract Configurator style
            if (ccType != null)
            {
                IEnumerable<ConfiguredContract> acceptedContract = ContractSystem.Instance.Contracts.OfType<ConfiguredContract>().
                    Where(c => c != null && c.contractType != null && c.contractType.name.Equals(ccType) && c.ContractState == Contract.State.Active);
                accepted = acceptedContract.Count();
            }
            // Finished contracts - stock style
            else
            {
                // Call the GetCompletedContracts with our type, and get the count
                IEnumerable<Contract> acceptedContract = ContractSystem.Instance.Contracts.Where(c => c != null && c.GetType() == contractClass &&
                    c.ContractState == Contract.State.Active);
                accepted = acceptedContract.Count();
            }

            // Return based on the min/max counts configured
            return (accepted >= minCount) && (accepted <= maxCount);
        }

        protected override string RequirementText()
        {
            return "Must " + (invertRequirement ? "not " : "") + "have accepted contract <color=#" + MissionControlUI.RequirementHighlightColor + ">'" + ContractTitle() + "'</color>";
        }
    }
}
