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
     * Parameter for checking whether a vessel has a crew.
     */
    public class HasCrew : VesselParameter
    {
        protected string title { get; set; }
        protected int minCrew { get; set; }
        protected int maxCrew { get; set; }

        public HasCrew()
            : this(null)
        {
        }

        public HasCrew(string title, int minCrew = 1, int maxCrew = int.MaxValue)
            : base()
        {
            this.minCrew = minCrew;
            this.maxCrew = maxCrew;
            if (title == null)
            {
                this.title = "Crew: ";
                if (maxCrew == 0)
                {
                    this.title += "Unmanned";
                }
                else if (maxCrew == int.MaxValue)
                {
                    this.title += "At least " + minCrew + " Kerbal" + (minCrew != 1 ? "s" : "");
                }
                else if (minCrew == 0)
                {
                    this.title += "At most " + maxCrew + " Kerbal" + (maxCrew != 1 ? "s" : "");
                }
                else
                {
                    this.title += "Between " + minCrew + " and " + maxCrew + " Kerbals";
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
            node.AddValue("minCrew", minCrew);
            node.AddValue("maxCrew", maxCrew);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            minCrew = Convert.ToInt32(node.GetValue("minCrew"));
            maxCrew = Convert.ToInt32(node.GetValue("maxCrew"));
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onStageSeparation.Add(new EventData<EventReport>.OnEvent(OnStageSeparation));
            GameEvents.onSameVesselDock.Add(new EventData<GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode>>.OnEvent(OnSameVesselDockUndock));
            GameEvents.onSameVesselUndock.Add(new EventData<GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode>>.OnEvent(OnSameVesselDockUndock));
            GameEvents.onPartUndock.Add(new EventData<Part>.OnEvent(OnPartUndock));
            GameEvents.onPartJointBreak.Add(new EventData<PartJoint>.OnEvent(OnPartJointBreak));
            GameEvents.onKerbalAdded.Add(new EventData<ProtoCrewMember>.OnEvent(OnKerbalChanged));
            GameEvents.onKerbalRemoved.Add(new EventData<ProtoCrewMember>.OnEvent(OnKerbalChanged));
            GameEvents.onVesselCreate.Add(new EventData<Vessel>.OnEvent(OnVesselCreateChange));
            GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(OnVesselCreateChange));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onStageSeparation.Remove(new EventData<EventReport>.OnEvent(OnStageSeparation));
            GameEvents.onSameVesselDock.Remove(new EventData<GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode>>.OnEvent(OnSameVesselDockUndock));
            GameEvents.onSameVesselUndock.Remove(new EventData<GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode>>.OnEvent(OnSameVesselDockUndock));
            GameEvents.onPartUndock.Remove(new EventData<Part>.OnEvent(OnPartUndock));
            GameEvents.onPartJointBreak.Remove(new EventData<PartJoint>.OnEvent(OnPartJointBreak));
            GameEvents.onKerbalAdded.Remove(new EventData<ProtoCrewMember>.OnEvent(OnKerbalChanged));
            GameEvents.onKerbalRemoved.Remove(new EventData<ProtoCrewMember>.OnEvent(OnKerbalChanged));
            GameEvents.onVesselCreate.Remove(new EventData<Vessel>.OnEvent(OnVesselCreateChange));
            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(OnVesselCreateChange));
        }

        protected void OnStageSeparation(EventReport e)
        {
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected void OnSameVesselDockUndock(GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode> e)
        {
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected void OnPartUndock(Part p)
        {
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected void OnPartJointBreak(PartJoint p)
        {
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected void OnKerbalChanged(ProtoCrewMember p)
        {
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        /*
         * Whether this vessel meets the parameter condition.
         */
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            int count = vessel.GetCrewCount();
            return count >= minCrew && count <= maxCrew;
        }
    }
}
