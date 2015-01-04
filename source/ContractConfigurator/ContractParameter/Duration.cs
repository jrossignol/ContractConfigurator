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
    /// Parameter for ensuring that a certain time must elapse before the contract completes.
    /// </summary>
    public class Duration : ContractParameter
    {
        protected double duration { get; set; }
        protected double endTime { get; set; }
        protected string preWaitText { get; set; }
        protected string waitingText { get; set; }
        protected string completionText { get; set; }

        private double lastUpdate = 0.0;
        private bool resetClock = false;

        private List<string> titleTracker = new List<string>();

        public Duration()
            : this(0.0)
        {
        }

        public Duration(double duration, string preWaitText = null, string waitingText = null, string completionText = null)
            : base()
        {
            this.duration = duration;
            this.preWaitText = preWaitText;
            this.waitingText = waitingText;
            this.completionText = completionText;
            endTime = 0.0;
        }

        protected override string GetTitle()
        {
            if (endTime > 0.01)
            {
                string title = null;
                if (endTime - Planetarium.GetUniversalTime() > 0.0)
                {
                    title = (waitingText ?? "Time to completion") + ": " + DurationUtil.StringValue(endTime - Planetarium.GetUniversalTime());
                }
                else
                {
                    title = completionText ?? "Wait time over";
                }

                // Add the string that we returned to the titleTracker.  This is used to update
                // the contract title element in the GUI directly, as it does not support dynamic
                // text.
                titleTracker.Add(title);

                return title;
            }
            else
            {
                return (preWaitText ?? "Waiting time required") + ": " + DurationUtil.StringValue(duration);
            }
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("duration", duration);
            node.AddValue("endTime", endTime);
            if (preWaitText != null)
            {
                node.AddValue("preWaitText", preWaitText);
            }
            if (waitingText != null)
            {
                node.AddValue("waitingText", waitingText);
            }
            if (completionText != null)
            {
                node.AddValue("completionText", completionText);
            }
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            duration = Convert.ToDouble(node.GetValue("duration"));
            endTime = Convert.ToDouble(node.GetValue("endTime"));
            preWaitText = node.HasValue("preWaitText") ? ConfigNodeUtil.ParseValue<string>(node, "preWaitText") : null;
            waitingText = node.HasValue("waitingText") ? ConfigNodeUtil.ParseValue<string>(node, "waitingText") : null;
            completionText = node.HasValue("completionText") ? ConfigNodeUtil.ParseValue<string>(node, "completionText") : null;
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.Contract.onParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.Contract.onParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected void OnParameterChange(Contract contract, ContractParameter param)
        {
            // Set the end time
            if (contract == Root)
            {
                bool completed = true;
                foreach (ContractParameter child in Root.AllParameters)
                {
                    if (child != this && child.State != ParameterState.Complete)
                    {
                        completed = false;
                        break;
                    }
                }

                if (completed)
                {
                    endTime = Planetarium.GetUniversalTime() + duration;
                }
                else
                {
                    endTime = 0.0;
                    resetClock = true;
                }
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Every time the clock ticks over, make an attempt to update the contract window
            // title.  We do this because otherwise the window will only ever read the title once,
            // so this is the only way to get our fancy timer to work.
            if (Planetarium.GetUniversalTime() - lastUpdate > 1.0f && (endTime != 0.0 || resetClock))
            {
                // Completed
                if (endTime != 0.0 && Planetarium.GetUniversalTime() > endTime)
                {
                    SetComplete();
                }
                lastUpdate = Planetarium.GetUniversalTime();

                // Go through all the list items in the contracts window
                UIScrollList list = ContractsApp.Instance.cascadingList.cascadingList;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        // Try to find a rich text control that matches the expected text
                        UIListItemContainer listObject = (UIListItemContainer)list.GetItem(i);
                        SpriteTextRich richText = listObject.GetComponentInChildren<SpriteTextRich>();
                        if (richText != null)
                        {
                            // Check for any string in titleTracker
                            string found = null;
                            foreach (string title in titleTracker)
                            {
                                if (richText.Text.Contains(title))
                                {
                                    found = title;
                                    break;
                                }
                            }

                            // Clear the titleTracker, and replace the text
                            if (found != null)
                            {
                                titleTracker.Clear();
                                richText.Text = richText.Text.Replace(found, GetTitle());
                            }
                        }
                    }
                }

                resetClock = false;
            }
        }
    }
}
