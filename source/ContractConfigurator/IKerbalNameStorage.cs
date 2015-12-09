using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator
{
    /// <summary>
    /// Interface for objects that store Kerbal names.
    /// </summary>
    public interface IKerbalNameStorage
    {
        IEnumerable<string> KerbalNames();
    }
}
