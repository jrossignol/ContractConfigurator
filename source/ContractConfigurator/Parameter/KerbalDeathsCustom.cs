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
    /// <summary>
    /// Custom implementation of the KerbalDeaths parameter
    /// </summary>
    public class KerbalDeathsCustom : ContractConfiguratorParameter, IKerbalNameStorage
    {
        int countMax;
        int count = 0;
        protected List<Kerbal> kerbals = new List<Kerbal>();
        protected VesselIdentifier vesselIdentifier;

        private TitleTracker titleTracker = new TitleTracker();

        public KerbalDeathsCustom()
            : base()
        {
            disableOnStateChange = false;
        }

        public KerbalDeathsCustom(int countMax, IEnumerable<Kerbal> kerbals, VesselIdentifier vesselIdentifier, string title)
            : base(title)
        {
            this.countMax = countMax;
            this.kerbals = kerbals.ToList();
            this.vesselIdentifier = vesselIdentifier;

            disableOnStateChange = false;

            CreateDelegates();
        }

        protected override string GetParameterTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                if (vesselIdentifier != null)
                {
                    if (countMax == 1)
                    {
                        output = "Kill no Kerbals";
                    }
                    else
                    {
                        output = "Kill no more than " + countMax + " Kerbals";
                    }
                    output += " on vessel " + ContractVesselTracker.GetDisplayName(vesselIdentifier.identifier);
                }
                else if (!kerbals.Any())
                {
                    if (countMax == 1)
                    {
                        output = "Kill no Kerbals";
                    }
                    else
                    {
                        output = "Kill no more than " + countMax + " Kerbals";
                    }
                }
                else
                {
                    output = "Do not kill";
                    if (state != ParameterState.Incomplete || ParameterCount == 1)
                    {
                        if (ParameterCount == 1)
                        {
                            hideChildren = true;
                        }

                        output += ": " + ParameterDelegate<ProtoCrewMember>.GetDelegateText(this);
                    }
                }
            }
            else
            {
                output = title;
            }

            // Add the string that we returned to the titleTracker.  This is used to update
            // the contract title element in the GUI directly, as it does not support dynamic
            // text.
            titleTracker.Add(output);

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
            ContractVesselTracker.OnVesselAssociation.Add(new EventData<GameEvents.HostTargetAction<Vessel, string>>.OnEvent(OnVesselAssociation));
            ContractVesselTracker.OnVesselDisassociation.Add(new EventData<GameEvents.HostTargetAction<Vessel, string>>.OnEvent(OnVesselDisassociation));
            GameEvents.Contract.onParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
            ContractConfigurator.OnParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected override void OnUnregister()
        {
            GameEvents.onCrewKilled.Remove(new EventData<EventReport>.OnEvent(OnCrewKilled));
            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(OnVesselChange));
            ContractVesselTracker.OnVesselAssociation.Remove(new EventData<GameEvents.HostTargetAction<Vessel, string>>.OnEvent(OnVesselAssociation));
            ContractVesselTracker.OnVesselDisassociation.Remove(new EventData<GameEvents.HostTargetAction<Vessel, string>>.OnEvent(OnVesselDisassociation));
            GameEvents.Contract.onParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
            ContractConfigurator.OnParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
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
                LoggingUtil.LogVerbose(this, "wrong contract");
                return;
            }

            if (Root.GetChildren().All(param => param.State == ParameterState.Complete || param == this || param.Optional))
            {
                SetState(ParameterState.Complete);
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

        private void OnVesselAssociation(GameEvents.HostTargetAction<Vessel, string> hta)
        {
            LoggingUtil.LogVerbose(this, "OnVesselAssociation");
            if (vesselIdentifier != null && hta.target == vesselIdentifier.identifier)
            {
                HandleVessel(hta.host);
                titleTracker.UpdateContractWindow(this, GetTitle());
            }
        }

        private void OnVesselDisassociation(GameEvents.HostTargetAction<Vessel, string> hta)
        {
            LoggingUtil.LogVerbose(this, "OnVesselDisassociation");
            if (vesselIdentifier != null && hta.target == vesselIdentifier.identifier)
            {
                titleTracker.UpdateContractWindow(this, GetTitle());
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
            LoggingUtil.LogVerbose(this, "    report.sender = " + report.sender);

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
