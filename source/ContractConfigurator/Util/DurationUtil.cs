using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using ContractConfigurator.Parameters;
using KSP.Localization;

namespace ContractConfigurator
{
    /// <summary>
    /// Class for handling duration related stuff.
    /// </summary>
    public class DurationUtil
    {
        private static bool cached = false;
        private static string YEAR;
        private static string YEARS;
        private static string DAY;
        private static string DAYS;

        /// <summary>
        /// Parses a duration in the form "1y 2d 3h 4m 5s"
        /// </summary>
        /// <param name="durationStr"></param>
        /// <returns></returns>
        public static double ParseDuration(string durationStr)
        {
            Match m = Regex.Match(durationStr, @"((\d+)?y,?\s*)?((\d+)?d,?\s*)?((\d+)?h,?\s*)?((\d+)?m,?\s*)?((\d+)?s,?\s*)?");
            int years = m.Groups[2].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[2].Value);
            int days = m.Groups[4].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[4].Value);
            int hours = m.Groups[6].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[6].Value);
            int minutes = m.Groups[8].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[8].Value);
            int seconds = m.Groups[10].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[10].Value);

            SetTimeConsts();
            return seconds +
                minutes * KSPUtil.dateTimeFormatter.Minute +
                hours * KSPUtil.dateTimeFormatter.Hour +
                days * KSPUtil.dateTimeFormatter.Day +
                years * KSPUtil.dateTimeFormatter.Year;
        }
        
        /// <summary>
        /// Gets the string value of the duration.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static string StringValue(double duration)
        {
            return StringValue(duration, true);
        }

        /// <summary>
        /// Gets the string value of the duration.
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="displayDaysAndYears"></param>
        /// <param name="displayMilli">Whether to display milliseconds</param>
        /// <returns></returns>
        public static string StringValue(double duration, bool displayDaysAndYears, bool displayMilli = false)
        {
            double time = duration;
            SetTimeConsts();

            StringBuilder sb = StringBuilderCache.Acquire(64);

            if (displayDaysAndYears)
            {
                int years = (int)(time / KSPUtil.dateTimeFormatter.Year);
                time -= years * KSPUtil.dateTimeFormatter.Year;

                int days = (int)(time / KSPUtil.dateTimeFormatter.Day);
                time -= days * KSPUtil.dateTimeFormatter.Day;

                if (years != 0)
                {
                    sb.Append(years);
                    sb.Append(" ");
                    sb.Append(years == 1 ? YEAR : YEARS);
                }
                if (days != 0)
                {
                    if (sb.Length != 0) sb.Append(", ");
                    sb.Append(days);
                    sb.Append(" ");
                    sb.Append(days == 1 ? DAY : DAYS);
                }
            }

            int hours = (int)(time / KSPUtil.dateTimeFormatter.Hour);
            time -= hours * KSPUtil.dateTimeFormatter.Hour;

            int minutes = (int)(time / KSPUtil.dateTimeFormatter.Minute);
            time -= minutes * KSPUtil.dateTimeFormatter.Minute;

            int seconds = (int)(time);

            if (hours != 0 || minutes != 0 || seconds != 0 || sb.Length == 0)
            {
                if (sb.Length != 0) sb.Append(", ");
                sb.Append(hours.ToString("D2"));
                sb.Append(":");
                sb.Append(minutes.ToString("D2"));
                sb.Append(":");
                sb.Append(seconds.ToString("D2"));
            }

            if (displayMilli)
            {
                time -= seconds;
                int millis = (int)(time * 1000);
                sb.Append(".");
                sb.Append(millis.ToString("D3"));
            }

            return sb.ToStringAndRelease();
        }

        private static void SetTimeConsts()
        {
            if (!cached)
            {
                YEAR = Localizer.GetStringByTag("#autoLOC_6002334");
                YEARS = Localizer.GetStringByTag("#autoLOC_6002335");
                DAY = Localizer.GetStringByTag("#autoLOC_6002337");
                DAYS = Localizer.GetStringByTag("#autoLOC_6002336");
            }
        }

    }
}
