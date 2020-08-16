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
            orMiddle  = Localizer.GetStringByTag("#cc.list.or.middle").Replace("<<1>>", "").Replace("<<2>>", "");
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
    }
}
