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

        public override string Name()
        {
            return "Fixed Camera";
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

        public override void OnDraw()
        {
            string val;
            double dVal;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Latitude", GUILayout.Width(CutSceneConfigurator.CutSceneConfigurator.DETAIL_LABEL_WIDTH));
            val = GUILayout.TextField(latitude.ToString(), GUILayout.Width(CutSceneConfigurator.CutSceneConfigurator.DETAIL_ENTRY_WIDTH));
            if (double.TryParse(val, out dVal))
            {
                latitude = dVal;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Longitude", GUILayout.Width(CutSceneConfigurator.CutSceneConfigurator.DETAIL_LABEL_WIDTH));
            val = GUILayout.TextField(longitude.ToString(), GUILayout.Width(CutSceneConfigurator.CutSceneConfigurator.DETAIL_ENTRY_WIDTH));
            if (double.TryParse(val, out dVal))
            {
                longitude = dVal;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Altitude", "Enter the altitude above the terrain."), GUILayout.Width(CutSceneConfigurator.CutSceneConfigurator.DETAIL_LABEL_WIDTH));
            val = GUILayout.TextField(altitude.ToString(), GUILayout.Width(CutSceneConfigurator.CutSceneConfigurator.DETAIL_ENTRY_WIDTH));
            if (double.TryParse(val, out dVal))
            {
                altitude = dVal;
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Set Position to Current"))
            {
                latitude = FlightGlobals.currentMainBody.GetLatitude(FlightCamera.fetch.transform.position);
                longitude = FlightGlobals.currentMainBody.GetLongitude(FlightCamera.fetch.transform.position);
                double terrainHeight = LocationUtil.TerrainHeight(latitude, longitude, FlightGlobals.currentMainBody);
                Vector3d ground = FlightGlobals.currentMainBody.GetWorldSurfacePosition(latitude, longitude, terrainHeight);
                altitude = (ground - FlightCamera.fetch.transform.position).magnitude;
            }
            if (GUILayout.Button("Jump to Camera Position"))
            {
                MakeActive();
            }

            GUILayout.EndHorizontal();
        }

        public override void MakeActive()
        {
            LoggingUtil.LogVerbose(this, "CutScene: camera '" + name + "' active");

            double alt = altitude + LocationUtil.TerrainHeight(latitude, longitude, FlightGlobals.currentMainBody);
            Vector3d pos = FlightGlobals.currentMainBody.GetWorldSurfacePosition(latitude, longitude, alt);
            FlightCamera.fetch.SetCamCoordsFromPosition(pos);
        }
    }
}
