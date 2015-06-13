using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// Static class with extensions to stock classes.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets all the parameter's descendents.
        /// </summary>
        /// <param name="p">Contract parameter</param>
        /// <returns>Enumerator of descendents</returns>
        public static IEnumerable<ContractParameter> GetAllDescendents(this IContractParameterHost p)
        {
            for (int i = 0; i < p.ParameterCount; i++)
            {
                ContractParameter child = p.GetParameter(i);
                yield return child;
                foreach (ContractParameter descendent in child.GetAllDescendents())
                {
                    yield return descendent;
                }
            }
        }

        /// <summary>
        /// Gets all the parameter's children.
        /// </summary>
        /// <param name="p">Contract parameter</param>
        /// <returns>Enumerator of descendents</returns>
        public static IEnumerable<ContractParameter> GetChildren(this IContractParameterHost p)
        {
            for (int i = 0; i < p.ParameterCount; i++)
            {
                yield return p.GetParameter(i);
            }
        }

        /// <summary>
        /// Gets all the kerbals for the given roster.
        /// </summary>
        /// <param name="p">Contract parameter</param>
        /// <returns>Enumerator of descendents</returns>
        public static IEnumerable<ProtoCrewMember> AllKerbals(this KerbalRoster roster)
        {
            for (int i = 0; i < roster.Count; i++)
            {
                yield return roster[i];
            }
        }

        /// <summary>
        /// Gets the quantity of the given resource for the vessel.
        /// </summary>
        /// <param name="vessel">Vessel to check</param>
        /// <param name="resource">Resource to check for</param>
        /// <returns></returns>
        public static double ResourceQuantity(this Vessel vessel, PartResourceDefinition resource)
        {
            if (vessel == null)
            {
                return 0.0;
            }

            double quantity = 0.0;
            foreach (Part part in vessel.Parts)
            {
                PartResource pr = part.Resources[resource.name];
                if (pr != null)
                {
                    quantity += pr.amount;
                }
            }
            return quantity;
        }

        /// <summary>
        /// Gets the quantity of the given resource for the vessel.
        /// </summary>
        /// <param name="vessel">Vessel to check</param>
        /// <param name="resource">Resource to check for</param>
        /// <returns></returns>
        public static double ResourceCapacity(this Vessel vessel, PartResourceDefinition resource)
        {
            if (vessel == null)
            {
                return 0.0;
            }

            double quantity = 0.0;
            foreach (Part part in vessel.Parts)
            {
                PartResource pr = part.Resources[resource.name];
                if (pr != null)
                {
                    quantity += pr.maxAmount;
                }
            }
            return quantity;
        }

        /// <summary>
        /// Create a hash of the vessel.
        /// </summary>
        /// <param name="vessel">The vessel to hash</param>
        /// <returns>A list of hashes for this vessel</returns>
        public static IEnumerable<uint> GetHashes(this Vessel vessel)
        {
            if (vessel.protoVessel == null || vessel.protoVessel.protoPartSnapshots == null)
            {
                yield break;
            }

            Queue<ProtoPartSnapshot> queue = new Queue<ProtoPartSnapshot>();
            Dictionary<ProtoPartSnapshot, int> visited = new Dictionary<ProtoPartSnapshot, int>();
            Dictionary<uint, uint> dockedParts = new Dictionary<uint, uint>();
            Queue<ProtoPartSnapshot> otherVessel = new Queue<ProtoPartSnapshot>();

            IEnumerable<ProtoPartSnapshot> parts = vessel.protoVessel.protoPartSnapshots;

            // Add the root
            queue.Enqueue(vessel.protoVessel.protoPartSnapshots.First());
            visited[queue.First()] = 1;

            // Do a BFS of all parts.
            uint hash = 0;
            int count = 0;
            while (queue.Count > 0 || otherVessel.Count > 0)
            {
                bool decoupler = false;

                // Start a new ship
                if (queue.Count == 0)
                {
                    // Reset our hash
                    if (count != 0)
                    {
                        yield return hash;
                    }
                    count = 0;
                    hash = 0;

                    // Find an unhandled part to use as the new vessel
                    ProtoPartSnapshot px;
                    while ((px = otherVessel.Dequeue()) != null)
                    {
                        if (visited[px] != 2)
                        {
                            queue.Enqueue(px);
                            break;
                        }
                    }
                    dockedParts.Clear();
                    continue;
                }

                ProtoPartSnapshot p = queue.Dequeue();

                // Check if this is for a new vessel
                if (dockedParts.ContainsKey(p.flightID))
                {
                    otherVessel.Enqueue(p);
                    continue;
                }

                // Special handling of certain modules
                foreach (ProtoPartModuleSnapshot pm in p.modules)
                {
                    if (pm.moduleName == "ModuleDecouple" || pm.moduleName == "ModuleDockingNode" || pm.moduleName == "ModuleGrappleNode")
                    {
                        // Just assume all parts can decouple from this, it's easier and
                        // effectively the same thing
                        decoupler = true;

                        // Parent may be null if this is the root of the stack
                        if (p.parent != null)
                        {
                            dockedParts[p.parent.flightID] = p.parent.flightID;
                        }

                        // Add all children as possible new vessels
                        foreach (ProtoPartSnapshot child in parts.Where(childPart => childPart.parent == p))
                        {
                            dockedParts[child.flightID] = child.flightID;
                        }

                        if (pm.moduleName == "ModuleGrappleNode")
                        {
                            ModuleGrappleNode grapple = pm.moduleRef as ModuleGrappleNode;
                            ProtoPartSnapshot dockedPart = parts.Where(childPart => childPart.flightID == grapple.dockedPartUId).FirstOrDefault();
                            if (dockedPart != null)
                            {
                                otherVessel.Enqueue(dockedPart);
                            }
                        }
                    }
                }

                // Go through our child parts
                foreach (ProtoPartSnapshot child in parts.Where(childPart => childPart.parent == p))
                {
                    if (!visited.ContainsKey(child))
                    {
                        queue.Enqueue(child);
                        visited[child] = 1;
                    }
                }

                // Confirm if parent part has been visited
                if (p.parent != null && !visited.ContainsKey(p.parent))
                {
                    queue.Enqueue(p.parent);
                    visited[p.parent] = 1;
                }

                // Add this part to the hash
                if (!decoupler)
                {
                    count++;
                    hash ^= p.flightID;
                }

                // We've processed this node
                visited[p] = 2;
            }

            // Return the last hash
            if (count != 0)
            {
                yield return hash;
            }
        }
    }
}
