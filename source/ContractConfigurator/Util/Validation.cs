using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using FinePrint;

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

        /// <summary>
        /// Checks that the given PartModule is valid.
        /// </summary>
        /// <param name="name">name of the PartModule</param>
        /// <returns>True if valid, exeption otherwise</returns>
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

        /// <summary>
        /// Checks that the given type of PartModule is valid.
        /// </summary>
        /// <param name="name">name of the PartModule type</param>
        /// <returns>True if valid, exeption otherwise</returns>
        public static bool ValidatePartModuleType(string name)
        {
            if (ContractDefs.GetModules(name).Count == 0)
            {
                throw new ArgumentException("No PartModules found for type '" + name + "'.");
            }

            return true;
        }

        /// <summary>
        /// Validates that the argument is not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <returns></returns>
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
