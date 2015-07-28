using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// Class for handling duration related stuff.
    /// </summary>
    public class DurationUtil
    {
        private static uint SecondsPerYear;
        private static uint SecondsPerDay;
        private static uint SecondsPerHour;
        private static uint SecondsPerMinute;

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
                minutes * SecondsPerMinute +
                hours * SecondsPerHour +
                days * SecondsPerDay +
                years * SecondsPerYear;
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

            string output = "";

            if (displayDaysAndYears)
            {
                int years = (int)(time / SecondsPerYear);
                time -= years * SecondsPerYear;

                int days = (int)(time / SecondsPerDay);
                time -= days * SecondsPerDay;

                if (years != 0)
                {
                    output += years + (years == 1 ? " year" : " years");
                }
                if (days != 0)
                {
                    if (output.Length != 0) output += ", ";
                    output += days + (days == 1 ? " day" : " days");
                }
            }

            int hours = (int)(time / SecondsPerHour);
            time -= hours * SecondsPerHour;

            int minutes = (int)(time / SecondsPerMinute);
            time -= minutes * SecondsPerMinute;

            int seconds = (int)(time);

            if (hours != 0 || minutes != 0 || seconds != 0 || output.Length == 0)
            {
                if (output.Length != 0) output += ", ";
                output += hours.ToString("D2") + ":" + minutes.ToString("D2") + ":" + seconds.ToString("D2");
            }

            if (displayMilli)
            {
                time -= seconds;
                int millis = (int)(time * 1000);
                output += "." + millis.ToString("D3");
            }


            return output;
        }

        private static void SetTimeConsts()
        {
            // Earthtime
            SecondsPerYear = 31536000; // = 365d
            SecondsPerDay = 86400;     // = 24h
            SecondsPerHour = 3600;     // = 60m
            SecondsPerMinute = 60;     // = 60s

            if (GameSettings.KERBIN_TIME)
            {
                SecondsPerYear = 9201600;  // = 426d
                SecondsPerDay = 21600;     // = 6h
                SecondsPerHour = 3600;     // = 60m
                SecondsPerMinute = 60;     // = 60s
            }
        }

    }
}
