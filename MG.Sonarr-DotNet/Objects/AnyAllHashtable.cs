using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr
{
    public class AnyAllHashtable : HashSet<int>
    {
        private const string ALL = "All";
        private const string ANY = "Any";

        private IEqualityComparer<string> _comparer;

        //public int Count => this.Ids.Count;
        //public HashSet<int> Ids { get; }
        public bool IsAll { get; private set; }
        
        private AnyAllHashtable() : base()
        {
            _comparer = ClassFactory.NewIgnoreCase();
        }
        public AnyAllHashtable(IEnumerable<int> ids) : base(ids)
        {
            _comparer = ClassFactory.NewIgnoreCase();
        }
        public AnyAllHashtable(object[] objs) : base(FindAllTags(objs))
        {
            _comparer = ClassFactory.NewIgnoreCase();
        }

        public static implicit operator AnyAllHashtable(Hashtable ht)
        {
            AnyAllHashtable anyall = null;
            if (ht.Count <= 0)
                return new AnyAllHashtable();

            else
                anyall = new AnyAllHashtable();

            IEnumerable<string> keys = ht.Keys.Cast<string>();
            if (keys.Contains(ALL, anyall._comparer))
            {
                string key = GetAllKey(keys);
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
            else if (keys.Contains(ANY, anyall._comparer))
            {
                string key = GetAnyKey(keys);
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
        private static string GetAllKey(IEnumerable<string> keys) => keys.Single(x => x.Equals(ALL, StringComparison.CurrentCultureIgnoreCase));
        private static string GetAnyKey(IEnumerable<string> keys) => keys.Single(x => x.Equals(ANY, StringComparison.CurrentCultureIgnoreCase));
    }
}
