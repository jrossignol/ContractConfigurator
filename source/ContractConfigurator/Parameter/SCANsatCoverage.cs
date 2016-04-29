using ContractConfigurator.Parameters;
using Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContractConfigurator;
using System;
using UnityEngine;

namespace ContractConfigurator
{
    public class SCANsatCoverage : ContractConfiguratorParameter
    {
        public double coverage { get; set; }
        public CelestialBody targetBody { get; set;}
        public string scanName { get; set; }
        public int scanType { get; set; }

        private float lastRealUpdate = 0.0f;
        private double lastGameTimeUpdate = 0.0;
        private int consecutive_successes = 0;
        private const float REAL_UPDATE_FREQUENCY = 5.0f;
        private const double GAME_UPDATE_FREQUENCY = 100.0;
        private const int CONSECUTIVE_SUCCESSES_REQUIRED = 2;
        private double currentCoverage = 0.0;

        private Dictionary<string, string> nameRemap = new Dictionary<string, string>();

        public SCANsatCoverage()
            : base(null)
        {
        }

        public SCANsatCoverage(double coverage, string scanName, CelestialBody targetBody, string title)
            : base(title)
        {
            this.coverage = coverage;
            this.scanName = scanName;
            this.scanType = SCANsatUtil.GetSCANtype(scanName);
            this.targetBody = targetBody;
        }

        protected override string GetParameterTitle()
        {
            string output;
            if (string.IsNullOrEmpty(title))
            {
                // Re-label a couple of scan names to make them nicer
                nameRemap["AltimetryLoRes"] = "Low resolution altimetry";
                nameRemap["AltimetryHiRes"] = "High resolution altimetry";

                string scanTypeName = nameRemap.ContainsKey(scanName) ? nameRemap[scanName] : scanName;
                output = scanTypeName + " scan of " + targetBody.theName + ": ";
                if (currentCoverage > 0.0 && state != ParameterState.Complete)
                {
                    output += currentCoverage.ToString("N0") + "% / ";
                }
                output += coverage.ToString("N0") + "%";
            }
            else
            {
                output = title;
            }

            return output;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            node.AddValue("coverage", coverage);
            node.AddValue("scanName", scanName);
            node.AddValue("scanType", scanType);
            node.AddValue("targetBody", targetBody.name);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            coverage = ConfigNodeUtil.ParseValue<double>(node, "coverage");
            scanType = ConfigNodeUtil.ParseValue<int>(node, "scanType");
            scanName = ConfigNodeUtil.ParseValue<string>(node, "scanName", "");
            targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody");
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (targetBody == null)
            {
                return;
            }

            // Do a check if either:
            //   REAL_UPDATE_FREQUENCY of real time has elapsed
            //   GAME_UPDATE_FREQUENCY of game time has elapsed
            if (UnityEngine.Time.fixedTime - lastRealUpdate > REAL_UPDATE_FREQUENCY ||
                Planetarium.GetUniversalTime() - lastGameTimeUpdate > GAME_UPDATE_FREQUENCY)
            {
                lastRealUpdate = UnityEngine.Time.fixedTime;
                lastGameTimeUpdate = Planetarium.GetUniversalTime();
                currentCoverage = SCANsatUtil.GetCoverage(scanType, targetBody);

                // Count the number of sucesses
                if (currentCoverage > coverage)
                {
                    consecutive_successes++;
                }
                else
                {
                    consecutive_successes = 0;
                }

                // We've had enough successes to be sure that the scan is complete
                if (consecutive_successes >= CONSECUTIVE_SUCCESSES_REQUIRED)
                {
                    SetState(ParameterState.Complete);
                }

                // Force a call to GetTitle to update the contracts app
                GetTitle();
            }
        }
    }
}
