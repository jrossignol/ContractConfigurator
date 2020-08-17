using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using FinePrint.Utilities;
using KSP.Localization;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Parameter for checking that a vessel is launched after the contract was accepted.
    /// </summary>
    public class NewVessel : VesselParameter
    {
        private uint launchID;

        public NewVessel()
            : this(null)
        {
        }

        public NewVessel(string title)
            : base(title)
        {
            this.title = string.IsNullOrEmpty(title) ? Localizer.GetStringByTag("#cc.param.NewVessel") : title;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);

            node.AddValue("launchID", launchID);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);

            launchID = ConfigNodeUtil.ParseValue<uint>(node, "launchID");
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.Contract.onAccepted.Add(new EventData<Contract>.OnEvent(OnContractAccepted));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.Contract.onAccepted.Remove(new EventData<Contract>.OnEvent(OnContractAccepted));
        }

        protected void OnContractAccepted(Contract c)
        {
            if (c == Root)
            {
                launchID = HighLogic.CurrentGame.launchID;
            }
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: {0}", vessel.id);
            return VesselUtilities.VesselLaunchedAfterID(launchID, vessel);
        }
    }
}
