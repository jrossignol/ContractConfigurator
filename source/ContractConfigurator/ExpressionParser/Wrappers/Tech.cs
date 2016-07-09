using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator
{
    /// <summary>
    /// Wrapper for tech tree stuff
    /// </summary>
    public class Tech
    {
        private static Dictionary<string, Tech> allTech = null;

        public string techID;
        public string title;
        public string description;
        public float cost;
        public bool anyToUnlock;
        public int level = -1;
        public List<Tech> children = new List<Tech>();

        private Tech(string techID)
        {
            this.techID = techID;
        }

        public static Tech GetTech(string techID)
        {
            if (!SetupTech())
            {
                return null;
            }

            return allTech.ContainsKey(techID) ? allTech[techID] : null;
        }

        public bool IsUnlocked()
        {
            if (ResearchAndDevelopment.Instance == null)
            {
                return false;
            }

            ProtoTechNode ptn = ResearchAndDevelopment.Instance.GetTechState(techID);
            if (ptn == null)
            {
                return false;
            }

            return ptn.state == RDTech.State.Available;
        }

        public override string ToString()
        {
            return techID;
        }

        public override int GetHashCode()
        {
            return techID.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            Tech otherTech = obj as Tech;
            if (otherTech == null)
            {
                return false;
            }

            return techID.Equals(otherTech.techID);
        }

        private static bool SetupTech()
        {
            if (HighLogic.CurrentGame == null)
            {
                return false;
            }

            // Cache the tech tree
            if (allTech == null)
            {
                ConfigNode techTreeRoot = ConfigNode.Load(HighLogic.CurrentGame.Parameters.Career.TechTreeUrl);
                ConfigNode techTree = null;
                if (techTreeRoot != null)
                {
                    techTree = techTreeRoot.GetNode("TechTree");
                }

                if (techTreeRoot == null || techTree == null)
                {
                    LoggingUtil.LogError(typeof(Tech), "Couldn't load tech tree from " + HighLogic.CurrentGame.Parameters.Career.TechTreeUrl);
                    return false;
                }

                allTech = new Dictionary<string, Tech>();

                foreach (ConfigNode techNode in techTree.GetNodes("RDNode"))
                {
                    Tech current = new Tech(techNode.GetValue("id"));
                    current.title = ConfigNodeUtil.ParseValue<string>(techNode, "title");
                    current.description = ConfigNodeUtil.ParseValue<string>(techNode, "description");
                    current.cost = ConfigNodeUtil.ParseValue<float>(techNode, "cost");
                    current.anyToUnlock = ConfigNodeUtil.ParseValue<bool>(techNode, "anyToUnlock");

                    bool hasParent = false;
                    foreach (ConfigNode parentNode in techNode.GetNodes("Parent"))
                    {
                        string parentID = parentNode.GetValue("parentID");
                        if (allTech.ContainsKey(parentID))
                        {
                            hasParent = true;
                            allTech[parentID].children.Add(current);

                            current.level = allTech[parentID].level + 1;
                        }
                    }

                    if (!hasParent)
                    {
                        current.level = 0;
                    }

                    allTech[current.techID] = current;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets all the tech nodes.
        /// </summary>
        public static IEnumerable<Tech> AllTech()
        {
            if (!SetupTech())
            {
                yield break;
            }

            foreach (Tech tech in allTech.Values)
            {
                yield return tech;
            }
        }

        /// <summary>
        /// Gets all parent nodes for the given node.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Tech> ParentNodes()
        {
            if (!SetupTech())
            {
                yield break;
            }

            foreach (Tech tech in allTech.Values.Where(t => t.children.Contains(this)))
            {
                yield return tech;
            }
        }

        public bool IsReadyToUnlock()
        {
            if (!SetupTech() || IsUnlocked())
            {
                return false;
            }

            if (anyToUnlock)
            {
                return ParentNodes().Any(p => p.IsUnlocked());
            }
            else
            {
                return ParentNodes().All(p => p.IsUnlocked());
            }
        }
    }
}
