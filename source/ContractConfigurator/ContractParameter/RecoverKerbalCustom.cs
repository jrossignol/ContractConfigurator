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

        public RecoverKerbalCustom(IEnumerable<ProtoCrewMember> kerbals, int index, int count, string title)
            : base(title)
        {
            this.index = index;
            this.count = count;
            this.kerbals = kerbals.Select<ProtoCrewMember, string>(k => k.name).ToList();
            foreach (ProtoCrewMember kerbal in kerbals)
            {
                recovered[kerbal.name] = false;
            }

            CreateDelegates();
        }

        protected override string GetTitle()
        {
            string output = "";
            if (string.IsNullOrEmpty(title))
            {
                output = "Kerbal" + (kerbals.Count != 1 ? "s" : "") + " recovered";
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
                    unused => recovered[kerbal], ParameterDelegateMatchType.VALIDATE_ALL));
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
            count = ConfigNodeUtil.ParseValue<int>(node, "count");
            index = ConfigNodeUtil.ParseValue<int>(node, "index");

            foreach (ConfigNode childNode in node.GetNodes("KERBAL"))
            {
                string kerbal = ConfigNodeUtil.ParseValue<string>(childNode, "kerbal");
                kerbals.Add(kerbal);
                recovered[kerbal] = ConfigNodeUtil.ParseValue<bool>(childNode, "recovered");
            }

            ParameterDelegate<string>.OnDelegateContainerLoad(node);
            CreateDelegates();
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onVesselRecovered.Add(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
            GameEvents.onCrewKilled.Add(new EventData<EventReport>.OnEvent(OnCrewKilled));
            GameEvents.Contract.onAccepted.Add(new EventData<Contract>.OnEvent(OnContractAccepted));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onVesselRecovered.Remove(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
            GameEvents.onCrewKilled.Remove(new EventData<EventReport>.OnEvent(OnCrewKilled));
            GameEvents.Contract.onAccepted.Remove(new EventData<Contract>.OnEvent(OnContractAccepted));
        }

        private void OnVesselRecovered(ProtoVessel v)
        {
            foreach (ProtoCrewMember crew in v.GetVesselCrew())
            {
                if (recovered.ContainsKey(crew.name))
                {
                    recovered[crew.name] = true;
                }
            }

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
                    ContractConfigurator.OnParameterChange.Fire(Root, this);
                }
            }

            ContractConfigurator.OnParameterChange.Fire(Root, this);
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
    }
}
