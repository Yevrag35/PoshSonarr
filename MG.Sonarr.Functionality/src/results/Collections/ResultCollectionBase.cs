using MG.Api.Json;
using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MG.Sonarr.Results.Collections
{
    /// <summary>
    /// A base collection class exposing certain methods of <see cref="List{T}"/> like indexing, Find, FindAll, and other 
    /// methods while hiding the list manipulation methods.
    /// </summary>
    /// <remarks>
    ///     This collection model is also inherently serializable and deserializable into JSON.
    /// </remarks>
    /// <typeparam name="T">The .NET type of each element in the collection.</typeparam>
    public abstract class ResultCollectionBase<T> : IEnumerable<T>, IJsonResult
    {
        #region FIELDS/CONSTANTS
        /// <summary>
        /// The internal backing <see cref="List{T}"/> collection that all methods of <see cref="ResultCollectionBase{T}"/> invoke against.
        /// </summary>
        protected List<T> InnerList { get; }

        #endregion

        #region INDEXERS
        //public virtual T this[int index] => this.InnerList[index];

        #endregion

        #region PROPERTIES
        /// <summary>
        /// The number of elements contained in the <see cref="ResultCollectionBase{T}"/>.
        /// </summary>
        public int Count => this.InnerList.Count;

        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// The default constructor which initializes a new instance which is empty.
        /// </summary>
        public ResultCollectionBase() => this.InnerList = new List<T>();
        /// <summary>
        /// Initializes a new instance of <see cref="ResultCollectionBase{T}"/> with the specified capacity.
        /// </summary>
        /// <param name="capacity">The number of elements the collection can initially store.</param>
        public ResultCollectionBase(int capacity) => this.InnerList = new List<T>(capacity);
        /// <summary>
        /// Initializes a new instance of <see cref="ResultCollectionBase{T}"/> that contains elements copied from the specified
        /// collection and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="items">The collection whose items are copied into the <see cref="ResultCollectionBase{T}"/>.</param>
        public ResultCollectionBase(IEnumerable<T> items)
        {
            if (items == null)
                this.InnerList = new List<T>();

            else
                this.InnerList = new List<T>(items);
        }

        #endregion

        #region ENUMERATORS
        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="ResultCollectionBase{T}"/>.
        /// </summary>
        /// <returns>
        ///      A <see cref="IEnumerator{T}"/> for the <see cref="ResultCollectionBase{T}"/>.
        /// </returns>
        public IEnumerator<T> GetEnumerator() => this.InnerList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.InnerList.GetEnumerator();

        #endregion

        #region METHODS
        /// <summary>
        /// Determines whether an element is in the <see cref="ResultCollectionBase{T}"/>.
        /// </summary>
        /// <param name="item">
        /// The object to locate in the <see cref="ResultCollectionBase{T}"/>.  The value can be null for reference types.
        /// </param>
        public virtual bool Contains(T item) => this.InnerList.Contains(item);
        /// <summary>
        ///     Determines whether the <see cref="ResultCollectionBase{T}"/> contains elements that
        ///     match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">
        ///     The <see cref="Predicate{T}"/> delegate that defines the conditions of the 
        ///     elements to search for.
        /// </param>
        /// <returns>
        /// <see langword="true"/>:
        ///     if the <see cref="ResultCollectionBase{T}"/> contains one or more elements that
        ///     <paramref name="match"/> defined.
        /// <see langword="false"/>:
        ///     otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="match"/> is <see langword="null"/>.</exception>
        public bool Contains(Predicate<T> match) => this.InnerList.Exists(match);
        /// <summary>
        ///     Searches for an element that matches the conditions defined by the specified
        ///     predicate, and returns the first occurrence within the entire <see cref="ResultCollectionBase{T}"/>.
        /// </summary>
        /// <param name="match">
        ///     The <see cref="Predicate{T}"/> delegate that defines the conditions of the
        ///     elements to search for.
        /// </param>
        /// <returns>
        ///     The first element that matches the conditions if found; otherwise the default value for <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="match"/> is <see langword="null"/>.</exception>
        public T Find(Predicate<T> match) => this.InnerList.Find(match);
        /// <summary>
        /// Retrieves all of the elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">The <see cref="Predicate{T}"/> delegate the defines the conditions of the elements to search for.</param>
        /// <returns>
        /// A <see cref="List{T}"/> containing all of the elements that match the conditions if found; otherwise, an empty <see cref="List{T}"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="match"/> is <see langword="null"/>.</exception>
        protected List<T> FindAllAsList(Predicate<T> match) => this.InnerList.FindAll(match);
        /// <summary>
        /// Retrieves all of the elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">The <see cref="Predicate{T}"/> delegate the defines the conditions of the elements to search for.</param>
        /// <returns>
        ///     An <see cref="IEnumerable{T}"/> collection containing all of the elements that match the conditions if found; 
        ///     otherwise, an empty collection.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="match"/> is <see langword="null"/>.</exception>
        public IEnumerable<T> FindAll(Predicate<T> match) => this.FindAllAsList(match);
        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first occurrence
        /// within the entire <see cref="ResultCollectionBase{T}"/>.
        /// </summary>
        /// <param name="item">
        ///     The object to locate in the <see cref="ResultCollectionBase{T}"/>.  
        ///     The value can be <see langword="null"/> for reference types.
        /// </param>
        public int IndexOf(T item) => this.InnerList.IndexOf(item);
        public virtual string ToJson()
        {
            var converter = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DefaultValueHandling = DefaultValueHandling.Populate,
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Error
            };
            converter.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
            return JsonConvert.SerializeObject(this, converter);
        }
        string IJsonObject.ToJson(IDictionary parameters)
        {
            var camel = new CamelCasePropertyNamesContractResolver();
            var cSerialize = new JsonSerializer
            {
                ContractResolver = camel
            };

            var serializer = new JsonSerializerSettings
            {
                ContractResolver = camel,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DefaultValueHandling = DefaultValueHandling.Populate,
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Error
            };
            serializer.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));

            var job = JObject.FromObject(this, cSerialize);

            string[] keys = parameters.Keys.Cast<string>().ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                job.Add(key, JToken.FromObject(parameters[key], cSerialize));
            }

            return JsonConvert.SerializeObject(job, serializer);
        }
        /// <summary>
        /// Determines whether every element in the <see cref="ResultCollectionBase{T}"/> matches the conditions
        /// defined by the specified predicate.
        /// </summary>
        /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions to check against the elements.</param>
        /// <returns>
        /// <see langword="true"/>: if every element in the list matches the conditions defined; 
        /// otherwise, <see langword="false"/>.
        /// If the list has no elements, the return value is <see langword="true"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="match"/> is <see langword="null"/>.</exception>
        public bool TrueForAll(Predicate<T> match) => this.InnerList.TrueForAll(match);
        /// <summary>
        /// Escapes and changes the all wildcard characters in a given string to their Regex equivalents.
        /// </summary>
        /// <param name="wildcardString">The wildcard string to transform.</param>
        /// <returns>An properly formatted Regex string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="wildcardString"/> is <see langword="null"/>.</exception>
        protected string WildcardStringToRegex(string wildcardString)
        {
            return string.Format(
                "^{0}$", 
                Regex.Escape(wildcardString.Replace("{", "{{").Replace("}", "}}"))
                    .Replace("\\*", ".*")
                    .Replace("\\?", "."));
        }

        #endregion
    }
}
