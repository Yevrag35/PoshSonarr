﻿using MG.Api.Rest.Generic;
using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Client;
using MG.Sonarr.Functionality.Strings;
using MG.Sonarr.Cmdlets;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Management.Automation;
using System.Threading.Tasks;

namespace MG.Sonarr
{
    /// <summary>
    /// The static context used by the PoshSonarr module.  The 'Context' is set with the <see cref="ConnectInstance"/> cmdlet.
    /// </summary>
    internal static partial class Context
    {
        #region FIELDS/CONSTANTS
        //internal const string SLASH_STR = "/";
        internal const char SLASH = (char)47;
        internal const string ZERO_ONE = "{0}{1}";

        private static string Prefix = string.Empty;

        #endregion

        #region PROPERTIES

        internal static QualityDictionary AllQualities { get; private set; }

        /// <summary>
        /// The main <see cref="HttpClient"/> from which all PoshSonarr cmdlets issue their REST requests.
        /// </summary>
        internal static ISonarrClient ApiCaller { get; private set; }

        internal static IndexerSchemaCollection IndexerSchemas { get; private set; }

        /// <summary>
        /// Returns true when <see cref="Context.ApiCaller"/> is not null, has a base address, and contains a valid API key header.
        /// </summary>
        internal static bool IsConnected => ApiCaller != null && ApiCaller.BaseAddress != null && ApiCaller.DefaultRequestHeaders.Contains("X-Api-Key");

        public static bool NoCache { get; private set; }

        /// <summary>
        /// The <see cref="SonarrUrl"/> representation of the base URL for all subsequent REST calls.
        /// </summary>
        internal static ISonarrUrl SonarrUrl { get; set; }

        internal static ITagManager TagManager { get; private set; }

        #endregion

        #region METHODS
        internal static PSObject GetConnectionStatus()
        {
            var pso = new PSObject();
            pso.Properties.Add(new PSNoteProperty("IsConnected", IsConnected));
            pso.Properties.Add(new PSNoteProperty("SonarrUrl", SonarrUrl?.Url));
            return pso;
        }
        internal static object Disinitialize(bool passThru)
        {
            ApiCaller?.Dispose();
            ApiCaller = null;
            Prefix = null;
            IndexerSchemas = null;
            NoCache = false;
            AllQualities = null;
            TagManager?.Dispose();
            TagManager = null;
            SonarrUrl = null;

            object o = null;
            if (passThru)
                o = GetConnectionStatus();

            return o;
        }
        private static string GetEndpoint(string endpoint) => !string.IsNullOrEmpty(Prefix) ? Prefix + endpoint : endpoint;

        internal static void Initialize(ISonarrClient client, bool useApiPrefix, bool useCache)
        {
            ApiCaller = client;

            if (IsConnected)
            {
                if (useApiPrefix)
                    Prefix = "/api";

                InitializeAsync(ApiCaller, useApiPrefix, useCache).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            else
                ApiCaller = null;
        }
        private async static Task InitializeAsync(ISonarrClient client, bool useApiPrefix, bool useCache)
        {
            if (client.IsAuthenticated)
            {
                Task<IndexerSchemaCollection> schemaTask = GetIndexerSchemasAsync(client);
                Task<QualityDictionary> defTask = GetQualityDictionaryAsync(client);
                Task<ITagManager> tagTask = SonarrFactory.GenerateTagManagerAsync(client, useApiPrefix);

                if (!History.IsInitialized())
                    History.Initialize();

                NoCache = !useCache;

                IndexerSchemas = await schemaTask;
                AllQualities = await defTask;
                TagManager = await tagTask;
            }
        }

        #region INITIALIZATION ASYNC METHODS
        private async static Task<QualityDictionary> GetQualityDictionaryAsync(ISonarrClient client)
        {
            IRestListResponse<QualityDefinition> defResponse = await client.GetAsJsonListAsync<QualityDefinition>(GetEndpoint(ApiEndpoints.QualityDefinitions));
            return !defResponse.IsFaulted ? new QualityDictionary(defResponse.Content.Select(x => x.Quality)) : null;
        }

        private async static Task<IndexerSchemaCollection> GetIndexerSchemasAsync(ISonarrClient client)
        {
            IRestListResponse<IndexerSchema> schResponse = await client.GetAsJsonListAsync<IndexerSchema>(GetEndpoint(ApiEndpoints.IndexerSchema));
            return !schResponse.IsFaulted ? IndexerSchemaCollection.FromSchemas(schResponse.Content) : null;
        }

        #endregion

        #endregion
    }
}