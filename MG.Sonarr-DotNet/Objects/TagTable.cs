using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
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
        public HashSet<TagEntry> Entries { get; }

        public IEnumerable<int> Add => this.Entries.Where(x => x.Action == TagAction.Add).Select(x => x.Id);
        public IEnumerable<int> Remove => this.Entries.Where(x => x.Action == TagAction.Remove).Select(x => x.Id);
        public IEnumerable<int> Set => this.Entries.Where(x => x.Action == TagAction.Set).Select(x => x.Id);

        #endregion

        #region CONSTRUCTORS
        public TagTable(params object[] input)
        {
            this.Entries = new HashSet<TagEntry>(new TagEntryEquality());
            if (input != null && input.Length > 0)
                this.ProcessInput(input);
        }

        #endregion

        #region PUBLIC METHODS
        public void ModifyObject(ISupportsTagUpdate input)
        {
            if (_isSetting)
            {
                input.Tags.Clear();
                input.Tags.UnionWith(this.Set);
            }
            else
            {
                if (_isAdding)
                {
                    input.Tags.UnionWith(this.Add);
                }
                if (_isRemoving)
                {
                    input.Tags.ExceptWith(this.Remove);
                }
            }
        }

        #endregion
        private void ProcessInput(object[] input)
        {
            if (this.TryInputAsDictionary(input, out IDictionary outDict))
            {
                _isSetting = false;
                IEnumerable<string> keys = this.GetStringKeys(outDict.Keys);

                _isAdding = this.TryGetKey(keys, ADD, out string outAddKey);
                string _addingKey = outAddKey;

                _isRemoving = this.TryGetKey(keys, REMOVE, out string outRemKey);
                string _removingKey = outRemKey;

                if (_isAdding)
                {
                    object addO = outDict[_addingKey];
                    this.AddToEntries(TagAction.Add, addO);
                }
                if (_isRemoving)
                {
                    object remO = outDict[_removingKey];
                    this.AddToEntries(TagAction.Remove, remO);
                }
            }
            else
            {
                _isSetting = true;
                this.AddToEntries(TagAction.Set, input);
            }
        }

        private void AddToEntries(TagAction action, object value)
        {
            if (value is ICollection icol)
            {
                foreach (object o in icol)
                {
                    this.AddToEntries(action, o);
                }
            }
            else if (Context.TagManager.TryGetTag(value, out Tag tag))
            {
                this.Entries.Add(new TagEntry(tag, action));
            }
        }
        private IEnumerable<string> GetStringKeys(ICollection keys)
        {
            foreach (object o in keys)
            {
                if (o is string oStr)
                {
                    yield return oStr;
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
    }
}