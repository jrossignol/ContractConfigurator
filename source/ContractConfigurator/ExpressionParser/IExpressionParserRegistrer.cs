using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Interface for class that registers one or more expression parsers.
    /// </summary>
    public interface IExpressionParserRegistrer
    {
        void RegisterExpressionParsers();
    }
}
