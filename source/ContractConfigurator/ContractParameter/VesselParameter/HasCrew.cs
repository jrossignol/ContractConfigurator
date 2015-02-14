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
    /// Parameter for checking whether a vessel has a crew.
    /// </summary>
    public class HasCrew : VesselParameter
    {
        protected string trait { get; set; }
        protected int minCrew { get; set; }
        protected int maxCrew { get; set; }
        protected int minExperience { get; set; }
        protected int maxExperience { get; set; }

        public HasCrew()
            : this(null, null)
        {
        }

        public HasCrew(string title, string trait, int minCrew = 1, int maxCrew = int.MaxValue, int minExperience = 0, int maxExperience = 5)
            : base(title)
        {
            this.minCrew = minCrew;
            this.maxCrew = maxCrew;
            this.minExperience = minExperience;
            this.maxExperience = maxExperience;
            this.trait = trait;

            // Validate min/max crew
            if (maxCrew == 0)
            {
                minCrew = 0;
            }
            else if (minCrew > maxCrew)
            {
                throw new ArgumentException("HasCrew parameter: minCrew must be less than maxCrew!");
            }

            if (string.IsNullOrEmpty(title))
            {
                string traitString = String.IsNullOrEmpty(trait) ? "Kerbal" : trait;

                this.title = "Crew: ";
                if (maxCrew == 0)
                {
                    this.title += "Unmanned";
                }
                else if (maxCrew == int.MaxValue)
                {
                    this.title += "At least " + minCrew + " " + traitString + (minCrew != 1 ? "s" : "");
                }
                else if (minCrew == 0)
                {
                    this.title += "At most " + maxCrew + " " + traitString + (maxCrew != 1 ? "s" : "");
                }
                else if (minCrew == maxCrew)
                {
                    this.title += minCrew + " " + traitString + (minCrew != 1 ? "s" : "");
                }
                else
                {
                    this.title += "Between " + minCrew + " and " + maxCrew + " " + traitString + "s";
                }

                if (minExperience != 0 && maxExperience != 5)
                {
                    if (minExperience == 0)
                    {
                        this.title += " with experience level of at most " + maxExperience;
                    }
                    else if (maxExperience == 5)
                    {
                        this.title += " with experience level of at least " + minExperience;
                    }
                    else
                    {
                        this.title += " with experience level between " + minExperience + " and " + maxExperience;
                    }
                }
            }
            else
            {
                this.title = title;
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
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            trait = ConfigNodeUtil.ParseValue<string>(node, "trait", (string)null);
            minExperience = Convert.ToInt32(node.GetValue("minExperience"));
            maxExperience = Convert.ToInt32(node.GetValue("maxExperience"));
            minCrew = Convert.ToInt32(node.GetValue("minCrew"));
            maxCrew = Convert.ToInt32(node.GetValue("maxCrew"));
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onCrewTransferred.Add(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(OnCrewTransferred));            
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onCrewTransferred.Remove(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(OnCrewTransferred));
        }

        protected override void OnPartAttach(GameEvents.HostTargetAction<Part, Part> e)
        {
            base.OnPartAttach(e);
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected override void OnPartJointBreak(PartJoint p)
        {
            base.OnPartJointBreak(p);
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> a)
        {
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
            IEnumerable<ProtoCrewMember> crew = vessel.GetVesselCrew();
            
            // Filter by trait
            if (!string.IsNullOrEmpty(trait))
            {
                crew = crew.Where<ProtoCrewMember>(cm => cm.experienceTrait.TypeName == trait);
            }

            // Filter by experience
            crew = crew.Where<ProtoCrewMember>(cm => cm.experienceLevel >= minExperience && cm.experienceLevel <= maxExperience);

            // Check counts
            int count = crew.Count();
            return count >= minCrew && count <= maxCrew;
        }
    }
}
