using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Adds some print methods.
    /// </summary>
    public static class CollectScienceExtensions
    {
        public static string Print(this ExperimentSituations exp)
        {
            switch (exp)
            {
                case ExperimentSituations.FlyingHigh:
                    return "Flying high";
                case ExperimentSituations.FlyingLow:
                    return "Flying low";
                case ExperimentSituations.InSpaceHigh:
                    return "High in space";
                case ExperimentSituations.InSpaceLow:
                    return "Low in space";
                case ExperimentSituations.SrfLanded:
                    return "Landed";
                case ExperimentSituations.SrfSplashed:
                    return "Splashed down";
                default:
                    throw new ArgumentException("Unexpected experiment situation: " + exp);
            }
        }

        public static string Print(this CollectScienceCustom.RecoveryMethod recoveryMethod)
        {
            if (recoveryMethod == CollectScienceCustom.RecoveryMethod.RecoverOrTransmit)
            {
                return "Recover or transmit";
            }
            return recoveryMethod.ToString();
        }
    }

    /// <summary>
    /// Custom version of the stock CollectScience parameter.
    /// </summary>
    public class CollectScienceCustom : VesselParameter
    {
        public enum RecoveryMethod : int
        {
            None = 0,
            Recover = 1,
            Transmit = 2,
            RecoverOrTransmit = 3,
        }

        private class VesselData
        {
            public Dictionary<string, bool> subjects = new Dictionary<string, bool>();
            public bool recovery = false;
        }

        protected CelestialBody targetBody { get; set; }
        protected string biome { get; set; }
        protected ExperimentSituations? situation { get; set; }
        protected BodyLocation? location { get; set; }
        protected string experiment { get; set; }
        protected RecoveryMethod recoveryMethod { get; set; }

        private static Vessel.Situations[] landedSituations = new Vessel.Situations[] { Vessel.Situations.LANDED, Vessel.Situations.PRELAUNCH, Vessel.Situations.SPLASHED };

        private Dictionary<Guid, VesselData> vesselData = new Dictionary<Guid, VesselData>();

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        private bool testMode = false;

        public CollectScienceCustom()
            : base(null)
        {
        }

        public CollectScienceCustom(CelestialBody targetBody, string biome, ExperimentSituations? situation, BodyLocation? location,
            string experiment, RecoveryMethod recoveryMethod, string title)
            : base(title)
        {
            this.targetBody = targetBody;
            this.biome = biome;
            this.situation = situation;
            this.location = location;
            this.experiment = experiment;
            this.recoveryMethod = recoveryMethod;

            CreateDelegates();
        }

        protected override string GetTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                output = "Collect science";
                if (state == ParameterState.Complete)
                {
                    output += ": " + ParameterDelegate<Vessel>.GetDelegateText(this);
                }
            }
            else
            {
                output = title;
            }
            return output;
        }

        protected void CreateDelegates()
        {
            // Filter for celestial bodies
            if (targetBody != null)
            {
                AddParameter(new ParameterDelegate<Vessel>("Destination: " + targetBody.theName,
                    v => VesselExperimentMet(v) && !testMode || v.mainBody == targetBody, true));
            }

            // Filter for biome
            if (!string.IsNullOrEmpty(biome))
            {
                AddParameter(new ParameterDelegate<Vessel>("Biome: " + biome,
                    v => VesselExperimentMet(v) && !testMode || CheckBiome(v)));
            }

            // Filter for situation
            if (situation != null)
            {
                AddParameter(new ParameterDelegate<Vessel>("Situation: " + situation.Value.Print(),
                    v => VesselExperimentMet(v) && !testMode || ScienceUtil.GetExperimentSituation(v) == situation));
            }

            // Filter for location
            if (location != null)
            {
                AddParameter(new ParameterDelegate<Vessel>("Location: " + location,
                    v => VesselExperimentMet(v) && !testMode || (location != BodyLocation.Surface ^ v.LandedOrSplashed), true));
            }

            // Add the actual data collection
            string experimentStr = string.IsNullOrEmpty(experiment) ? "Any" : ExperimentName(experiment);
            AddParameter(new ParameterDelegate<Vessel>("Experiment: " + experimentStr, v => testMode || VesselExperimentMet(v), string.IsNullOrEmpty(experiment)));

            // Filter for recovery
            if (recoveryMethod != RecoveryMethod.None)
            {
                AddParameter(new ParameterDelegate<Vessel>("Recovery: " + recoveryMethod.Print(),
                    v => testMode || (vesselData.ContainsKey(v.id) && vesselData[v.id].recovery)));
            }
        }

        private bool VesselExperimentMet(Vessel v)
        {
            return vesselData.ContainsKey(v.id) && vesselData[v.id].subjects.Count > 0;
        }

        private bool CheckBiome(Vessel vessel)
        {
            // Fixes problems with special biomes like KSC buildings (total different naming)
            if (landedSituations.Contains(vessel.situation))
            {
                if (Vessel.GetLandedAtString(vessel.landedAt) == biome)
                {
                    return true;
                }
            }

            return ScienceUtil.GetExperimentBiome(vessel.mainBody, vessel.latitude, vessel.longitude) == biome;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);

            if (targetBody != null)
            {
                node.AddValue("targetBody", targetBody.name);
            }
            
            if (!string.IsNullOrEmpty(biome))
            {
                node.AddValue("biome", biome);
            }

            if (situation != null)
            {
                node.AddValue("situation", situation);
            }

            if (location != null)
            {
                node.AddValue("location", location);
            }

            if (!string.IsNullOrEmpty(experiment))
            {
                node.AddValue("experiment", experiment);
            }

            node.AddValue("recoveryMethod", recoveryMethod);

            foreach (KeyValuePair<Guid, VesselData> pair in vesselData)
            {
                ConfigNode childNode = new ConfigNode("VESSEL_DATA");
                node.AddNode(childNode);

                childNode.AddValue("vessel", pair.Key);
                foreach (string subject in pair.Value.subjects.Keys)
                {
                    childNode.AddValue("subject", subject);
                }
                childNode.AddValue("recovery", pair.Value.recovery);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody", (CelestialBody)null);
            biome = ConfigNodeUtil.ParseValue<string>(node, "biome", "");
            situation = ConfigNodeUtil.ParseValue<ExperimentSituations?>(node, "situation", (ExperimentSituations?)null);
            location = ConfigNodeUtil.ParseValue<BodyLocation?>(node, "location", (BodyLocation?)null);
            experiment = ConfigNodeUtil.ParseValue<string>(node, "experiment", "");
            recoveryMethod = ConfigNodeUtil.ParseValue<RecoveryMethod>(node, "recoveryMethod");

            foreach (ConfigNode child in node.GetNodes("VESSEL_DATA"))
            {
                Guid vid = ConfigNodeUtil.ParseValue<Guid>(child, "vessel");
                if (vid != null && FlightGlobals.Vessels.Where(v => v.id == vid).Any())
                {
                    vesselData[vid] = new VesselData();
                    foreach (string subject in ConfigNodeUtil.ParseValue<List<string>>(child, "subject", new List<string>()))
                    {
                        vesselData[vid].subjects[subject] = true;
                    }
                    vesselData[vid].recovery = ConfigNodeUtil.ParseValue<bool>(child, "recovery");
                }
            }

            ParameterDelegate<Vessel>.OnDelegateContainerLoad(node);
            CreateDelegates();
        }


        protected override void OnUpdate()
        {
            Vessel v = FlightGlobals.ActiveVessel;
            if (v == null)
            {
                return;
            }

            // Need to do frequent checks to catch biome changes
            base.OnUpdate();
            if (UnityEngine.Time.fixedTime - lastUpdate > UPDATE_FREQUENCY)
            {
                lastUpdate = UnityEngine.Time.fixedTime;
                
                // For EVAs, check if a kerbal grabbed science.  Note this runs the CheckVessel
                // call, so don't run it a second time.
                if (v.vesselType == VesselType.EVA)
                {
                    OnVesselChange(v);
                }
                else
                {
                    CheckVessel(v);
                }
            }
        }

        protected override void OnRegister()
        {
            base.OnRegister();

            GameEvents.OnExperimentDeployed.Add(new EventData<ScienceData>.OnEvent(OnExperimentDeployed));
            GameEvents.OnScienceRecieved.Add(new EventData<float, ScienceSubject>.OnEvent(OnScienceReceived));
            GameEvents.onVesselRecovered.Add(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();

            GameEvents.OnExperimentDeployed.Remove(new EventData<ScienceData>.OnEvent(OnExperimentDeployed));
            GameEvents.OnScienceRecieved.Remove(new EventData<float, ScienceSubject>.OnEvent(OnScienceReceived));
            GameEvents.onVesselRecovered.Remove(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
        }

        protected void OnExperimentDeployed(ScienceData scienceData)
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel == null)
            {
                return;
            }
            LoggingUtil.LogVerbose(this, "OnExperimentDeployed: " + scienceData.subjectID + ", " + vessel.id);

            // Add to the list
            if (!vesselData.ContainsKey(vessel.id))
            {
                vesselData[vessel.id] = new VesselData();
            }

            CheckSubject(vessel, scienceData.subjectID);
            CheckVessel(vessel);
        }

        private void CheckSubject(Vessel vessel, string subjectID)
        {
            LoggingUtil.LogVerbose(this, "OnScienceReceived: " + subjectID + ", " + vessel.id);

            // Check the experiment type
            if (!string.IsNullOrEmpty(experiment) && !subjectID.StartsWith(experiment + "@"))
            {
                return;
            }

            // Temporarily set to test mode
            testMode = true;

            // Check whether we meet the conditions (with the exception of recovery)
            bool experimentPassed = ParameterDelegate<Vessel>.CheckChildConditions(this, vessel, true);

            // Reset test mode
            testMode = false;

            // Add the subject if it passed
            if (experimentPassed)
            {
                vesselData[vessel.id].subjects[subjectID] = true;
            }
        }

        protected void OnScienceReceived(float science, ScienceSubject subject)
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel == null)
            {
                return;
            }
            LoggingUtil.LogVerbose(this, "OnScienceReceived: " + subject.id + ", " + vessel.id);

            // Is it in our list, and are we looking for a transmission
            if (vesselData.ContainsKey(vessel.id) && vesselData[vessel.id].subjects.ContainsKey(subject.id) && (recoveryMethod & RecoveryMethod.Transmit) != 0)
            {
                vesselData[vessel.id].recovery = true;
                CheckVessel(vessel);
            }
        }

        private void OnVesselRecovered(ProtoVessel v)
        {
            bool runCheck = false;

            if (vesselData.ContainsKey(v.vesselID) && (recoveryMethod & RecoveryMethod.Recover) != 0)
            {
                VesselData vd = vesselData[v.vesselID];

                foreach (string subjectID in GetVesselSubjects(v))
                {
                    if (vd.subjects.ContainsKey(subjectID))
                    {
                        vd.recovery = true;
                        runCheck = true;
                    }
                }
            }

            if (runCheck)
            {
                CheckVessel(v.vesselRef);
            }
        }

        /// <summary>
        /// Runs when vessel is changed.  Do a search through our modules and handle any
        /// experiments that may have been transferred into the ship.
        /// </summary>
        /// <param name="vessel">The vessel</param>
        protected override void OnVesselChange(Vessel vessel)
        {
            if (!vesselData.ContainsKey(vessel.id))
            {
                vesselData[vessel.id] = new VesselData();
            }

            foreach (string subjectID in GetVesselSubjects(vessel))
            {
                if (!vesselData[vessel.id].subjects.ContainsKey(subjectID))
                {
                    CheckSubject(vessel, subjectID);
                }
            }

            base.OnVesselChange(vessel);
        }

        private IEnumerable<string> GetVesselSubjects(ProtoVessel v)
        {
            foreach (ProtoPartSnapshot pps in v.protoPartSnapshots)
            {
                foreach (ProtoPartModuleSnapshot ppms in pps.modules)
                {
                    ConfigNode mod = ppms.moduleValues;
                    foreach (ConfigNode scienceData in mod.GetNodes("ScienceData"))
                    {
                        string subjectID = ConfigNodeUtil.ParseValue<string>(scienceData, "subjectID");
                        if (!string.IsNullOrEmpty(subjectID))
                        {
                            yield return subjectID;
                        }
                    }
                }
            }
        }

        private IEnumerable<string> GetVesselSubjects(Vessel v)
        {
            foreach (Part p in v.parts)
            {
                for (int i = 0; i < p.Modules.Count; i++)
                {
                    PartModule pm = p.Modules[i];

                    // Ugh, not figuring out how to get this stuff in two ways, just dump it to a config node
                    ConfigNode mod = new ConfigNode("MODULE");
                    pm.Save(mod);

                    foreach (ConfigNode scienceData in mod.GetNodes("ScienceData"))
                    {
                        string subjectID = ConfigNodeUtil.ParseValue<string>(scienceData, "subjectID");
                        if (!string.IsNullOrEmpty(subjectID))
                        {
                            yield return subjectID;
                        }
                    }
                }
            }
        }

        private string ExperimentName(string experiment)
        {
            if (experiment == "evaReport")
            {
                return "EVA Report";
            }
            else if (experiment.StartsWith("SCANsat"))
            {
                return "SCANsat " + ExperimentName(experiment.Substring("SCANsat".Length));
            }
            else
            {
                string output = Regex.Replace(experiment, "(\\B[A-Z])", " $1");
                return output.Substring(0, 1).ToUpper() + output.Substring(1);
            }
        }
        
        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            return ParameterDelegate<Vessel>.CheckChildConditions(this, vessel);
        }
    }
}
