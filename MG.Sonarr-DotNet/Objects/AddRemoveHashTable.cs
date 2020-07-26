using MG.Sonarr.Functionality;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace MG.Sonarr
{
    public class AddRemoveHashtable
    {
        private const string ADD = "Add";
        private const string REMOVE = "Remove";
        private const string REPLACE = "Replace";

        public string[] AddTerms { get; private set; } = new string[] { };
        public string[] RemoveTerms { get; private set; } = new string[] { };
        public string[] ReplaceTerms { get; set; } = new string[] { };

        public AddRemoveHashtable() { }
        internal AddRemoveHashtable(IDictionary dict)
        {
            string[] keys = dict.Keys.Cast<string>().ToArray();
            var igc = SonarrFactory.NewIgnoreCase();
            if (!keys.Contains(REPLACE, igc))
            { 
                if (keys.Contains(ADD, igc))
                {
                    string addKey = keys.Single(x => x.Equals(ADD, StringComparison.CurrentCultureIgnoreCase));
                    object addObj = dict[addKey];
                    if (addObj is string str)
                        this.AddTerms = new string[1] { str };

                    else if (addObj is string[] strs)
                        this.AddTerms = strs;

                    else if (addObj is object[] array)
                    {
                        this.AddTerms = new string[array.Length];
                        for (int i = 0; i < array.Length; i++)
                        {
                            this.AddTerms[i] = Convert.ToString(array[i]);
                        }
                    }
                }

                if (keys.Contains(REMOVE, igc))
                {
                    string remKey = keys.Single(x => x.Equals(REMOVE, StringComparison.CurrentCultureIgnoreCase));
                    object remObj = dict[remKey];
                    if (remObj is string str)
                        this.RemoveTerms = new string[1] { str };

                    else if (remObj is string[] strs)
                        this.RemoveTerms = strs;

                    else if (remObj is object[] array)
                    {
                        this.RemoveTerms = new string[array.Length];
                        for (int i = 0; i < array.Length; i++)
                        {
                            this.RemoveTerms[i] = Convert.ToString(array[i]);
                        }
                    }
                }
            }
            else
            {
                string repKey = keys.FirstOrDefault(x => x.Equals(REPLACE, StringComparison.CurrentCultureIgnoreCase));
                object repObj = dict[repKey];
                if (repObj is IEnumerable enumerable)
                {
                    this.ReplaceTerms = this.GetReplaceTerms(enumerable).ToArray();
                }
                else if (repObj is IConvertible icon)
                    this.ReplaceTerms = new string[1] { Convert.ToString(icon) };
            }
        }

        private IEnumerable<string> GetReplaceTerms(IEnumerable enumerable)
        {
            foreach (object o in enumerable)
            {
                if (o is IConvertible icon)
                    yield return Convert.ToString(icon);
            }
        }

        public static implicit operator AddRemoveHashtable(Hashtable ht) => new AddRemoveHashtable(ht);
        public static implicit operator AddRemoveHashtable(OrderedDictionary orderedDict) => new AddRemoveHashtable(orderedDict);
    }
}