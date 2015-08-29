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
    /// Parameter for checking that a vessel does not get destroyed.
    /// </summary>
    public class VesselNotDestroyed : VesselParameter
    {
        protected List<string> vessels { get; set; }

        private float lastVesselChange = 0.0f;

        public VesselNotDestroyed()
            : base(null)
        {
        }

        public VesselNotDestroyed(IEnumerable<string> vessels, string title)
            : base(title)
        {
            this.vessels = vessels.ToList();
            this.state = ParameterState.Complete;
        }

        protected override string GetParameterTitle()
        {
            string output = "";
            if (string.IsNullOrEmpty(title))
            {
                if (vessels.Count == 1)
                {
                    output = ContractVesselTracker.GetDisplayName(vessels[0]) + ": Not destroyed";
                }
                else if (vessels.Count != 0)
                {
                    output = "Vessels not destroyed: ";
                    bool first = true;
                    foreach (string vessel in vessels)
                    {
                        output += (first ? "" : ", ") + ContractVesselTracker.GetDisplayName(vessel);
                        first = false;
                    }
                }
                else if (Parent is VesselParameterGroup && ((VesselParameterGroup)Parent).VesselList.Any())
                {
                    IEnumerable<string> vesselList = ((VesselParameterGroup)Parent).VesselList;
                    if (vesselList.Count() == 1)
                    {
                        output = ContractVesselTracker.GetDisplayName(vesselList.First()) + ": Not destroyed";
                    }
                    else
                    {
                        output = "Vessels not destroyed: ";
                        bool first = true;
                        foreach (string vessel in vesselList)
                        {
                            output += (first ? "" : ", ") + ContractVesselTracker.GetDisplayName(vessel);
                            first = false;
                        }
                    }
                }
                else
                {
                    output = "No vessels destroyed";
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
            base.OnParameterSave(node);
            foreach (string vessel in vessels)
            {
                node.AddValue("vessel", vessel);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            vessels = ConfigNodeUtil.ParseValue<List<string>>(node, "vessel", new List<string>());
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onVesselWillDestroy.Add(new EventData<Vessel>.OnEvent(OnVesselWillDestroy));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onVesselWillDestroy.Remove(new EventData<Vessel>.OnEvent(OnVesselWillDestroy));
        }

        protected override void OnVesselChange(Vessel vessel)
        {
            base.OnVesselChange(vessel);

            lastVesselChange = Time.fixedTime;
        }

        protected virtual void OnVesselWillDestroy(Vessel v)
        {
            LoggingUtil.LogVerbose(this, "OnVesselWillDestroy: " + v.id);

            // Give a quarter second grace for detecting a "destroyed" EVA that is actually just a boarding event
            if (v.vesselType == VesselType.EVA && Time.fixedTime - lastVesselChange < 0.25)
            {
                return;
            }

            IEnumerable<string> vesselIterator;
            if (vessels.Count != 0)
            {
                vesselIterator = vessels;
            }
            else if (Parent is VesselParameterGroup && ((VesselParameterGroup)Parent).VesselList.Any())
            {
                vesselIterator = ((VesselParameterGroup)Parent).VesselList;
            }
            else if (v.vesselType == VesselType.Debris)
            {
                return;
            }
            else
            {
                LoggingUtil.LogVerbose(this, "Any vessel match, failing parameter.");
                // Fail on any vessel
                SetState(ParameterState.Failed);
                return;
            }

            // Check for any match
            IEnumerable<string> keys = ContractVesselTracker.Instance.GetAssociatedKeys(v);
            foreach (string vessel in vesselIterator)
            {
                if (keys.Contains(vessel))
                {
                    LoggingUtil.LogVerbose(this, "Specific vessel match on '" + vessel + "', failing parameter.");
                    SetState(ParameterState.Failed);
                    return;
                }
            }
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Always true</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            return true;
        }
    }
}
