using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP.Localization;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for LaunchSite.
    /// </summary>
    public class LaunchSiteParser : ClassExpressionParser<LaunchSite>, IExpressionParserRegistrer
    {
        static LaunchSiteParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(LaunchSite), typeof(LaunchSiteParser));
        }

        public static void RegisterMethods()
        {
            RegisterMethod(new Method<LaunchSite, string>("Name", ls => ls != null ? ls.name : null));
            RegisterMethod(new Method<LaunchSite, PQSCity>("PQSCity", ls => ls != null ? ls.pqsCity : null));
            RegisterMethod(new Method<LaunchSite, Location>("Location", ls =>
            {
                if (ls == null)
                {
                    return null;
                }
                LaunchSite.SpawnPoint spawnPoint = ls.GetSpawnPoint(ls.name);
                return new Location(ls.Body, spawnPoint.latitude, spawnPoint.longitude);
            }));

            RegisterGlobalFunction(new Function<List<LaunchSite>>("AllLaunchSites", () => PSystemSetup.Instance.LaunchSites));
            RegisterGlobalFunction(new Function<List<LaunchSite>>("AllEnabledLaunchSites", () => PSystemSetup.Instance.LaunchSites.Where(ls => ls.IsSetup).ToList()));
            RegisterGlobalFunction(new Function<List<LaunchSite>>("AllStockLaunchSites", () => PSystemSetup.Instance.StockLaunchSites.ToList()));
            RegisterGlobalFunction(new Function<List<LaunchSite>>("AllNonStockLaunchSites", () => PSystemSetup.Instance.NonStockLaunchSites.ToList()));
        }

        public LaunchSiteParser()
        {
        }

        public override U ConvertType<U>(LaunchSite value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)(Localizer.GetStringByTag(value.launchSiteName));
            }
            return base.ConvertType<U>(value);
        }

        public override LaunchSite ParseIdentifier(Token token)
        {
            if (token.sval.Equals("null", StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }

            return ConfigNodeUtil.ParseLaunchSiteValue(token.sval);
        }
    }
}
