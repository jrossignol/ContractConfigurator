using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator
{
    public class Resource
    {
        public PartResourceDefinition res;

        public Resource(PartResourceDefinition res)
        {
            this.res = res;
        }

        public override string ToString()
        {
            return res != null ? res.name : "";
        }
    }
}
