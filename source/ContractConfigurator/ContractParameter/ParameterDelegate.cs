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
    /// Parent classes must implement this interface, and should make the following call if
    /// ChildChanged is true:
    ///     ContractConfigurator.OnParameterChange.Fire(this.Root, this);
    /// </summary>
    public interface ParameterDelegateContainer
    {
        bool ChildChanged { get; set; }
    }

    public enum ParameterDelegateMatchType
    {
        FILTER,
        VALIDATE,
        VALIDATE_ALL,
        NONE
    }

    public static class MatchExtension
    {
        public static string Prefix(this ParameterDelegateMatchType type)
        {
            switch (type)
            {
                case ParameterDelegateMatchType.FILTER:
                    return "With ";
                case ParameterDelegateMatchType.VALIDATE:
                    return "With ";
                case ParameterDelegateMatchType.VALIDATE_ALL:
                    return "All have ";
                case ParameterDelegateMatchType.NONE:
                    return "None have ";
            }
            return null;
        }
    }

    /// <summary>
    /// Special parameter child class for filtering and validating a list of items.  Parent
    /// parameter classes MUST implement the ParameterDelegate.Container interface.
    /// </summary>
    /// <typeparam name="T">The type of item that will be validated.</typeparam>
    public class ParameterDelegate<T> : ContractConfiguratorParameter
    {
        protected Func<T, bool> filterFunc;
        protected ParameterDelegateMatchType matchType;
        protected bool trivial;

        public ParameterDelegate()
            : this(null, null, false)
        {
        }

        public ParameterDelegate(string title, Func<T, bool> filterFunc, ParameterDelegateMatchType matchType = ParameterDelegateMatchType.FILTER, bool trivial = false)
            : this(title, filterFunc, trivial, matchType)
        {
        }

        public ParameterDelegate(string title, Func<T, bool> filterFunc, bool trivial, ParameterDelegateMatchType matchType = ParameterDelegateMatchType.FILTER)
            : base(title)
        {
            this.id = title;
            this.filterFunc = filterFunc;
            this.matchType = matchType;
            this.trivial = trivial;
            disableOnStateChange = false;

            OnRegister();
        }

        protected override void OnParameterSave(ConfigNode node)
        {
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
        }

        protected new void SetState(ParameterState newState)
        {
            if (state != newState)
            {
                LoggingUtil.LogVerbose(this, "Setting state for '" + title + "', state = " + newState);
                state = newState;

                IContractParameterHost current = this;
                ParameterDelegateContainer container = null;
                while (container == null)
                {
                    current = current.Parent;
                    container = current as ParameterDelegateContainer;
                }
                container.ChildChanged = true;
            }
        }

        public void ClearTitle()
        {
            title = "";
        }

        public void ResetTitle()
        {
            title = id;
        }

        public void SetTitle(string newTitle)
        {
            title = newTitle;
        }

        /// <summary>
        /// Apply the filter to the enumerator, and set our state based on the whether the
        /// incoming/outgoing values were empty.
        /// </summary>
        /// <param name="values">Enumerator to filter</param>
        /// <param name="conditionMet">Current state of the condition.</param>
        /// <returns>Enumerator after filtering</returns>
        protected virtual IEnumerable<T> SetState(IEnumerable<T> values, ref bool conditionMet, bool checkOnly = false)
        {
            // Only checking, no state change allowed
            if (checkOnly)
            {
                return values.Where(filterFunc);
            }

            // Uncertain - return incomplete
            if (!values.Any())
            {
                SetState(matchType != ParameterDelegateMatchType.NONE ? ParameterState.Incomplete : ParameterState.Complete);
                return values;
            }

            // Apply the filter
            int count = values.Count();
            values = values.Where(filterFunc);

            // Some values - success
            if (matchType == ParameterDelegateMatchType.VALIDATE_ALL ? values.Count() == count : values.Any())
            {
                SetState(matchType != ParameterDelegateMatchType.NONE ? ParameterState.Complete : ParameterState.Failed);
            }
            // No values - failure
            else
            {
                SetState(matchType != ParameterDelegateMatchType.NONE ? ParameterState.Failed : ParameterState.Complete);
            }

            return values;
        }

        /// <summary>
        /// Set the state for a single value of T.
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <param name="checkOnly">Whether to actually set the state or just perform a check</param>
        /// <returns>Whether value met the criteria.</returns>
        protected virtual bool SetState(T value, bool checkOnly = false)
        {
            LoggingUtil.LogVerbose(this, "Checking condition for '" + title + "', value = " + value);

            bool result = filterFunc.Invoke(value);

            // Is state change allowed?
            if (!checkOnly)
            {
                SetState(result ? ParameterState.Complete : matchType != ParameterDelegateMatchType.FILTER ? ParameterState.Failed : ParameterState.Incomplete);
            }

            return result;
        }

        /// <summary>
        /// To be called from the parent's OnParameterLoad function.  Removes all child nodes, preventing
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
            bool conditionMet = true;
            int count = values.Count();
            foreach (ContractParameter child in param.AllParameters)
            {
                if (child is ParameterDelegate<T>)
                {
                    ParameterDelegate<T> paramDelegate = (ParameterDelegate<T>)child;
                    LoggingUtil.LogVerbose(paramDelegate, "Checking condition for '" + paramDelegate.title + "', input.Any() = " + values.Any());
                    IEnumerable<T> newValues = paramDelegate.SetState(values, ref conditionMet, checkOnly);
                    if (paramDelegate.matchType == ParameterDelegateMatchType.FILTER)
                    {
                        values = newValues;
                    }
                    switch (paramDelegate.matchType)
                    {
                        case ParameterDelegateMatchType.FILTER:
                            conditionMet &= values.Any();
                            count = values.Count();
                            break;
                        case ParameterDelegateMatchType.VALIDATE:
                            conditionMet &= newValues.Any();
                            break;
                        case ParameterDelegateMatchType.VALIDATE_ALL:
                            conditionMet &= count == newValues.Count();
                            break;
                        case ParameterDelegateMatchType.NONE:
                            conditionMet &= !newValues.Any();
                            break;
                    }
                }
            }

            return conditionMet;
        }

        /// <summary>
        /// Checks the child conditions for each child parameter delegate in the given parent.
        /// </summary>
        /// <param name="param">The contract parameter that we are called from.</param>
        /// <param name="values">The values to enumerator over.</param>
        /// <param name="checkOnly">Only perform a check, don't change values.</param>
        /// <returns></returns>
        public static bool CheckChildConditions(ContractParameter param, T value, bool checkOnly = false)
        {
            bool conditionMet = true;
            foreach (ContractParameter child in param.AllParameters)
            {
                if (child is ParameterDelegate<T>)
                {
                    conditionMet &= ((ParameterDelegate<T>)child).SetState(value);
                }
            }

            return conditionMet;
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

        public CountParameterDelegate()
            : base()
        {

        }

        public CountParameterDelegate(int minCount, int maxCount, string extraTitle = "")
            : this(minCount, maxCount, DefaultFilter, extraTitle)
        {
        }

        public CountParameterDelegate(int minCount, int maxCount, Func<T, bool> filterFunc, string extraTitle = "")
            : base("", filterFunc, (minCount == 1 && maxCount == int.MaxValue))
        {
            this.minCount = minCount;
            this.maxCount = maxCount;

            title = filterFunc == DefaultFilter ? "Count: " : "";
            if (maxCount == 0)
            {
                title += filterFunc == DefaultFilter && string.IsNullOrEmpty(extraTitle) ? "None" : "No";
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
            title += " " + extraTitle;
        }

        private static bool DefaultFilter(T t)
        {
            return true;
        }

        protected override IEnumerable<T> SetState(IEnumerable<T> values, ref bool conditionMet, bool checkOnly = false)
        {
            // Set our state
            IEnumerable<T> newValues = values.Where(filterFunc);
            int count = newValues.Count();
            bool countConditionMet = (count >= minCount && count <= maxCount);
            if (!checkOnly)
            {
                if (countConditionMet)
                {
                    SetState(ParameterState.Complete);
                }
                // Something before us failed, so we're uncertain
                else if (!conditionMet)
                {
                    SetState(ParameterState.Incomplete);
                }
                else
                {
                    SetState(ParameterState.Failed);
                    conditionMet = false;
                }
            }

            return values;
        }
    }
}
