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
    /// Parameter for checking for a docking event between two craft.
    /// </summary>
    public class Docking : VesselParameter
    {
        protected List<string> vessels { get; set; }
        protected string defineDockedVessel { get; set; }

        private Vessel[] dockedVessels = new Vessel[2];

        public Docking()
            : base(null)
        {
        }

        public Docking(IEnumerable<string> vessels, string defineDockedVessel, string title)
            : base(title)
        {
            this.vessels = vessels.ToList();
            this.defineDockedVessel = defineDockedVessel;
            this.title = title;
            disableOnStateChange = true;
        }

        protected override string GetParameterTitle()
        {
            string output = "";
            if (string.IsNullOrEmpty(title))
            {
                if (Parent is VesselParameterGroup)
                {
                    output = "Docked with: ";
                    if (vessels.Count > 0)
                    {
                        output += ContractVesselTracker.Instance.GetDisplayName(vessels[0]);
                    }
                    else
                    {
                        output += "Any vessel";
                    }
                }
                else
                {
                    output = "Docked: " + ContractVesselTracker.Instance.GetDisplayName(vessels[0]) + " and ";
                    if (vessels.Count > 1)
                    {
                        output += ContractVesselTracker.Instance.GetDisplayName(vessels[1]);
                    }
                    else
                    {
                        output += "any vessel";
                    }
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
            node.AddValue("title", title);
            foreach (string vessel in vessels)
            {
                node.AddValue("vessel", vessel);
            }
            node.AddValue("defineDockedVessel", defineDockedVessel);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            title = node.GetValue("title");
            vessels = ConfigNodeUtil.ParseValue<List<string>>(node, "vessel", new List<string>());
            defineDockedVessel = ConfigNodeUtil.ParseValue<string>(node, "defineDockedVessel", (string)null);
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onVesselDestroy.Add(new EventData<Vessel>.OnEvent(OnVesselDestroy));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onVesselDestroy.Remove(new EventData<Vessel>.OnEvent(OnVesselDestroy));
        }

        protected virtual void OnVesselDestroy(Vessel v)
        {
            if (dockedVessels[0] != null && !string.IsNullOrEmpty(defineDockedVessel))
            {
                LoggingUtil.LogVerbose(this, "OnVesselDestroy(" + v.id + ")");

                // Figure out which of the two vessels was kept after docking
                Vessel keptVessel = null;
                if (dockedVessels[0] == v)
                {
                    keptVessel = dockedVessels[1];
                }
                if (dockedVessels[1] == v)
                {
                    keptVessel = dockedVessels[0];
                }

                // If we are seeing a destroy event for something we care about
                if (keptVessel != null)
                {
                    LoggingUtil.LogVerbose(this, "Associating '" + defineDockedVessel + "' to " + keptVessel.id);
                    ContractVesselTracker.Instance.AssociateVessel(defineDockedVessel, keptVessel);
                    dockedVessels[0] = dockedVessels[1] = null;
                }
            }
        }

        protected override void OnPartAttach(GameEvents.HostTargetAction<Part, Part> e)
        {
            base.OnPartAttach(e);

            if (HighLogic.LoadedScene == GameScenes.EDITOR || e.host.vessel == null || e.target.vessel == null)
            {
                return;
            }

            LoggingUtil.LogVerbose(this, "OnPartAttach");
            Vessel v1 = null;
            Vessel v2 = null;

            if (Parent is VesselParameterGroup)
            {
                v1 = ((VesselParameterGroup)Parent).TrackedVessel ?? FlightGlobals.ActiveVessel;
                v2 = vessels.Count > 0 ? ContractVesselTracker.Instance.GetAssociatedVessel(vessels[0]) : null;

                // No vessel association
                if (vessels.Count > 0 && v2 == null)
                {
                    return;
                }
            }
            else
            {
                v1 = ContractVesselTracker.Instance.GetAssociatedVessel(vessels[0]);
                v2 = vessels.Count > 1 ? ContractVesselTracker.Instance.GetAssociatedVessel(vessels[1]) : null;

                // No vessel association
                if (v1 == null || vessels.Count > 1 && v2 == null)
                {
                    return;
                }
            }

            LoggingUtil.LogVerbose(this, "v1 = " + (v1 == null ? "null" : v1.id.ToString()));
            LoggingUtil.LogVerbose(this, "v2 = " + (v2 == null ? "null" : v2.id.ToString()));
            LoggingUtil.LogVerbose(this, "e.host.vessel = " + e.host.vessel.id.ToString());
            LoggingUtil.LogVerbose(this, "e.target.vessel = " + e.target.vessel.id.ToString());

            // Check for match
            bool forceStateChange = false;
            if (e.host.vessel == (v1 ?? e.host.vessel) && e.target.vessel == (v2 ?? e.target.vessel) ||
                e.host.vessel == (v2 ?? e.host.vessel) && e.target.vessel == (v1 ?? e.target.vessel))
            {
                LoggingUtil.LogVerbose(this, "Setting e.host.vessel to complete");
                forceStateChange |= SetState(e.host.vessel, ParameterState.Complete);

                LoggingUtil.LogVerbose(this, "Setting e.target.vessel to complete");
                forceStateChange |= SetState(e.target.vessel, ParameterState.Complete);

                dockedVessels[0] = e.host.vessel;
                dockedVessels[1] = e.target.vessel;
            }

            CheckVessel(FlightGlobals.ActiveVessel, forceStateChange);
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            return GetState(vessel) == ParameterState.Complete;
        }
    }
}
