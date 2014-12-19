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
        protected string kerbal { get; set; }
        protected int index { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get Kerbal
            kerbal = "";
            if (configNode.HasValue("kerbal"))
            {
                kerbal = configNode.GetValue("kerbal");
                index = -1;
            }
            else if (configNode.HasValue("index"))
            {
                kerbal = null;
                index = Convert.ToInt32(configNode.GetValue("index"));
            }
            else
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": missing required value 'kerbal' or 'index'.");
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
