using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ContractConfigurator;

namespace ContractConfigurator.CutScene
{
    public class FixedCamera : CutSceneCamera
    {
        public double latitude;
        public double longitude;
        public double altitude;

        public FixedCamera()
        {

        }

        public override void OnSave(ConfigNode configNode)
        {
            base.OnSave(configNode);
            configNode.AddValue("latitude", latitude);
            configNode.AddValue("longitude", longitude);
            configNode.AddValue("altitude", altitude);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);
            latitude = ConfigNodeUtil.ParseValue<double>(configNode, "latitude");
            longitude = ConfigNodeUtil.ParseValue<double>(configNode, "longitude");
            altitude = ConfigNodeUtil.ParseValue<double>(configNode, "altitude");
        }

        public override void MakeActive()
        {
            Debug.Log("CutScene: camera '" + name + "' active");

            double alt = altitude + LocationUtil.TerrainHeight(latitude, longitude, FlightGlobals.currentMainBody);
            Vector3d pos = FlightGlobals.currentMainBody.GetWorldSurfacePosition(latitude, longitude, alt);
            FlightCamera.fetch.SetCamCoordsFromPosition(pos);
        }
    }
}
