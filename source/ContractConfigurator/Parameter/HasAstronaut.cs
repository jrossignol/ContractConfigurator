using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using KSP.Localization;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Parameter for checking the space program's astronauts.
    /// </summary>
    public class HasAstronaut : ContractConfiguratorParameter
    {
        protected string trait { get; set; }
        protected int minCount { get; set; }
        protected int maxCount { get; set; }
        protected int minExperience { get; set; }
        protected int maxExperience { get; set; }

        public HasAstronaut()
            : base(null)
        {
        }

        public HasAstronaut(string title, string trait, int minCount = 1, int maxCount = int.MaxValue, int minExperience = 0, int maxExperience = 5)
            : base(title)
        {
            this.minCount = minCount;
            this.maxCount = maxCount;
            this.minExperience = minExperience;
            this.maxExperience = maxExperience;
            this.trait = trait;
        }

        protected override string GetParameterTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                // Build the strings
                string countStr;
                string traitStr = null;
                string experienceStr = null;
                HasAstronaut.GetTitleStrings(minCount, maxCount, trait, minExperience, maxExperience, out countStr, out traitStr, out experienceStr);

                // Build the output string
                if (String.IsNullOrEmpty(traitStr))
                {
                    if (String.IsNullOrEmpty(experienceStr))
                    {
                        output = Localizer.Format("#cc.param.HasAstronaut.1", countStr);
                    }
                    else
                    {
                        output = Localizer.Format("#cc.param.HasAstronaut.2", countStr, experienceStr);
                    }
                }
                else if (String.IsNullOrEmpty(experienceStr))
                {
                    output = Localizer.Format("#cc.param.HasAstronaut.2", countStr, traitStr);
                }
                else
                {
                    output = Localizer.Format("#cc.param.HasAstronaut.3", countStr, traitStr, experienceStr);
                }
            }
            else
            {
                output = title;
            }
            return output;
        }

        public static void GetTitleStrings(int minCount, int maxCount, string trait, int minExperience, int maxExperience, out string countStr, out string traitStr, out string experienceStr)
        {
            // Build the count string
            if (maxCount == int.MaxValue)
            {
                countStr = Localizer.Format("#cc.param.count.atLeast", minCount);
            }
            else if (minCount == 0)
            {
                countStr = Localizer.Format("#cc.param.count.atMost", maxCount);
            }
            else if (minCount == maxCount)
            {
                countStr = Localizer.Format("#cc.param.count.exact", minCount);
            }
            else
            {
                countStr = Localizer.Format("#cc.param.count.between", minCount, maxCount);
            }

            // Build the trait string
            if (!String.IsNullOrEmpty(trait))
            {
                traitStr = Localizer.Format("#cc.param.HasAstronaut.trait", LocalizationUtil.TraitTitle(trait));
            }
            else
            {
                traitStr = null;
            }

            // Build the experience string
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
            else
            {
                experienceStr = null;
            }
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            if (trait != null)
            {
                node.AddValue("trait", trait);
            }
            node.AddValue("minCount", minCount);
            node.AddValue("maxCount", maxCount);
            node.AddValue("minExperience", minExperience);
            node.AddValue("maxExperience", maxExperience);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            trait = ConfigNodeUtil.ParseValue<string>(node, "trait", (string)null);
            minExperience = Convert.ToInt32(node.GetValue("minExperience"));
            maxExperience = Convert.ToInt32(node.GetValue("maxExperience"));
            minCount = Convert.ToInt32(node.GetValue("minCount"));
            maxCount = Convert.ToInt32(node.GetValue("maxCount"));
        }

        protected override void OnRegister()
        {
            base.OnRegister();

            GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(OnVesselChange));
            GameEvents.onKerbalStatusChange.Add(new EventData<ProtoCrewMember, ProtoCrewMember.RosterStatus, ProtoCrewMember.RosterStatus>.OnEvent(OnKerbalStatusChange));
            GameEvents.Contract.onAccepted.Add(new EventData<Contract>.OnEvent(OnContractAccepted));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();

            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(OnVesselChange));
            GameEvents.onKerbalStatusChange.Remove(new EventData<ProtoCrewMember, ProtoCrewMember.RosterStatus, ProtoCrewMember.RosterStatus>.OnEvent(OnKerbalStatusChange));
            GameEvents.Contract.onAccepted.Remove(new EventData<Contract>.OnEvent(OnContractAccepted));
        }

        protected void OnVesselChange(Vessel v)
        {
            CheckStatus();
        }

        protected void OnKerbalStatusChange(ProtoCrewMember pcm, ProtoCrewMember.RosterStatus oldStatus, ProtoCrewMember.RosterStatus newStatus)
        {
            CheckStatus();
        }

        private void OnContractAccepted(Contract c)
        {
            if (c != Root || HighLogic.LoadedScene != GameScenes.FLIGHT)
            {
                return;
            }

            CheckStatus();
        }

        protected void CheckStatus()
        {
            IEnumerable<ProtoCrewMember> crew = HighLogic.CurrentGame.CrewRoster.Crew;

            // Filter by trait
            if (trait != null)
            {
                crew = crew.Where<ProtoCrewMember>(cm => cm.experienceTrait.TypeName == trait);
            }

            // Filter by experience
            crew = crew.Where<ProtoCrewMember>(cm => cm.experienceLevel >= minExperience && cm.experienceLevel <= maxExperience);

            // Check counts
            int count = crew.Count();
            SetState(count >= minCount && count <= maxCount ? ParameterState.Complete : ParameterState.Incomplete);
        }
    }
}
