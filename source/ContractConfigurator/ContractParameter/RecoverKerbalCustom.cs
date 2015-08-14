using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Behaviour;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Parameter for recovering a Kerbal.
    /// </summary>
    public class RecoverKerbalCustom : ContractConfiguratorParameter, ParameterDelegateContainer
    {
        protected int index;
        protected int count;
        protected List<string> kerbals = new List<string>();
        protected Dictionary<string, bool> recovered = new Dictionary<string, bool>();

        public bool ChildChanged { get; set; }

        public RecoverKerbalCustom()
            : base(null)
        {
        }

        public RecoverKerbalCustom(IEnumerable<string> kerbals, int index, int count, string title)
            : base(title)
        {
            this.index = index;
            this.count = count;
            this.kerbals = kerbals.ToList();
            foreach (string kerbal in kerbals)
            {
                recovered[kerbal] = false;
            }

            if (kerbals.Count() + count == 1)
            {
                hideChildren = true;
            }

            CreateDelegates();
        }

        protected override string GetParameterTitle()
        {
            string output = "";
            if (string.IsNullOrEmpty(title))
            {
                if (kerbals.Count == 1)
                {
                    output = kerbals[0] + " recovered";
                }
                else
                {
                    output = "Kerbals recovered";
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
            foreach (string kerbal in kerbals)
            {
                AddParameter(new ParameterDelegate<string>(kerbal + ": Recovered",
                    unused => recovered[kerbal], ParameterDelegateMatchType.FILTER));
            }
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            node.AddValue("count", count);
            node.AddValue("index", index);
            foreach (string kerbal in kerbals)
            {
                ConfigNode childNode = new ConfigNode("KERBAL");
                node.AddNode(childNode);
                childNode.AddValue("kerbal", kerbal);
                childNode.AddValue("recovered", recovered[kerbal]);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            try
            {
                count = ConfigNodeUtil.ParseValue<int>(node, "count");
                index = ConfigNodeUtil.ParseValue<int>(node, "index");

                foreach (ConfigNode childNode in node.GetNodes("KERBAL"))
                {
                    string kerbal = ConfigNodeUtil.ParseValue<string>(childNode, "kerbal");
                    kerbals.Add(kerbal);
                    recovered[kerbal] = ConfigNodeUtil.ParseValue<bool>(childNode, "recovered");
                }

                CreateDelegates();
            }
            finally
            {
                ParameterDelegate<string>.OnDelegateContainerLoad(node);
            }
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onVesselCreate.Add(new EventData<Vessel>.OnEvent(OnVesselCreate));
            GameEvents.onCrewTransferred.Add(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(OnCrewTransferred));
            GameEvents.onVesselRecovered.Add(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
            GameEvents.onCrewKilled.Add(new EventData<EventReport>.OnEvent(OnCrewKilled));
            GameEvents.Contract.onAccepted.Add(new EventData<Contract>.OnEvent(OnContractAccepted));
            ContractConfigurator.OnParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onVesselCreate.Remove(new EventData<Vessel>.OnEvent(OnVesselCreate));
            GameEvents.onCrewTransferred.Remove(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(OnCrewTransferred));
            GameEvents.onVesselRecovered.Remove(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
            GameEvents.onCrewKilled.Remove(new EventData<EventReport>.OnEvent(OnCrewKilled));
            GameEvents.Contract.onAccepted.Remove(new EventData<Contract>.OnEvent(OnContractAccepted));
            ContractConfigurator.OnParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        private void OnVesselCreate(Vessel v)
        {
            foreach (ProtoCrewMember crew in v.GetVesselCrew())
            {
                if (recovered.ContainsKey(crew.name))
                {
                    recovered[crew.name] = false;
                }
            }

            TestConditions();
        }

        private void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> evt)
        {
            if (recovered.ContainsKey(evt.host.name))
            {
                recovered[evt.host.name] = false;
            }

            TestConditions();
        }

        private void OnVesselRecovered(ProtoVessel v)
        {
            // Don't check if we're not ready to complete
            if (!ReadyToComplete())
            {
                return;
            }

            foreach (ProtoCrewMember crew in v.GetVesselCrew())
            {
                if (recovered.ContainsKey(crew.name))
                {
                    recovered[crew.name] = true;
                }
            }

            TestConditions();
        }

        private void OnCrewKilled(EventReport evt)
        {
            if (recovered.ContainsKey(evt.sender))
            {
                MessageSystem.Instance.AddMessage(new MessageSystem.Message("Contract failed", evt.sender + " has been killed!",
                    MessageSystemButton.MessageButtonColor.RED, MessageSystemButton.ButtonIcons.MESSAGE));
                SetState(ParameterState.Failed);
            }
        }

        protected void OnContractAccepted(Contract contract)
        {
            if (contract == Root && kerbals.Count == 0)
            {
                int count = this.count == 0 ? ((ConfiguredContract)contract).GetSpawnedKerbalCount() : this.count;
                for (int i = 0; i < count; i++)
                {
                    ProtoCrewMember kerbal = ((ConfiguredContract)contract).GetSpawnedKerbal(index + i);
                    kerbals.Add(kerbal.name);
                    recovered[kerbal.name] = false;
                }

                CreateDelegates();
            }
        }

        private void OnParameterChange(Contract contract, ContractParameter parameter)
        {
            if (contract != Root || parameter == this)
            {
                return;
            }

            TestConditions();
        }

        private void TestConditions()
        {
            // Retest the conditions
            bool success = ParameterDelegate<string>.CheckChildConditions(this, "");
            if (ChildChanged || success)
            {
                ChildChanged = false;
                if (success)
                {
                    SetState(ParameterState.Complete);
                }
                else
                {
                    SetState(ParameterState.Incomplete);
                    ContractConfigurator.OnParameterChange.Fire(Root, this);
                }
            }
        }
    }
}
