using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator
{
    /*
     * ParameterFactory wrapper for KerbalDeaths ContractParameter.  Also, if you need a KSP themed
     * band name, I think Kerbal Death Factory seems suitable...
     */
    public class KerbalDeathsFactory : ParameterFactory
    {
        protected int countMax { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get countMax
            if (configNode.HasValue("countMax"))
            {
                if (Convert.ToInt32(configNode.GetValue("countMax")) <= 0)
                {
                    valid = false;
                    LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                        ": invalid value of " + configNode.GetValue("countMax") + " for countMax.  Must be an integer greater than zero.");
                }
                countMax = Convert.ToInt32(configNode.GetValue("countMax"));
            }
            else
            {
                countMax = 1;
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new KerbalDeaths(countMax);
        }
    }
}
