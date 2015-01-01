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
    /*
     * Parameter for checking whether a vessel has a part.
     */
    public class HasPart : VesselParameter
    {
        protected string title { get; set; }
        protected AvailablePart part { get; set; }
        protected int minCount { get; set; }
        protected int maxCount { get; set; }

        public HasPart()
            : this(null)
        {
        }

        public HasPart(AvailablePart part, int minCount = 1, int maxCount = int.MaxValue, string title = null)
            : base()
        {
            // Vessels should fail if they don't meet the part conditions
            failWhenUnmet = true;

            this.part = part;
            this.minCount = minCount;
            this.maxCount = maxCount;
            if (title == null && part != null)
            {
                this.title += "Part: " + part.title + ": ";

                if (maxCount == 0)
                {
                    this.title += "None";
                }
                else if (maxCount == int.MaxValue)
                {
                    this.title += "At least " + minCount;
                }
                else if (minCount == 0)
                {
                    this.title += "At most " + maxCount;
                }
                else if (minCount == maxCount)
                {
                    this.title += "Exactly " + minCount;
                }
                else
                {
                    this.title += "Between " + minCount + " and " + maxCount;
                }
            }
            else
            {
                this.title = title;
            }
        }

        protected override string GetTitle()
        {
            return title;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
            node.AddValue("minCount", minCount);
            node.AddValue("maxCount", maxCount);
            node.AddValue("part", part.name);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            minCount = Convert.ToInt32(node.GetValue("minCount"));
            maxCount = Convert.ToInt32(node.GetValue("maxCount"));
            part = ConfigNodeUtil.ParseValue<AvailablePart>(node, "part");
        }

        protected override void OnRegister()
        {
            base.OnRegister();
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
        }

        protected override void OnPartAttach(GameEvents.HostTargetAction<Part, Part> e)
        {
            base.OnPartAttach(e);
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected override void OnPartJointBreak(PartJoint p)
        {
            base.OnPartJointBreak(p);
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        /*
         * Whether this vessel meets the parameter condition.
         */
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            int count = vessel.parts.Count<Part>(p => p.partInfo.name == part.name);
            return count >= minCount && count <= maxCount;
        }
    }
}
