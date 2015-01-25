using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;

namespace ContractConfigurator.SCANsat
{
    /// <summary>
    /// ContractRequirement for SCANsat coverage of a specific location.
    /// </summary>
    public class SCANsatLocationCoverageRequirement : ContractRequirement
    {
        protected string scanType;
        protected double latitude;
        protected double longitude;
        protected PQSCity pqsCity;

        public override bool Load(ConfigNode configNode)
        {
            // Before loading, verify the SCANsat version
            if (!SCANsatUtil.VerifySCANsatVersion())
            {
                return false;
            }

            // Load base class
            bool valid = base.Load(configNode);

            // Do not check the requirement on active contracts.  Otherwise when they scan the
            // contract is invalidated, which is usually not what's meant.
            checkOnActiveContract = false;

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "scanType", ref scanType, this, "Anomaly", SCANsatUtil.ValidateSCANname);
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "latitude", ref latitude, this, 0.0);
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "longitude", ref longitude, this, 0.0);

            valid &= ConfigNodeUtil.MutuallyExclusive(configNode, new string[] { "latitude", "longitude" }, new string[] { "pqsCity" }, this);
            valid &= ValidateTargetBody(configNode);

            string pqsName = null;
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "pqsCity", ref pqsName, this, (string)null);
            if (pqsName != null)
            {
                try
                {
                    CelestialBody body = FlightGlobals.Bodies.Where(b => b == targetBody).First();
                    pqsCity = body.GetComponentsInChildren<PQSCity>(true).Where(pqs => pqs.name == pqsName).First();
                }
                catch (Exception e)
                {
                    LoggingUtil.LogError(this, "Couldn't load PQSCity with name '" + pqsCity + "'");
                    LoggingUtil.LogException(e);
                    valid = false;
                }
            }

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            if (pqsCity != null)
            {
                latitude = targetBody.GetLatitude(pqsCity.transform.position);
                longitude = targetBody.GetLongitude(pqsCity.transform.position);
                pqsCity = null;
            }

            return SCANsatUtil.IsCovered(latitude, longitude, SCANsatUtil.GetSCANtype(scanType), targetBody);
        }
    }
}
