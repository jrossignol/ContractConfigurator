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
        public class EVAWaypoint
        {
            public double latitude;
            public double longitude;
        }

        public string actorName;
        public List<EVAWaypoint> waypoints = new List<EVAWaypoint>();

        private IEnumerator<EVAWaypoint> waypointEnumerator;
        private EVAWaypoint currentWaypoint;

        private KerbalActor actor;
        private KerbalEVA kerbalEVA;
        private CelestialBody body;
        private Transform dest;
        private double altitude;
        private Vector3d nrm;

        private float lastDist = float.MaxValue;
        private bool done = false;

        public override void InvokeAction()
        {
            // Store a transform for the destination position
            GameObject dummyObject = new GameObject("DummyLocation");
            dest = dummyObject.transform;

            // Get the actor and details
            actor = cutSceneDefinition.actor(actorName) as KerbalActor;
            Vessel eva = actor.eva;
            body = eva.mainBody;
            kerbalEVA = eva.gameObject.GetComponent<KerbalEVA>();

            // Set up the animation
            kerbalEVA.Animations.walkLowGee.State.speed = 2.7f;
            KerbalAnimationState animState = body.GeeASL > kerbalEVA.minWalkingGee ? kerbalEVA.Animations.walkFwd : kerbalEVA.Animations.walkLowGee;
            kerbalEVA.animation.CrossFade(animState.animationName);

            // Create the enumerator
            waypointEnumerator = waypoints.GetEnumerator();

            // Start the next waypoint
            NextWaypoint();
        }

        private bool NextWaypoint()
        {
            if (!waypointEnumerator.MoveNext())
            {
                return false;
            }

            // Get details for the current waypoint
            currentWaypoint = waypointEnumerator.Current;
            altitude = LocationUtil.TerrainHeight(currentWaypoint.latitude, currentWaypoint.longitude, FlightGlobals.currentMainBody);
            Vector3d pos = FlightGlobals.currentMainBody.GetWorldSurfacePosition(currentWaypoint.latitude, currentWaypoint.longitude, altitude);
            nrm = FlightGlobals.currentMainBody.GetSurfaceNVector(currentWaypoint.latitude, currentWaypoint.longitude);
            dest.position = pos;

            // Turn towards the destination
            actor.Transform.LookAt(dest, nrm);

            return true;
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
                if (NextWaypoint())
                {
                    currentDistance = float.MaxValue;
                }
                else
                {
                    done = true;
                }
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
            foreach (EVAWaypoint w in waypoints)
            {
                ConfigNode waypointNode = new ConfigNode("WAYPOINT");
                configNode.AddNode(waypointNode);

                waypointNode.AddValue("latitude", w.latitude);
                waypointNode.AddValue("longitude", w.longitude);
            }
        }

        public override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);
            actorName = ConfigNodeUtil.ParseValue<string>(configNode, "actorName");
            foreach (ConfigNode node in configNode.GetNodes("WAYPOINT"))
            {
                EVAWaypoint w = new EVAWaypoint();
                w.latitude = ConfigNodeUtil.ParseValue<double>(node, "latitude");
                w.longitude = ConfigNodeUtil.ParseValue<double>(node, "longitude");
            }
        }

    }
}
