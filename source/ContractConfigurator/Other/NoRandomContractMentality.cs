using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Contracts.Agents;

namespace ContractConfigurator.Other
{
    /// <summary>
    /// Mentality that can be used to prevent random contracts from being assigned to the agent.
    /// </summary>
    public class NoRandomContractMentality : AgentMentality
    {
        public override bool CanProcessContract(Contracts.Contract contract)
        {
            // Clean house
            foreach (Agent agent in AgentList.Instance.Agencies)
            {
                if (agent.Mentality.Contains(this) && agent.Mentality.Count > 1)
                {
                    agent.Mentality.RemoveAll(am => am != this);
                }
            }
            return false;
        }

        public override KeywordScore ScoreKeyword(string keyword)
        {
            return KeywordScore.NegativeHigh;
        }
    }
}
