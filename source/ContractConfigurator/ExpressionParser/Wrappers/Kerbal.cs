using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator
{
    public class Kerbal
    {
        public ProtoCrewMember pcm;

        public Kerbal(ProtoCrewMember pcm)
        {
            this.pcm = pcm;
        }

        public override string ToString()
        {
            return pcm != null ? pcm.name : "";
        }
    }
}
