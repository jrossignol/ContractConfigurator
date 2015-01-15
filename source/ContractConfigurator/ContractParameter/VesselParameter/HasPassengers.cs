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
     * Parameter for checking whether a vessel has space for passengers.
     */
    public class HasPassengers : VesselParameter
    {
        protected string title { get; set; }
        protected int minPassengers { get; set; }
        protected int maxPassengers { get; set; }

        public HasPassengers()
            : this(null)
        {
        }

        public HasPassengers(string title, int minPassengers = 1, int maxPassengers = int.MaxValue)
            : base()
        {
            this.minPassengers = minPassengers;
            this.maxPassengers = maxPassengers;

            // Validate min/max Passengers
            if (minPassengers > maxPassengers)
            {
                throw new ArgumentException("HasPassengers parameter: minPassengers must be less than maxPassengers!");
            }

            if (title == null)
            {
                this.title = "Passengers: ";
                if (maxPassengers == int.MaxValue)
                {
                    this.title += "At least " + minPassengers + (minPassengers != 1 ? " Kerbals" : " Kerbal");
                }
                else if (minPassengers == 0)
                {
                    this.title += "At most " + maxPassengers + (maxPassengers != 1 ? " Kerbals" : " Kerbal");
                }
                else if (minPassengers == maxPassengers)
                {
                    this.title += "Exactly " + minPassengers + (minPassengers != 1 ? " Kerbals" : " Kerbal");
                }
                else
                {
                    this.title += "Between " + minPassengers + " and " + maxPassengers + " Kerbals";
                }
            }
            else
            {
                this.title = title;
            }
        }

        protected override string GetTitle()
        {
            return title;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
            node.AddValue("minPassengers", minPassengers);
            node.AddValue("maxPassengers", maxPassengers);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            minPassengers = Convert.ToInt32(node.GetValue("minPassengers"));
            maxPassengers = Convert.ToInt32(node.GetValue("maxPassengers"));
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onCrewTransferred.Add(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(OnCrewTransferred));            
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onCrewTransferred.Remove(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(OnCrewTransferred));
        }

        protected override void OnPartAttach(GameEvents.HostTargetAction<Part, Part> e)
        {
            base.OnPartAttach(e);
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected override void OnPartJointBreak(PartJoint p)
        {
            base.OnPartJointBreak(p);
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> a)
        {
            // Check both, as the Kerbal/ship swap spots depending on whether the vessel is
            // incoming or outgoing
            CheckVessel(a.from.vessel);
            CheckVessel(a.to.vessel);
        }

        /*
         * Whether this vessel meets the parameter condition.
         */
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            int passengerCount = vessel.GetCrewCapacity() - vessel.GetVesselCrew().Count;

            return passengerCount >= minPassengers && passengerCount <= maxPassengers;
        }
    }
}
