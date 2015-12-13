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
    public class CopyCraftFile : TriggeredBehaviour
    {
        protected string url;
        protected EditorFacility craftType;

        public CopyCraftFile()
            : base()
        {
        }

        public CopyCraftFile(string url, EditorFacility craftType, State onState, List<string> parameter)
            : base(onState, parameter)
        {
            this.url = url;
            this.craftType = craftType;
        }

        protected override void TriggerAction()
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
            base.OnLoad(configNode);

            configNode.AddValue("url", url);
            configNode.AddValue("craftType", craftType);
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            base.OnSave(configNode);

            url = ConfigNodeUtil.ParseValue<string>(configNode, "url");
            craftType = ConfigNodeUtil.ParseValue<EditorFacility>(configNode, "craftType");
        }
    }
}
