using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr
{
    internal class TermTable : Table<string>
    {

        public TermTable() : base(SonarrFactory.NewIgnoreCase())
        {
        }

        internal void ModifyObject(ISet<string> set)
        {
            if (this.Set.Count <= 0)
            {
                if (this.Add.Count > 0)
                {
                    set.UnionWith(this.Add);
                }
                if (this.Remove.Count > 0)
                {
                    set.ExceptWith(this.Remove);
                }
            }
            else
            {
                set.Clear();
                set.UnionWith(this.Set);
            }
        }

        internal void Process(object o)
        {
            if (o is IDictionary dictionary)
            {
                IEnumerable<string> keys = dictionary.Keys.Cast<string>();
                if (base.TryGetKey(ADD, keys, this.Comparer, StringComparison.CurrentCultureIgnoreCase, out string addKey))
                {
                    base.AddKey = addKey;
                    this.Add.AddRange(base.ConvertFromObject(dictionary[addKey]));
                }
                if (base.TryGetKey(REMOVE, keys, this.Comparer, StringComparison.CurrentCultureIgnoreCase, out string remKey))
                {
                    base.RemoveKey = remKey;
                    this.Remove.AddRange(base.ConvertFromObject(dictionary[remKey]));
                }
            }
            else
            {
                this.Set.AddRange(base.ConvertFromObject(o));
            }
        }
    }
}
