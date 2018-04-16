using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Experience;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for ExperienceTrait.
    /// </summary>
    public class ExperienceTraitParser : ClassExpressionParser<ExperienceTrait>, IExpressionParserRegistrer
    {
        static ExperienceTraitParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(ExperienceTrait), typeof(ExperienceTraitParser));
        }

        public static void RegisterMethods()
        {
            RegisterGlobalFunction(new Function<List<ExperienceTrait>>("AllExperienceTraits", () => HighLogic.CurrentGame == null ? new List<ExperienceTrait>() :
                GameDatabase.Instance.ExperienceConfigs.Categories.Select<ExperienceTraitConfig, ExperienceTrait>(etc => 
                    ExperienceTrait.Create(KerbalRoster.GetExperienceTraitType(etc.Name) ?? typeof(ExperienceTrait), etc, null)
                ).ToList(), false));
            RegisterGlobalFunction(new Function<List<ExperienceTrait>>("AllExperienceTraitsNoTourist", () => HighLogic.CurrentGame == null ? new List<ExperienceTrait>() :
                GameDatabase.Instance.ExperienceConfigs.Categories.Where(etc => etc.Name != "Tourist").Select<ExperienceTraitConfig, ExperienceTrait>(etc =>
                    ExperienceTrait.Create(KerbalRoster.GetExperienceTraitType(etc.Name) ?? typeof(ExperienceTrait), etc, null)
                ).ToList(), false));
            RegisterGlobalFunction(new Function<ExperienceTrait, ExperienceTrait>("ExperienceTrait", k => k));
        }

        public ExperienceTraitParser()
        {
        }

        public override U ConvertType<U>(ExperienceTrait value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)(value == null ? "" : value.Title);
            }
            return base.ConvertType<U>(value);
        }

        public override bool EQ(ExperienceTrait a, ExperienceTrait b)
        {
            if (base.EQ(a, b))
            {
                return true;
            }

            return a.TypeName == b.TypeName;
        }

        public override ExperienceTrait ParseIdentifier(Token token)
        {
            // Try to parse more, as ExperienceTrait names can have spaces
            Match m = Regex.Match(expression, @"^((?>\s*[\w\d]+)+).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length ? expression.Substring(identifier.Length) : "");
            identifier = token.sval + identifier;

            if (identifier.Equals("null", StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }

            if (HighLogic.CurrentGame == null)
            {
                currentDataNode.SetDeterministic(currentKey, false);
                return null;
            }

            for (int index = 0; index < GameDatabase.Instance.ExperienceConfigs.Categories.Count; ++index)
            {
                if (identifier == GameDatabase.Instance.ExperienceConfigs.Categories[index].Name)
                {
                    Type type = KerbalRoster.GetExperienceTraitType(identifier) ?? typeof(ExperienceTrait);
                    return ExperienceTrait.Create(type, GameDatabase.Instance.ExperienceConfigs.Categories[index], null);
                }
            }

            LoggingUtil.LogError(this, StringBuilderCache.Format("Unknown experience trait '{0}'.", identifier));
            return null;
        }
    }
}
