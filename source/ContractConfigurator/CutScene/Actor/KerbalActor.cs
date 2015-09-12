using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ContractConfigurator.CutScene
{
    public class KerbalActor : Actor
    {
        public string kerbalName;

        public override void OnSave(ConfigNode configNode)
        {
            base.OnSave(configNode);
            configNode.AddValue("kerbalName", kerbalName);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);
            kerbalName = ConfigNodeUtil.ParseValue<string>(configNode, "kerbalName");
        }

        protected ProtoCrewMember pcm
        {
            get
            {
                return HighLogic.CurrentGame.CrewRoster.AllKerbals().Where(cm => cm != null && cm.name == kerbalName).FirstOrDefault();
            }
        }

        public Vessel vessel
        {
            get
            {
                return FlightGlobals.Vessels.SingleOrDefault(v => v.GetVesselCrew().Any(cm => cm != null && cm.name == kerbalName));
            }
        }

        public Vessel eva
        {
            get
            {
                Vessel v = vessel;
                return v != null && v.vesselType == VesselType.EVA ? v : null;
            }
        }

        public override Transform Transform
        {
            get
            {
                return eva.transform;
            }
        }
    }
}
