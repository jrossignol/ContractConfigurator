using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator.CutScene
{
    interface CutSceneItem
    {
        string FullDescription();
        void Draw();
    }
}
