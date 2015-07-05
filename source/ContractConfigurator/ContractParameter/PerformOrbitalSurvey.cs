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
    /// Parameter requiring that an orbital resource survey is done
    /// </summary>
    public class PerformOrbitalSurvey : ContractConfiguratorParameter
    {
        protected CelestialBody targetBody;

        private double lastUpdate = 0.0;

        public PerformOrbitalSurvey()
            : base(null)
        {
        }

        public PerformOrbitalSurvey(string title,  CelestialBody targetBody)
            : base(title)
        {
            disableOnStateChange = true;

            this.targetBody = targetBody;
        }

        protected override string GetParameterTitle()
        {
            string output;
            if (string.IsNullOrEmpty(title))
            {
                output = "Perform an orbital resource survey of " + targetBody.theName;
            }
            else 
            {
                output = title;
            }
            return output;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            node.AddValue("targetBody", targetBody.name);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody");
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Do a check every second or so
            if (Planetarium.GetUniversalTime() - lastUpdate > 1.0f)
            {
                lastUpdate = Planetarium.GetUniversalTime();

                if (ResourceScenario.Instance != null &&
                    ResourceScenario.Instance.gameSettings.GetPlanetScanInfo().Where(psd => psd.PlanetId == targetBody.flightGlobalsIndex).Any())
                {
                    SetState(ParameterState.Complete);
                }
            }
        }
    }
}
