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
        public enum CraftType
        {
            VAB = 1,
            SPH = 2,
            SubAssembly = 3,
        }

        protected string url;
        protected CraftType craftType;

        public CopyCraftFile()
            : base()
        {
        }

        public CopyCraftFile(string url, CraftType craftType, State onState, List<string> parameter)
            : base(onState, parameter)
        {
            this.url = url;
            this.craftType = craftType;
        }

        protected override void TriggerAction()
        {
            string[] srcPathComponents = new string[] { KSPUtil.ApplicationRootPath, "GameData" }.Concat(url.Split("/".ToCharArray())).ToArray();
            List<string> destPathComponents = new string[] { KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder }.ToList();
            if (craftType == CraftType.SubAssembly)
            {
                destPathComponents.Add("Subassemblies");
            }
            else
            {
                destPathComponents.Add("Ships");
                destPathComponents.Add(craftType.ToString());
            }
            destPathComponents.Add(srcPathComponents.Last());

            string srcPath = string.Join(Path.DirectorySeparatorChar.ToString(), srcPathComponents);
            string destPath = string.Join(Path.DirectorySeparatorChar.ToString(), destPathComponents.ToArray());

            LoggingUtil.LogDebug(this, "Copying from '" + srcPath + "' to '" + destPath + "'.");
            try
            {
                File.Copy(srcPath, destPath, true);
            }
            catch (Exception e)
            {
                LoggingUtil.LogException(e);
            }
        }

        protected override void OnSave(ConfigNode configNode)
        {
            base.OnSave(configNode);

            configNode.AddValue("url", url);
            configNode.AddValue("craftType", craftType);
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);

            url = ConfigNodeUtil.ParseValue<string>(configNode, "url");
            craftType = ConfigNodeUtil.ParseValue<CraftType>(configNode, "craftType");
        }
    }
}
