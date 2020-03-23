using MG.Sonarr.Functionality;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr
{
    public class AnyAllStringSet : AnyAllSet<string>
    {
        private AnyAllStringSet() : base() { }
        public AnyAllStringSet(IEnumerable<string> strings) : base(strings)
        {
        }

        public static implicit operator AnyAllStringSet(Hashtable ht)
        {
            AnyAllStringSet anyall = null;
            if (ht.Count <= 0)
                return new AnyAllStringSet();

            else
                anyall = new AnyAllStringSet();

            IEnumerable<string> keys = ht.Keys.Cast<string>();
            if (keys.Contains(ALL, anyall.StringComparer))
            {
                string key = anyall.GetAllKey(keys);
                if (ht[key] is IEnumerable ienum1)
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

                anyall.IsAll = true;
            }
            else if (keys.Contains(ANY, anyall.StringComparer))
            {
                string key = anyall.GetAnyKey(keys);
                if (ht[key] is IEnumerable moreThanOne2)
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
                anyall.IsAll = false;
            }

            return anyall;
        }
    }
}
