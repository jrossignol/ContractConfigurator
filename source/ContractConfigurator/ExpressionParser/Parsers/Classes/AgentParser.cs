using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Contracts.Agents;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for Agent.
    /// </summary>
    public class AgentParser : ClassExpressionParser<Agent>, IExpressionParserRegistrer
    {
        static AgentParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(Agent), typeof(AgentParser));
        }

        public static void RegisterMethods()
        {
            RegisterGlobalFunction(new Function<List<Agent>>("AllAgents", () => AgentList.Instance.Agencies, true));
            RegisterGlobalFunction(new Function<Agent, Agent>("Agent", k => k));
        }

        public AgentParser()
        {
        }

        public override U ConvertType<U>(Agent value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)(value == null ? "" : value.Title);
            }
            return base.ConvertType<U>(value);
        }

        public override Agent ParseIdentifier(Token token)
        {
            // Try to parse more, as Agent names can have spaces
            Match m = Regex.Match(expression, @"^((?>\s*[\w\d-+/*!@#$%^&*()']+)+).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length ? expression.Substring(identifier.Length) : "");
            identifier = token.sval + identifier;

            if (identifier.Equals("null", StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }

            return AgentList.Instance.GetAgent(identifier);
        }
    }
}
