using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator
{
    public interface IContractConfiguratorFactory
    {
        string ErrorPrefix();
        string ErrorPrefix(ConfigNode configNode);

        Type iteratorType { get; set; }
        string iteratorKey { get; set; }
        string config { get; }
        string log { get; }
        DataNode dataNode { get; }
        bool hasWarnings { get; set; }
        Version minVersion { get; }
    }
}
