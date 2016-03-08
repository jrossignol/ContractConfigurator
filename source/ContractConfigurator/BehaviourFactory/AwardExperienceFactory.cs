using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator.Parameters;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// BehaviourFactory wrapper for AwardExperience ContractBehaviour.
    /// </summary>
    public class AwardExperienceFactory : BehaviourFactory
    {
        private List<string> parameter;
        private List<Kerbal> kerbals;
        private int experience;
        private bool awardImmediately;

        /// <summary>
        /// Static initializer to add our entries into the experience/flight log system.
        /// It's done on the factory to guarantee it's always run, otherwise uninstalling a
        /// contract pack could have the side effect of wiping some XP from a saved game.
        /// </summary>
        static AwardExperienceFactory()
        {
            LoggingUtil.LogVerbose(typeof(AwardExperienceFactory), "Doing setup of Kerbal Experience extras");

            for (int i = 3; i <= 64; i++)
            {
                KerbalRoster.AddExperienceType(AwardExperience.SPECIAL_XP + i, "Special experience from", 0.0f, (float)i);
            }
        }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "parameter", x => parameter = x, this, new List<string>());
            valid &= ConfigNodeUtil.ParseValue<List<Kerbal>>(configNode, "kerbal", x => kerbals = x, this, new List<Kerbal>());
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "experience", x => experience = x, this, 1);
            valid &= ConfigNodeUtil.ParseValue<bool?>(configNode, "awardImmediately", x => awardImmediately = x.Value, this, (bool?)false);

            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "parameter", "kerbal" }, this);

            return valid;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new AwardExperience(parameter, kerbals, experience, awardImmediately);
        }
    }
}
