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
    public class Delay : CutSceneAction
    {
        public float delayTime;

        protected float endTime;

        public override void InvokeAction()
        {
            endTime = UnityEngine.Time.fixedTime + delayTime;
        }

        public override bool ReadyForNextAction()
        {
            return UnityEngine.Time.fixedTime > endTime;
        }

        public override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("delayTime", delayTime);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            delayTime = ConfigNodeUtil.ParseValue<float>(configNode, "delayTime");
        }

    }
}
