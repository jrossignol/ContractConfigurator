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
    /// <summary>
    /// ContractRequirement to provide a check against contracts.
    /// </summary>
    public abstract class ContractCheckRequirement : ContractRequirement
    {
        protected string ccType;
        protected Type contractClass;
        protected uint minCount;
        protected uint maxCount;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            checkOnActiveContract = true;

            // Get type
            string contractType = null;
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "contractType", x => contractType = x, this);
            if (valid)
            {
                valid &= SetValues(contractType);
            }

            valid &= ConfigNodeUtil.ParseValue<uint>(configNode, "minCount", x => minCount = x, this, 1);
            valid &= ConfigNodeUtil.ParseValue<uint>(configNode, "maxCount", x => maxCount = x, this, UInt32.MaxValue);

            return valid;
        }

        private bool SetValues(string contractType)
        {
            bool valid = true;
            if (ContractType.GetContractType(contractType) != null)
            {
                ccType = contractType;
            }
            else
            {
                ccType = null;

                IEnumerable<Type> classes = ContractConfigurator.GetAllTypes<Contract>().Where(t => t.Name == contractType);
                if (!classes.Any())
                {
                    valid = false;
                    LoggingUtil.LogError(this.GetType(), "contractType '" + contractType +
                        "' must either be a Contract sub-class or ContractConfigurator contract type");
                }
                else
                {
                    contractClass = classes.First();
                }
            }
            return valid;
        }

        public override void SaveToPersistence(ConfigNode configNode)
        {
            base.SaveToPersistence(configNode);

            configNode.AddValue("minCount", minCount);
            configNode.AddValue("maxCount", maxCount);
            if (ccType != null)
            {
                configNode.AddValue("contractType", ccType);
            }
            else
            {
                configNode.AddValue("contractType", contractClass.Name);
            }
        }

        public override void LoadFromPersistence(ConfigNode configNode)
        {
            base.LoadFromPersistence(configNode);

            minCount = ConfigNodeUtil.ParseValue<uint>(configNode, "minCount");
            maxCount = ConfigNodeUtil.ParseValue<uint>(configNode, "maxCount");

            string contractType = ConfigNodeUtil.ParseValue<string>(configNode, "contractType");
            SetValues(contractType);
        }
    }
}
