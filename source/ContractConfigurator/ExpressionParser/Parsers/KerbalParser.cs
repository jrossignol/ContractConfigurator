using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for ProtoCrewMember (Kerbals).
    /// </summary>
    public class KerbalParser : ClassExpressionParser<ProtoCrewMember>, IExpressionParserRegistrer
    {
        static KerbalParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(ProtoCrewMember), typeof(KerbalParser));
        }

        internal static void RegisterMethods()
        {
            RegisterMethod(new Method<ProtoCrewMember, float>("Experience", k => k == null ? 0.0f : k.experience));
            RegisterMethod(new Method<ProtoCrewMember, int>("ExperienceLevel", k => k == null ? 0 : k.experienceLevel));
            RegisterMethod(new Method<ProtoCrewMember, Experience.ExperienceTrait>("ExperienceTrait", k => k == null ? null : k.experienceTrait));
            RegisterMethod(new Method<ProtoCrewMember, ProtoCrewMember.RosterStatus>("RosterStatus", k => k == null ? ProtoCrewMember.RosterStatus.Dead : k.rosterStatus));
            RegisterMethod(new Method<ProtoCrewMember, ProtoCrewMember.KerbalType>("Type", k => k == null ? ProtoCrewMember.KerbalType.Applicant : k.type));

            RegisterGlobalFunction(new Function<List<ProtoCrewMember>>("AllKerbals", () => HighLogic.CurrentGame.CrewRoster.AllKerbals().ToList(), false));
        }

        public KerbalParser()
        {
        }

        internal override ProtoCrewMember ParseIdentifier(Token token)
        {
            // Try to parse more, as Kerbal names can have spaces
            Match m = Regex.Match(expression, @"^((?>\s*[A-Za-z][\w\d]*)+).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length ? expression.Substring(identifier.Length) : "");
            identifier = token.sval + identifier;

            // In parse mode we typically don't have a save game loaded, so
            // don't try to get a Kerbal.  Give the benefit of the doubt
            // and assume that it will be a valid Kerbal (ie. no exception)
            if (parseMode)
            {
                return null;
            }

            return HighLogic.CurrentGame.CrewRoster.AllKerbals().Where(pcm => pcm.name == identifier).FirstOrDefault();
        }
    }
}
