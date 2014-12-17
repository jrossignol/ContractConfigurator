using ContractConfigurator.Parameters;
using Contracts;
using SCANsat;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContractConfigurator;
using System;
using UnityEngine;

namespace ContractConfigurator
{
    public class ScanSatCoverage : Contracts.ContractParameter
    {

        protected string title { get; set; }
        public double coverage { get; set; }
        public CelestialBody targetBody { get; set;}
        public int scanType { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.1f;

        public ScanSatCoverage()
            : this(95.0f, 0, null, null, null)
        {
        }

        public ScanSatCoverage(double coverage, int scanType, string scanTypeName, CelestialBody targetBody, string title)
            : base()
        {

            Debug.Log("ScanSatCoverage in Constructor");

            this.title = title;
            if (title == null) {
                this.title = "Get a minimum of " + coverage + "% coverage using a " + scanTypeName + "-Scan of " + targetBody + ".";
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
