using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private Vector3d nrm;

        private float lastDist = float.MaxValue;
        private bool done = false;

        public override void InvokeAction()
        {
            altitude = LocationUtil.TerrainHeight(latitude, longitude, FlightGlobals.currentMainBody);

            Vector3d pos = FlightGlobals.currentMainBody.GetWorldSurfacePosition(latitude, longitude, altitude);
            nrm = FlightGlobals.currentMainBody.GetSurfaceNVector(latitude, longitude);
            actor = cutSceneDefinition.actor(actorName) as KerbalActor;

            // Store a transform for the destination position
            GameObject dummyObject = new GameObject("DummyLocation");
            dest = dummyObject.transform;
            dest.position = pos;

            // Get some stuff
            Vessel eva = actor.eva;
            body = eva.mainBody;
            kerbalEVA = eva.gameObject.GetComponent<KerbalEVA>();

            // Set up the animation
            actor.Transform.LookAt(dest, nrm);
            kerbalEVA.Animations.walkLowGee.State.speed = 2.7f;
            KerbalAnimationState animState = body.GeeASL > kerbalEVA.minWalkingGee ? kerbalEVA.Animations.walkFwd : kerbalEVA.Animations.walkLowGee;
            kerbalEVA.animation.CrossFade(animState.animationName);
        }

        public override void FixedUpdate()
        {
            float speed = 2.0f * Time.fixedDeltaTime;
            Transform transform = actor.Transform;
            transform.LookAt(dest, nrm);
            transform.position += transform.forward * speed;
        }

        public override void OnDestroy()
        {
            kerbalEVA.animation.CrossFade(kerbalEVA.Animations.idle, 0.2f);
            UnityEngine.Object.Destroy(dest.gameObject);
            dest = null;
        }

        public override void Update()
        {
            float currentDistance = Vector3.Distance(actor.Transform.position, dest.position);
            if (currentDistance > lastDist + 0.005 || currentDistance < 0.5)
            {
                done = true;
            }

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
