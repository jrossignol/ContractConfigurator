using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Behaviour;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for RecoverKerbal ContractParameter.
    /// </summary>
    public class RecoverKerbalFactory : ParameterFactory
    {
        protected string kerbal;
        protected int index;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "kerbal", x => kerbal = x, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "index", x => index = x, this, -1);
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "kerbal", "index" }, this);

            // Manually validate, since the default is technically invalid
            if (kerbal == null)
            {
                valid &= Validation.GE(index, 0);
            }
            
            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            RecoverKerbal contractParam = new RecoverKerbal(title);

            // Add the kerbal
            string kerbalName;
            if (kerbal != null)
            {
                kerbalName = kerbal;
            }
            else
            {
                kerbalName = ((ConfiguredContract)contract).GetSpawnedKerbal(index).name;
            }

            contractParam.AddKerbal(kerbalName);

            // Get title
            if (title == null)
            {
                contractParam.SetTitle(kerbalName + ": Recovered");
            }

            return contractParam;
        }
    }
}
