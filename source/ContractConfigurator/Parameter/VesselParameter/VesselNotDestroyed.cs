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
    /// Parameter for checking that a vessel does not get destroyed.
    /// </summary>
    public class VesselNotDestroyed : VesselParameter
    {
        protected List<string> vessels { get; set; }

        private List<Vessel> brokenVessels = new List<Vessel>();
        private float lastVesselChange = 0.0f;
        private float lastVesselAdd = 0.0f;
        private bool addNextVessel = false;

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
                    output = Localizer.Format("#cc.param.VesselNotDestroyed", ContractVesselTracker.GetDisplayName(vessels[0]));
                }
                else if (vessels.Count != 0)
                {
                    output = Localizer.Format("#cc.param.VesselNotDestroyed", LocalizationUtil.LocalizeList<string>(LocalizationUtil.Conjunction.OR, vessels, v => ContractVesselTracker.GetDisplayName(v)));
                }
                else if (Parent is VesselParameterGroup && ((VesselParameterGroup)Parent).VesselList.Any())
                {
                    IEnumerable<string> vesselList = ((VesselParameterGroup)Parent).VesselList;
                    if (vesselList.Count() == 1)
                    {
                        output = Localizer.Format("#cc.param.VesselNotDestroyed", ContractVesselTracker.GetDisplayName(vesselList.First()));
                    }
                    else
                    {
                        output = Localizer.Format("#cc.param.VesselNotDestroyed", LocalizationUtil.LocalizeList<string>(LocalizationUtil.Conjunction.OR, vesselList, v => ContractVesselTracker.GetDisplayName(v)));
                    }
                }
                else
                {
                    output = Localizer.GetStringByTag("#cc.param.VesselNotDestroyed.any");
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
            GameEvents.onPartDie.Add(new EventData<Part>.OnEvent(OnPartDie));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onVesselWillDestroy.Remove(new EventData<Vessel>.OnEvent(OnVesselWillDestroy));
            GameEvents.onPartDie.Remove(new EventData<Part>.OnEvent(OnPartDie));
        }

        protected override void OnVesselChange(Vessel vessel)
        {
            base.OnVesselChange(vessel);

            lastVesselChange = Time.fixedTime;
        }

        protected override void OnPartJointBreak(PartJoint p, float breakForce)
        {
            base.OnPartJointBreak(p, breakForce);

            Vessel v = p.Parent.vessel;
            if (v == null)
            {
                LoggingUtil.LogDebug(this, "OnPartJointBreak: p.Parent.vessel was null");
                return;
            }
            LoggingUtil.LogVerbose(this, "OnPartJointBreak: {0}", v.id);
            if (v.vesselType == VesselType.Debris)
            {
                return;
            }

            // Check if this is one of our vessels
            IEnumerable<string> vesselIterator;
            if (vessels.Count != 0)
            {
                vesselIterator = vessels;
            }
            else if (Parent is VesselParameterGroup && ((VesselParameterGroup)Parent).VesselList.Any())
            {
                vesselIterator = ((VesselParameterGroup)Parent).VesselList;
            }
            else
            {
                return;
            }

            // Add the vessel when created
            IEnumerable<string> keys = ContractVesselTracker.Instance.GetAssociatedKeys(v);
            foreach (string vessel in vesselIterator)
            {
                if (keys.Contains(vessel))
                {
                    lastVesselAdd = Time.fixedTime;
                    brokenVessels.AddUnique(v);
                    addNextVessel = true;
                    return;
                }
            }
        }

        protected override void OnVesselCreate(Vessel vessel)
        {
            base.OnVesselCreate(vessel);
            LoggingUtil.LogVerbose(this, "OnVesselCreate: {0}", vessel.id);

            if (addNextVessel)
            {
                lastVesselAdd = Time.fixedTime;
                addNextVessel = false;
                brokenVessels.AddUnique(vessel);
            }
        }

        protected virtual void OnVesselWillDestroy(Vessel v)
        {
            LoggingUtil.LogVerbose(this, "OnVesselWillDestroy: {0}", v.id);

            // Give a quarter second grace for detecting a "destroyed" EVA that is actually just a boarding event
            if (v.vesselType == VesselType.EVA && Time.fixedTime - lastVesselChange < 0.25)
            {
                return;
            }

            // Clear out broken vessels that are no longer being checked (one second grace)
            if (lastVesselAdd + 1.0 < Time.fixedTime)
            {
                brokenVessels.Clear();
            }
            // Check if this was once part of our vessel
            else if (brokenVessels.Contains(v))
            {
                LoggingUtil.LogVerbose(this, "Broken match, failing parameter.");
                SetState(ParameterState.Failed);
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
                SetState(ParameterState.Failed);
                return;
            }

            // Check for any match
            IEnumerable<string> keys = ContractVesselTracker.Instance.GetAssociatedKeys(v);
            foreach (string vessel in vesselIterator)
            {
                if (keys.Contains(vessel))
                {
                    LoggingUtil.LogVerbose(this, "Specific vessel match on '{0}', failing parameter.", vessel);
                    SetState(ParameterState.Failed);
                    return;
                }
            }
        }

        protected void OnPartDie(Part p)
        {
            if (p == null || p.vessel == null)
            {
                return;
            }

            Vessel v = p.vessel;
            LoggingUtil.LogVerbose(this, "OnPartDie: {0}", v.id);
            if (!v.IsControllable)
            {
                LoggingUtil.LogVerbose(this, "Vessel not contrallable, treating part death as vessel death");
                OnVesselWillDestroy(v);
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
