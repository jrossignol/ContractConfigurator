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
        public SCANdata.SCANtype scanType { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        private Dictionary<string, string> nameRemap = new Dictionary<string, string>();

        public SCANsatCoverage()
            : this(95.0f, SCANdata.SCANtype.Altimetry, null, "")
        {
        }

        public SCANsatCoverage(double coverage, SCANdata.SCANtype scanType, CelestialBody targetBody, string title)
            : base()
        {
            this.title = title;
            if (title == null)
            {
                // Re-label a couple of scan names to make them nicer
                nameRemap["AltimetryLoRes"] = "Low resolution altimetry";
                nameRemap["AltimetryHiRes"] = "High resolution altimetry";

                string scanTypeName = nameRemap.ContainsKey(scanType.ToString()) ? nameRemap[scanType.ToString()] : scanType.ToString();
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
            coverage = ConfigNodeUtil.ParseValue<double>(node, "coverage");
            scanType = ConfigNodeUtil.ParseValue<SCANdata.SCANtype>(node, "scanType");
            targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody");
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
                double coverageInPercentage = SCANUtil.GetCoverage((int)scanType, targetBody);
                // While loading flight, SCANsat returns 100% coverage, that will complete any coverage parameter instant :(
                if (coverageInPercentage != 100 && coverageInPercentage > coverage)
                {
                    SetComplete();
                }
            }
        }
    }
}
