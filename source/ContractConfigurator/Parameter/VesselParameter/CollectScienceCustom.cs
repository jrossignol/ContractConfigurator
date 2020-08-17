using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Util;
using KSP.Localization;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Adds some print methods.
    /// </summary>
    public static class CollectScienceExtensions
    {
        public static string Print(this ScienceRecoveryMethod recoveryMethod)
        {
            if (recoveryMethod != ScienceRecoveryMethod.Ideal)
            {
                return Localizer.GetStringByTag(StringBuilderCache.Format("#cc.param.CollectScience.rm.{0}", recoveryMethod.ToString()));
            }
            return recoveryMethod.ToString();
        }
    }

    /// <summary>
    /// Enum listing methods of recovering science
    /// </summary>
    public enum ScienceRecoveryMethod : int
    {
        None = 0,
        Recover = 1,
        Transmit = 2,
        RecoverOrTransmit = 3,
        Ideal = 4,
    }

    /// <summary>
    /// Custom version of the stock CollectScience parameter.
    /// </summary>
    public class CollectScienceCustom : VesselParameter
    {
        private static Dictionary<string, ScienceRecoveryMethod> idealRecoverMethodCache = new Dictionary<string, ScienceRecoveryMethod>();

        protected string biome { get; set; }
        protected ExperimentSituations? situation { get; set; }
        protected BodyLocation? location { get; set; }
        protected List<string> experiment { get; set; }
        protected ScienceRecoveryMethod recoveryMethod { get; set; }

        private static Vessel.Situations[] landedSituations = new Vessel.Situations[] { Vessel.Situations.LANDED, Vessel.Situations.PRELAUNCH, Vessel.Situations.SPLASHED };

        private Dictionary<string, ScienceSubject> matchingSubjects = new Dictionary<string, ScienceSubject>();
        private Dictionary<string, bool> recoveryDone = new Dictionary<string, bool>();

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;
        private int updateTicks = 0;
        private static Vessel lastVessel = null;
        private static string lastBiome = null;
        private static float nextOffset = 0.0f;
        private const float OFFSET_INCREMENT = 0.7f;

        public CollectScienceCustom()
            : base(null)
        {
            lastUpdate = UnityEngine.Time.fixedTime + nextOffset;
            nextOffset += OFFSET_INCREMENT;
            nextOffset -= (int)nextOffset;
        }

        public CollectScienceCustom(CelestialBody targetBody, string biome, ExperimentSituations? situation, BodyLocation? location,
            List<string> experiment, ScienceRecoveryMethod recoveryMethod, string title)
            : base(title)
        {
            lastUpdate = UnityEngine.Time.fixedTime + nextOffset;
            nextOffset += OFFSET_INCREMENT;
            nextOffset -= (int)nextOffset;

            this.targetBody = targetBody;
            this.biome = biome;
            this.situation = situation;
            this.location = location;
            this.experiment = experiment;
            this.recoveryMethod = recoveryMethod;

            disableOnStateChange = true;

            if (experiment.Count == 0)
            {
                experiment.Add("");
            }

            CreateDelegates();
        }

        protected override string GetParameterTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                if (state != ParameterState.Complete)
                {
                    output = Localizer.GetStringByTag("#cc.param.CollectScience.0");
                }
                else
                {
                    string experimentStr = (experiment.Count > 1 ? Localizer.GetStringByTag("#cc.science.experiment.many") : ExperimentName(experiment[0]));
                    string biomeStr = string.IsNullOrEmpty(biome) ? targetBody.displayName : new Biome(targetBody, biome).ToString();
                    string situationStr = situation != null ? situation.Value.Print().ToLower() :
                        location != null ? Localizer.GetStringByTag(location.Value == BodyLocation.Surface ? "#cc.science.location.Surface" : "#cc.science.location.Space") : null;

                    if (situationStr == null)
                    {
                        output = Localizer.Format("#cc.param.CollectScience.2", experimentStr, biomeStr);
                    }
                    else
                    {
                        output = Localizer.Format("#cc.param.CollectScience.3", experimentStr, biomeStr, situationStr);
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
            if (targetBody != null && string.IsNullOrEmpty(biome))
            {
                AddParameter(new ParameterDelegate<Vessel>(Localizer.Format("#cc.param.CollectScience.destination", targetBody.displayName),
                    subj => FlightGlobals.currentMainBody == targetBody, true)).ID = "destination";
            }

            // Filter for biome
            if (!string.IsNullOrEmpty(biome))
            {
                Biome b = new Biome(targetBody, biome);
                string title = Localizer.Format(b.IsKSC() ? "#cc.param.CollectScience.location" : "#cc.param.CollectScience.biome", b);

                AddParameter(new ParameterDelegate<Vessel>(title,
                    subj => CheckBiome(FlightGlobals.ActiveVessel))).ID = "biome";
            }

            // Filter for situation
            if (situation != null)
            {
                AddParameter(new ParameterDelegate<Vessel>(Localizer.Format("#cc.param.CollectScience.situation", situation.Value.Print()),
                    subj => FlightGlobals.ActiveVessel != null && ScienceUtil.GetExperimentSituation(FlightGlobals.ActiveVessel) == situation)).ID = "situation";
            }

            // Filter for location
            if (location != null)
            {
                AddParameter(new ParameterDelegate<Vessel>(Localizer.Format("#cc.param.CollectScience.location", location),
                    subj => FlightGlobals.ActiveVessel != null && ((location != BodyLocation.Surface) ^ FlightGlobals.ActiveVessel.LandedOrSplashed))).ID = "location";
            }

            // Add the experiments
            foreach (string exp in experiment)
            {
                string experimentStr = string.IsNullOrEmpty(exp) ? Localizer.GetStringByTag("#cc.science.experiment.any") : ExperimentName(exp);
                ContractParameter experimentParam = AddParameter(new ParameterDelegate<Vessel>(Localizer.Format("#cc.param.CollectScience.experiment",
                    experimentStr), subj => recoveryDone.ContainsKey(exp)));
                experimentParam.ID = "experiment";

                // Add the subject
                experimentParam.AddParameter(new ParameterDelegate<Vessel>("", subj => true)).ID = exp + "Subject";

                // Filter for recovery
                if (recoveryMethod != ScienceRecoveryMethod.None)
                {
                    experimentParam.AddParameter(new ParameterDelegate<Vessel>(Localizer.Format("#cc.param.CollectScience.recovery",
                        RecoveryMethod(exp).Print()), subj => false)).ID = "recovery";
                }
            }
        }

        protected void UpdateDelegates()
        {
            foreach (ContractParameter genericParam in this.GetAllDescendents())
            {
                ParameterDelegate<Vessel> param = genericParam as ParameterDelegate<Vessel>;
                if (param == null)
                {
                    continue;
                }

                string oldTitle = param.Title;
                if (matchingSubjects.Count == experiment.Count)
                {
                    if (param.ID.Equals("destination") || param.ID.Equals("biome") || param.ID.Equals("situation") ||
                        param.ID.Equals("location"))
                    {
                        param.ClearTitle();
                    }
                    else if (param.ID.Contains("Subject"))
                    {
                        string exp = param.ID.Remove(param.ID.IndexOf("Subject"));

                        param.SetTitle(matchingSubjects[exp].title);
                        param.SetState(ParameterState.Complete);
                    }
                }
                else
                {
                    if (param.ID.Contains("Subject"))
                    {
                        string exp = param.ID.Remove(param.ID.IndexOf("Subject"));
                        if (matchingSubjects.ContainsKey(exp))
                        {
                            param.SetTitle(matchingSubjects[exp].title);
                            param.SetState(ParameterState.Complete);
                        }
                        else
                        {
                            param.ClearTitle();
                        }
                    }
                    else
                    {
                        param.ResetTitle();
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

            string vesselBiome = null;
            if (landedSituations.Contains(vessel.situation) && !string.IsNullOrEmpty(vessel.landedAt))
            {
                // Fixes problems with special biomes like KSC buildings (total different naming)
                vesselBiome = Vessel.GetLandedAtString(vessel.landedAt);
            }
            else
            {
                vesselBiome = ScienceUtil.GetExperimentBiome(vessel.mainBody, vessel.latitude, vessel.longitude);
            }

            return vesselBiome.Replace(" ", "") == biome.Replace(" ", "");
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

            foreach (string exp in experiment)
            {
                if (!string.IsNullOrEmpty(exp))
                {
                    node.AddValue("experiment", exp);
                }
            }

            foreach (string exp in recoveryDone.Keys)
            {
                node.AddValue("recovery", exp);
            }

            node.AddValue("recoveryMethod", recoveryMethod);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            try
            {
                base.OnParameterLoad(node);
                targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody", (CelestialBody)null);
                biome = ConfigNodeUtil.ParseValue<string>(node, "biome", "").Replace(" ", "");
                situation = ConfigNodeUtil.ParseValue<ExperimentSituations?>(node, "situation", (ExperimentSituations?)null);
                location = ConfigNodeUtil.ParseValue<BodyLocation?>(node, "location", (BodyLocation?)null);
                experiment = ConfigNodeUtil.ParseValue<List<string>>(node, "experiment", new string[] { "" }.ToList());
                recoveryMethod = ConfigNodeUtil.ParseValue<ScienceRecoveryMethod>(node, "recoveryMethod");

                List<string> recoveredExp = ConfigNodeUtil.ParseValue<List<string>>(node, "recovery", new List<string>());
                foreach (string exp in recoveredExp)
                {
                    recoveryDone[exp] = true;
                }

                CreateDelegates();
            }
            finally
            {
                ParameterDelegate<Vessel>.OnDelegateContainerLoad(node);
            }
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
                string biome;
                if (landedSituations.Contains(v.situation) && !string.IsNullOrEmpty(v.landedAt))
                {
                    biome = Vessel.GetLandedAtString(v.landedAt).Replace(" ", "");
                }
                else
                {
                    biome = ScienceUtil.GetExperimentBiome(v.mainBody, v.latitude, v.longitude);
                }
                
                // Run the OnVesselChange, this will pick up a kerbal that grabbed science,
                // or science that was dumped from a pod.
                if (updateTicks++ % 4 == 0)
                {
                    OnVesselChange(v);
                }
                else
                {
                    // Check if there was a biome change
                    if (biome != lastBiome)
                    {
                        // Update the delegates, that will do the biome check
                        UpdateDelegates();
                    }
                }

                lastVessel = v;
                lastBiome = biome;
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
            if (vessel == null || scienceData == null || !ReadyToComplete())
            {
                return;
            }
            LoggingUtil.LogVerbose(this, "OnExperimentDeployed: {0}, {1}", scienceData.subjectID, vessel.id);

            // Decide if this is a matching subject
            ScienceSubject subject = ResearchAndDevelopment.GetSubjectByID(scienceData.subjectID);
            foreach (string exp in experiment)
            {
                if (CheckSubject(exp, subject))
                {
                    matchingSubjects[exp] = subject;
                    if (recoveryMethod == ScienceRecoveryMethod.None)
                    {
                        recoveryDone[exp] = true;
                    }
                    UpdateDelegates();
                }
            }

            CheckVessel(vessel);
        }

        private bool CheckSubject(string exp, ScienceSubject subject)
        {
            if (subject == null)
            {
                return false;
            }

            LoggingUtil.LogVerbose(this, "CheckSubject: {0}, {1}", exp, subject.id);
            if (targetBody != null && !subject.id.Contains(targetBody.name))
            {
                LoggingUtil.LogVerbose(this, "    wrong target body");
                return false;
            }

            // Need to pick up a bit of the situation string to that Flats doesn't pick up GreaterFlats
            if (!string.IsNullOrEmpty(biome) &&
                !subject.id.Contains(StringBuilderCache.Format("High{0}", biome)) &&
                !subject.id.Contains(StringBuilderCache.Format("Low{0}", biome)) &&
                !subject.id.Contains(StringBuilderCache.Format("ed{0}", biome)))
            {
                LoggingUtil.LogVerbose(this, "    wrong situation (biome = {0})", (biome == null ? "null" : biome));
                return false;
            }

            if (situation != null && !subject.IsFromSituation(situation.Value))
            {
                LoggingUtil.LogVerbose(this, "    wrong situation2");
                return false;
            }

            if (location != null)
            {
                if (location.Value == BodyLocation.Surface &&
                    !subject.IsFromSituation(ExperimentSituations.SrfSplashed) &&
                    !subject.IsFromSituation(ExperimentSituations.SrfLanded))
                {
                    LoggingUtil.LogVerbose(this, "    wrong location");
                    return false;
                }
                if (location.Value == BodyLocation.Space &&
                    !subject.IsFromSituation(ExperimentSituations.InSpaceHigh) &&
                    !subject.IsFromSituation(ExperimentSituations.InSpaceLow))
                {
                    LoggingUtil.LogVerbose(this, "    wrong location2");
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(exp))
            {
                LoggingUtil.LogVerbose(this, "    doing final subject check for {0} containing {1}", subject.id, exp);
            }
            if (!string.IsNullOrEmpty(exp) && !subject.id.Contains(exp))
            {
                LoggingUtil.LogVerbose(this, "    wrong subject");
                return false;
            }

            LoggingUtil.LogVerbose(this, "    got a match");
            return true;
        }

        protected void OnScienceReceived(float science, ScienceSubject subject, ProtoVessel protoVessel, bool reverseEngineered)
        {
            if (protoVessel == null || reverseEngineered)
            {
                LoggingUtil.LogVerbose(this, "OnScienceReceived: returning, protoVessel = {0}, reverseEng = {1}", (protoVessel == null ? "null" : protoVessel.vesselName), reverseEngineered);
                return;
            }
            LoggingUtil.LogVerbose(this, "OnScienceReceived: {0}, {1}", subject.id, protoVessel.vesselID);

            // Check the given subject is okay
            foreach (string exp in experiment)
            {
                if (CheckSubject(exp, subject))
                {
                    if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                    {
                        if ((RecoveryMethod(exp) & ScienceRecoveryMethod.Transmit) != 0)
                        {
                            recoveryDone[exp] = true;
                        }
                    }
                    else
                    {
                        if ((RecoveryMethod(exp) & ScienceRecoveryMethod.Recover) != 0)
                        {
                            recoveryDone[exp] = true;
                        }
                    }
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
            matchingSubjects.Clear();
            foreach (ScienceSubject subject in GetVesselSubjects(vessel).GroupBy(subjid => subjid).Select(grp => ResearchAndDevelopment.GetSubjectByID(grp.Key)))
            {
                foreach (string exp in experiment)
                {
                    if (CheckSubject(exp, subject))
                    {
                        matchingSubjects[exp] = subject;
                        break;
                    }
                }
            }

            UpdateDelegates();
            CheckVessel(vessel);
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
                    IScienceDataContainer scienceContainer = p.Modules[i] as IScienceDataContainer;
                    if (scienceContainer != null)
                    {
                        foreach (ScienceData data in scienceContainer.GetData())
                        {
                            if (!string.IsNullOrEmpty(data.subjectID))
                            {
                                yield return data.subjectID;
                            }
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

            ScienceExperiment e = ResearchAndDevelopment.GetExperiment(experiment);
            if (e != null)
            {
                return e.experimentTitle;
            }

            string output = Regex.Replace(experiment, @"([A-Z]+?(?=[A-Z][^A-Z])|\B[A-Z]+?(?=[^A-Z]))", " $1");
            return output.Substring(0, 1).ToUpper() + output.Substring(1);
        }

        private ScienceRecoveryMethod RecoveryMethod(string exp)
        {
            if (recoveryMethod != ScienceRecoveryMethod.Ideal)
            {
                return recoveryMethod;
            }
            else if (string.IsNullOrEmpty(exp) || exp == "surfaceSample")
            {
                return ScienceRecoveryMethod.Recover;
            }
            else
            {
                if (!idealRecoverMethodCache.ContainsKey(exp))
                {
                    IEnumerable<ConfigNode> expNodes = PartLoader.Instance.loadedParts.
                        Where(p => p.moduleInfos.Any(mod => mod.moduleName == "Science Experiment")).
                        SelectMany(p =>
                            p.partConfig.GetNodes("MODULE").
                            Where(node => node.GetValue("name") == "ModuleScienceExperiment" && node.GetValue("experimentID") == exp)
                        );

                    // Either has no parts or a full science transmitter
                    if (!expNodes.Any() || expNodes.Any(n => ConfigNodeUtil.ParseValue<float>(n, "xmitDataScalar", 0.0f) >= 0.999))
                    {
                        idealRecoverMethodCache[exp] = ScienceRecoveryMethod.RecoverOrTransmit;
                    }
                    else
                    {
                        idealRecoverMethodCache[exp] = ScienceRecoveryMethod.Recover;
                    }
                }

                return idealRecoverMethodCache[exp];
            }
        }

        public override bool IsIgnoredVesselType(VesselType vesselType)
        {
            if (vesselType == VesselType.Debris)
            {
                return false;
            }
            return base.IsIgnoredVesselType(vesselType);
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: {0}", vessel.id);

            ParameterDelegate<Vessel>.CheckChildConditions(this, vessel);

            return recoveryDone.Count == experiment.Count;
        }
    }
}
