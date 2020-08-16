using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using KSP.Localization;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Parameter for checking whether a target vessel is destroyed or not.
    /// </summary>
    public class TargetDestroyed : ContractConfiguratorParameter, ParameterDelegateContainer
    {
        protected List<string> vessels { get; set; }

        public bool ChildChanged { get; set; }

        private List<string> destroyedTargets = new List<string>();
        private List<VesselWaypoint> vesselWaypoints = new List<VesselWaypoint>();

        public TargetDestroyed()
            : base(null)
        {
        }

        public TargetDestroyed(IEnumerable<string> vessels, string title)
            : base(title)
        {
            disableOnStateChange = true;

            this.vessels = vessels.ToList();

            CreateDelegates();
        }

        protected override string GetParameterTitle()
        {
            string output = "";
            if (string.IsNullOrEmpty(title))
            {
                output = Localizer.Format("#cc.param.TargetDestroyed", vessels.Count);
            }
            else
            {
                output = title;
            }
            return output;
        }

        protected void CreateDelegates()
        {
            foreach (string vessel in vessels)
            {
                AddParameter(new ParameterDelegate<string>("Target: " + (ContractVesselTracker.Instance != null ? ContractVesselTracker.GetDisplayName(vessel) : vessel),
                    ignored => CheckTargetDestroyed(vessel), ParameterDelegateMatchType.VALIDATE_ALL, true));
            }
        }

        private bool CheckTargetDestroyed(string vessel)
        {
            LoggingUtil.LogVerbose(this, "TargetDestroyed(" + vessel + ")");
            
            // Already tracked as destroyed
            if (destroyedTargets.Contains(vessel))
            {
                return true;
            }

            // Check for new debris
            Vessel v = ContractVesselTracker.Instance.GetAssociatedVessel(vessel);
            if (v != null && v.vesselType == VesselType.Debris)
            {
                destroyedTargets.Add(vessel);
                return true;
            }

            // Not destroyed!
            return false;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            foreach (string vessel in vessels)
            {
                node.AddValue("vessel", vessel);
            }
            foreach (string destroyedTarget in destroyedTargets)
            {
                node.AddValue("destroyedTarget", destroyedTarget);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            try
            {
                vessels = ConfigNodeUtil.ParseValue<List<string>>(node, "vessel", new List<string>());
                destroyedTargets = ConfigNodeUtil.ParseValue<List<string>>(node, "destroyedTarget", new List<string>());

                CreateDelegates();
            }
            finally
            {
                ParameterDelegate<string>.OnDelegateContainerLoad(node);
            }
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onVesselWillDestroy.Add(new EventData<Vessel>.OnEvent(OnVesselWillDestroy));
            GameEvents.onVesselWasModified.Add(new EventData<Vessel>.OnEvent(OnVesselWasModified));

            // Add a waypoint for each possible vessel in the list
            foreach (string vesselKey in vessels)
            {
                VesselWaypoint vesselWaypoint = new VesselWaypoint(Root, vesselKey);
                vesselWaypoints.Add(vesselWaypoint);
                vesselWaypoint.Register();
            }
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onVesselWillDestroy.Remove(new EventData<Vessel>.OnEvent(OnVesselWillDestroy));
            GameEvents.onVesselWasModified.Add(new EventData<Vessel>.OnEvent(OnVesselWasModified));

            foreach (VesselWaypoint vesselWaypoint in vesselWaypoints)
            {
                vesselWaypoint.Unregister();
            }
        }

        protected virtual void OnVesselWillDestroy(Vessel v)
        {
            LoggingUtil.LogVerbose(this, "OnVesselWillDestroy: " + v.id);
            foreach (string key in ContractVesselTracker.Instance.GetAssociatedKeys(v))
            {
                LoggingUtil.LogVerbose(this, "adding to destroyedTargets: " + key);
                destroyedTargets.AddUnique(key);
            }

            CheckVessels();
        }

        protected virtual void OnVesselWasModified(Vessel v)
        {
            CheckVessels();
        }

        protected virtual void CheckVessels()
        {
            bool success = ParameterDelegate<string>.CheckChildConditions(this, "");
            if (ChildChanged || success)
            {
                ChildChanged = false;
                if (success)
                {
                    SetState(ParameterState.Complete);
                }
                else
                {
                    ContractConfigurator.OnParameterChange.Fire(Root, this);
                }
            }
        }
    }
}
