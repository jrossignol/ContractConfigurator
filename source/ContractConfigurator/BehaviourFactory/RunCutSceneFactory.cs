using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator.CutScene;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// BehaviourFactory wrapper for RunCutScene ContractBehaviour.
    /// </summary>
    public class RunCutSceneFactory : BehaviourFactory
    {
        private RunCutScene.State onState;
        private List<string> parameter = new List<string>();
        private string cutSceneFileURL;
        private CutSceneDefinition cutSceneDefinition;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<TriggeredBehaviour.State>(configNode, "onState", x => onState = x, this, TriggeredBehaviour.State.PARAMETER_COMPLETED);
            if (onState == TriggeredBehaviour.State.PARAMETER_COMPLETED || onState == TriggeredBehaviour.State.PARAMETER_FAILED)
            {
                valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "parameter", x => parameter = x, this, new List<string>());
            }

            if (ConfigNodeUtil.ParseValue<string>(configNode, "cutSceneFileURL", x => cutSceneFileURL = x, this, Validation.ValidateFileURL))
            {
                cutSceneDefinition = new CutSceneDefinition();
                cutSceneDefinition.Load(cutSceneFileURL);
            }
            else
            {
                valid = false;
            }

            return valid;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new RunCutScene(onState, parameter, cutSceneDefinition);
        }
    }
}
