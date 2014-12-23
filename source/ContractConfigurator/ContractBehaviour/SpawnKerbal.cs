using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;

namespace ContractConfigurator.Behaviour
{
    /*
     * Class for spawning a Kerbal.
     */
    public class SpawnKerbal : ContractBehaviour
    {
        private class KerbalData
        {
            public string name = null;
            public ProtoCrewMember crewMember = null;
            public CelestialBody body = null;
            public Orbit orbit = null;
            public double latitude = 0.0;
            public double longitude = 0.0;
            public double altitude = 0.0;
            public bool landed = false;
            public bool owned = false;

            public KerbalData() { }
            public KerbalData(KerbalData k)
            {
                name = k.name;
                crewMember = k.crewMember;
                body = k.body;
                orbit = k.orbit;
                latitude = k.latitude;
                longitude = k.longitude;
                altitude = k.altitude;
                landed = k.landed;
                owned = k.owned;
            }
        }
        private List<KerbalData> kerbals = new List<KerbalData>();

        public SpawnKerbal() {}

        /*
         * Copy constructor.
         */
        public SpawnKerbal(SpawnKerbal orig)
        {
            foreach (KerbalData kerbal in orig.kerbals)
            {
                kerbals.Add(new KerbalData(kerbal));
            }

            // Create the CrewMember record
            foreach (KerbalData kerbal in kerbals)
            {
                kerbal.crewMember = HighLogic.CurrentGame.CrewRoster.GetNewKerbal(ProtoCrewMember.KerbalType.Unowned);

                // Have the name in both spots
                if (kerbal.name != null)
                {
                    kerbal.crewMember.name = kerbal.name;
                }
                else
                {
                    kerbal.name = kerbal.crewMember.name;
                }
            }
        }

        public static SpawnKerbal Create(ConfigNode configNode, CelestialBody defaultBody, SpawnKerbalFactory factory)
        {
            SpawnKerbal spawnKerbal = new SpawnKerbal();

            bool valid = true;
            foreach (ConfigNode child in configNode.GetNodes("KERBAL"))
            {
                KerbalData kerbal = new KerbalData();

                // Get name
                if (child.HasValue("name"))
                {
                    kerbal.name = child.GetValue("name");
                }

                // Get celestial body
                if (defaultBody == null)
                {
                    valid &= ConfigNodeUtil.ValidateMandatoryField(child, "targetBody", factory);
                }
                kerbal.body = child.HasValue("targetBody") ? 
                    ConfigNodeUtil.ParseCelestialBody(child, "targetBody") : defaultBody;

                // Get orbit
                valid &= ConfigNodeUtil.ValidateMandatoryChild(child, "ORBIT", factory);
                kerbal.orbit = new OrbitSnapshot(child.GetNode("ORBIT")).Load();
                kerbal.orbit.referenceBody = kerbal.body;

                // Get landed stuff
                if (child.HasValue("lat") && child.HasValue("lon") && child.HasValue("alt"))
                {
                    kerbal.latitude = Convert.ToDouble(child.GetValue("lat"));
                    kerbal.longitude = Convert.ToDouble(child.GetValue("lon"));
                    kerbal.altitude = Convert.ToDouble(child.GetValue("alt"));
                    kerbal.landed = true;
                }

                // Get owned flag
                if (child.HasValue("owned"))
                {
                    kerbal.owned = Convert.ToBoolean(child.GetValue("owned"));
                }

                // Add to the list
                spawnKerbal.kerbals.Add(kerbal);
            }

            return valid ? spawnKerbal : null;
        }

        protected override void OnAccepted()
        {
            // Actually spawn the kerbals in the game world!
            foreach (KerbalData kerbal in kerbals)
            {
                uint flightId = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);

                // Create crew member array
                ProtoCrewMember[] crewArray = new ProtoCrewMember[1];
                crewArray[0] = kerbal.crewMember;

                // Create part nodes
                ConfigNode[] partNodes = new ConfigNode[1];
                partNodes[0] = ProtoVessel.CreatePartNode("kerbalEVA", flightId, crewArray);

                // Create additional nodes
                ConfigNode[] additionalNodes = new ConfigNode[1];
                DiscoveryLevels discoveryLevel = kerbal.owned ? DiscoveryLevels.Owned : DiscoveryLevels.Unowned;
                additionalNodes[0] = ProtoVessel.CreateDiscoveryNode(discoveryLevel, UntrackedObjectClass.A, contract.TimeDeadline, contract.TimeDeadline);

                // Create the config node representation of the ProtoVessel
                ConfigNode protoVesselNode = ProtoVessel.CreateVesselNode(kerbal.name, VesselType.EVA, kerbal.orbit, 0, partNodes, additionalNodes);

                // Additional seetings for a landed Kerbal
                if (kerbal.landed)
                {
                    // Create the config node representation of the ProtoVessel
                    protoVesselNode.SetValue("sit", Vessel.Situations.LANDED.ToString());
                    protoVesselNode.SetValue("landed", true.ToString());
                    protoVesselNode.SetValue("lat", kerbal.latitude.ToString());
                    protoVesselNode.SetValue("lon", kerbal.longitude.ToString());
                    protoVesselNode.SetValue("alt", kerbal.altitude.ToString());
                }

                // Add vessel to the game
                HighLogic.CurrentGame.AddVessel(protoVesselNode);
            }
        }

        protected override void OnCancelled()
        {
            RemoveKerbals();
        }

        protected override void OnDeadlineExpired()
        {
            RemoveKerbals();
        }

        protected override void OnDeclined()
        {
            RemoveKerbals();
        }

        protected override void OnGenerateFailed()
        {
            RemoveKerbals();
        }

        protected override void OnOfferExpired()
        {
            RemoveKerbals();
        }

        protected override void OnWithdrawn()
        {
            RemoveKerbals();
        }

        private void RemoveKerbals()
        {
            foreach (KerbalData kerbal in kerbals)
            {
                HighLogic.CurrentGame.CrewRoster.Remove(kerbal.crewMember.name);
                kerbal.crewMember = null;
            }
        }

        public string GetKerbalName(int index)
        {
            if (index < 0 || index >= kerbals.Count)
            {
                throw new Exception("ContractConfigurator: index " + index +
                    " is out of range for number of Kerbals spawned (" + kerbals.Count + ").");
            }

            return kerbals[index].name;
        }
    }
}
