using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator
{
    public static class ExceptionUtil
    {
        /// <summary>
        /// Unwraps the TargetInvocationException that reflection methods helpfully give us.
        /// </summary>
        /// <param name="tie">Exception to unwrap</param>
        /// <returns>A copy of the inner exception, for a few cases.</returns>
        public static Exception UnwrapTargetInvokationException(TargetInvocationException tie)
        {
            if (tie.InnerException.GetType() == typeof(DataStoreCastException))
            {
                DataStoreCastException orig = (DataStoreCastException)tie.InnerException;
                return new DataStoreCastException(orig.FromType, orig.ToType, tie);
            }
            else if (tie.InnerException.GetType() == typeof(DataNode.ValueNotInitialized))
            {
                DataNode.ValueNotInitialized orig = (DataNode.ValueNotInitialized)tie.InnerException;
                throw new DataNode.ValueNotInitialized(orig.key, tie);
            }
            else if (tie.InnerException.GetType() == typeof(NotSupportedException))
            {
                NotSupportedException orig = (NotSupportedException)tie.InnerException;
                throw new NotSupportedException(orig.Message, tie);
            }
            return null;
        }
    }
}
