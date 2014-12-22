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
     * Special VesselParamete which delegates the actual checking to another parameter.  Use this
     * to bring in parameters that were not meant to be used with VesselParameter.
     */
    public class VesselParameterDelegator : VesselParameter
    {
        protected string title { get; set; }
        protected ContractParameter delegateParam { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.1f;

        public VesselParameterDelegator()
            : this(null, null)
        {
        }

        public VesselParameterDelegator(ContractParameter delegateParam, string title = null)
            : base()
        {
            this.title = title;
            this.delegateParam = delegateParam;
        }

        protected override string GetTitle()
        {
            if (string.IsNullOrEmpty(title))
            {
                return delegateParam.Title;
            }
            else
            {
                return title;
            }
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
            node.AddValue("paramLocation", GetPathFromParam(delegateParam).Reverse().Aggregate<int, string>("", (s, i) => s + (s == "" ? "" : ",") + i));
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            delegateParam = GetParamFromPath(node.GetValue("paramLocation"));
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (UnityEngine.Time.fixedTime - lastUpdate > UPDATE_FREQUENCY)
            {
                lastUpdate = UnityEngine.Time.fixedTime;
                CheckVessel(FlightGlobals.ActiveVessel);
            }
        }

        /*
         * Gets the "path" to the given contract parameter from the root.
         */
        protected static IEnumerable<int> GetPathFromParam(ContractParameter p)
        {
            IContractParameterHost h = p;
            while (h != p.Root)
            {
                for (int i = 0; i < h.Parent.ParameterCount; i++)
                {
                    if (h.Parent.GetParameter(i) == h)
                    {
                        yield return i;
                        break;
                    }
                }
                h = h.Parent;
            }
            yield break;
        }

        /*
         * Follows the given path to get the parameter.
         */
        protected ContractParameter GetParamFromPath(string path)
        {
            IContractParameterHost h = Root;
            foreach (int i in path.Split(new char[] { ',' }).Select<string, int>(s => Convert.ToInt32(s)))
            {
                h = h.GetParameter(i);
            }
            return h as ContractParameter;
        }

        /*
         * Whether this vessel meets the parameter condition.
         */
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            if (vessel == FlightGlobals.ActiveVessel)
            {
                return delegateParam.State == ParameterState.Complete;
            }
            else
            {
                return GetState(vessel) == ParameterState.Complete;
            }
        }
    }
}
