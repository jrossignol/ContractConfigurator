using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP.Localization;

namespace ContractConfigurator
{
    /// <summary>
    /// Class for representing a biome where science can be done.
    /// </summary>
    public class Biome
    {
        private static List<string> allKerbinBiomes = null;
        private static List<string> otherKerbinBiomes = null;
        private static List<string> kscBiomes = null;

        public static IEnumerable<string> AllKerbinBiomes
        {
            get
            {
                if (HighLogic.CurrentGame == null)
                {
                    return Enumerable.Empty<string>();
                }

                return allKerbinBiomes = allKerbinBiomes ?? UnityEngine.Object.FindObjectsOfType<Collider>()
                    .Where(x => x.gameObject.layer == 15)
                    .Select(x => x.gameObject.tag)
                    .Where(x => x != "Untagged")
                    .Where(x => !x.Contains("KSC_Runway_Light"))
                    .Where(x => !x.Contains("KSC_Pad_Flag_Pole"))
                    .Where(x => !x.Contains("Ladder"))
                    .Select(x => Vessel.GetLandedAtString(x))
                    .Select(x => x.Replace(" ", ""))
                    .Distinct()
                    .ToList();
            }
        }

        public static IEnumerable<string> KSCBiomes
        {
            get
            {
                if (HighLogic.CurrentGame == null)
                {
                    return Enumerable.Empty<string>();
                }

                return kscBiomes = kscBiomes ?? AllKerbinBiomes
                    .Where(x => !x.Contains("Desert") && !x.Contains("Woomerang") && !x.Contains("Island") && !x.Contains("Baikerbanur"))
                    .ToList();
            }
        }

        public static IEnumerable<string> OtherKerbinBiomes
        {
            get
            {
                if (HighLogic.CurrentGame == null)
                {
                    return Enumerable.Empty<string>();
                }

                return otherKerbinBiomes = otherKerbinBiomes ?? AllKerbinBiomes
                    .Where(x => x.Contains("Desert") || x.Contains("Woomerang") || x.Contains("Island") || x.Contains("Baikerbanur"))
                    .ToList();
            }
        }

        public static IEnumerable<string> MainKSCBiomes
        {
            get
            {
                yield return "KSC";
                yield return "Administration";
                yield return "Crawlerway";
                yield return "LaunchPad";
                yield return "MissionControl";
                yield return "R&D";
                yield return "Runway";
                yield return "SPH";
                yield return "TrackingStation";
                yield return "VAB";

                // The AC is buggy for biomes before level 2
                int aclevel = (int)Math.Round(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex) *
                    ScenarioUpgradeableFacilities.GetFacilityLevelCount(SpaceCenterFacility.AstronautComplex)) + 1;
                if (aclevel >= 2)
                {
                    yield return "AstronautComplex";
                }
            }
        }

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
                return ScienceUtil.GetBiomedisplayName(body, biome);
            }

            return Localizer.Format("#cc.science.biomeIdentifier", (body == null ? "" : IsKSC() ? Localizer.GetStringByTag("#autoLOC_300900") : body.displayName), ScienceUtil.GetBiomedisplayName(body, biome));
        }

        public override bool Equals(object obj)
        {
            Biome b = obj as Biome;
            if (b == null)
            {
                return false;
            }

            return body == b.body && biome == b.biome;
        }

        public override int GetHashCode()
        {
            return body.GetHashCode() ^ biome.GetHashCode();
        }

        public bool IsKSC()
        {
            if (body == null || body.BiomeMap == null || !body.isHomeWorld)
            {
                return false;
            }

            if (biome.Contains("Desert") || biome.Contains("Woomerang") || biome.Contains("Island") || biome.Contains("Baikerbanur") || biome.Contains("Ice"))
            {
                return false;
            }

            return !body.BiomeMap.Attributes.Any(attr => attr.name.Replace(" ", string.Empty) == biome);
        }
    }
}
