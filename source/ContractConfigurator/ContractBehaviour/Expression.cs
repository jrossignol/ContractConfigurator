using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Class for defining/running an expression.
    /// </summary>
    public class Expression : ContractBehaviour
    {
        private class ExpVal
        {
            public string key = null;
            public string val = null;

            public ExpVal(string key, string val)
            {
                this.key = key;
                this.val = val;
            }
        }

        private List<ExpVal> onAcceptExpr = new List<ExpVal>();
        private List<ExpVal> onSuccessExpr = new List<ExpVal>();
        private List<ExpVal> onFailExpr = new List<ExpVal>();

        private Dictionary<string, List<ExpVal>> map = new Dictionary<string, List<ExpVal>>();

        public Expression()
        {
            SetupMap();
        }

        /*
         * Copy constructor.
         */
        public Expression(Expression source)
        {
            onAcceptExpr = new List<ExpVal>(source.onAcceptExpr);
            onSuccessExpr = new List<ExpVal>(source.onSuccessExpr);
            onFailExpr = new List<ExpVal>(source.onFailExpr);
            SetupMap();
        }

        private void SetupMap()
        {
            map["CONTRACT_ACCEPTED"] = onAcceptExpr;
            map["CONTRACT_COMPLETED_SUCCESS"] = onSuccessExpr;
            map["CONTRACT_COMPLETED_FAILURE"] = onFailExpr;
        }

        public static Expression Parse(ConfigNode configNode)
        {
            Expression e = new Expression();
            e.Load(configNode);

            return e;
        }

        protected override void OnAccepted()
        {
            ExecuteExpressions("CONTRACT_ACCEPTED");
        }

        protected override void OnCancelled()
        {
            ExecuteExpressions("CONTRACT_COMPLETED_FAILURE");
        }

        protected override void OnDeadlineExpired()
        {
            ExecuteExpressions("CONTRACT_COMPLETED_FAILURE");
        }

        protected override void OnFailed()
        {
            ExecuteExpressions("CONTRACT_COMPLETED_FAILURE");
        }

        protected override void OnCompleted()
        {
            ExecuteExpressions("CONTRACT_COMPLETED_SUCCESS");
        }

        private void ExecuteExpressions(string node)
        {
            foreach (ExpVal expVal in map[node])
            {
                ExpressionParser.ExecuteExpression(expVal.key, expVal.val);
            }
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            foreach (string node in map.Keys)
            {
                foreach (ConfigNode child in configNode.GetNodes(node))
                {
                    foreach (ConfigNode.Value pair in child.values)
                    {
                        // Parse the expression to validate
                        ExpressionParser.ParseExpression(pair.value);

                        // Store it for later
                        map[node].Add(new ExpVal(pair.name, pair.value));
                    }
                }
            }
        }

        protected override void OnSave(ConfigNode configNode)
        {
            foreach (string node in map.Keys)
            {
                ConfigNode child = new ConfigNode(node);
                foreach (ExpVal expVal in map[node])
                {
                    child.AddValue(expVal.key, expVal.val);
                }
                configNode.AddNode(child);
            }
        }
    }
}
