using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for Orbit.
    /// </summary>
    public class OrbitParser : ClassExpressionParser<Orbit>, IExpressionParserRegistrer
    {
        static OrbitParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(Orbit), typeof(OrbitParser));
        }

        internal static void RegisterMethods()
        {
            RegisterLocalFunction(new Function<List<double>, int, Orbit>("CreateOrbit", CreateOrbit));
        }

        public OrbitParser()
        {
        }

        protected static Orbit CreateOrbit(List<double> dVals, int refVal)
        {
            ConfigNode orbitNode = new ConfigNode("ORBIT");

            orbitNode.AddValue("SMA", dVals[0]);
            orbitNode.AddValue("ECC", dVals[1]);
            orbitNode.AddValue("INC", dVals[2]);
            orbitNode.AddValue("LPE", dVals[3]);
            orbitNode.AddValue("LAN", dVals[4]);
            orbitNode.AddValue("MNA", dVals[5]);
            orbitNode.AddValue("EPH", dVals[6]);
            orbitNode.AddValue("REF", refVal);

            return new OrbitSnapshot(orbitNode).Load();
        }
    }
}
