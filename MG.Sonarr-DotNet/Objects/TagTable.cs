using MG.Sonarr.Functionality;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr
{
    public class TagTable
    {
        #region FIELDS/CONSTANTS
        private const string ADD = "Add";
        private const string REMOVE = "Remove";

        private bool _isAdding;
        private bool _isRemoving;
        private bool _isSetting;

        #endregion

        #region PROPERTIES
        public int[] AddTagIds { get; private set; }
        public string[] AddTags { get; private set; }
        public bool IsAdding => _isAdding;
        public bool HasAddById => _isAdding && this.AddTagIds != null && this.AddTagIds.Length > 0;
        public bool HasRemoveById => _isRemoving && this.RemoveTagIds != null && this.RemoveTagIds.Length > 0;
        public bool IsRemoving => _isRemoving;
        public bool HasSetById => _isSetting && this.SetTagIds != null && this.SetTagIds.Length > 0;
        public bool IsSetting => _isSetting;
        public int[] RemoveTagIds { get; private set; }
        public string[] RemoveTags { get; private set; }
        public int[] SetTagIds { get; private set; }
        public string[] SetTags { get; private set; }

        #endregion

        #region CONSTRUCTORS
        public TagTable(params object[] input)
        {
            if (this.TryInputAsDictionary(input, out IDictionary outDict))
            {
                this.SetTagActions(outDict);
            }
            else if (this.TryInputAsInts(input, out int[] outInts))
            {
                _isSetting = true;
                this.SetTagIds = outInts;
            }
            else if (this.TryInputAsStrings(input, out string[] outStrs))
            {
                _isSetting = true;
                this.SetTags = outStrs;
            }
        }

        #endregion

        #region PUBLIC METHODS


        #endregion

        #region BACKEND/PRIVATE METHODS
        private IEnumerable<string> GetStringKeys(IDictionary dict)
        {
            foreach (object o in dict.Keys)
            {
                if (o is string oStr)
                    yield return oStr;

                else if (o is IConvertible icon)
                    yield return Convert.ToString(icon);
            }
        }
        private void SetTagActions(IDictionary dict)
        {
            var ints = new List<int>();
            var strs = new List<string>();
            IEnumerable<string> keys = this.GetStringKeys(dict);
            var igc = ClassFactory.NewIgnoreCase();
            if (keys.Contains(ADD, igc) && this.TryGetKey(keys, ADD, out string key))
            {
                _isAdding = true;
                object val = dict[key];
                if (!this.ValueFromEnumerable(val, ref strs, ref ints))
                {
                    if (val is string oneStr)
                    {
                        strs.Add(oneStr);
                    }
                    else if (val is int oneInt)
                    {
                        ints.Add(oneInt);
                    }   
                }
                if (strs.Count > 0)
                {
                    this.AddTags = strs.ToArray();
                    strs.Clear();
                }

                if (ints.Count > 0)
                {
                    this.AddTagIds = ints.ToArray();
                    ints.Clear();
                }
            }
            if (keys.Contains(REMOVE, igc) && this.TryGetKey(keys, REMOVE, out string remKey))
            {
                _isRemoving = true;
                object val = dict[remKey];
                if (!this.ValueFromEnumerable(val, ref strs, ref ints))
                {
                    if (val is string oneStr)
                    {
                        strs.Add(oneStr);
                    }
                    else if (val is int oneInt)
                    {
                        ints.Add(oneInt);
                    }
                }
                if (strs.Count > 0)
                {
                    this.RemoveTags = strs.ToArray();
                }

                if (ints.Count > 0)
                {
                    this.RemoveTagIds = ints.ToArray();
                }
            }
        }
        private bool TryGetKey(IEnumerable<string> keys, string wanted, out string outKey)
        {
            outKey = keys.SingleOrDefault(x => x.Equals(wanted, StringComparison.InvariantCultureIgnoreCase));
            return !string.IsNullOrEmpty(outKey);
        }
        private bool TryInputAsDictionary(object[] input, out IDictionary outDict)
        {
            outDict = null;
            if (input != null && input.Length == 1 && input[0] is IDictionary idict)
                outDict = idict;

            return outDict != null;
        }
        private bool TryInputAsInts(object[] input, out int[] outInts)
        {
            outInts = null;
            if (input != null)
            {
                var list = new List<int>(input.Length);
                try
                {
                    foreach (IConvertible icon in input)
                    {
                        if (int.TryParse(Convert.ToString(icon), out int conInt))
                        {
                            list.Add(conInt);
                        }
                    }
                    outInts = list.ToArray();
                }
                catch
                {
                    return false;
                }
            }
            return outInts != null && outInts.Length == input.Length;
        }
        private bool TryInputAsStrings(object[] input, out string[] outStrs)
        {
            outStrs = null;
            if (input != null)
            {
                try
                {
                    outStrs = new string[input.Length];
                    for (int i = 0; i < input.Length; i++)
                    {
                        if (input[i] is IConvertible icon)
                        {
                            outStrs[i] = Convert.ToString(icon);
                        }
                        else
                            return false;
                    }
                }
                catch (InvalidCastException)
                {
                    return false;
                }
            }
            return outStrs != null && outStrs.Length == input.Length;
        }

        private bool ValueFromEnumerable(object value, ref List<string> strs, ref List<int> ints)
        {
            bool result = false;
            if (value is IEnumerable ienum && !(value is string))
            {
                result = true;
                foreach (object o in ienum)
                {
                    if (o is string oStr)
                    {
                        strs.Add(oStr);
                    }
                    else if (o is IConvertible icon && int.TryParse(Convert.ToString(icon), out int outInt))
                    {
                        ints.Add(outInt);
                    }
                }
            }
            return result;
        }

        #endregion
    }
}