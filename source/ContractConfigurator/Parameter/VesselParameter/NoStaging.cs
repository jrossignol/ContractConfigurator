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
    /// Parameter for checking that a vessel is recovered.
    /// </summary>
    public class NoStaging : VesselParameter
    {
        protected HashSet<Vessel> staged = new HashSet<Vessel>();
        protected HashSet<Vessel> possibleStages = new HashSet<Vessel>();
        protected float lastPartJointTime;
        protected float lastUndockTime;

        public NoStaging()
            : this(false, null)
        {
        }

        public NoStaging(bool failContract, string title)
            : base(title)
        {
            this.title = title != null ? title : Localizer.GetStringByTag("#cc.param.NoStaging");

            failWhenUnmet = true;
            fakeFailures = !failContract;
            disableOnStateChange = false;

            state = ParameterState.Complete;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);

            ConfigNode stagedNode = node.AddNode("STAGED_VESSELS");
            foreach (Vessel v in staged)
            {
                stagedNode.AddValue("vessel", v.id);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);

            staged = new HashSet<Vessel>(ConfigNodeUtil.ParseValue<List<Vessel>>(node.GetNode("STAGED_VESSELS"), "vessel", new List<Vessel>()));
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onStageSeparation.Add(new EventData<EventReport>.OnEvent(OnStageSeparation));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onStageSeparation.Remove(new EventData<EventReport>.OnEvent(OnStageSeparation));
        }

        protected void OnStageSeparation(EventReport er)
        {
            LoggingUtil.LogVerbose(this, "OnStageSeparation");

            // We have a valid stage seperation
            if (lastPartJointTime == UnityEngine.Time.fixedTime)
            {
                foreach (Vessel v in possibleStages)
                {
                    // Add to staged list
                    staged.Add(v);

                    // Force a vessel check
                    CheckVessel(v);
                }
            }
        }

        protected override void OnPartJointBreak(PartJoint pj, float breakForce)
        {
            LoggingUtil.LogVerbose(this, "OnPartJointBreak");

            // Special docking port handling, because they don't fire an event if cross-feed is enabled
            // (the OnUndock event is only good is cross-feed is off)
            int dockingPortCount = 0;
            for (int i = 0; i < 2; i++)
            {
                Part p = i == 0 ? pj.Parent : pj.Child;
                foreach (PartModule pm in p.Modules)
                {
                    if (pm.moduleName.StartsWith("ModuleDocking"))
                    {
                        dockingPortCount++;
                        break;
                    }
                }
            }

            // This is a confirmation, add the first vessel id to staged
            if (dockingPortCount == 2)
            {
                staged.Add(pj.Parent.vessel);
                lastUndockTime = UnityEngine.Time.fixedTime;
            }
            // Need to check for a stage seperation
            else
            {
                possibleStages.Clear();
                possibleStages.Add(pj.Parent.vessel);
                lastPartJointTime = UnityEngine.Time.fixedTime;
            }

            // Vessel check happens here
            base.OnPartJointBreak(pj, breakForce);
        }

        protected override void OnVesselCreate(Vessel v)
        {
            LoggingUtil.LogVerbose(this, "OnVesselCreate");
            if (lastPartJointTime == UnityEngine.Time.fixedTime)
            {
                possibleStages.Add(v);
            }
            else if (lastUndockTime == UnityEngine.Time.fixedTime)
            {
                // For undocking, treat as a confirmed staging as this is the last event we'll see
                staged.Add(v);
            }

            base.OnVesselCreate(v);
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: {0}", vessel.id);

            return !staged.Contains(vessel);
        }
    }
}
