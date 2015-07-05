using ContractConfigurator.Parameters;
using Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using UnityEngine;
using RemoteTech;
using ContractConfigurator;

namespace ContractConfigurator.RemoteTech
{
    /// <summary>
    /// Parameter to check the percentage coverage of the surface of a celestial body.
    /// </summary>
    public class CelestialBodyCoverageParameter : ContractConfiguratorParameter
    {
        protected double coverage { get; set; }
        protected CelestialBody targetBody { get; set; }
        private double currentCoverage = -1.0;

        TitleTracker titleTracker = new TitleTracker();

        public CelestialBodyCoverageParameter()
            : this(0.0, null)
        {
        }

        public CelestialBodyCoverageParameter(double coverage, CelestialBody targetBody, string title = null)
            : base(title)
        {
            this.coverage = coverage;
            this.targetBody = targetBody;
            disableOnStateChange = false;
        }

        protected override string GetParameterTitle()
        {
            string output;
            if (string.IsNullOrEmpty(title))
            {
                output = targetBody.name + ": Communication coverage: ";
                if (currentCoverage >= 0.0 && state != ParameterState.Complete)
                {
                    output += (currentCoverage * 100).ToString("F0") + "% / ";
                }
                output += (coverage * 100).ToString("F0") + "%";
                titleTracker.Add(output);
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
            node.AddValue("targetBody", targetBody.name);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            coverage = ConfigNodeUtil.ParseValue<double>(node, "coverage");
            targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody");
        }

        protected override void OnUpdate()
        {
            if (RemoteTechProgressTracker.Instance != null)
            {
                double oldCoverage = currentCoverage;
                currentCoverage = RemoteTechProgressTracker.Instance.GetCoverage(targetBody);
                if (currentCoverage >= coverage)
                {
                    SetState(ParameterState.Complete);
                    RemoteTechProgressTracker.Instance.RemoveFromPriorityList(targetBody);
                }
                else
                {
                    SetState(ParameterState.Incomplete);
                    RemoteTechProgressTracker.Instance.AddToPriorityList(targetBody);
                }

                // Update contract window
                titleTracker.UpdateContractWindow(this, GetTitle());
            }
        }
    }
}
