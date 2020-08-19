using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;
using ContractConfigurator.CutScene;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Behaviour for running a CutScene.
    /// </summary>
    public class RunCutScene : TriggeredBehaviour
    {
        private CutSceneDefinition cutSceneDefinition;
        private CutSceneExecutor cutSceneExecutor;

        public RunCutScene()
        {
        }

        protected override void OnUnregister()
        {
            if (cutSceneExecutor != null && cutSceneExecutor.CanDestroy)
            {
                UnityEngine.Object.Destroy(cutSceneExecutor.gameObject);
            }
        }

        public RunCutScene(State onState, List<string> parameter, CutSceneDefinition cutSceneDefinition)
            : base(onState, parameter)
        {
            this.cutSceneDefinition = cutSceneDefinition;
        }

        protected override void TriggerAction()
        {
            LoggingUtil.LogVerbose(this, "Running cut scene '{0}'", cutSceneDefinition.name);

            GameObject cutScene = new GameObject("CutScene");
            cutSceneExecutor = cutScene.AddComponent<CutSceneExecutor>();
            cutSceneExecutor.cutSceneDefinition = cutSceneDefinition;
            cutSceneExecutor.ExecuteCutScene();
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);

            cutSceneDefinition = new CutSceneDefinition();
            cutSceneDefinition.OnLoad(configNode.GetNode("CUTSCENE_DEFINITION"));
        }

        protected override void OnSave(ConfigNode configNode)
        {
            base.OnSave(configNode);

            ConfigNode child = new ConfigNode("CUTSCENE_DEFINITION");
            cutSceneDefinition.OnSave(child);
            configNode.AddNode(child);
        }
    }
}
