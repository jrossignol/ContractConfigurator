using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContractConfigurator;

namespace ContractConfigurator
{
    public interface IHasKerbalBehaviour
    {
        int KerbalCount { get; }
        ProtoCrewMember GetKerbal(int index);
    }

    public static class IHasKerbalBehaviourExtensions
    {
        public static ProtoCrewMember GetSpawnedKerbal(this ConfiguredContract contract, int index)
        {
            int current = index;
            int total = 0;
            foreach (IHasKerbalBehaviour b in contract.Behaviours.OfType<IHasKerbalBehaviour>())
            {
                total += b.KerbalCount;
                if (current < b.KerbalCount)
                {
                    return b.GetKerbal(current);
                }
                current -= b.KerbalCount;
            }

            throw new Exception("ContractConfigurator: index " + index +
                " is out of range for number of Kerbals spawned (" + total + ").");
        }

        public static int GetSpawnedKerbalCount(this ConfiguredContract contract)
        {
            int total = 0;
            foreach (IHasKerbalBehaviour b in contract.Behaviours.OfType<IHasKerbalBehaviour>())
            {
                total += b.KerbalCount;
            }

            return total;
        }
    }
}
