using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for Part (AvailablePart).
    /// </summary>
    public class PartParser : ClassExpressionParser<AvailablePart>, IExpressionParserRegistrer
    {
        static PartParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(AvailablePart), typeof(PartParser));
        }

        public static void RegisterMethods()
        {
            RegisterMethod(new Method<AvailablePart, PartCategories>("Category", p => p == null ? 0 : p.category));
            RegisterMethod(new Method<AvailablePart, float>("Cost", p => p == null ? 0.0f : p.cost));
            RegisterMethod(new Method<AvailablePart, string>("Description", p => p == null ? "" : p.description));
            RegisterMethod(new Method<AvailablePart, string>("Manufacturer", p => p == null ? "" : p.manufacturer));
            RegisterMethod(new Method<AvailablePart, float>("Size", p => p == null ? 0.0f : p.partSize));
            RegisterMethod(new Method<AvailablePart, Tech>("TechRequired", p => p == null ? null : Tech.GetTech(p.TechRequired)));
            RegisterMethod(new Method<AvailablePart, bool>("IsUnlocked", p => p == null ? false : ResearchAndDevelopment.PartModelPurchased(p), false));
            RegisterMethod(new Method<AvailablePart, int>("CrewCapacity", p => p == null ? 0 : p.partPrefab.CrewCapacity));

            RegisterMethod(new Method<AvailablePart, float>("EngineAtmosphereThrust", GetEngineAtmoThrust));
            RegisterMethod(new Method<AvailablePart, float>("EngineVacuumThrust", GetEngineVacThrust));
            RegisterMethod(new Method<AvailablePart, float>("EngineAtmosphereISP", GetEngineAtmoISP));
            RegisterMethod(new Method<AvailablePart, float>("EngineVacuumISP", GetEngineVacISP));

            RegisterGlobalFunction(new Function<List<AvailablePart>>("AllParts", () => PartLoader.Instance.parts.ToList()));
            RegisterGlobalFunction(new Function<AvailablePart, AvailablePart>("AvailablePart", p => p));
        }

        public PartParser()
        {
        }

        public override U ConvertType<U>(AvailablePart value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)(value == null ? "" : value.name);
            }
            return base.ConvertType<U>(value);
        }

        public override Token ParseNumericConstant()
        {
            // Parse as an identifier
            Token t = new Token(TokenType.IDENTIFIER);
            t.sval = "";
            return t;
        }

        private static float GetEngineVacThrust(AvailablePart p)
        {
            if (p == null)
            {
                return 0.0f;
            }

            foreach (PartModule pm in p.partPrefab.Modules)
            {
                if (pm.moduleName != null && pm.moduleName.StartsWith("ModuleEngines"))
                {
                    ModuleEngines enginePM = pm as ModuleEngines;
                    return enginePM.maxThrust;
                }
            }

            return 0.0f;
        }

        private static float GetEngineAtmoThrust(AvailablePart p)
        {
            if (p == null)
            {
                return 0.0f;
            }

            foreach (PartModule pm in p.partPrefab.Modules)
            {
                if (pm.moduleName != null && pm.moduleName.StartsWith("ModuleEngines"))
                {
                    ModuleEngines enginePM = pm as ModuleEngines;
                    return enginePM.maxThrust * enginePM.atmosphereCurve.Evaluate(1) / enginePM.atmosphereCurve.Evaluate(0);
                }
            }

            return 0.0f;
        }

        private static float GetEngineVacISP(AvailablePart p)
        {
            if (p == null)
            {
                return 0.0f;
            }

            foreach (PartModule pm in p.partPrefab.Modules)
            {
                if (pm.moduleName != null && pm.moduleName.StartsWith("ModuleEngines"))
                {
                    ModuleEngines enginePM = pm as ModuleEngines;
                    return enginePM.atmosphereCurve.Evaluate(0);
                }
            }

            return 0.0f;
        }

        private static float GetEngineAtmoISP(AvailablePart p)
        {
            if (p == null)
            {
                return 0.0f;
            }

            foreach (PartModule pm in p.partPrefab.Modules)
            {
                if (pm.moduleName != null && pm.moduleName.StartsWith("ModuleEngines"))
                {
                    ModuleEngines enginePM = pm as ModuleEngines;
                    return enginePM.atmosphereCurve.Evaluate(1);
                }
            }

            return 0.0f;
        }

        public override AvailablePart ParseIdentifier(Token token)
        {
            // Try to parse more, as part names can have spaces and other weird characters
            Match m = Regex.Match(expression, @"^((?>\s*[\w\d-\.]+)+).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length ? expression.Substring(identifier.Length) : "");
            identifier = token.sval + identifier;

            // Underscores in part names get replaced with spaces.  Nobody knows why.
            string partName = identifier.Replace('_', '.');

            if (identifier.Equals("null", StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }

            // Get the part
            AvailablePart part = PartLoader.getPartInfoByName(partName);
            if (part == null)
            {
                throw new ArgumentException("'" + identifier + "' is not a valid Part.");
            }

            currentDataNode.SetDeterministic(currentKey, false);

            return part;
        }
    }
}
