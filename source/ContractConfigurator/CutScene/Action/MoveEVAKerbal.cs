using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ContractConfigurator.CutScene
{
    /// <summary>
    /// Move an EVA Kerbal.
    /// </summary>
    public class MoveEVAKerbal : CutSceneAction
    {
        public string actorName;

        public double latitude;
        public double longitude;

        private double altitude;
        private KerbalActor actor;
        private Transform dest;
        private KerbalEVA kerbalEVA;
        private CelestialBody body;

        private float lastDist = float.MaxValue;
        private bool done = false;
        private float stateTime = 0.0f;

        public override void InvokeAction()
        {
            altitude = LocationUtil.TerrainHeight(latitude, longitude, FlightGlobals.currentMainBody);

            Vector3d pos = FlightGlobals.currentMainBody.GetWorldSurfacePosition(latitude, longitude, altitude);
            Vector3d nrm = FlightGlobals.currentMainBody.GetSurfaceNVector(latitude, longitude);
            actor = cutSceneDefinition.actor(actorName) as KerbalActor;

            GameObject dummyObject = new GameObject("DummyLocation");
            dest = dummyObject.transform;
            dest.position = pos;

            Vessel eva = actor.eva;
            body = eva.mainBody;

            kerbalEVA = eva.gameObject.GetComponent<KerbalEVA>();
            Debug.Log("Got kerbal EVA: " + kerbalEVA);
            kerbalEVA.SetWaypoint(dest.position);
            //kerbalEVA.CharacterFrameMode = true;
            stateTime = Time.time;
            stateFrameCount = Time.frameCount;

            actor.Transform.LookAt(dest, nrm);
        }

        public override void FixedUpdate()
        {
            Debug.Log("FixedUpdate");
            kerbalEVA.SetWaypoint(dest.position);
            stateTime += Time.fixedTime;
            if (stateTime > 3.0f)
            {
                stateTime -= 3.0f;
            }
            kerbalEVA.fsm.CurrentState.TimeAtStateEnter = stateTime;

            float speed = (body.GeeASL > kerbalEVA.minWalkingGee ? kerbalEVA.walkSpeed : kerbalEVA.boundSpeed) * Time.fixedDeltaTime;
            Transform transform = actor.Transform;
            transform.position += transform.forward * speed;
        }

        public override void Update()
        {
            float currentDistance = Vector3.Distance(actor.Transform.position, dest.position);
            if (currentDistance > lastDist + 0.1 || currentDistance < 0.5)
            {
                done = true;
            }
            Debug.Log("Update, currentDistance = " + currentDistance);
            stateFrameCount++;

            lastDist = currentDistance;
        }

        public override bool ReadyForNextAction()
        {
            return done;
        }

        public override void OnSave(ConfigNode configNode)
        {
            base.OnSave(configNode);
            configNode.AddValue("actorName", actorName);
            configNode.AddValue("latitude", latitude);
            configNode.AddValue("longitude", longitude);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);
            actorName = ConfigNodeUtil.ParseValue<string>(configNode, "actorName");
            latitude = ConfigNodeUtil.ParseValue<double>(configNode, "latitude");
            longitude = ConfigNodeUtil.ParseValue<double>(configNode, "longitude");
        }

    }
}
