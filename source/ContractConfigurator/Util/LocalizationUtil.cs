using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.Localization;
using Experience;

namespace ContractConfigurator
{
    public class LocalizationUtil
    {
        private static LocalizationUtil _Instance;
        private static LocalizationUtil Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new LocalizationUtil();
                }
                return _Instance;
            }
        }

        public enum Conjunction
        {
            AND,
            OR
        }

        private string andMiddle;
        private string orMiddle;
        private string andEnd;
        private string orEnd;

        private LocalizationUtil()
        {
            andMiddle = Localizer.GetStringByTag("#cc.list.and.middle").Replace("<<1>>", "").Replace("<<2>>", "");
            orMiddle = Localizer.GetStringByTag("#cc.list.or.middle").Replace("<<1>>", "").Replace("<<2>>", "");
            andEnd = Localizer.GetStringByTag("#cc.list.and.end").Replace("<<1>>", "").Replace("<<2>>", "");
            orEnd = Localizer.GetStringByTag("#cc.list.or.end").Replace("<<1>>", "").Replace("<<2>>", "");
        }

        public static string LocalizeList<T>(Conjunction conjunction, IEnumerable<T> values)
        {
            return Instance._LocalizeList<T>(conjunction, values, (x) => { return x.ToString(); });
        }

        public static string LocalizeList<T>(Conjunction conjunction, IEnumerable<T> values, Func<T, string> strFunc)
        {
            return Instance._LocalizeList<T>(conjunction, values, strFunc);
        }

        private string _LocalizeList<T>(Conjunction conjunction, IEnumerable<T> values, Func<T, string> strFunc)
        {
            int count = values.Count();
            if (count == 0)
            {
                return "";
            }
            else if (count == 1)
            {
                return strFunc(values.ElementAt(0));
            }
            else if (count == 2)
            {
                return Localizer.Format(conjunction == Conjunction.AND ? "#cc.list.and.2" : "#cc.list.or.2", strFunc(values.ElementAt(0)), strFunc(values.ElementAt(1)));
            }
            else
            {
                StringBuilder sb = StringBuilderCache.Acquire(count * 16);

                int i = 0;
                string prev = null;
                foreach (T tval in values)
                {
                    string val = strFunc(tval);
                    if (i == 0)
                    {
                        prev = val;
                    }
                    else if (i == 1)
                    {
                        sb.Append(Localizer.Format(conjunction == Conjunction.AND ? "#cc.list.and.start" : "#cc.list.or.start", prev, val));
                    }
                    else if (i != count - 1)
                    {
                        sb.Append(conjunction == Conjunction.AND ? andMiddle : orMiddle);
                        sb.Append(val);
                    }
                    else
                    {
                        sb.Append(conjunction == Conjunction.AND ? andEnd : orEnd);
                        sb.Append(val);
                    }
                    i++;
                }

                return sb.ToStringAndRelease();
            }
        }

        public static string TraitTitle(string traitName)
        {
            ExperienceTraitConfig config = GameDatabase.Instance.ExperienceConfigs.Categories.Where(c => c.Name == traitName).FirstOrDefault();

            return config != null ? config.Title : traitName;
        }

        /// <summary>
        /// Performs a very sketchy heuristic match on a localized string.  Will possibly give bad results on stuff like "<<1>> <<2>>".
        /// </summary>
        /// <param name="searchString">The string to search in.</param>
        /// <param name="localizationTag">The localization tag.</param>
        /// <returns>Whether the searchString is a parametized version of localizationTag.</returns>
        public static bool IsLocalizedString(string searchString, string localizationTag)
        {
            LoggingUtil.LogVerbose(typeof(LocalizationUtil), "IsLocalizedString('{0}', '{1}')", searchString, localizationTag);

            // First check for no parameter tags - literal comparison
            int pos = localizationTag.IndexOf(">>");
            if (pos == -1)
            {
                return string.Equals(searchString, localizationTag);
            }
            else
            {
                // Move past the >> characters
                pos += 2;
            }

            // Find the second parameter
            int pos2 = localizationTag.IndexOf("<<", pos);
            if (pos2 == -1)
            {
                // No second parameter, just go to end of string
                pos2 = localizationTag.Length;
            }

            string searchValue = localizationTag.Substring(pos, pos2 - pos);
            bool match = (searchString.IndexOf(searchValue) > pos - 5);
            LoggingUtil.LogVerbose(typeof(LocalizationUtil), "    the searchValue is '{0}', match = {1}", searchValue, match);

            return match;
        }

        private static List<string> unlocalizedStringStorage = new List<string>();
        /// <summary>
        /// Reverses the localization of a string and gets the parameters out (as strings)
        /// </summary>
        /// <param name="input">Input string</param>
        /// <param name="localizationTag">Localization tag to use to reverse localization.</param>
        /// <returns></returns>
        public static IList<string> UnLocalizeString(string input, string localizationTag)
        {
            LoggingUtil.LogVerbose(typeof(LocalizationUtil), "UnLocalizeString('{0}', '{1}')", input, localizationTag);
            unlocalizedStringStorage.Clear();

            int inputPos = 0;
            int currentPos = 0;
            int paramIndex = localizationTag.IndexOf("<<", currentPos);
            int len = localizationTag.Length;
            while (paramIndex >= 0 && paramIndex < len)
            {
                // Fast forward the input to the same location.
                inputPos += paramIndex - currentPos;

                //<<1>> <<2>>
                // Get the location of the *next* parameter
                currentPos = paramIndex + 5;
                paramIndex = localizationTag.IndexOf("<<", currentPos);
                if (paramIndex == -1)
                {
                    paramIndex = len;
                }

                // Get the next literal part of the localization tag
                string searchValue = localizationTag.Substring(currentPos, paramIndex - currentPos);
                int index = (currentPos == paramIndex) ? input.Length : input.IndexOf(searchValue, inputPos);
                if (index == -1)
                {
                    LoggingUtil.LogError(typeof(LocalizationUtil), "Couldn't unlocalize string '{0}'", input);
                    return unlocalizedStringStorage.AsReadOnly();
                }

                // Get the parameter and fast forward past it
                string parameterValue = input.Substring(inputPos, index - inputPos);
                inputPos = index;
                LoggingUtil.LogVerbose(typeof(LocalizationUtil), "   adding parameter '{0}'", parameterValue);
                unlocalizedStringStorage.Add(parameterValue);
            }

            return unlocalizedStringStorage.AsReadOnly();
        }
    }
}
