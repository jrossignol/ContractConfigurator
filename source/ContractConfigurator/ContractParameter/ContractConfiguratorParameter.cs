using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Contracts;
using ContractConfigurator;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Base class for all Contract Configurator parameters (where possible)
    /// </summary>
    public abstract class ContractConfiguratorParameter : ContractParameter
    {
        protected string title;
        public bool completeInSequence;

        public ContractConfiguratorParameter()
            : this(null)
        {
        }

        public ContractConfiguratorParameter(string title)
        {
            this.title = title;
        }

        protected override string GetTitle()
        {
            return title;
        }

        protected sealed override void OnSave(ConfigNode node)
        {
            try
            {
                if (Root != null)
                {
                    node.AddValue("ContractIdentifier", Root.ToString());
                }
                node.AddValue("title", title ?? "");
                OnParameterSave(node);
            }
            catch (Exception e)
            {
                LoggingUtil.LogException(e);
                ExceptionLogWindow.DisplayFatalException(ExceptionLogWindow.ExceptionSituation.PARAMETER_SAVE, e, Root.ToString(), ID);
            }
        }

        /// <summary>
        /// Use this instead of OnSave.
        /// </summary>
        /// <param name="configNode">The ConfigNode to save to.</param>
        protected abstract void OnParameterSave(ConfigNode node);

        protected sealed override void OnLoad(ConfigNode node)
        {
            try
            {
                title = ConfigNodeUtil.ParseValue<string>(node, "title", "");
                OnParameterLoad(node);
            }
            catch (Exception e)
            {
                string contractName = "unknown";
                try
                {
                    contractName = ConfigNodeUtil.ParseValue<string>(node, "ContractIdentifier");
                }
                catch { }
                LoggingUtil.LogException(e);
                ExceptionLogWindow.DisplayFatalException(ExceptionLogWindow.ExceptionSituation.PARAMETER_LOAD, e, contractName, ID);
            }
        }

        /// <summary>
        /// Use this instead of OnLoad.
        /// </summary>
        /// <param name="configNode">The ConfigNode to laod from.</param>
        protected abstract void OnParameterLoad(ConfigNode node);

        /// <summary>
        /// Checks if this parameter is ready to complete.  If the completeInSequence flag is set,
        /// it will check if all previous parameters in the sequence have been completed.
        /// </summary>
        /// <returns>True if the parameter is ready to complete.</returns>
        private bool ReadyToComplete()
        {
            if (!completeInSequence)
            {
                return true;
            }

            // Go through the parent's parameters
            for (int i = 0; i < Parent.ParameterCount; i++)
            {
                ContractParameter param = Parent.GetParameter(i);
                // If we've made it all the way to us, we're ready
                if (System.Object.ReferenceEquals(param, this))
                {
                    return true;
                }
                else if (param.State != ParameterState.Complete)
                {
                    return false;
                }
            }

            // Shouldn't get here unless things are really messed up
            LoggingUtil.LogWarning(this.GetType(), "Unexpected state for SequenceNode parameter.  Log a GitHub issue!");
            return false;
        }

        /// <summary>
        /// Method to use in place of SetComplete/SetFailed/SetIncomplete.  Doesn't fire the stock change event because of 
        /// performance issues with the stock contracts app.
        /// </summary>
        /// <param name="state">New parameter state</param>
        protected virtual void SetState(ParameterState state)
        {
            // State already set
            if (this.state == state)
            {
                return;
            }

            // Check if the transition is allowed
            if (state == ParameterState.Complete && !ReadyToComplete())
            {
                return;
            }

            this.state = state;

            if (disableOnStateChange)
            {
                Disable();
            }

            if (state == ParameterState.Complete)
            {
                AwardCompletion();
            }
            else if (state == ParameterState.Failed)
            {
                PenalizeFailure();
            }

            OnStateChange.Fire(this, state);
            ContractConfigurator.OnParameterChange.Fire(Root, this);
            Parent.ParameterStateUpdate(this);
        }

        [Obsolete("Use SetState() instead.")]
        new protected void SetComplete()
        {
            base.SetComplete();
        }

        [Obsolete("Use SetState() instead.")]
        new protected void SetFailed()
        {
            base.SetFailed();
        }

        [Obsolete("Use SetState() instead.")]
        new protected void SetIncomplete()
        {
            base.SetIncomplete();
        }
    }
}
