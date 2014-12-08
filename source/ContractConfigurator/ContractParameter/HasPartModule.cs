using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator.Parameters
{
    /*
     * Parameter for checking whether a vessel has a part module.
     */
    public class HasPartModule : VesselParameter
    {
        protected string title { get; set; }
        protected string partModule { get; set; }
        protected int minCount { get; set; }
        protected int maxCount { get; set; }

        public HasPartModule()
            : this(null)
        {
        }

        public HasPartModule(string partModule, int minCount = 1, int maxCount = int.MaxValue, string title = null)
            : base()
        {
            // Vessels should fail if they don't meet the part conditions
            failWhenUnmet = true;

            this.partModule = partModule;
            this.minCount = minCount;
            this.maxCount = maxCount;
            if (title == null && partModule != null)
            {
                string moduleName = partModule.Replace("Module", "");
                moduleName = Regex.Replace(moduleName, "(\\B[A-Z])", " $1");
                this.title = "Module: " + moduleName + ": ";

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
            node.AddValue("partModule", partModule);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            minCount = Convert.ToInt32(node.GetValue("minCount"));
            maxCount = Convert.ToInt32(node.GetValue("maxCount"));
            partModule = node.GetValue("partModule");
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
            // No linq for part modules. :(
            int count = 0;
            foreach (Part p in vessel.parts)
            {
                foreach (PartModule pm in p.Modules)
                {
                    if (pm.moduleName == partModule)
                    {
                        count++;
                    }
                }
            }
            return count >= minCount && count <= maxCount;
        }
    }
}
