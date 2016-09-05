using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;
using ContractConfigurator.Parameters;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Class for defining/running an expression.
    /// </summary>
    public class Expression : ContractBehaviour
    {
        private class ExpVal
        {
            public Type type = null;
            public string key = null;
            public string val = null;

            public ExpVal(Type type, string key, string val)
            {
                this.type = type;
                this.key = key;
                this.val = val;
            }
        }

        private List<ExpVal> onOfferExpr = new List<ExpVal>();
        private List<ExpVal> onAcceptExpr = new List<ExpVal>();
        private List<ExpVal> onSuccessExpr = new List<ExpVal>();
        private List<ExpVal> onFailExpr = new List<ExpVal>();

        private Dictionary<string, List<ExpVal>> onParameterComplete = new Dictionary<string, List<ExpVal>>();

        private Dictionary<string, List<ExpVal>> map = new Dictionary<string, List<ExpVal>>();
        private DataNode dataNode;
        private ExpressionFactory factory;

        public Expression()
            : this((DataNode)null)
        {
        }

        public Expression(DataNode dataNode)
        {
            this.dataNode = dataNode;
            SetupMap();
        }

        /// <summary>
        /// Copy constructor. 
        /// </summary>
        /// <param name="source"></param>
        public Expression(Expression source)
        {
            dataNode = source.dataNode;
            onOfferExpr = new List<ExpVal>(source.onOfferExpr);
            onAcceptExpr = new List<ExpVal>(source.onAcceptExpr);
            onSuccessExpr = new List<ExpVal>(source.onSuccessExpr);
            onFailExpr = new List<ExpVal>(source.onFailExpr);
            onParameterComplete = new Dictionary<string, List<ExpVal>>(source.onParameterComplete);
            SetupMap();
        }

        private void SetupMap()
        {
            map["CONTRACT_OFFERED"] = onOfferExpr;
            map["CONTRACT_ACCEPTED"] = onAcceptExpr;
            map["CONTRACT_COMPLETED_SUCCESS"] = onSuccessExpr;
            map["CONTRACT_COMPLETED_FAILURE"] = onFailExpr;
        }

        public static Expression Parse(ConfigNode configNode, DataNode dataNode, ExpressionFactory factory)
        {
            Expression e = new Expression(dataNode);
            e.factory = factory;
            e.Load(configNode);
            e.factory = null;

            return e;
        }

        protected override void OnOffered()
        {
            ExecuteExpressions(map["CONTRACT_OFFERED"]);
            map["CONTRACT_OFFERED"].Clear();
        }

        protected override void OnAccepted()
        {
            ExecuteExpressions(map["CONTRACT_ACCEPTED"]);
            map["CONTRACT_ACCEPTED"].Clear();
        }

        protected override void OnCancelled()
        {
            ExecuteExpressions(map["CONTRACT_COMPLETED_FAILURE"]);
            map["CONTRACT_COMPLETED_FAILURE"].Clear();
        }

        protected override void OnDeadlineExpired()
        {
            ExecuteExpressions(map["CONTRACT_COMPLETED_FAILURE"]);
            map["CONTRACT_COMPLETED_FAILURE"].Clear();
        }

        protected override void OnFailed()
        {
            ExecuteExpressions(map["CONTRACT_COMPLETED_FAILURE"]);
            map["CONTRACT_COMPLETED_FAILURE"].Clear();
        }

        protected override void OnCompleted()
        {
            ExecuteExpressions(map["CONTRACT_COMPLETED_SUCCESS"]);
            map["CONTRACT_COMPLETED_SUCCESS"].Clear();
        }

        protected override void OnParameterStateChange(ContractParameter param)
        {
            if (param.State == ParameterState.Complete && onParameterComplete.ContainsKey(param.ID))
            {
                ExecuteExpressions(onParameterComplete[param.ID]);
                onParameterComplete[param.ID].Clear();
            }
        }

        private void ExecuteExpressions(IEnumerable<ExpVal> expressions)
        {
            foreach (ExpVal expVal in expressions)
            {
                BaseParser parser = BaseParser.NewParser(expVal.type);
                parser.ExecuteAndStoreExpression(expVal.key, expVal.val, dataNode);
            }
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            foreach (string node in map.Keys.Union(new string[] {"PARAMETER_COMPLETED"}))
            {
                foreach (ConfigNode child in ConfigNodeUtil.GetChildNodes(configNode, node))
                {
                    string parameter = ConfigNodeUtil.ParseValue<string>(child, "parameter", "");
                    Type type = ConfigNodeUtil.ParseValue<Type>(child, "type", typeof(double));
                    foreach (ConfigNode.Value pair in child.values)
                    {
                        if (pair.name != "parameter" && pair.name != "type" && !string.IsNullOrEmpty(pair.value))
                        {
                            ExpVal expVal = new ExpVal(type, pair.name, pair.value);
                            if (factory != null)
                            {
                                ConfigNodeUtil.ParseValue<string>(child, pair.name, x => expVal.val = x, factory, s =>
                                {
                                    // Parse the expression to validate
                                    BaseParser parser = BaseParser.NewParser(expVal.type);
                                    parser.ParseExpressionGeneric(pair.name, s, dataNode);
                                    return true;
                                });
                            }
                            else
                            {
                                // Parse the expression to validate
                                BaseParser parser = BaseParser.NewParser(expVal.type);
                                parser.ParseExpressionGeneric(pair.name, expVal.val, dataNode);
                            }

                            // Store it for later
                            if (child.name == "PARAMETER_COMPLETED")
                            {
                                if (!onParameterComplete.ContainsKey(parameter))
                                {
                                    onParameterComplete[parameter] = new List<ExpVal>();
                                }
                                onParameterComplete[parameter].Add(expVal);
                            }
                            else
                            {
                                map[node].Add(expVal);
                            }
                        }
                    }
                }
            }
        }

        protected override void OnSave(ConfigNode configNode)
        {
            foreach (string node in map.Keys)
            {
                ConfigNode child = new ConfigNode(node);
                configNode.AddNode(child);
                foreach (ExpVal expVal in map[node])
                {
                    string typeName = PersistentDataStore.GetTypeName(expVal.type);
                    if (!child.HasValue("type"))
                    {
                        child.AddValue("type", typeName);
                    }
                    // Just start a new node if the type changes
                    else if (child.GetValue("type") != typeName)
                    {
                        child = new ConfigNode(node);
                        configNode.AddNode(child);
                        child.AddValue("type", typeName);
                    }
                    child.AddValue(expVal.key, expVal.val);
                }
            }

            foreach (string parameter in onParameterComplete.Keys)
            {
                ConfigNode child = new ConfigNode("PARAMETER_COMPLETED");
                child.AddValue("parameter", parameter);
                foreach (ExpVal expVal in onParameterComplete[parameter])
                {
                    string typeName = PersistentDataStore.GetTypeName(expVal.type);
                    if (!child.HasValue("type"))
                    {
                        child.AddValue("type", typeName);
                    }
                    child.AddValue(expVal.key, expVal.val);
                }
                configNode.AddNode(child);
            }
        }
    }
}
