using MG.Dynamic;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "IndexerTemplate")]
    [OutputType(typeof(IndexerTemplate))]
    public class GetIndexerTemplate : BaseSonarrCmdlet, IDynamicParameters
    {
        #region FIELDS/CONSTANTS
        private DynamicLibrary _lib;
        private const string PARAM = "Schema";

        #endregion

        #region PARAMETERS


        #endregion

        #region DYNAMIC
        public object GetDynamicParameters()
        {
            _lib = new DynamicLibrary();
            if (Context.IsConnected)
            {
                var idp = new DynamicParameter<IndexerSchema>(PARAM, true, Context.IndexerSchemas, x => x.Name)
                {
                    Mandatory = true,
                    Position = 0
                };
                _lib.Add(idp);
            }
            return _lib;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

        }

        protected override void ProcessRecord()
        {
            IEnumerable<IndexerSchema> chosen = _lib.GetUnderlyingValues<IndexerSchema>(PARAM);
            foreach (IndexerSchema schema in chosen)
            {
                var template = new IndexerTemplate(schema);
                base.WriteObject(template);
            }
        }

        protected override void EndProcessing()
        {

        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}