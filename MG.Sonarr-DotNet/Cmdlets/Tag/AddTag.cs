﻿using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Extensions;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Add, "Tag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true, DefaultParameterSetName = "AddNewTag")]
    [CmdletBinding(PositionalBinding = false)]
    public class AddTag : TagCmdlet
    {
        #region FIELDS/CONSTANTS
        //private const string TAG_EP = "/tag";
        private const string DBG_MSG_FORMAT = "Total Missing Tags: {0}\n\nMissing Labels: {1}\nMissing IDs: {2}";
        private const string VERB_MSG_1 = "Some labels did not map to existing tags... ";
        private const string VERB_MSG_APP = "Creating them.";
        private const string VERB_MSG_DEC = "But we've been told not to create them.";

        private string _ep;
        private List<int> _ids;
        private bool _noCreateOnMissing;
        private bool _passThru;
        private bool _whatIfOnTag;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public ISupportsTagUpdate InputObject { get; set; }

        [Parameter(Mandatory = true, Position = 0)]
        public object[] Tag { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter DontCreateWhenMissing
        {
            get => _noCreateOnMissing;
            set => _noCreateOnMissing = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru
        {
            get => _passThru;
            set => _passThru = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            HashSet<object> objs = this.CheckForEnumerables(this.Tag);

            IEnumerable<Tag> existingTags = Context.TagManager.GetTags(objs, out HashSet<string> createStrs, out HashSet<int> missingIds);

            base.WriteFormatDebug(DBG_MSG_FORMAT, createStrs.Count + missingIds.Count, createStrs.Count, missingIds.Count);

            _ids = new List<int>(existingTags.Select(t => t.Id));

            if (_ids.Count != this.Tag.Length)
            {
                if (!_noCreateOnMissing)
                {
                    base.WriteVerbose(VERB_MSG_1 + VERB_MSG_APP);
                    foreach (string newLabel in createStrs)
                    {
                        if (base.FormatShouldProcess("Add", "Create New Tag: {0}", newLabel))
                            _ids.Add(Context.TagManager.AddNew(newLabel));
                        
                        else  // This will indicate that a WhatIf has been accomplished so as not to go any further.
                            _whatIfOnTag = true;
                        
                    }
                }
                else
                    base.WriteVerbose(VERB_MSG_1 + VERB_MSG_DEC);
            }
        }

        protected override void ProcessRecord()
        {
            if (_whatIfOnTag)
                return;

            _ep = this.InputObject.GetEndpoint();

            if (_ids.Count > 0 && base.FormatShouldProcess("Add", "Tags on Item: {0}", this.InputObject.Id))
            {
                base.WriteFormatVerbose("Adding {0} tags to item \"{1}\"", _ids.Count, this.InputObject.Id);

                this.InputObject.Tags.AddRange(_ids);
                base.WriteDebug(this.InputObject.ToJson());
                object updated = base.SendSonarrPut(_ep, this.InputObject, this.InputObject.GetType());
                if (_passThru)
                    base.SendToPipeline(updated);
            }
        }

        protected override void EndProcessing()
        {
            if (_ids.Count <= 0 && !_whatIfOnTag && !_noCreateOnMissing)
            {
                base.WriteError(new InvalidOperationException("No tags were found that could be added."), ErrorCategory.ObjectNotFound);
            }
        }

        #endregion

        private HashSet<object> CheckForEnumerables(object[] inputTags)
        {
            Type hType = typeof(IEnumerable<int>);
            HashSet<object> combined = new HashSet<object>(inputTags.Length);
            if (inputTags.Any(x => x is PSObject xpso && xpso.ImmediateBaseObject is IEnumerable<int>))
            {
                IEnumerable<object> innerInts = inputTags
                    .OfType<PSObject>()
                        .Where(pso => pso.ImmediateBaseObject is IEnumerable<int>)
                            .SelectMany(o => (IEnumerable<int>)o.ImmediateBaseObject)
                                .Cast<object>();

                combined.AddRange(innerInts);
            }
            if (inputTags.Any(x => x is PSObject xpso && xpso.ImmediateBaseObject is IConvertible))
            {
                combined.AddRange(inputTags
                    .OfType<PSObject>()
                        .Where(x => x.ImmediateBaseObject is IConvertible)
                            .Select(x => x.ImmediateBaseObject));
            }
            if (inputTags.Any(x => x is IEnumerable<int>))
            {
                combined.AddRange(inputTags.OfType<IEnumerable<int>>().SelectMany(x => x).Cast<object>());
            }

            combined.AddRange(inputTags.Where(x => x is IConvertible));
            return combined;
        }
    }
}