using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Contracts;
using ContractConfigurator;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Base class for all Contract Configurator parameters (where possible)
    /// </summary>
    public abstract class ContractConfiguratorParameter : ContractParameter
    {
        public ContractConfiguratorParameter() { }

        protected sealed override void OnSave(ConfigNode node)
        {
            try
            {
                if (Root != null)
                {
                    node.AddValue("ContractIdentifier", Root.ToString());
                }
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
    }
}
