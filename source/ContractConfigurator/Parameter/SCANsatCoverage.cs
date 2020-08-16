using ContractConfigurator.Parameters;
using Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContractConfigurator;
using System;
using UnityEngine;
using KSP.Localization;

namespace ContractConfigurator
{
    public class SCANsatCoverage : ContractConfiguratorParameter
    {
        public double coverage { get; set; }
        public string scanName { get; set; }
        public int scanType { get; set; }

        private float lastRealUpdate = 0.0f;
        private double lastGameTimeUpdate = 0.0;
        private int consecutive_successes = 0;
        private const float REAL_UPDATE_FREQUENCY = 5.0f;
        private const double GAME_UPDATE_FREQUENCY = 100.0;
        private const int CONSECUTIVE_SUCCESSES_REQUIRED = 2;
        private double currentCoverage = 0.0;

        private static Dictionary<string, string> nameRemap = null;

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

        public static string ScanDisplayName(string scanName)
        {
            if (nameRemap == null)
            {
                nameRemap = new Dictionary<string, string>();

                // Localized scan names
                nameRemap["AltimetryLoRes"]  = Localizer.GetStringByTag("#autoLOC_SCANsat_Science_Lo_Title");
                nameRemap["AltimetryHiRes"]  = Localizer.GetStringByTag("#autoLOC_SCANsat_Science_Hi_Title");
                nameRemap["Anomaly"]         = Localizer.GetStringByTag("#cc.scansat.scan.Anomaly");
                nameRemap["AnomalyDetail"]   = Localizer.GetStringByTag("#cc.scansat.scan.AnomalyDetail");
                nameRemap["Biome"]           = Localizer.GetStringByTag("#cc.scansat.scan.Biome");
                nameRemap["ResourceLoRes"]   = Localizer.GetStringByTag("#cc.scansat.scan.ResourceLoRes");
                nameRemap["ResourceHiRes"]   = Localizer.GetStringByTag("#cc.scansat.scan.ResourceHiRes");
                nameRemap["VisualLoRes"]     = Localizer.GetStringByTag("#cc.scansat.scan.VisualLoRes");
                nameRemap["VisualHiRes"]     = Localizer.GetStringByTag("#cc.scansat.scan.VisualHiRes");
            }

            return nameRemap.ContainsKey(scanName) ? nameRemap[scanName] : scanName;
        }

        protected override string GetParameterTitle()
        {
            string output;
            if (string.IsNullOrEmpty(title))
            {
                if (currentCoverage > 0.0 && state != ParameterState.Complete)
                {
                    output = Localizer.Format("#cc.scansat.param.SCANsatCoverage.inProgress", ScanDisplayName(scanName), targetBody.CleanDisplayName(true), currentCoverage.ToString("N0"), coverage.ToString("N0"));
                }
                else
                {
                    output = Localizer.Format("#cc.scansat.param.SCANsatCoverage", ScanDisplayName(scanName), targetBody.CleanDisplayName(true), coverage.ToString("N0"));
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
