using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using ContractConfigurator.Parameters;

namespace ContractConfigurator.ExpressionParser
{
    public class BooleanValueExpressionParser : ValueExpressionParser<bool>
    {
        public BooleanValueExpressionParser()
            : base()
        {
        }

        protected override bool EQ(bool a, bool b)
        {
            return a == b;
        }

        protected override bool NE(bool a, bool b)
        {
            return a != b;
        }

        protected override bool Not(bool val)
        {
            return !val;
        }

        protected override bool Or(bool a, bool b)
        {
            return a || b;
        }

        protected override bool And(bool a, bool b)
        {
            return a && b;
        }
    }
}
