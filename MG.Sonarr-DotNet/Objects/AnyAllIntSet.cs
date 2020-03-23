using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr
{
    public class AnyAllIntSet : AnyAllSet<int>
    {   
        private AnyAllIntSet() : base()
        {
        }
        public AnyAllIntSet(IEnumerable<int> ids) : base(ids)
        {
        }
        public AnyAllIntSet(object[] objs) : base(FindAllTags(objs))
        {
        }

        public static implicit operator AnyAllIntSet(Hashtable ht)
        {
            AnyAllIntSet anyall = null;
            if (ht.Count <= 0)
                return new AnyAllIntSet();

            else
                anyall = new AnyAllIntSet();

            IEnumerable<string> keys = ht.Keys.Cast<string>();
            if (keys.Contains(ALL, anyall.StringComparer))
            {
                string key = anyall.GetAllKey(keys);
                if (ht[key] is IEnumerable moreThanOne1)
                {
                    anyall.AddAllTags(moreThanOne1);
                }
                else if (Context.TagManager.TryGetTag(ht[key], out Tag foundYou))
                {
                    anyall.Add(foundYou.Id);
                }

                anyall.IsAll = true;
            }
            else if (keys.Contains(ANY, anyall.StringComparer))
            {
                string key = anyall.GetAnyKey(keys);
                if (ht[key] is IEnumerable moreThanOne2)
                {
                    anyall.AddAllTags(moreThanOne2);
                }
                else if (Context.TagManager.TryGetTag(ht[key], out Tag foundYou2))
                {
                    anyall.Add(foundYou2.Id);
                }
                anyall.IsAll = false;
            }

            return anyall;
        }

        private void AddAllTags(IEnumerable coll)
        {
            foreach (object o in coll)
            {
                if (Context.TagManager.TryGetTag(o, out Tag foundYou))
                {
                    base.Add(foundYou.Id);
                }
            }
        }
        private static IEnumerable<int> FindAllTags(object[] objs)
        {
            foreach (object o in objs)
            { 
                if (Context.TagManager.TryGetTag(o, out Tag foundYou))
                {
                    yield return foundYou.Id;
                }
            }
        }
        
    }
}
