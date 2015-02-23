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
    public class SpawnKerbal : ContractBehaviour, IHasKerbalBehaviour
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
            public bool addToRoster = true;

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
                addToRoster = k.addToRoster;
            }
        }
        private List<KerbalData> kerbals = new List<KerbalData>();

        public int KerbalCount { get { return kerbals.Count; } }

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
                valid &= ConfigNodeUtil.ParseValue<CelestialBody>(child, "targetBody", x => kerbal.body = x, factory, defaultBody, Validation.NotNull);

                // Get orbit
                valid &= ConfigNodeUtil.ValidateMandatoryChild(child, "ORBIT", factory);
                kerbal.orbit = new OrbitSnapshot(child.GetNode("ORBIT")).Load();
                kerbal.orbit.referenceBody = kerbal.body;

                // Get landed stuff
                if (child.HasValue("lat") && child.HasValue("lon") && child.HasValue("alt"))
                {
                    valid &= ConfigNodeUtil.ParseValue<double>(child, "lat", x => kerbal.latitude = x, factory);
                    valid &= ConfigNodeUtil.ParseValue<double>(child, "lon", x => kerbal.longitude = x, factory);
                    valid &= ConfigNodeUtil.ParseValue<double>(child, "alt", x => kerbal.altitude = x, factory);
                    kerbal.landed = true;
                }

                // Get additional flags
                valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "owned", x => kerbal.owned = x, factory, false);
                valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "addToRoster", x => kerbal.addToRoster = x, factory, true);

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
                LoggingUtil.LogVerbose(this, "Spawning a Kerbal named " + kerbal.name);

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

        protected override void OnSave(ConfigNode configNode)
        {
            base.OnLoad(configNode);

            foreach (KerbalData kd in kerbals)
            {
                ConfigNode child = new ConfigNode("KERBAL_DETAIL");

                child.AddValue("name", kd.name);
                child.AddValue("body", kd.body.name);
                child.AddValue("lat", kd.latitude);
                child.AddValue("lon", kd.longitude);
                child.AddValue("alt", kd.altitude);
                child.AddValue("landed", kd.landed);
                child.AddValue("owned", kd.owned);
                child.AddValue("addToRoster", kd.addToRoster);

                ConfigNode orbitNode = new ConfigNode("ORBIT");
                new OrbitSnapshot(kd.orbit).Save(orbitNode);
                child.AddNode(orbitNode);

                configNode.AddNode(child);
            }
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);

            foreach (ConfigNode child in configNode.GetNodes("KERBAL_DETAIL"))
            {
                // Read all the orbit data
                KerbalData kd = new KerbalData();
                kd.name = child.GetValue("name");
                kd.body = ConfigNodeUtil.ParseValue<CelestialBody>(child, "body");
                kd.latitude = ConfigNodeUtil.ParseValue<double>(child, "lat");
                kd.longitude = ConfigNodeUtil.ParseValue<double>(child, "lon");
                kd.altitude = ConfigNodeUtil.ParseValue<double>(child, "alt");
                kd.landed = ConfigNodeUtil.ParseValue<bool>(child, "landed");
                kd.owned = ConfigNodeUtil.ParseValue<bool>(child, "owned");
                kd.addToRoster = ConfigNodeUtil.ParseValue<bool>(child, "addToRoster");

                kd.orbit = new OrbitSnapshot(child.GetNode("ORBIT")).Load();

                // Find the ProtoCrewMember
                kd.crewMember = HighLogic.CurrentGame.CrewRoster.AllKerbals().Where(cm => cm.name == kd.name).First();

                // Add to the global list
                kerbals.Add(kd);
            }
        }

        protected override void OnRegister()
        {
            GameEvents.onVesselRecovered.Add(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
        }

        protected override void OnUnregister()
        {
            GameEvents.onVesselRecovered.Remove(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
        }

        private void OnVesselRecovered(ProtoVessel v)
        {
            LoggingUtil.LogVerbose(this, "OnVesselRecovered: " + v);

            // EVA vessel
            if (v.vesselType == VesselType.EVA)
            {
                foreach (ProtoPartSnapshot p in v.protoPartSnapshots)
                {
                    {
                        LoggingUtil.LogVerbose(this, "    p: " + p);
                        foreach (string name in p.protoCrewNames)
                        {
                            // Find this crew member in our data
                            foreach (KerbalData kd in kerbals)
                            {
                                if (kd.name == name && kd.addToRoster)
                                {
                                    // Add them to the roster
                                    kd.crewMember.type = ProtoCrewMember.KerbalType.Crew;
                                }
                            }
                        }
                    }
                }

            }

            // Vessel with crew
            foreach (ProtoCrewMember crewMember in v.GetVesselCrew())
            {
                foreach (KerbalData kd in kerbals)
                {
                    if (kd.crewMember == crewMember && kd.addToRoster)
                    {
                        // Add them to the roster
                        crewMember.type = ProtoCrewMember.KerbalType.Crew;
                    }
                }
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
