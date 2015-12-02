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
    /// Behaviour for changing vessel ownership.
    /// </summary>
    public class ChangeVesselOwnership : TriggeredBehaviour
    {
        private List<string> vessels;
        private bool owned;

        public ChangeVesselOwnership()
        {
        }

        public ChangeVesselOwnership(State onState, List<string> vessels, bool owned, List<string> parameter)
            : base(onState, parameter)
        {
            this.vessels = vessels;
            this.owned = owned;
        }

        protected override void TriggerAction()
        {
            foreach (Vessel vessel in vessels.Select(v => ContractVesselTracker.Instance.GetAssociatedVessel(v)))
            {
                if (vessel != null)
                {
                    vessel.DiscoveryInfo.SetLevel(owned ? DiscoveryLevels.Owned : DiscoveryLevels.Unowned);
                }
            }
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);
            owned = ConfigNodeUtil.ParseValue<bool>(configNode, "owned");
            vessels = ConfigNodeUtil.ParseValue<List<string>>(configNode, "vessel", new List<string>());
        }

        protected override void OnSave(ConfigNode configNode)
        {
            base.OnSave(configNode);
            configNode.AddValue("owned", owned);
            foreach (string v in vessels)
            {
                configNode.AddValue("vessel", v);
            }
        }
    }
}
