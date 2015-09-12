using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ContractConfigurator.CutScene
{
    /// <summary>
    /// Delay before moving on to the next cut scene action.
    /// </summary>
    public class SetCamera : CutSceneAction
    {
        public string cameraName;

        public override void InvokeAction()
        {
            cutSceneDefinition.camera(cameraName).MakeActive();
        }

        public override bool ReadyForNextAction()
        {
            return true;
        }

        public override void OnSave(ConfigNode configNode)
        {
            base.OnSave(configNode);
            configNode.AddValue("cameraName", cameraName);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);
            cameraName = ConfigNodeUtil.ParseValue<string>(configNode, "cameraName");
        }

    }
}
