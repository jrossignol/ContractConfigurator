using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ContractConfigurator.CutScene
{
    /// <summary>
    /// Position an actor.
    /// </summary>
    public class PositionActor : CutSceneAction
    {
        public string actorName;
        public double latitude;
        public double longitude;
        public double altitude;

        public override void InvokeAction()
        {
            double alt = altitude + LocationUtil.TerrainHeight(latitude, longitude, FlightGlobals.currentMainBody);
            Vector3d pos = FlightGlobals.currentMainBody.GetWorldSurfacePosition(latitude, longitude, alt);
            Actor actor = cutSceneDefinition.actor(actorName);
            actor.Transform.position = pos;
        }

        public override bool ReadyForNextAction()
        {
            return true;
        }

        public override void OnSave(ConfigNode configNode)
        {
            base.OnSave(configNode);
            configNode.AddValue("actorName", actorName);
            configNode.AddValue("latitude", latitude);
            configNode.AddValue("longitude", longitude);
            configNode.AddValue("altitude", altitude);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);
            actorName = ConfigNodeUtil.ParseValue<string>(configNode, "actorName");
            latitude = ConfigNodeUtil.ParseValue<double>(configNode, "latitude");
            longitude = ConfigNodeUtil.ParseValue<double>(configNode, "longitude");
            altitude = ConfigNodeUtil.ParseValue<double>(configNode, "altitude");
        }

    }
}
