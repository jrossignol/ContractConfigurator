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
    /// Parameter for checking whether a vessel has space for passengers.
    /// </summary>
    public class HasPassengers : VesselParameter
    {
        protected int index;
        protected int count;
        private List<ProtoCrewMember> passengers = new List<ProtoCrewMember>();

        public HasPassengers()
            : base(null)
        {
        }

        public HasPassengers(string title, int index, int count)
            : base(title)
        {
            this.index = index;
            this.count = count;

            CreateDelegates();
        }

        protected override string GetTitle()
        {
            string output = "";
            if (string.IsNullOrEmpty(title))
            {
                if (passengers.Count == 0)
                {
                    output = "Load " + count + " passenger" + (count > 1 ? "s" : "") + " while on the launchpad/runway";
                }
                else if (state == ParameterState.Complete)
                {
                    output = "Passengers: " + count;
                }
                else
                {
                    output = "Passengers";
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
            // Filter for celestial bodies
            if (passengers.Count > 0)
            {
                foreach (ProtoCrewMember passenger in passengers)
                {
                    AddParameter(new ParameterDelegate<Vessel>("On Board: " + passenger.name,
                        v => v.GetVesselCrew().Contains(passenger), ParameterDelegateMatchType.VALIDATE_ALL));
                }
            }
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);
            node.AddValue("count", count);

            foreach (ProtoCrewMember passenger in passengers)
            {
                node.AddValue("passenger", passenger.name);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            count = Convert.ToInt32(node.GetValue("count"));
            passengers = ConfigNodeUtil.ParseValue<List<ProtoCrewMember>>(node, "passenger", new List<ProtoCrewMember>());

            ParameterDelegate<Vessel>.OnDelegateContainerLoad(node);
            CreateDelegates();
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onCrewTransferred.Add(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(OnCrewTransferred));
            GameEvents.Contract.onAccepted.Add(new EventData<Contract>.OnEvent(OnContractAccepted));
            SpawnPassenger.onPassengersLoaded.Add(new EventVoid.OnEvent(OnPassengersLoaded));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onCrewTransferred.Remove(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(OnCrewTransferred));
            GameEvents.Contract.onAccepted.Remove(new EventData<Contract>.OnEvent(OnContractAccepted));
            SpawnPassenger.onPassengersLoaded.Remove(new EventVoid.OnEvent(OnPassengersLoaded));
        }

        protected void OnContractAccepted(Contract contract)
        {
            if (contract == Root)
            {
                for (int i = 0; i < count; i++)
                {
                    ProtoCrewMember kerbal = ((ConfiguredContract)contract).GetSpawnedKerbal(index+i);
                    passengers.Add(kerbal);
                }
                CreateDelegates();
            }
        }

        protected void OnPassengersLoaded()
        {
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected override void OnPartAttach(GameEvents.HostTargetAction<Part, Part> e)
        {
            base.OnPartAttach(e);
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected override void OnVesselCreate(Vessel vessel)
        {
            base.OnVesselCreate(vessel);
            CheckVessel(vessel);
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> a)
        {
            // Check both, as the Kerbal/ship swap spots depending on whether the vessel is
            // incoming or outgoing
            CheckVessel(a.from.vessel);
            CheckVessel(a.to.vessel);
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check.</param>
        /// <returns>Whether the vessel meets the conditions.</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);

            // No passengers loaded
            if (passengers.Count == 0)
            {
                return false;
            }

            return ParameterDelegate<Vessel>.CheckChildConditions(this, vessel);
        }
    }
}
