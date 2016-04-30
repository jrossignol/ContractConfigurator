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
    /// Special VesselParamete which delegates the actual checking to another parameter.  Use this
    /// to bring in parameters that were not meant to be used with VesselParameter.
    /// </summary>
    public class VesselParameterDelegator : VesselParameter
    {
        public interface INotesProvider
        {
            string VesselParameterNotes();
        }

        protected ContractParameter delegateParam { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.1f;

        public bool displayNotes;

        public VesselParameterDelegator()
            : this(null, true, null)
        {
        }

        public VesselParameterDelegator(ContractParameter delegateParam, bool displayNotes, string title = null)
            : base(title)
        {
            this.delegateParam = delegateParam;
            this.displayNotes = displayNotes;
        }

        protected override string GetParameterTitle()
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

        protected override string GetNotes()
        {
            string baseNotes = base.GetNotes();
            if (string.IsNullOrEmpty(baseNotes) && displayNotes)
            {
                INotesProvider notesProvider = delegateParam as INotesProvider;
                if (notesProvider != null)
                {
                    return notesProvider.VesselParameterNotes();
                }
                else
                {
                    return delegateParam.Notes;
                }
            }
            else
            {
                return baseNotes;
            }
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);
            node.AddValue("paramLocation", GetPathFromParam(delegateParam).Reverse().Aggregate<int, string>("", (s, i) => s + (s == "" ? "" : ",") + i));
            if (!displayNotes)
            {
                node.AddValue("displayNotes", displayNotes);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            delegateParam = GetParamFromPath(node.GetValue("paramLocation"));
            displayNotes = ConfigNodeUtil.ParseValue<bool?>(node, "displayNotes", null) ?? true;
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

        /// <summary>
        /// Gets the "path" to the given contract parameter from the root.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected static IEnumerable<int> GetPathFromParam(ContractParameter p)
        {
            IContractParameterHost h = p;
            while (h != p.Root)
            {
                for (int i = 0; i < h.Parent.ParameterCount; i++)
                {
                    ContractParameter tmp;
                    try
                    {
                        tmp = h.Parent.GetParameter(i);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        yield break;
                    }
                    if (tmp == h)
                    {
                        yield return i;
                        break;
                    }
                }
                h = h.Parent;
            }
            yield break;
        }

        /// <summary>
        /// Follows the given path to get the parameter.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected ContractParameter GetParamFromPath(string path)
        {
            IContractParameterHost h = Root;
            foreach (int i in path.Split(new char[] { ',' }).Select<string, int>(s => Convert.ToInt32(s)))
            {
                h = h.GetParameter(i);
            }
            return h as ContractParameter;
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
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
