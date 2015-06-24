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
    public class SpawnVesselParser : BehaviourParser<SpawnVesselFactory>, IExpressionParserRegistrer
    {
        static SpawnVesselParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(SpawnVesselFactory), typeof(SpawnVesselParser));
        }

        internal static void RegisterMethods()
        {
            RegisterMethod(new Method<SpawnVesselFactory, List<Vessel>>("Vessels",
                svf => {
                    if (svf.Current != null)
                    {
                        return svf.Current.Vessels().Select(v => v.vesselRef).ToList();
                    }
                    else
                    {
                        CheckInitialized(svf);
                        return new List<Vessel>();
                    }
                }, false));
        }

        public SpawnVesselParser()
        {
        }

        protected static void CheckInitialized(SpawnVesselFactory svf)
        {
            foreach (DataNode dataNode in svf.dataNode.Children)
            {
                foreach (string identifier in new string[] { "name", "vesselType", "targetBody", "lat", "lon", "alt", "heading", "pitch", "roll", "owned" })
                {
                    if (!dataNode.IsInitialized(identifier))
                    {
                        throw new DataNode.ValueNotInitialized(dataNode.Path() + identifier);
                    }
                }
            }
        }
    }
}
