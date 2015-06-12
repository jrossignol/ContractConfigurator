using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator
{
    public class Kerbal
    {
        public string _name;
        public string name
        {
            get
            {
                return _pcm == null ? _name : _pcm.name;
            }
        }

        public ProtoCrewMember _pcm;
        public ProtoCrewMember pcm
        {
            get
            {
                return _pcm ?? HighLogic.CurrentGame.CrewRoster.AllKerbals().Where(pcm => pcm.name == name).FirstOrDefault();
            }
        }

        public Kerbal(string name)
        {
            this._name = name;
        }

        public Kerbal(ProtoCrewMember pcm)
        {
            this._pcm = pcm;
        }

        public override string ToString()
        {
            return _pcm != null ? _pcm.name : _name;
        }
    }
}
