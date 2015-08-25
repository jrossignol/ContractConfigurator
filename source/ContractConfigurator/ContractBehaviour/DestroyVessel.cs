using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;
using ContractConfigurator.Parameters;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Behaviour for destroying one or more vessels.
    /// </summary>
    public class DestroyVessel : TriggeredBehaviour
    {
        private List<string> vessels;

        public DestroyVessel()
        {
        }

        public DestroyVessel(State onState, List<string> vessels, List<string> parameter)
            : base(onState, parameter)
        {
            this.vessels = vessels;
        }

        protected override void TriggerAction()
        {
            foreach (Vessel vessel in vessels.Select(v => ContractVesselTracker.Instance.GetAssociatedVessel(v)))
            {
                if (vessel != null)
                {
                    vessel.Die();
                }
            }
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);
            vessels = ConfigNodeUtil.ParseValue<List<string>>(configNode, "vessel", new List<string>());
        }

        protected override void OnSave(ConfigNode configNode)
        {
            base.OnSave(configNode);
            foreach (string v in vessels)
            {
                configNode.AddValue("vessel", v);
            }
        }
    }
}
