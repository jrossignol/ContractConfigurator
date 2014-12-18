using ContractConfigurator.Parameters;
using Contracts;
using SCANsat;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContractConfigurator;
using System;
using UnityEngine;

namespace ContractConfigurator.SCANsat
{
    public class SCANsatCoverage : Contracts.ContractParameter
    {
        protected string title { get; set; }
        public double coverage { get; set; }
        public CelestialBody targetBody { get; set;}
        public int scanType { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        private Dictionary<string, string> nameRemap = new Dictionary<string, string>();

        public SCANsatCoverage()
            : this(95.0f, 0, null, null, "")
        {
        }

        public SCANsatCoverage(double coverage, int scanType, string scanTypeName, CelestialBody targetBody, string title)
            : base()
        {
            this.title = title;
            if (title == null) {
                // Re-label a couple of scan names to make them nicer
                nameRemap["AltimetryLoRes"] = "Low resolution altimetry";
                nameRemap["AltimetryHiRes"] = "High resolution altimetry";
                if (nameRemap.ContainsKey(scanTypeName))
                {
                    scanTypeName = nameRemap[scanTypeName];
                }

                this.title = scanTypeName + " scan: " + coverage.ToString("N0") + "% coverage of " + targetBody.name;
            }

            this.coverage = coverage;
            this.scanType = scanType;
            this.targetBody = targetBody;
        }

        protected override string GetTitle()
        {
            return title;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            node.AddValue("title", title);
            node.AddValue("coverage", coverage);
            node.AddValue("scanType", scanType);
            node.AddValue("targetBody", targetBody.name);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            title = node.GetValue("title");
            coverage = (double)Convert.ToDouble(node.GetValue("coverage"));
            scanType = (int)Convert.ToInt32(node.GetValue("scanType"));
            targetBody = ConfigNodeUtil.ParseCelestialBody(node, "targetBody");
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (targetBody == null)
            {
                return;
            }

            if (UnityEngine.Time.fixedTime - lastUpdate > UPDATE_FREQUENCY)
            {
                lastUpdate = UnityEngine.Time.fixedTime;
                double coverageInPercentage = SCANUtil.GetCoverage(scanType, targetBody);
                if (coverageInPercentage > coverage) {
                    SetComplete();
                }
            }
        }
    }
}
