using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using Experience;
using KSP.Localization;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Parameter for checking whether a vessel has a crew.
    /// </summary>
    public class HasCrew : VesselParameter, IKerbalNameStorage
    {
        protected string trait { get; set; }
        protected int minCrew { get; set; }
        protected int maxCrew { get; set; }
        protected int minExperience { get; set; }
        protected int maxExperience { get; set; }
        protected List<Kerbal> kerbals = new List<Kerbal>();
        protected List<Kerbal> excludeKerbals = new List<Kerbal>();

        public HasCrew()
            : base(null)
        {
        }

        public HasCrew(string title, IEnumerable<Kerbal> kerbals, IEnumerable<Kerbal> excludeKerbals, string trait, int minCrew = 1, int maxCrew = int.MaxValue, int minExperience = 0, int maxExperience = 5)
            : base(title)
        {
            if (minCrew > maxCrew)
            {
                minCrew = maxCrew;
            }

            this.minCrew = minCrew;
            this.maxCrew = maxCrew;
            this.minExperience = minExperience;
            this.maxExperience = maxExperience;
            this.trait = trait;
            this.kerbals = kerbals == null ? new List<Kerbal>() : kerbals.ToList();
            this.excludeKerbals = excludeKerbals == null ? new List<Kerbal>() : excludeKerbals.ToList();

            CreateDelegates();
        }

        protected override string GetParameterTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                if (kerbals.Count == 0 && (state == ParameterState.Complete || ParameterCount == 1))
                {
                    if (ParameterCount == 1)
                    {
                        hideChildren = true;
                    }

                    // Build the count string
                    string countStr;
                    if (maxCrew == 0)
                    {
                        countStr = Localizer.GetStringByTag("#cc.param.HasCrew.unmanned");
                    }
                    else if (maxCrew == int.MaxValue)
                    {
                        countStr = Localizer.Format("#cc.param.count.atLeast", minCrew);
                    }
                    else if (minCrew == 0)
                    {
                        countStr = Localizer.Format("#cc.param.count.atMost", maxCrew);
                    }
                    else if (minCrew == maxCrew)
                    {
                        countStr = Localizer.Format("#cc.param.count.exact", minCrew);
                    }
                    else
                    {
                        countStr = Localizer.Format("#cc.param.count.between", minCrew, maxCrew);
                    }

                    // Build the trait string
                    string traitStr = null;
                    if (!String.IsNullOrEmpty(trait))
                    {
                        traitStr = Localizer.Format("#cc.param.HasAstronaut.trait", LocalizationUtil.TraitTitle(trait));
                    }

                    // Build the experience string
                    string experienceStr = null;
                    if (minExperience != 0 && maxExperience != 5)
                    {
                        if (minExperience == 0)
                        {
                            experienceStr = Localizer.Format("#cc.param.HasAstronaut.experience.atMost", maxExperience);
                        }
                        else if (maxExperience == 5)
                        {
                            experienceStr = Localizer.Format("#cc.param.HasAstronaut.experience.atLeast", minExperience);
                        }
                        else if (minExperience == maxExperience)
                        {
                            experienceStr = Localizer.Format("#cc.param.HasAstronaut.experience.exact", minExperience);
                        }
                        else
                        {
                            experienceStr = Localizer.Format("#cc.param.HasAstronaut.experience.between", minExperience, maxExperience);
                        }
                    }

                    // Build the output string
                    if (String.IsNullOrEmpty(traitStr))
                    {
                        if (String.IsNullOrEmpty(experienceStr))
                        {
                            output = Localizer.Format("#cc.param.HasCrew.1", countStr);
                        }
                        else
                        {
                            output = Localizer.Format("#cc.param.HasCrew.2", countStr, experienceStr);
                        }
                    }
                    else if (String.IsNullOrEmpty(experienceStr))
                    {
                        output = Localizer.Format("#cc.param.HasCrew.2", countStr, traitStr);
                    }
                    else
                    {
                        output = Localizer.Format("#cc.param.HasCrew.3", countStr, traitStr, experienceStr);
                    }
                }
                else
                {
                    if (state == ParameterState.Complete || ParameterCount == 1)
                    {
                        if (ParameterCount == 1)
                        {
                            hideChildren = true;
                        }

                        output = Localizer.Format("#cc.param.HasCrew.1", ParameterDelegate<ProtoCrewMember>.GetDelegateText(this));
                    }
                    else
                    {
                        output = Localizer.GetStringByTag("#cc.param.HasCrew");
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
            // Experience trait
            if (!string.IsNullOrEmpty(trait))
            {
                AddParameter(new ParameterDelegate<ProtoCrewMember>(Localizer.Format("#cc.param.HasCrew.trait", LocalizationUtil.TraitTitle(trait)),
                    cm => cm.experienceTrait.Config.Name == trait));
            }

            // Filter for experience
            if (minExperience != 0 || maxExperience != 5)
            {
                string countText;
                if (minExperience == 0)
                {
                    countText = Localizer.Format("#cc.param.count.atMost.num", maxExperience);
                }
                else if (maxExperience == 5)
                {
                    countText = Localizer.Format("#cc.param.count.atLeast.num", minExperience);
                }
                else if (minExperience == maxExperience)
                {
                    countText = Localizer.Format("#cc.param.count.exact.num", minExperience);
                }
                else
                {
                    countText = Localizer.Format("#cc.param.count.between.num", minExperience, maxExperience);
                }

                AddParameter(new ParameterDelegate<ProtoCrewMember>(Localizer.Format("#cc.param.HasCrew.experience", countText),
                    cm => cm.experienceLevel >= minExperience && cm.experienceLevel <= maxExperience));
            }

            // Validate count
            if (kerbals.Count == 0)
            {
                // Special handling for unmanned
                if (minCrew == 0 && maxCrew == 0)
                {
                    AddParameter(new ParameterDelegate<ProtoCrewMember>(Localizer.GetStringByTag("#cc.param.HasCrew.unmanned"), pcm => true, ParameterDelegateMatchType.NONE));
                }
                else
                {
                    AddParameter(new CountParameterDelegate<ProtoCrewMember>(minCrew, maxCrew));
                }
            }

            // Validate specific kerbals
            foreach (Kerbal kerbal in kerbals)
            {
                AddParameter(new ParameterDelegate<ProtoCrewMember>(Localizer.Format("#cc.param.HasCrew.specific", kerbal.name),
                    pcm => pcm == kerbal.pcm, ParameterDelegateMatchType.VALIDATE));
            }
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);
            if (trait != null)
            {
                node.AddValue("trait", trait);
            }
            node.AddValue("minCrew", minCrew);
            node.AddValue("maxCrew", maxCrew);
            node.AddValue("minExperience", minExperience);
            node.AddValue("maxExperience", maxExperience);
            foreach (Kerbal kerbal in kerbals)
            {
                ConfigNode kerbalNode = new ConfigNode("KERBAL");
                node.AddNode(kerbalNode);

                kerbal.Save(kerbalNode);
            }
            foreach (Kerbal kerbal in excludeKerbals)
            {
                ConfigNode kerbalNode = new ConfigNode("KERBAL_EXCLUDE");
                node.AddNode(kerbalNode);

                kerbal.Save(kerbalNode);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            try
            {
                base.OnParameterLoad(node);
                trait = ConfigNodeUtil.ParseValue<string>(node, "trait", (string)null);
                minExperience = Convert.ToInt32(node.GetValue("minExperience"));
                maxExperience = Convert.ToInt32(node.GetValue("maxExperience"));
                minCrew = Convert.ToInt32(node.GetValue("minCrew"));
                maxCrew = Convert.ToInt32(node.GetValue("maxCrew"));

                foreach (ConfigNode kerbalNode in node.GetNodes("KERBAL"))
                {
                    kerbals.Add(Kerbal.Load(kerbalNode));
                }
                foreach (ConfigNode kerbalNode in node.GetNodes("KERBAL_EXCLUDE"))
                {
                    excludeKerbals.Add(Kerbal.Load(kerbalNode));
                }

                CreateDelegates();
            }
            finally
            {
                ParameterDelegate<Vessel>.OnDelegateContainerLoad(node);
            }
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onCrewTransferred.Add(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(OnCrewTransferred));
            GameEvents.onVesselWasModified.Add(new EventData<Vessel>.OnEvent(OnVesselWasModified));
            GameEvents.Contract.onAccepted.Add(new EventData<Contract>.OnEvent(OnContractAccepted));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onCrewTransferred.Remove(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(OnCrewTransferred));
            GameEvents.onVesselWasModified.Remove(new EventData<Vessel>.OnEvent(OnVesselWasModified));
            GameEvents.Contract.onAccepted.Remove(new EventData<Contract>.OnEvent(OnContractAccepted));
        }

        private void OnContractAccepted(Contract c)
        {
            if (c != Root)
            {
                return;
            }

            foreach (Kerbal kerbal in kerbals.Union(excludeKerbals))
            {
                // Instantiate the kerbals if necessary
                if (kerbal.pcm == null)
                {
                    kerbal.GenerateKerbal();
                }
            }
        }

        protected override void OnPartAttach(GameEvents.HostTargetAction<Part, Part> e)
        {
            base.OnPartAttach(e);
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected void OnVesselWasModified(Vessel v)
        {
            LoggingUtil.LogVerbose(this, "OnVesselWasModified: " + v);
            CheckVessel(v);
        }

        protected override void OnPartJointBreak(PartJoint p, float breakForce)
        {
            LoggingUtil.LogVerbose(this, "OnPartJointBreak: " + p);
            base.OnPartJointBreak(p, breakForce);

            if (HighLogic.LoadedScene == GameScenes.EDITOR || p.Parent.vessel == null)
            {
                return;
            }

            CheckVessel(p.Parent.vessel);
        }

        protected override void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> a)
        {
            base.OnCrewTransferred(a);

            // Check both, as the Kerbal/ship swap spots depending on whether the vessel is
            // incoming or outgoing
            CheckVessel(a.from.vessel);
            CheckVessel(a.to.vessel);
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);

            // If we're a VesselParameterGroup child, only do actual state change if we're the tracked vessel
            bool checkOnly = false;
            if (Parent is VesselParameterGroup)
            {
                checkOnly = ((VesselParameterGroup)Parent).TrackedVessel != vessel;
            }

            return ParameterDelegate<ProtoCrewMember>.CheckChildConditions(this, GetVesselCrew(vessel, maxCrew == int.MaxValue), checkOnly);
        }

        /// <summary>
        /// Gets the vessel crew and works for EVAs as well
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        protected IEnumerable<ProtoCrewMember> GetVesselCrew(Vessel v, bool includeTourists)
        {
            if (v == null)
            {
                yield break;
            }

            // EVA vessel
            if (v.vesselType == VesselType.EVA)
            {
                if (v.parts == null)
                {
                    yield break;
                }

                foreach (Part p in v.parts)
                {
                    foreach (ProtoCrewMember pcm in p.protoModuleCrew)
                    {
                        if (!excludeKerbals.Any(k => k.pcm == pcm))
                        {
                            yield return pcm;
                        }
                    }
                }
            }
            else
            {
                // Vessel with crew
                foreach (ProtoCrewMember pcm in v.GetVesselCrew())
                {
                    if (!excludeKerbals.Any(k => k.pcm == pcm) && (includeTourists || pcm.type == ProtoCrewMember.KerbalType.Crew))
                    {
                        yield return pcm;
                    }
                }
            }
        }

        public IEnumerable<string> KerbalNames()
        {
            foreach (Kerbal kerbal in kerbals)
            {
                yield return kerbal.name;
            }
        }
    }
}
