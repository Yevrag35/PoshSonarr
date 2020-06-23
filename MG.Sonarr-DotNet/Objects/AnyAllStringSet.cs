using MG.Sonarr.Functionality;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr
{
    public class AnyAllStringSet : AnyAllSet<string>
    {
        private AnyAllStringSet() : base() => base.Type = AnyAllNoneSet.All;
        public AnyAllStringSet(IEnumerable<string> strings) : base(strings)
        {
            base.Type = AnyAllNoneSet.All;
        }

        private static bool IsEnumerableNonString(object o, out IEnumerable result)
        {
            result = null;
            if (!(o is string) && o is IEnumerable gotcha)
            {
                result = gotcha;
                return true;
            }
            else
                return false;
        }

        public static implicit operator AnyAllStringSet(Hashtable ht)
        {
            AnyAllStringSet anyall = null;
            if (ht.Count <= 0)
                return new AnyAllStringSet();

            else
                anyall = new AnyAllStringSet();

            IEnumerable<string> keys = ht.Keys.Cast<string>();
            if (keys.Contains(AnyAllNoneSet.All.ToString(), anyall.StringComparer))
            {
                string key = anyall.GetAllKey(keys);
                if (IsEnumerableNonString(ht[key], out IEnumerable ienum1))
                {
                    foreach (IConvertible icon in ienum1)
                    {
                        anyall.Add(Convert.ToString(icon));
                    }
                }
                else
                {
                    anyall.Add(Convert.ToString(ht[key]));
                }

                anyall.Type = AnyAllNoneSet.All;
            }
            else if (keys.Contains(AnyAllNoneSet.Any.ToString(), anyall.StringComparer))
            {
                string key = anyall.GetAnyKey(keys);
                if (IsEnumerableNonString(ht[key], out IEnumerable moreThanOne2))
                {
                    foreach (IConvertible icon in moreThanOne2)
                    {
                        anyall.Add(Convert.ToString(icon));
                    }
                }
                else
                {
                    anyall.Add(Convert.ToString(ht[key]));
                }
                anyall.Type = AnyAllNoneSet.Any;
            }
            else if (keys.Contains(AnyAllNoneSet.None.ToString(), anyall.StringComparer))
            {
                string key = anyall.GetNoneKey(keys);
                object realKey = ht[key];
                if (IsEnumerableNonString(realKey, out IEnumerable moreThanOne3))
                {
                    foreach (IConvertible icon in moreThanOne3)
                    {
                        anyall.Add(Convert.ToString(icon));
                    }
                }
                else
                {
                    anyall.Add(Convert.ToString(ht[key]));
                }
                anyall.Type = AnyAllNoneSet.None;
            }

            return anyall;
        }
    }
}
