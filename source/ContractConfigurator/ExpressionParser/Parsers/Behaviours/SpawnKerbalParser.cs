using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using ContractConfigurator.Behaviour;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for SpawnKerbal behaviour.
    /// </summary>
    public class SpawnKerbalParser : BehaviourParser<SpawnKerbalFactory>, IExpressionParserRegistrer
    {
        static SpawnKerbalParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(SpawnKerbalFactory), typeof(SpawnKerbalParser));
        }

        internal static void RegisterMethods()
        {
            RegisterMethod(new Method<SpawnKerbalFactory, List<Kerbal>>("Kerbals",
                skf => skf.Current != null ? skf.Current.Kerbals().Select<ProtoCrewMember, Kerbal>(pcm => new Kerbal(pcm)).ToList() : new List<Kerbal>(), false));
        }

        public SpawnKerbalParser()
        {
        }
    }
}
