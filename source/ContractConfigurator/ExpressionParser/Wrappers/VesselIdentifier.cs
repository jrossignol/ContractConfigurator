using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator
{
    public class VesselIdentifier
    {
        public string identifier;

        public VesselIdentifier(string identifier)
        {
            this.identifier = identifier;
        }

        public override string ToString()
        {
            return identifier;
        }
    }
}
