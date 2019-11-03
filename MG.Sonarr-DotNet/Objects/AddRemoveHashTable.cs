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

        public string[] AddTerms { get; }
        public string[] RemoveTerms { get; }

        private AddRemoveHashtable(IDictionary dict)
        {
            string[] keys = dict.Keys.Cast<string>().ToArray();
            var igc = ClassFactory.NewIgnoreCase();
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

        public static implicit operator AddRemoveHashtable(Hashtable ht) => new AddRemoveHashtable(ht);
        public static implicit operator AddRemoveHashtable(OrderedDictionary orderedDict) => new AddRemoveHashtable(orderedDict);
    }
}