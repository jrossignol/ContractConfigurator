using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using KSP.Localization;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Custom implementation of the KerbalDeaths parameter
    /// </summary>
    public class KerbalDeathsCustom : ContractConfiguratorParameter, IKerbalNameStorage
    {
        int countMax;
        int count = 0;
        protected List<Kerbal> kerbals = new List<Kerbal>();
        protected VesselIdentifier vesselIdentifier;

        public KerbalDeathsCustom()
            : base()
        {
        }

        public KerbalDeathsCustom(int countMax, IEnumerable<Kerbal> kerbals, VesselIdentifier vesselIdentifier, string title)
            : base(title)
        {
            this.countMax = countMax;
            this.kerbals = kerbals.ToList();
            this.vesselIdentifier = vesselIdentifier;

            disableOnStateChange = false;
            state = ParameterState.Complete;

            CreateDelegates();
        }

        protected override string GetParameterTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                if (vesselIdentifier != null)
                {
                    output = Localizer.Format("#cc.param.KerbalDeaths.vessel", countMax - 1, ContractVesselTracker.GetDisplayName(vesselIdentifier.identifier));
                }
                else if (!kerbals.Any())
                {
                    output = Localizer.Format("#cc.param.KerbalDeaths.generic", countMax - 1);
                }
                else
                {
                    output = Localizer.Format("#cc.param.KerbalDeaths.specific");
                    if (state != ParameterState.Incomplete || ParameterCount == 1)
                    {
                        if (ParameterCount == 1)
                        {
                            output = StringBuilderCache.Format("{0}: {1}", output, ParameterDelegate<ProtoCrewMember>.GetDelegateText(this));
                            hideChildren = true;
                        }
                    }
                }
            }
            else
            {
                output = title;
            }

            return output;
        }

        protected void CreateDelegates()
        {
            // Validate specific kerbals
            foreach (Kerbal kerbal in kerbals)
            {
                AddParameter(new ParameterDelegate<ProtoCrewMember>(kerbal.name, pcm => true));
            }
        }

        protected override void OnRegister()
        {
            GameEvents.onCrewKilled.Add(new EventData<EventReport>.OnEvent(OnCrewKilled));
            GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(OnVesselChange));
            GameEvents.onVesselCreate.Add(new EventData<Vessel>.OnEvent(OnVesselCreate));
            ContractVesselTracker.OnVesselAssociation.Add(new EventData<GameEvents.HostTargetAction<Vessel, string>>.OnEvent(OnVesselAssociation));
            ContractVesselTracker.OnVesselDisassociation.Add(new EventData<GameEvents.HostTargetAction<Vessel, string>>.OnEvent(OnVesselDisassociation));
            GameEvents.Contract.onParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected override void OnUnregister()
        {
            GameEvents.onCrewKilled.Remove(new EventData<EventReport>.OnEvent(OnCrewKilled));
            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(OnVesselChange));
            GameEvents.onVesselCreate.Remove(new EventData<Vessel>.OnEvent(OnVesselCreate));
            ContractVesselTracker.OnVesselAssociation.Remove(new EventData<GameEvents.HostTargetAction<Vessel, string>>.OnEvent(OnVesselAssociation));
            ContractVesselTracker.OnVesselDisassociation.Remove(new EventData<GameEvents.HostTargetAction<Vessel, string>>.OnEvent(OnVesselDisassociation));
            GameEvents.Contract.onParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            node.AddValue("countMax", countMax);
            node.AddValue("count", count);
            if (vesselIdentifier != null)
            {
                node.AddValue("vesselIdentifier", vesselIdentifier);
            }

            foreach (Kerbal kerbal in kerbals)
            {
                ConfigNode kerbalNode = new ConfigNode("KERBAL");
                node.AddNode(kerbalNode);

                kerbal.Save(kerbalNode);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            try
            {
                countMax = ConfigNodeUtil.ParseValue<int>(node, "countMax");
                count = ConfigNodeUtil.ParseValue<int>(node, "count");
                vesselIdentifier = ConfigNodeUtil.ParseValue<VesselIdentifier>(node, "vesselIdentifier", (VesselIdentifier)null);

                foreach (ConfigNode kerbalNode in node.GetNodes("KERBAL"))
                {
                    kerbals.Add(Kerbal.Load(kerbalNode));
                }

                CreateDelegates();
            }
            finally
            {
                ParameterDelegate<ProtoCrewMember>.OnDelegateContainerLoad(node);
            }
        }

        protected void OnParameterChange(Contract c, ContractParameter p)
        {
            LoggingUtil.LogVerbose(this, "OnParameterChange");
            if (c != Root)
            {
                return;
            }
        }

        private void OnVesselChange(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "OnVesselChange");
            if (vesselIdentifier != null && vessel != null && ContractVesselTracker.Instance.GetAssociatedVessel(vesselIdentifier.identifier) == vessel)
            {
                HandleVessel(vessel);
            }
        }

        private void OnVesselCreate(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "OnVesselCreate");
            if (vesselIdentifier != null && vessel != null && ContractVesselTracker.Instance.GetAssociatedVessel(vesselIdentifier.identifier) == vessel)
            {
                HandleVessel(vessel);
            }
        }

        private void OnVesselAssociation(GameEvents.HostTargetAction<Vessel, string> hta)
        {
            LoggingUtil.LogVerbose(this, "OnVesselAssociation");
            if (vesselIdentifier != null && hta.target == vesselIdentifier.identifier)
            {
                HandleVessel(hta.host);

                // Force a call to GetTitle to update the contracts app
                GetTitle();
            }
        }

        private void OnVesselDisassociation(GameEvents.HostTargetAction<Vessel, string> hta)
        {
            LoggingUtil.LogVerbose(this, "OnVesselDisassociation");
            if (vesselIdentifier != null && hta.target == vesselIdentifier.identifier)
            {
                // Force a call to GetTitle to update the contracts app
                GetTitle();
            }
        }

        private void HandleVessel(Vessel vessel)
        {
            foreach (ProtoCrewMember pcm in vessel.GetVesselCrew())
            {
                AddCrewToList(pcm);
            }
        }

        private void AddCrewToList(ProtoCrewMember pcm)
        {
            if (!kerbals.Any(k => k.pcm == pcm))
            {
                kerbals.Add(new Kerbal(pcm));
                AddParameter(new ParameterDelegate<ProtoCrewMember>(pcm.name, unused => true));
            }
        }

        private void OnCrewKilled(EventReport report)
        {
            if (report.eventType != FlightEvents.CREW_KILLED)
            {
                return;
            }

            LoggingUtil.LogVerbose(this, "OnCrewKilled");
            LoggingUtil.LogVerbose(this, "    report.sender = {0}", report.sender);

            if (kerbals.Any() || vesselIdentifier != null)
            {
                if (kerbals.Any(k => k.name == report.sender))
                {
                    SetState(ParameterState.Failed);
                }
            }
            else if (++count >= countMax) 
            {
                SetState(ParameterState.Failed);
            }

        }

        public IEnumerable<string> KerbalNames()
        {
            foreach (Kerbal kerbal in kerbals)
            {
                yield return kerbal.name;
            }
        }
    }
}
