using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContractConfigurator;

namespace ContractConfigurator.CutScene
{
    public abstract class CutSceneCamera
    {
        public string name;

        public virtual void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("name", name);
        }

        public virtual void OnLoad(ConfigNode configNode)
        {
            name = ConfigNodeUtil.ParseValue<string>(configNode, "name");
        }

        public abstract void MakeActive();
    }
}
