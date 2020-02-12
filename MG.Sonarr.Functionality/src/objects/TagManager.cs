using MG.Api.Json.Extensions;
using MG.Api.Rest;
using MG.Api.Rest.Generic;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MG.Sonarr
{
    public class TagManager : IDisposable
    {
        #region FIELDS/CONSTANTS
        private bool _disposed;

        public const string Endpoint = "/tag";
        private const string ID_END = Endpoint + "/{0}";
        private SonarrRestClient _client;

        #endregion

        #region PROPERTIES
        public TagCollection AllTags { get; private set; }

        #endregion

        #region CONSTRUCTORS
        public TagManager(SonarrRestClient restClient)
        {
            if (restClient.IsAuthenticated)
            {
                _client = restClient;
                this.LoadTags();
            }
            else
                throw new ArgumentException("The specified rest client is not properly authenticated.");
        }

        #endregion

        #region PUBLIC METHODS
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #region TAG LOCATION METHODS

        public bool Exists(int tagId) => this.AllTags.Contains(tagId);
        public bool Exists(string tagLabel, StringComparison comparison = StringComparison.CurrentCulture) =>
            this.AllTags.Contains(x => x.Label.Equals(tagLabel, comparison));

        public int GetId(string tagLabel, StringComparison comparison = StringComparison.CurrentCulture) =>
            this.GetTag(tagLabel, comparison).Id;
        public string GetLabel(int tagId) => this.GetTag(tagId)?.Label;

        public Tag GetTag(int id) => this.AllTags.Find(x => x.Id == id);
        public Tag GetTag(object idOrLabel)
        {
            Tag result = null;
            if (idOrLabel is IConvertible icon)
            {
                string idStr = Convert.ToString(icon);
                if (int.TryParse(idStr, out int tryInt) && this.TryGetTag(tryInt, out Tag outTag))
                {
                    result = outTag;
                }
                else if (this.TryGetTag(idStr, StringComparison.CurrentCultureIgnoreCase, out Tag outTag2))
                {
                    result = outTag2;
                }
            }
            return result;
        }
        public Tag GetTag(string label, StringComparison comparison = StringComparison.CurrentCulture) => 
            this.AllTags.Find(x => x.Label.Equals(label, comparison));

        public IEnumerable<Tag> GetTags(IEnumerable<int> ids)
        {
            foreach (int id in ids)
            {
                if (this.TryGetTag(id, out Tag outTag))
                    yield return outTag;
            }
        }
        public IEnumerable<Tag> GetTags(IEnumerable<string> labels)
        {
            foreach (string lbl in labels)
            {
                if (this.TryGetTag(lbl, StringComparison.CurrentCultureIgnoreCase, out Tag outTag))
                    yield return outTag;
            }
        }
        public List<Tag> GetTags(IEnumerable<object> possibles, out HashSet<string> nonMatchingStrs, out HashSet<int> nonMatchingIds)
        {
            nonMatchingIds = new HashSet<int>();
            nonMatchingStrs = new HashSet<string>();
            var found = new List<Tag>();
            //Type hType = typeof(IEnumerable<int>);
            //HashSet<object> combined = null;
            //if (possibles.Any(x => x is IEnumerable<int> || (x is PSObject ))
            //{
            //    IEnumerable<int> innerInts = possibles.OfType<IEnumerable<int>>().SelectMany(x => x);
            //    possibles = possibles.Where(x => !x.GetType().Equals(hType)).Concat(innerInts.Cast<object>());
            //    combined = new HashSet<object>(possibles);
            //}

            foreach (object possible in possibles)
            {
                if (possible is IConvertible icon)
                {
                    string idStr = Convert.ToString(icon);
                    if (int.TryParse(idStr, out int intRes))
                    {
                        if (this.TryGetTag(intRes, out Tag outTag))
                            found.Add(outTag);

                        else
                            nonMatchingIds.Add(intRes);
                    }
                    else if (this.TryGetTag(idStr, StringComparison.CurrentCultureIgnoreCase, out Tag outTag2))
                        found.Add(outTag2);

                    else
                        nonMatchingStrs.Add(idStr);
                }
            }
            return found;
        }
        //private void _GetTagWithId(IEnumerable<int> checkIds)
        //{

        //}

        public bool TryGetId(string tagLabel, out int tagId)
        {
            tagId = -1;
            int? possible = this.GetTag(tagLabel)?.Id;
            if (possible.HasValue)
            {
                tagId = possible.Value;
                return true;
            }
            else
                return false;
        }
        public bool TryGetLabel(int tagId, out string tagLabel)
        {
            tagLabel = null;
            Tag maybe = this.GetTag(tagId);
            if (maybe != null)
            {
                tagLabel = maybe.Label;
                return true;
            }
            else
                return false;
        }
        public bool TryGetTag(string tagLabel, StringComparison comparison, out Tag outTag)
        {
            outTag = this.GetTag(tagLabel, comparison);
            return outTag != null;
        }
        public bool TryGetTag(int tagId, out Tag outTag)
        {
            outTag = this.GetTag(tagId);
            return outTag != null;
        }
        public bool TryGetTag(object tagObj, out Tag outTag)
        {
            outTag = this.GetTag(tagObj);
            return outTag != null;
        }

        #endregion

        #region TAG MANAGEMENT METHODS
        public int AddNew(string label)
        {
            if (this.TryGetTag(label, StringComparison.CurrentCultureIgnoreCase, out Tag outTag))
                return outTag.Id;
            
            else
            {
                return this.AllTags.Add(this.CreateTag(label));
            }
        }
        public Tag Edit(int id, string newLabel)
        {
            Tag resultTag = null;
            if (this.TryGetTag(id, out Tag outTag))
            {
                this.AllTags.SetTag(id, newLabel);
                resultTag = this.EditTag(outTag);
            }
            return resultTag;
        }
        public bool Remove(int id)
        {
            if (this.TryGetTag(id, out Tag outTag) && this.RemoveTag(outTag))
                return this.AllTags.Remove(outTag);
            
            else
                return false;
        }

        #endregion

        #endregion

        #region BACKEND/PRIVATE METHODS
        private Tag CreateTag(TagNew newTag)
        {
            IRestResponse<Tag> response = _client.PostAsJsonAsync<Tag>(new Uri(Endpoint, UriKind.Relative), newTag).GetAwaiter().GetResult();
            return this.ProcessResponse(response);
        }
        private Tag EditTag(Tag tag)
        {
            IRestResponse<Tag> response = _client.PutAsJsonAsync<Tag>(new Uri(Endpoint, UriKind.Relative), tag).GetAwaiter().GetResult();
            return this.ProcessResponse(response);
        }
        private bool RemoveTag(Tag tag)
        {
            IRestResponse response = _client.DeleteAsJsonAsync(new Uri(string.Format(ID_END, tag.Id), UriKind.Relative)).GetAwaiter().GetResult();
            this.ProcessResponse(response);
            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _client.Dispose();
            }
            _disposed = true;
        }
        private void LoadTags() => this.AllTags = this.LoadTagsAsync().GetAwaiter().GetResult();
        private async Task<TagCollection> LoadTagsAsync()
        {
            IRestListResponse<Tag> response = await _client.GetAsJsonListAsync<Tag>(new Uri(Endpoint, UriKind.Relative)).ConfigureAwait(false);
            return new TagCollection(this.ProcessResponse(response));
        }

        private void ProcessResponse(IRestResponse response)
        {
            if (!response.IsFaulted)
                return;

            else if (response.IsFaulted && response.HasException)
                throw response.Exception;

            else
                throw new InvalidOperationException("An unknown error occurred.");
        }
        private Tag ProcessResponse(IRestResponse<Tag> response)
        {
            if (!response.IsFaulted)
                return response.Content;

            else if (response.IsFaulted && response.HasException)
                throw response.Exception;

            else
                throw new InvalidOperationException("An unknown error occurred.");
        }
        private List<Tag> ProcessResponse(IRestListResponse<Tag> response)
        {
            if (!response.IsFaulted)
                return response.Content;

            else if (response.IsFaulted && response.HasException)
                throw response.Exception;

            else
                throw new InvalidOperationException("An unknown error occurred.");
        }

        #endregion
    }
}