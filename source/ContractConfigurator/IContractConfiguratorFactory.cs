using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator
{
    public interface IContractConfiguratorFactory
    {
        string ErrorPrefix();
        string ErrorPrefix(ConfigNode configNode);
    }
}
