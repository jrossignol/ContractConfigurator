using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator.Parameters
{
    /*
     * Special parameter wrapper which is always completed, and invisible.  Use it to wrap other 
     * parameters that need to be hidden and not impact the contract.
     */
    public class AlwaysTrue : Contracts.ContractParameter
    {
        protected string title { get; set; }

        public AlwaysTrue()
            : base()
        {
            disableOnStateChange = false;
        }


        protected override string GetTitle()
        {
            return "";
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (state != ParameterState.Complete && Root != null)
            {
                SetComplete();
            }
        }

        protected override string GetHashString()
        {
            return (this.Root.MissionSeed.ToString() + this.Root.DateAccepted.ToString() + this.ID);
        }

        public static AlwaysTrue FetchOrAdd(Contract contract)
        {
            // Fetch the AlwaysTrue wrapper
            IEnumerable<ContractParameter> parameters = contract.AllParameters.Where<ContractParameter>(p => p.GetType() == typeof(AlwaysTrue));
            if (parameters.Count() == 0)
            {
                AlwaysTrue alwaysTrue = new AlwaysTrue();
                contract.AddParameter(alwaysTrue);
                return alwaysTrue;
            }
            else
            {
                return parameters.First() as AlwaysTrue;
            }
        }

    }
}
