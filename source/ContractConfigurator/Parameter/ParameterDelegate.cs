using System;
using System.Collections;
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
        protected BitArray src = new BitArray(32);
        protected BitArray dest = new BitArray(32);

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
            if (title != newTitle)
            {
                title = newTitle;

                // Force a call to GetTitle to update the contracts app
                GetTitle();
            }
        }

        /// <summary>
        /// Apply the filter to the enumerator, and set our state based on the whether the
        /// incoming/outgoing values were empty.
        /// </summary>
        /// <param name="values">Enumerator to filter</param>
        /// <param name="conditionMet">Current state of the condition.</param>
        /// <returns>Enumerator after filtering</returns>
        protected virtual void SetState(IEnumerable<T> values, ref bool conditionMet, bool checkOnly = false)
        {
            // Only checking, no state change allowed
            if (checkOnly)
            {
                ApplyFilterToDest(values);
                return;
            }

            // Get the source count
            int srcCount = GetCount(values, src);

            // Apply the filter
            ApplyFilterToDest(values);

            // Uncertain - return incomplete
            if (srcCount == 0)
            {
                SetState(matchType != ParameterDelegateMatchType.NONE ? ParameterState.Incomplete : ParameterState.Complete);
                return;
            }

            int destCount = GetCount(values, dest);

            // Some values - success
            if (matchType == ParameterDelegateMatchType.VALIDATE_ALL ? destCount == srcCount : destCount > 0)
            {
                SetState(matchType != ParameterDelegateMatchType.NONE ? ParameterState.Complete : ParameterState.Failed);
            }
            // No values - failure
            else
            {
                SetState(matchType != ParameterDelegateMatchType.NONE ? ParameterState.Failed : ParameterState.Complete);
            }
        }

        /// <summary>
        /// Set the state for a single value of T.
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <param name="checkOnly">Whether to actually set the state or just perform a check</param>
        /// <returns>Whether value met the criteria.</returns>
        protected virtual bool SetState(T value, bool checkOnly = false)
        {
            bool result = filterFunc.Invoke(value);

            // Is state change allowed?
            if (!checkOnly)
            {
                SetState(result ? ParameterState.Complete : matchType != ParameterDelegateMatchType.FILTER ? ParameterState.Failed : ParameterState.Incomplete);
            }

            return result;
        }

        /// <summary>
        /// Initializes the bit arrays based on the given list
        /// </summary>
        protected void InitializeBitArrays(IEnumerable<T> values, BitArray current)
        {
            // Grow to accomodate
            int length = src.Length;
            while (length < values.Count())
            {
                length *= 2;
            }
            if (length != src.Length)
            {
                src.Length = length;
                dest.Length = length;
            }

            dest.SetAll(true);
            src.SetAll(true);
            if (current != null)
            {
                src.And(current);
            }
        }

        /// <summary>
        /// Applies the filter conditions to the destination array
        /// </summary>
        protected void ApplyFilterToDest(IEnumerable<T> values)
        {
            int i = 0;
            foreach (T value in values)
            {
                if (src.Get(i))
                {
                    bool result = filterFunc.Invoke(value);
                    dest.Set(i++, result);
                }
                else
                {
                    dest.Set(i++, false);
                }
            }
        }

        /// <summary>
        /// Gets the count based on the values and given BitArray.
        /// </summary>
        protected static int GetCount(IEnumerable<T> values, BitArray current)
        {
            int count = 0;
            for (int i = values.Count(); i-- > 0;)
            {
                if (current.Get(i))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// To be called from the parent's OnParameterLoad function.  Removes all child nodes, preventing
        /// stock logic from creating them.
        /// </summary>
        /// <param name="node">The config node to operate on.</param>
        public static void OnDelegateContainerLoad(ConfigNode node)
        {
            // No delegate child parameters allowed!
            foreach (ConfigNode child in node.GetNodes("PARAM"))
            {
                if (child.GetValue("name").EndsWith("`1"))
                {
                    node.RemoveNode(child);
                }
            }
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
            CheckChildConditions(param, values, ref conditionMet, checkOnly);
            return conditionMet;
        }

        /// <summary>
        /// Checks the child conditions for each child parameter delegate in the given parent.
        /// </summary>
        /// <param name="param">The contract parameter that we are called from.</param>
        /// <param name="values">The values to enumerator over.</param>
        /// <param name="checkOnly">Only perform a check, don't change values.</param>
        /// <returns></returns>
        protected static BitArray CheckChildConditions(ContractParameter param, IEnumerable<T> values, ref bool conditionMet, bool checkOnly = false)
        {
            int count = values.Count();
            BitArray current = null;

            int paramCount = param.ParameterCount;
            for (int i = 0; i < paramCount; i++)
            {
                ParameterDelegate<T> paramDelegate = param[i] as ParameterDelegate<T>;
                if (paramDelegate != null)
                {
                    LoggingUtil.LogVerbose(paramDelegate, "Checking condition for '" + paramDelegate.title + "', conditionMet = " + conditionMet);

                    paramDelegate.InitializeBitArrays(values, current);
                    paramDelegate.SetState(values, ref conditionMet, checkOnly);

                    LoggingUtil.LogVerbose(paramDelegate, "  after, conditionMet = " + conditionMet);

                    if (paramDelegate.matchType == ParameterDelegateMatchType.FILTER)
                    {
                        current = paramDelegate.dest;
                    }
                    int newCount = GetCount(values, paramDelegate.dest);
                    switch (paramDelegate.matchType)
                    {
                        case ParameterDelegateMatchType.FILTER:
                            count = newCount;
                            break;
                        case ParameterDelegateMatchType.VALIDATE:
                            conditionMet &= newCount > 0;
                            break;
                        case ParameterDelegateMatchType.VALIDATE_ALL:
                            conditionMet &= count == newCount;
                            break;
                        case ParameterDelegateMatchType.NONE:
                            conditionMet &= newCount == 0;
                            break;
                    }
                }
            }

            return current;
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
            int count = param.ParameterCount;
            for (int i = 0; i < param.ParameterCount; i++)
            {
                ParameterDelegate<T> delegateParam = param[i] as ParameterDelegate<T>;
                if (delegateParam != null)
                {
                    conditionMet &= delegateParam.SetState(value);
                }
            }

            return conditionMet;
        }

        /// <summary>
        /// Gets the text of all the child delegates in one big string.  Useful for printing out
        /// the full details for completed parameters.
        /// </summary>
        /// <param name="param">The parent parameters.</param>
        /// <returns>The full delegate string</returns>
        public static string GetDelegateText(ContractParameter param)
        {
            string output = "";
            foreach (ContractParameter child in param.GetChildren())
            {
                if (child is ParameterDelegate<T> && !((ParameterDelegate<T>)child).trivial)
                {
                    if (!string.IsNullOrEmpty(output))
                    {
                        output += "; ";
                    }
                    output += ((ParameterDelegate<T>)child).title;

                    if (child is AllParameterDelegate<T>)
                    {
                        output += ": " + GetDelegateText(child);
                    }
                }
            }
            return output;
        }
    }
    
    /// <summary>
    /// Special ParameterDelegate class that looks for child parameters.
    /// </summary>
    /// <typeparam name="T">The type that will be enumerated over, ignored.</typeparam>
    public class AllParameterDelegate<T> : ParameterDelegate<T>
    {
        public AllParameterDelegate()
            : base(null, null, false)
        {
        }

        public AllParameterDelegate(string title, ParameterDelegateMatchType matchType = ParameterDelegateMatchType.FILTER)
            : base(title, null, matchType)
        {
            filterFunc = (t => this.AllChildParametersComplete());
        }

        protected override string GetParameterTitle()
        {
            string title = base.GetParameterTitle();

            if (state != ParameterState.Incomplete)
            {
                title += ": " + ParameterDelegate<T>.GetDelegateText(this);
            }

            return title;
        }

        protected override void SetState(IEnumerable<T> values, ref bool conditionMet, bool checkOnly = false)
        {
            BitArray current = ParameterDelegate<T>.CheckChildConditions(this, values, ref conditionMet, checkOnly);
            if (current != null)
            {
                src.And(current);
            }
            base.SetState(values, ref conditionMet, checkOnly);
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
        public bool ignorePreviousFailures = true;

        public CountParameterDelegate()
            : base()
        {

        }

        public CountParameterDelegate(int minCount, int maxCount, string extraTitle = "", bool ignorePreviousFailures=true)
            : this(minCount, maxCount, DefaultFilter, extraTitle, ignorePreviousFailures)
        {
        }

        public CountParameterDelegate(int minCount, int maxCount, Func<T, bool> filterFunc, string extraTitle = "", bool ignorePreviousFailures = true)
            : base("", filterFunc, (minCount == 1 && maxCount == int.MaxValue))
        {
            this.minCount = minCount;
            this.maxCount = maxCount;
            this.ignorePreviousFailures = ignorePreviousFailures;

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

        protected override void SetState(IEnumerable<T> values, ref bool conditionMet, bool checkOnly = false)
        {
            // Set our state
            ApplyFilterToDest(values);
            int count = GetCount(values, dest);
            LoggingUtil.LogVerbose(this, "Count = " + count);
            bool countConditionMet = (count >= minCount && count <= maxCount);
            if (!checkOnly)
            {
                if (countConditionMet)
                {
                    SetState(ParameterState.Complete);
                }
                // Something before us failed, so we're uncertain
                else if (!conditionMet && ignorePreviousFailures)
                {
                    SetState(ParameterState.Incomplete);
                }
                else
                {
                    SetState(ParameterState.Failed);
                    conditionMet = false;
                }
            }
        }
    }
}
