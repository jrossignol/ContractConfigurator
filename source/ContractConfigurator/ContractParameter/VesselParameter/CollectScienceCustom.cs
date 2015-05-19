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
            Ideal = 4,
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

        private ScienceSubject matchingSubject;
        private bool recoveryDone = false;

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

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

            if (recoveryMethod != RecoveryMethod.Ideal)
            {
                this.recoveryMethod = recoveryMethod;
            }
            else if (string.IsNullOrEmpty(experiment) || experiment == "surfaceSample")
            {
                this.recoveryMethod = RecoveryMethod.Recover;
            }
            else
            {
                IEnumerable<ConfigNode> expNodes = PartLoader.Instance.parts.
                    Where(p => p.moduleInfos.Any(mod => mod.moduleName == "Science Experiment")).
                    SelectMany(p =>
                        p.partConfig.GetNodes("MODULE").
                        Where(node => node.GetValue("name") == "ModuleScienceExperiment" && node.GetValue("experimentID") == experiment)
                    );

                // Either has no parts or a full science transmitter
                if (!expNodes.Any() || expNodes.Any(n => ConfigNodeUtil.ParseValue<float>(n, "xmitDataScalar", 0.0f) >= 0.999))
                {
                    this.recoveryMethod = RecoveryMethod.RecoverOrTransmit;
                }
                else
                {
                    this.recoveryMethod = RecoveryMethod.Recover;
                }
            }

            disableOnStateChange = true;

            CreateDelegates();
        }

        protected override string GetTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                output = "Collect science";
                if (state == ParameterState.Complete || matchingSubject != null)
                {
                    output += ": " + ExperimentName(experiment) + " from ";

                    if (!string.IsNullOrEmpty(biome))
                    {
                        output += new Biome(targetBody, Regex.Replace(biome, "(\\B[A-Z])", " $1").ToLower()).ToString();
                    }
                    else
                    {
                        output += targetBody.theName;
                    }
                    
                    if (situation != null)
                    {
                        output += " while " + situation.Value.Print().ToLower();
                    }
                    else if (location != null)
                    {
                        output += location.Value == BodyLocation.Surface ? " while on the surface" : " while in space";
                    }

                    if (recoveryMethod != RecoveryMethod.None)
                    {
                        output += " (" + recoveryMethod.Print().ToLower() + ")";
                    }
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
                AddParameter(new ParameterDelegate<ScienceSubject>("Destination: " + targetBody.theName,
                    subj => FlightGlobals.currentMainBody == targetBody, true));
            }

            // Filter for biome
            if (!string.IsNullOrEmpty(biome))
            {
                AddParameter(new ParameterDelegate<ScienceSubject>("Biome: " + Regex.Replace(biome, "(\\B[A-Z])", " $1"),
                    subj => CheckBiome(FlightGlobals.ActiveVessel)));
            }

            // Filter for situation
            if (situation != null)
            {
                AddParameter(new ParameterDelegate<ScienceSubject>("Situation: " + situation.Value.Print(),
                    subj => FlightGlobals.ActiveVessel != null && ScienceUtil.GetExperimentSituation(FlightGlobals.ActiveVessel) == situation));
            }

            // Filter for location
            if (location != null)
            {
                AddParameter(new ParameterDelegate<ScienceSubject>("Location: " + location,
                    subj => FlightGlobals.ActiveVessel != null && (location != BodyLocation.Surface ^ FlightGlobals.ActiveVessel.LandedOrSplashed)));
            }

            // Add the experiment
            string experimentStr = string.IsNullOrEmpty(experiment) ? "Any" : ExperimentName(experiment);
            AddParameter(new ParameterDelegate<ScienceSubject>("Experiment: " + experimentStr,
                subj => false));

            // Add the subject
            ContractParameter subjectParam = new ParameterDelegate<ScienceSubject>("", subj => true);
            subjectParam.ID = "Subject";
            AddParameter(subjectParam);

            // Filter for recovery
            if (recoveryMethod != RecoveryMethod.None)
            {
                AddParameter(new ParameterDelegate<ScienceSubject>("Recovery: " + recoveryMethod.Print(),
                    subj => recoveryDone));
            }
        }

        protected void UpdateDelegates()
        {
            foreach (ContractParameter genericParam in this.GetChildren())
            {
                ParameterDelegate<ScienceSubject> param = genericParam as ParameterDelegate<ScienceSubject>;
                string oldTitle = param.Title;
                if (matchingSubject != null)
                {
                    if (param.ID.Contains("Destination:") || param.ID.Contains("Biome:") || param.ID.Contains("Situation:") ||
                        param.ID.Contains("Location:") || param.ID.Contains("Experiment:"))
                    {
                        param.ClearTitle();
                    }
                    else if (param.ID == "Subject")
                    {
                        param.SetTitle(matchingSubject.title);
                    }
                }
                else
                {
                    if (param.ID != "Subject")
                    {
                        param.ResetTitle();
                    }
                    else
                    {
                        param.ClearTitle();
                    }
                }

                if (param.Title != oldTitle)
                {
                    ContractsWindow.SetParameterTitle(param, param.Title);
                    ContractConfigurator.OnParameterChange.Fire(Root, param);
                }
            }

            ContractsWindow.SetParameterTitle(this, GetTitle());
        }

        private bool CheckBiome(Vessel vessel)
        {
            if (vessel == null)
            {
                return false;
            }

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

            ParameterDelegate<ScienceSubject>.OnDelegateContainerLoad(node);
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
                
                // Run the OnVesselChange, this will pick up a kerbal that grabbed science,
                // or science that was dumped from a pod.
                OnVesselChange(v);
            }
        }

        protected override void OnRegister()
        {
            base.OnRegister();

            GameEvents.OnExperimentDeployed.Add(new EventData<ScienceData>.OnEvent(OnExperimentDeployed));
            GameEvents.OnScienceRecieved.Add(new EventData<float, ScienceSubject, ProtoVessel, bool>.OnEvent(OnScienceReceived));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();

            GameEvents.OnExperimentDeployed.Remove(new EventData<ScienceData>.OnEvent(OnExperimentDeployed));
            GameEvents.OnScienceRecieved.Remove(new EventData<float, ScienceSubject, ProtoVessel, bool>.OnEvent(OnScienceReceived));
        }

        protected void OnExperimentDeployed(ScienceData scienceData)
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel == null)
            {
                return;
            }
            LoggingUtil.LogVerbose(this, "OnExperimentDeployed: " + scienceData.subjectID + ", " + vessel.id);

            // Decide if this is a matching subject
            ScienceSubject subject = ResearchAndDevelopment.GetSubjectByID(scienceData.subjectID);
            if (CheckSubject(subject))
            {
                matchingSubject = subject;
                if (recoveryMethod == RecoveryMethod.None)
                {
                    recoveryDone = true;
                }
                UpdateDelegates();
            }

            CheckVessel(vessel);
        }

        private bool CheckSubject(ScienceSubject subject)
        {
            if (subject == null)
            {
                return false;
            }

            if (!subject.id.Contains(targetBody.name))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(biome) && !subject.id.Contains(biome))
            {
                return false;
            }

            if (situation != null && !subject.IsFromSituation(situation.Value))
            {
                return false;
            }

            if (location != null)
            {
                if (location.Value == BodyLocation.Surface &&
                    !subject.IsFromSituation(ExperimentSituations.SrfSplashed) &&
                    !subject.IsFromSituation(ExperimentSituations.SrfLanded))
                {
                    return false;
                }
                if (location.Value == BodyLocation.Space &&
                    !subject.IsFromSituation(ExperimentSituations.InSpaceHigh) &&
                    !subject.IsFromSituation(ExperimentSituations.InSpaceLow))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(experiment) && !subject.id.Contains(experiment))
            {
                return false;
            }

            return true;
        }

        protected void OnScienceReceived(float science, ScienceSubject subject, ProtoVessel protoVessel, bool reverseEngineered)
        {
            if (protoVessel == null || reverseEngineered)
            {
                return;
            }
            LoggingUtil.LogVerbose(this, "OnScienceReceived: " + subject.id + ", " + protoVessel.vesselID);

            // Check the given subject is okay
            if (CheckSubject(subject))
            {
                if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    recoveryDone = (recoveryMethod & RecoveryMethod.Transmit) != 0;
                }
                else
                {
                    recoveryDone = (recoveryMethod & RecoveryMethod.Recover) != 0;
                }
            }
            UpdateDelegates();

            CheckVessel(protoVessel.vesselRef);
        }

        /// <summary>
        /// Runs when vessel is changed.  Do a search through our modules and handle any
        /// experiments that may have been transferred into the ship.
        /// </summary>
        /// <param name="vessel">The vessel</param>
        protected override void OnVesselChange(Vessel vessel)
        {
            matchingSubject = null;
            foreach (ScienceSubject subject in GetVesselSubjects(vessel).GroupBy(subjid => subjid).Select(grp => ResearchAndDevelopment.GetSubjectByID(grp.Key)))
            {
                if (CheckSubject(subject))
                {
                    matchingSubject = subject;
                    break;
                }
            }

            UpdateDelegates();
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
            if (string.IsNullOrEmpty(experiment))
            {
                return "Any experiment";
            }
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

            ParameterDelegate<ScienceSubject>.CheckChildConditions(this, matchingSubject);

            return recoveryDone;
        }
    }
}
