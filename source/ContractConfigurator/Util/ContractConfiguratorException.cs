using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    public class ContractConfiguratorException : Exception
    {
        public ContractConfiguratorException(ConfiguredContract c, Exception innerException)
            : base(StringBuilderCache.Format("Error in contract '{0}'", c != null && c.contractType != null ? c.contractType.name : "<unknown contract>"), innerException)
        {
        }

        public ContractConfiguratorException(ContractConfiguratorParameter p, Exception innerException)
            : this(p.Root as ConfiguredContract, innerException)
        {
        }
    }
}
