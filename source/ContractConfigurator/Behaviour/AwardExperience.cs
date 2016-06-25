using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;
using ContractConfigurator.Parameters;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Behaviour for awarding experience to a crew. 
    /// </summary>
    public class AwardExperience : ContractBehaviour
    {
        private List<string> parameter;
        private List<Kerbal> kerbals;
        private int experience;
        private bool awardImmediately;

        public const string SPECIAL_XP = "SpecialExperience";
        private static CelestialBody homeworld;

        private List<ProtoCrewMember> crew = new List<ProtoCrewMember>();

        static AwardExperience()
        {
        }

        public AwardExperience()
        {
        }

        public AwardExperience(IEnumerable<string> parameter, List<Kerbal> kerbals, int experience, bool awardImmediately)
        {
            this.parameter = parameter.ToList();
            this.kerbals = kerbals.ToList();
            this.experience = experience;
            this.awardImmediately = awardImmediately;
        }

        protected override void OnCompleted()
        {
            DoAwarding();
        }

        protected override void OnParameterStateChange(ContractParameter param)
        {
            if (param.State == ParameterState.Complete && parameter.Contains(param.ID))
            {
                VesselParameterGroup vpg = param as VesselParameterGroup;
                SetCrew(vpg.TrackedVessel);

                // If the parameter is the last one to complete, then the events fire in the wrong
                // order.  So check for that condition.
                if (contract.ContractState == Contract.State.Completed)
                {
                    DoAwarding();
                }

                // Sometimes we can also get multiple events for the same parameter (even without
                // disableOnStateChange set to false).  So prevent duplicate experience rewards.
                parameter.Remove(param.ID);
            }
        }

        protected void SetCrew(Vessel v)
        {
            LoggingUtil.LogVerbose(this, "Setting crew to those on vessel " + v.vesselName);
            foreach (ProtoCrewMember pcm in v.GetVesselCrew())
            {
                LoggingUtil.LogVerbose(this, "    Adding " + pcm.name + " to crew list.");
                crew.AddUnique(pcm);
            }
        }

        protected void DoAwarding()
        {
            IEnumerable<ProtoCrewMember> awardees = crew.Union(kerbals.Where(k => k.pcm != null).Select(k => k.pcm));

            LoggingUtil.LogVerbose(this, "Awarding " + experience + " points to " + awardees.Count() + " crew member(s)");

            // Set the homeworld
            if (homeworld == null)
            {
                homeworld = FlightGlobals.Bodies.Where(cb => cb.isHomeWorld).FirstOrDefault();
            }

            foreach (ProtoCrewMember pcm in awardees.Where(pcm => pcm != null))
            {
                LoggingUtil.LogVerbose(this, "    Awarding experience to " + pcm.name);

                // Find existing entries
                int currentValue = 2;
                foreach (FlightLog.Entry entry in pcm.careerLog.Entries.Concat(pcm.flightLog.Entries).Where(e => e.type.Contains(SPECIAL_XP)))
                {
                    // Get the entry with the largest value
                    int entryValue = Convert.ToInt32(entry.type.Substring(SPECIAL_XP.Length, entry.type.Length - SPECIAL_XP.Length));
                    currentValue = Math.Max(currentValue, entryValue);
                }

                // Can't go above 64 special experience
                int value = Math.Min(currentValue + experience, 64);

                // Increment the entry's experience value
                string type = SPECIAL_XP + value.ToString();

                // Do the awarding
                pcm.flightLog.AddEntry(type, homeworld.name);
                if (pcm.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
                {
                    pcm.ArchiveFlightLog();
                }
                else if (awardImmediately)
                {
                    pcm.experience += experience;
                    pcm.experienceLevel = KerbalRoster.CalculateExperienceLevel(pcm.experience);
                }
            }

            // Prevent duplicate awards
            crew.Clear();
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            parameter = ConfigNodeUtil.ParseValue<List<string>>(configNode, "parameter", new List<string>());
            kerbals = ConfigNodeUtil.ParseValue<List<Kerbal>>(configNode, "kerbal", new List<Kerbal>());
            experience = ConfigNodeUtil.ParseValue<int>(configNode, "experience");
            awardImmediately = ConfigNodeUtil.ParseValue<bool>(configNode, "awardImmediately");

            crew = ConfigNodeUtil.ParseValue<List<ProtoCrewMember>>(configNode, "crew", new List<ProtoCrewMember>());
        }

        protected override void OnSave(ConfigNode configNode)
        {
            foreach (string p in parameter)
            {
                configNode.AddValue("parameter", p);
            }
            foreach (Kerbal k in kerbals)
            {
                configNode.AddValue("kerbal", k.name);
            }
            configNode.AddValue("experience", experience);
            configNode.AddValue("awardImmediately", awardImmediately);

            foreach (ProtoCrewMember pcm in crew.Where(pcm => pcm != null))
            {
                configNode.AddValue("crew", pcm.name);
            }
        }
    }
}
