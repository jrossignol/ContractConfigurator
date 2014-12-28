using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;

namespace ContractConfigurator
{
    public class Validation
    {
        public static bool GT<T>(T val, T referenceValue) where T : IComparable
        {
            if (!(val.CompareTo(referenceValue) > 0))
            {
                throw new ArgumentException("Must be greater than " + referenceValue);
            }
            return true;
        }

        public static bool GE<T>(T val, T referenceValue) where T : IComparable
        {
            if (!(val.CompareTo(referenceValue) >= 0))
            {
                throw new ArgumentException("Must be greater than or equal to " + referenceValue);
            }
            return true;
        }

        public static bool EQ<T>(T val, T referenceValue) where T : IComparable
        {
            if (!(val.CompareTo(referenceValue) == 0))
            {
                throw new ArgumentException("Must be equal to " + referenceValue);
            }
            return true;
        }

        public static bool NE<T>(T val, T referenceValue) where T : IComparable
        {
            if (!(val.CompareTo(referenceValue) != 0))
            {
                throw new ArgumentException("Must not be equal to " + referenceValue);
            }
            return true;
        }

        public static bool LT<T>(T val, T referenceValue) where T : IComparable
        {
            if (!(val.CompareTo(referenceValue) < 0))
            {
                throw new ArgumentException("Must be less than " + referenceValue);
            }
            return true;
        }

        public static bool LE<T>(T val, T referenceValue) where T : IComparable
        {
            if (!(val.CompareTo(referenceValue) <= 0))
            {
                throw new ArgumentException("Must be less than or equal to " + referenceValue);
            }
            return true;
        }

        public static bool Between<T>(T val, T lower, T upper) where T : IComparable
        {
            return BetweenInclusive<T>(val, lower, upper);
        }

        public static bool BetweenInclusive<T>(T val, T lower, T upper) where T : IComparable
        {
            if (val.CompareTo(lower) < 0 || val.CompareTo(upper) > 0)
            {
                throw new ArgumentException("Must be between " + lower + " and " + upper);
            }
            return true;
        }

        public static bool BetweenExclusive<T>(T val, T lower, T upper) where T : IComparable
        {
            if (val.CompareTo(lower) <= 0 || val.CompareTo(upper) >= 0)
            {
                throw new ArgumentException("Must be exclusively between " + lower + " and " + upper);
            }
            return true;
        }

        /*
         * Parses the PartModule from the given ConfigNode and key.  Returns true if valid
         */
        public static bool ValidatePartModule(string name)
        {
            Type classType = AssemblyLoader.GetClassByName(typeof(PartModule), name);
            if (classType == null)
            {
                throw new ArgumentException("No PartModule class for '" + name + "'.");
            }
            else
            {
                // One would think there's a better way than this to get a PartModule instance,
                // but this is the best I've come up with
                GameObject go = new GameObject();
                PartModule partModule = (PartModule)go.AddComponent(classType);
                if (partModule == null)
                {
                    throw new ArgumentException("Unable to instantiate PartModule '" + name + "'.");
                }
            }

            return true;
        }

        public static bool NotNull<T>(T val)
        {
            if (val == null)
            {
                throw new ArgumentException("Cannot be null.");
            }
            return true;
        }
    }
}
