using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using Contracts;

namespace ContractConfigurator
{
    /*
     * ContractRequirement to provide requirement for player having completed other contracts.
     */
    public class CompleteContractRequirement : ContractRequirement
    {
        protected string ccType { get; set; }
        protected Type contractClass { get; set; }
        protected uint minCount { get; set; }
        protected uint maxCount { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get type
            if (!configNode.HasValue("contractType"))
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": missing required value 'contractType'.");
            }
            else
            {
                string contractType = configNode.GetValue("contractType");

                if (ContractType.contractTypes.Keys.Contains(contractType))
                {
                    ccType = contractType;
                }
                else
                {
                    ccType = null;

                    // Search for the correct type
                    var classes =
                        from assembly in AppDomain.CurrentDomain.GetAssemblies()
                        from type in assembly.GetTypes()
                        where type.IsSubclassOf(typeof(Contract)) && type.Name.Equals(contractType)
                        select type;

                    if (classes.Count() < 1)
                    {
                        valid = false;
                        Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) + ": contractType '" + contractType +
                            "' must either be a Contract sub-class or ContractConfigurator contract type");
                    }
                    contractClass = classes.First();
                }
            }

            // Get minCount
            if (!configNode.HasValue("minCount"))
            {
                minCount = 1;
            }
            else
            {
                try
                {
                    minCount = Convert.ToUInt32(configNode.GetValue("minCount"));
                }
                catch (Exception e)
                {
                    valid = false;
                    Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                        ": minCount: " + e.Message);
                }
            }

            // Get maxCount
            if (!configNode.HasValue("maxCount"))
            {
                maxCount = UInt32.MaxValue;
            }
            else
            {
                try
                {
                    maxCount = Convert.ToUInt32(configNode.GetValue("maxCount"));
                }
                catch (Exception e)
                {
                    valid = false;
                    Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                        ": maxCount: " + e.Message);
                }
            }

            return valid;
        }

        public override bool RequirementMet(ContractType contractType)
        {
            int finished = 0;
            if (ccType != null)
            {
                finished = ContractSystem.Instance.GetCompletedContracts<ConfiguredContract>().Count(c => c.contractType.name.Equals(ccType));

            }
            else
            {
                // Call the GetCompletedContracts with our type, and get the count
                Contract[] completedContract = (Contract[])typeof(ContractSystem).GetMethod("GetCompletedContracts").MakeGenericMethod(contractClass).Invoke(ContractSystem.Instance, null);
                finished = completedContract.Count();
            }
            return (finished >= minCount) && (finished <= maxCount);
        }
    }
}
