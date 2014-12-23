using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using FinePrint.Contracts.Parameters;
using FinePrint.Utilities;

namespace ContractConfigurator.Parameters
{
    /*
     * ContractParameter that is successful when all child parameters are successful.
     */
    public class SpecificOrbitWrapper : SpecificOrbitParameter
    {
        public SpecificOrbitWrapper()
        {
        }

        public SpecificOrbitWrapper(OrbitType orbitType, double inclination, double eccentricity, double sma, double lan, double argumentOfPeriapsis, double meanAnomalyAtEpoch, double epoch, CelestialBody targetBody, double difficultyFactor, double deviationWindow)
            : base(orbitType, inclination, eccentricity, sma, lan, argumentOfPeriapsis, meanAnomalyAtEpoch, epoch, targetBody, difficultyFactor, deviationWindow)
        {
        }

        protected override string GetNotes()
        {
            return "";
        }
    }
}
