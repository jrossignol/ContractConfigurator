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
    /*
     * ParameterFactory wrapper for RecoverKerbal ContractParameter.
     */
    public class RecoverKerbalFactory : ParameterFactory
    {
        protected string kerbal;
        protected int index;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "vessel", ref kerbal, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "index", ref index, this, -1);
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "vessel", "index" }, this);

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

            // Add the vessel
            string kerbalName;
            if (kerbal != null)
            {
                kerbalName = kerbal;
            }
            else
            {
                SpawnKerbal spawnKerbal = ((ConfiguredContract)contract).Behaviours.OfType<SpawnKerbal>().First<SpawnKerbal>();
                kerbalName = spawnKerbal.GetKerbalName(index);
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
