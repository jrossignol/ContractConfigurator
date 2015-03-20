using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator
{
    public class Duration
    {
        public double Value;

        public Duration(double value)
        {
            Value = value;
        }

        public Duration(string durationStr)
        {
            Value = DurationUtil.ParseDuration(durationStr);
        }

        public override string ToString()
        {
            return "Duration[" + DurationUtil.StringValue(Value) + "]";
        }
    }
}
