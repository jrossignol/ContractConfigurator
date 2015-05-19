using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ContractConfigurator
{
    /// <summary>
    /// Class for representing a biome where science can be done.
    /// </summary>
    public class Biome
    {
        public CelestialBody body;
        public string biome;

        public Biome(CelestialBody body, string biome)
        {
            this.body = body;
            this.biome = biome;
        }

        public override string ToString()
        {
            if (biome == "KSC")
            {
                return biome;
            }

            return (body == null ? "" : IsKSC() ? "KSC's " : (body.theName + "'s ")) + Regex.Replace(biome, @"(\B[A-Z]+?(?=[A-Z][^A-Z])|\B[A-Z]+?(?=[^A-Z]))", " $1");
        }

        public bool IsKSC()
        {
            if (body == null || body.BiomeMap == null || !body.isHomeWorld)
            {
                return false;
            }

            return !body.BiomeMap.Attributes.Any(attr => attr.name.Replace(" ", string.Empty) == biome);
        }
    }
}
