using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement that requires the player to have crew of a certain type and/or
    /// experience level in their space program.
    /// </summary>
    [Obsolete("HasCrew is obsolete since Contract Configurator 1.1.0, use HasAstronaut instead.")]
    public class HasCrewRequirement : HasAstronautRequirement
    {
        public override bool Load(ConfigNode configNode)
        {
            LoggingUtil.LogWarning(this, "HasCrew is obsolete since Contract Configurator 1.1.0, use HasAstronaut instead.");

            return base.Load(configNode);
        }
    }
}
