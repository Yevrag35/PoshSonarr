using MG.Posh.Extensions.Bound;
using MG.Posh.Extensions.Shoulds;
using MG.Posh.Extensions.Writes;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "Indexer", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    [OutputType(typeof(Indexer))]
    public class NewIndexer : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        //private RuntimeDefinedParameterDictionary _lib;

        #endregion

        #region PARAMETERS

        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        public IndexerTemplate InputObject { get; set; }

        #endregion

        #region DYNAMIC PARAMETERS
        //public object GetDynamicParameters()
        //{
        //    _lib = new RuntimeDefinedParameterDictionary();
        //    if (this.ContainsParameter(x => x.Schema))
        //    {
        //        IndexerSchema schema = this.GetChosenSchema(this.Schema);
        //        foreach (Field field in schema.Fields)
        //        {
        //            var rtParam = this.NewParameter(field);
        //            _lib.Add(field.GetLabelNoSpaces(), rtParam);
        //        }
        //    }

        //    return _lib;
        //}

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

        }

        protected override void ProcessRecord()
        {
            if (string.IsNullOrWhiteSpace(this.InputObject.Name))
            {
                this.WriteError("A new indexer must have a name.", typeof(ArgumentNullException), ErrorCategory.InvalidArgument, this.InputObject);
            }

            Indexer posted = base.SendSonarrPost<Indexer>(ApiEndpoints.Indexer, this.InputObject);
            base.SendToPipeline(posted);
        }

        protected override void EndProcessing()
        {

        }

        #endregion

        #region BACKEND METHODS

        #endregion
    }
}