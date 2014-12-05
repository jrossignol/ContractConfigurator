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
    /*
     * Class for handling duration related stuff.
     */
    public class DurationUtil
    {
        public const int SECONDS_PER_MINUTE = 60;
        public const int SECONDS_PER_HOUR = SECONDS_PER_MINUTE * 60;
        public const int SECONDS_PER_DAY = SECONDS_PER_HOUR * 6;
        public const int SECONDS_PER_YEAR = 9203545;

        /*
         * Parses a duration in the form "1y 2d 3h 4m 5s"
         */
        public static double ParseDuration(ConfigNode configNode, string key)
        {
            if (configNode.HasValue(key))
            {
                string durationStr = configNode.GetValue(key);
                Match m = Regex.Match(durationStr, @"((\d+)?y,?\s*)?((\d+)?d,?\s*)?((\d+)?h,?\s*)?((\d+)?m,?\s*)?((\d+)?s,?\s*)?");
                int years = m.Groups[2].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[2].Value);
                int days = m.Groups[4].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[4].Value);
                int hours = m.Groups[6].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[6].Value);
                int minutes = m.Groups[8].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[8].Value);
                int seconds = m.Groups[10].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[10].Value);

                return seconds +
                    minutes * SECONDS_PER_MINUTE +
                    hours * SECONDS_PER_HOUR +
                    days * SECONDS_PER_DAY +
                    years * SECONDS_PER_YEAR;
            }

            return 0.0;
        }
        
        /*
         * Gets the string value of the duration
         */
        public static string StringValue(double duration)
        {
            double time = duration;

            int years = (int)(time / SECONDS_PER_YEAR);
            time -= years * SECONDS_PER_YEAR;

            int days = (int)(time / SECONDS_PER_DAY);
            time -= days * SECONDS_PER_DAY;

            int hours = (int)(time / SECONDS_PER_HOUR);
            time -= hours * SECONDS_PER_HOUR;

            int minutes = (int)(time / SECONDS_PER_MINUTE);
            time -= minutes * SECONDS_PER_MINUTE;

            int seconds = (int)(time);

            string output = "";
            if (years != 0)
            {
                output += years + (years == 1 ? "year" : " years");
            }
            if (days != 0)
            {
                if (output.Length != 0) output += ", ";
                output += days + (days == 1 ? "days" : " days");
            }
            if (hours != 0 || minutes != 0 || seconds != 0)
            {
                if (output.Length != 0) output += ", ";
                output += hours.ToString("D2") + ":" + minutes.ToString("D2") + ":" + seconds.ToString("D2");
            }

            return output;
        }

    }
}
