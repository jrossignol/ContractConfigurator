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
        private int experience;
        private bool awardImmediately;

        /// <summary>
        /// Static initializer to hack the kerbal experience/flight log system to add our entries.
        /// It's done on the factory to guarantee it's always run, otherwise uninstalling a
        /// contract pack could have the side effect of wiping some XP from a saved game.
        /// </summary>
        static AwardExperienceFactory()
        {
            LoggingUtil.LogVerbose(typeof(AwardExperienceFactory), "Doing setup of Kerbal Experience extras");

            FieldInfo[] fields = typeof(KerbalRoster).GetFields(BindingFlags.NonPublic | BindingFlags.Static);

            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(null);
                IEnumerable<string> strValues = value as IEnumerable<string>;
                if (strValues != null)
                {
                    // We're looking for the Kerbin lists that contain Training, but not PlantFlag
                    if (strValues.Contains("Training") && !strValues.Contains("PlantFlag"))
                    {
                        LoggingUtil.LogVerbose(typeof(AwardExperienceFactory), "Adding SpecialExperience items");
                        List<string> newValues = strValues.ToList();
                        // Allow up to 64 XP (max level)
                        for (int i = 3; i <= 64; i++)
                        {
                            newValues.Add(AwardExperience.SPECIAL_XP + i);
                        }
                        field.SetValue(null, newValues.ToArray());
                    }
                    // Also there's the printed version
                    else if (strValues.Contains("Train at") && !strValues.Contains("Plant flag on"))
                    {
                        LoggingUtil.LogVerbose(typeof(AwardExperienceFactory), "Adding 'Special experience from' items");
                        List<string> newValues = strValues.ToList();
                        // Allow up to 64 XP (max level)
                        for (int i = 3; i <= 64; i++)
                        {
                            newValues.Add("Special experience from");
                        }
                        field.SetValue(null, newValues.ToArray());
                    }

                    continue;
                }

                IEnumerable<float> floatValues = value as IEnumerable<float>;
                if (floatValues != null)
                {
                    // Get the list of experience points for the above string entries
                    if (floatValues.First() == 1.0f && floatValues.Count() == 5)
                    {
                        LoggingUtil.LogVerbose(typeof(AwardExperienceFactory), "Adding float items");
                        List<float> newValues = floatValues.ToList();
                        // Allow up to 64 XP (max level)
                        for (int i = 3; i <= 64; i++)
                        {
                            newValues.Add((float)i);
                        }
                        field.SetValue(null, newValues.ToArray());
                    }

                    continue;
                }
            }
        }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "parameter", x => parameter = x, this);
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "experience", x => experience = x, this, 1);
            valid &= ConfigNodeUtil.ParseValue<bool?>(configNode, "awardImmediately", x => awardImmediately = x.Value, this, (bool?)false);

            return valid;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new AwardExperience(parameter, experience, awardImmediately);
        }
    }
}
