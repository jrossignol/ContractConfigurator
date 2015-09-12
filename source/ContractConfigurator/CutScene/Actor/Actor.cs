using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ContractConfigurator.CutScene
{
    public abstract class Actor
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

        public abstract Transform Transform { get; }
    }
}
