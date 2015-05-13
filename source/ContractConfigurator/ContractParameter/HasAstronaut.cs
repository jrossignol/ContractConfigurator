using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Parameter for checking the space program's astronautts.
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

        protected override string GetTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                string traitString = String.IsNullOrEmpty(trait) ? "Kerbal" : trait;
                output = "Astronauts: ";
                if (maxCount == int.MaxValue)
                {
                    output += "At least " + minCount + " " + traitString + (minCount != 1 ? "s" : "");
                }
                else if (minCount == 0)
                {
                    output += "At most " + maxCount + " " + traitString + (maxCount != 1 ? "s" : "");
                }
                else if (minCount == maxCount)
                {
                    output += minCount + " " + traitString + (minCount != 1 ? "s" : "");
                }
                else
                {
                    output += "Between " + minCount + " and " + maxCount + " " + traitString + "s";
                }

                if (minExperience != 0 && maxExperience != 5)
                {
                    if (minExperience == 0)
                    {
                        output += " with experience level of at most " + maxExperience;
                    }
                    else if (maxExperience == 5)
                    {
                        output += " with experience level of at least " + minExperience;
                    }
                    else
                    {
                        output += " with experience level between " + minExperience + " and " + maxExperience;
                    }
                }
            }
            else
            {
                output = title;
            }
            return output;
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

            GameEvents.onCrewKilled.Add(new EventData<EventReport>.OnEvent(OnCrewKilled));
            GameEvents.OnCrewmemberHired.Add(new EventData<ProtoCrewMember, int>.OnEvent(OnCrewEvent));
            GameEvents.OnCrewmemberLeftForDead.Add(new EventData<ProtoCrewMember, int>.OnEvent(OnCrewEvent));
            GameEvents.OnCrewmemberSacked.Add(new EventData<ProtoCrewMember, int>.OnEvent(OnCrewEvent));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();

            GameEvents.onCrewKilled.Remove(new EventData<EventReport>.OnEvent(OnCrewKilled));
            GameEvents.OnCrewmemberHired.Remove(new EventData<ProtoCrewMember, int>.OnEvent(OnCrewEvent));
            GameEvents.OnCrewmemberLeftForDead.Remove(new EventData<ProtoCrewMember, int>.OnEvent(OnCrewEvent));
            GameEvents.OnCrewmemberSacked.Remove(new EventData<ProtoCrewMember, int>.OnEvent(OnCrewEvent));
        }

        protected void OnCrewEvent(ProtoCrewMember pcm, int ignored)
        {
            CheckStatus();
        }

        protected void OnCrewKilled(EventReport report)
        {
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
