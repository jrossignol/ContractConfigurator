using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Special parameter child class for filtering and validating a list of items.  Parent
    /// parameter classes MUST implement the ParameterDelegate.Container interface.
    /// </summary>
    /// <typeparam name="T">The type of item that will be validated.</typeparam>
    public class ParameterDelegate<T> : ContractParameter
    {
        /// <summary>
        /// Parent classes must implement this interface, and should make the following call if
        /// ChildChanged is true:
        ///     GameEvents.Contract.onParameterChange.Fire(this.Root, this);
        /// </summary>
        public interface Container
        {
            bool ChildChanged { get; set; }
        }

        protected string title;
        protected Func<T, bool> filterFunc;
        protected bool trivial;

        public ParameterDelegate()
            : this(null, null)
        {
        }

        public ParameterDelegate(string title, Func<T, bool> filterFunc, bool trivial = false)
            : base()
        {
            this.title = title;
            this.filterFunc = filterFunc;
            this.trivial = trivial;
            disableOnStateChange = false;
        }

        protected override string GetTitle()
        {
            return title;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = ConfigNodeUtil.ParseValue<string>(node, "title");
        }

        protected void SetState(ParameterState newState)
        {
            if (state != newState)
            {
                LoggingUtil.LogVerbose(this, "Setting state for '" + title + "', state = " + newState);
                state = newState;
                ((Container)Parent).ChildChanged = true;
            }
        }

        /// <summary>
        /// Apply the filter to the enumerator, and set our state based on the whether the
        /// incoming/outgoing values were empty.
        /// </summary>
        /// <param name="values">Enumerator to filter</param>
        /// <param name="fail">Whether there was an outright failure or the return value can be checked.</param>
        /// <returns>Enumerator after filtering</returns>
        protected virtual IEnumerable<T> SetState(IEnumerable<T> values, out bool fail, bool checkOnly = false)
        {
            LoggingUtil.LogVerbose(this, "Checking condition for '" + title + "', input.Any() = " + values.Any());
            fail = false;

            // Only checking, no state change allowed
            if (checkOnly)
            {
                return values.Where(filterFunc);
            }

            // Uncertain - return incomplete
            if (!values.Any())
            {
                SetState(ParameterState.Incomplete);
                return values;
            }

            // Apply the filter
            values = values.Where(filterFunc);

            // Some values - success
            if (values.Any())
            {
                SetState(ParameterState.Complete);
            }
            // No values - failure
            else
            {
                SetState(state = ParameterState.Failed);
            }

            return values;
        }

        /// <summary>
        /// To be called from the parent's OnLoad function.  Removes all child nodes, preventing
        /// stock logic from creating them.
        /// </summary>
        /// <param name="node">The config node to operate on.</param>
        public static void OnDelegateContainerLoad(ConfigNode node)
        {
            // No child parameters allowed!
            node.RemoveNodes("PARAM");
        }

        /// <summary>
        /// Checks the child conditions for each child parameter delegate in the given parent.
        /// </summary>
        /// <param name="param">The contract parameter that we are called from.</param>
        /// <param name="values">The values to enumerator over.</param>
        /// <param name="checkOnly">Only perform a check, don't change values.</param>
        /// <returns></returns>
        public static bool CheckChildConditions(ContractParameter param, IEnumerable<T> values, bool checkOnly = false)
        {
            bool fail = false;
            foreach (ContractParameter child in param.AllParameters)
            {
                if (child is ParameterDelegate<T>)
                {
                    values = ((ParameterDelegate<T>)child).SetState(values, out fail, checkOnly);
                }
            }

            return !fail && values.Any();
        }

        /// <summary>
        /// Gets the text of all the child delegates in one big string.  Useful for printing out
        /// the full details for completed parameters.
        /// </summary>
        /// <param name="param">Th parent parameters.</param>
        /// <returns>The full delegate string</returns>
        public static string GetDelegateText(ContractParameter param)
        {
            string output = "";
            foreach (ContractParameter child in param.AllParameters)
            {
                if (child is ParameterDelegate<T> && !((ParameterDelegate<T>)child).trivial)
                {
                    if (!string.IsNullOrEmpty(output))
                    {
                        output += "; ";
                    }
                    output += ((ParameterDelegate<T>)child).title;
                }
            }
            return output;
        }
    }

    /// <summary>
    /// Special ParameterDelegate class that counts the number of matches.
    /// </summary>
    /// <typeparam name="T">The type that will be enumerated over, ignored.</typeparam>
    public class CountParameterDelegate<T> : ParameterDelegate<T>
    {
        private int minCount;
        private int maxCount;

        public CountParameterDelegate(int minCount, int maxCount)
            : base("", x => true)
        {
            this.minCount = minCount;
            this.maxCount = maxCount;

            title = "Count: ";
            if (maxCount == 0)
            {
                title += "None";
            }
            else if (maxCount == int.MaxValue)
            {
                title += "At least " + minCount;
            }
            else if (minCount == 0)
            {
                title += "At most " + maxCount;
            }
            else if (minCount == maxCount)
            {
                title += "Exactly " + minCount;
            }
            else
            {
                title += "Between " + minCount + " and " + maxCount;
            }
        }

        protected override IEnumerable<T> SetState(IEnumerable<T> values, out bool fail, bool checkOnly = false)
        {
            // Set our state
            int count = values.Count();
            bool conditionMet = count >= minCount && count <= maxCount;
            if (!checkOnly)
            {
                SetState(conditionMet ? ParameterState.Complete : ParameterState.Incomplete);
            }

            fail = !conditionMet;

            return values;
        }
    }
}
