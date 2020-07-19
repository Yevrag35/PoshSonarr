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

        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 1)]
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

        }

        protected override void EndProcessing()
        {

        }

        #endregion

        #region BACKEND METHODS
        //private IndexerSchema GetChosenSchema(string schemaName) => Context.IndexerSchemas[schemaName];

        //private RuntimeDefinedParameter NewParameter(Field field)
        //{
        //    var col = new Collection<Attribute>()
        //    {
        //        new ParameterAttribute { Mandatory = true }
        //    };

        //    if (field.Type == FieldType.Select)
        //    {
        //        col.Add(new ValidateSetAttribute(field.SelectOptions.Select(x => x.Name).ToArray()));
        //    }

        //    return new RuntimeDefinedParameter(field.GetLabelNoSpaces(), field.GetDotNetTypeFromFieldType(), col);
        //}

        #endregion
    }
}