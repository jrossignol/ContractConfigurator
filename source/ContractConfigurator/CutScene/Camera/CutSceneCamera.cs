using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ContractConfigurator;

namespace ContractConfigurator.CutScene
{
    public abstract class CutSceneCamera : CutSceneItem
    {
        public string name;

        public abstract string Name();
        public string FullDescription()
        {
            return Name() + " (" + name + ")";
        }

        public abstract void OnDraw();

        public virtual void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("name", name);
        }

        public virtual void OnLoad(ConfigNode configNode)
        {
            name = ConfigNodeUtil.ParseValue<string>(configNode, "name");
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", GUILayout.Width(CutSceneConfigurator.CutSceneConfigurator.DETAIL_LABEL_WIDTH));
            name = GUILayout.TextField(name, GUILayout.Width(CutSceneConfigurator.CutSceneConfigurator.DETAIL_ENTRY_WIDTH));
            GUILayout.EndHorizontal();

            OnDraw();
        }

        public abstract void MakeActive();
    }
}
