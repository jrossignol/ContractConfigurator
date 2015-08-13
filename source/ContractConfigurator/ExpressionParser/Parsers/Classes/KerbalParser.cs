using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for Kerbal (ProtoCrewMember).
    /// </summary>
    public class KerbalParser : ClassExpressionParser<Kerbal>, IExpressionParserRegistrer
    {
        static KerbalParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(Kerbal), typeof(KerbalParser));
        }

        internal static void RegisterMethods()
        {
            RegisterMethod(new Method<Kerbal, float>("Experience", k => k == null ? 0.0f : k.pcm.experience));
            RegisterMethod(new Method<Kerbal, int>("ExperienceLevel", k => k == null ? 0 : k.pcm.experienceLevel));
            RegisterMethod(new Method<Kerbal, string>("ExperienceTrait", k => k == null ? null : k.pcm.experienceTrait.Title));
            RegisterMethod(new Method<Kerbal, ProtoCrewMember.RosterStatus>("RosterStatus", k => k == null ? ProtoCrewMember.RosterStatus.Dead : k.pcm.rosterStatus));
            RegisterMethod(new Method<Kerbal, ProtoCrewMember.KerbalType>("Type", k => k == null ? ProtoCrewMember.KerbalType.Applicant : k.pcm.type));
            RegisterMethod(new Method<Kerbal, ProtoCrewMember.Gender>("Gender", k => k == null ? ProtoCrewMember.Gender.Male : k.pcm.gender));

            RegisterGlobalFunction(new Function<List<Kerbal>>("AllKerbals", () => HighLogic.CurrentGame == null ? new List<Kerbal>() :
                HighLogic.CurrentGame.CrewRoster.AllKerbals().Select<ProtoCrewMember, Kerbal>(pcm => new Kerbal(pcm)).ToList(), false));
            RegisterGlobalFunction(new Function<Kerbal, Kerbal>("Kerbal", k => k));
        }

        public KerbalParser()
        {
        }

        internal override U ConvertType<U>(Kerbal value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)(value == null ? "" : value.ToString());
            }
            return base.ConvertType<U>(value);
        }

        internal override Kerbal ParseIdentifier(Token token)
        {
            // Try to parse more, as Kerbal names can have spaces
            Match m = Regex.Match(expression, @"^((?>\s*[\w\d]+)+).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length ? expression.Substring(identifier.Length) : "");
            identifier = token.sval + identifier;

            if (identifier.Equals("null", StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }

            return new Kerbal(identifier);
        }
    }
}
