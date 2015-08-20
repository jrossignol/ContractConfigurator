using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Behaviour for copying a craft file into a player's save.
    /// </summary>
    public class CopyCraftFile : ContractBehaviour
    {
        public enum Condition
        {
            CONTRACT_FAILED,
            CONTRACT_COMPLETED,
            PARAMETER_FAILED,
            PARAMETER_COMPLETED
        }

        protected string url;
        protected EditorFacility craftType;
        protected Condition condition;
        protected string parameter;

        public CopyCraftFile()
            : base()
        {
        }

        public CopyCraftFile(string url, EditorFacility craftType, Condition condition, string parameter)
        {
            this.url = url;
            this.craftType = craftType;
            this.condition = condition;
            this.parameter = parameter;
        }

        protected override void OnParameterStateChange(ContractParameter param)
        {
            if (param.State == ParameterState.Complete && condition == Condition.PARAMETER_COMPLETED ||
                param.State == ParameterState.Failed && condition == Condition.PARAMETER_FAILED)
            {
                DoCopy();
            }
        }

        protected override void OnCompleted()
        {
            if (condition == Condition.CONTRACT_COMPLETED)
            {
                DoCopy();
            }
        }

        protected override void OnFailed()
        {
            if (condition == Condition.CONTRACT_FAILED)
            {
                DoCopy();
            }
        }

        protected void DoCopy()
        {
            string[] srcPathComponents = new string[] { KSPUtil.ApplicationRootPath, "GameData" }.Concat(url.Split("/".ToCharArray())).ToArray();
            string[] destPathComponents = new string[] { KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, "Ships", craftType.ToString(), srcPathComponents.Last() };
            string srcPath = string.Join(Path.DirectorySeparatorChar.ToString(), srcPathComponents);
            string destPath = string.Join(Path.DirectorySeparatorChar.ToString(), destPathComponents);

            LoggingUtil.LogDebug(this, "Copying from '" + srcPath + "' to '" + destPath + "'.");
            File.Copy(srcPath, destPath);
        }

        protected override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("url", url);
            configNode.AddValue("craftType", craftType);
            configNode.AddValue("condition", condition);
            if (!string.IsNullOrEmpty(parameter))
            {
                configNode.AddValue("parameter", parameter);
            }
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            url = ConfigNodeUtil.ParseValue<string>(configNode, "url");
            craftType = ConfigNodeUtil.ParseValue<EditorFacility>(configNode, "craftType");
            condition = ConfigNodeUtil.ParseValue<Condition>(configNode, "condition");
            parameter = ConfigNodeUtil.ParseValue<string>(configNode, "parameter", "");
        }
    }
}
