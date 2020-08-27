using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Functionality.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [Serializable]
    public class FieldCollection : SortedListBase<Field>, IReadOnlyList<Field>, IReadOnlyList<IField>
    {
        #region INDEXERS
        public Field this[int index]
        {
            get
            {
                int posIndex = this.GetPositiveIndex<Field>(index);
                return posIndex > -1 ? base.InnerList.Values[posIndex] : null;
            }
        }
        IField IReadOnlyList<IField>.this[int index] => this[index];

        #endregion

        #region CONSTRUCTORS
        [JsonConstructor]
        internal FieldCollection(IEnumerable<Field> items)
            : base(items, x => x.Name)
        {
        }

        internal FieldCollection(IEnumerable<IField> items)
            : base(items.Select(f => new Field(f)), x => x.Name)
        {
        }

        #endregion

        #region PUBLIC METHODS
        IEnumerator<IField> IEnumerable<IField>.GetEnumerator()
        {
            foreach (IField field in base.InnerList.Values)
            {
                yield return field;
            }
        }
        /// <summary>
        /// Retrieves a list of fields by the specified types.
        /// </summary>
        /// <param name="types">The types of fields to retrieve from the collection.</param>
        /// <returns>
        ///     A list of fields where the type matches one of the values in <paramref name="types"/>.
        ///     If <paramref name="types"/> is empty or <see langword="null"/>, then an empty list is returned.
        /// </returns>
        public IList<IField> GetSettingByType(params FieldType[] types)
        {
            if (types == null || types.Length <= 0)
                return new List<IField>();

            var list = new List<IField>(this.Count);
            foreach (IField field in this)
            {
                if (types.Contains(field.Type))
                    list.Add(field);
            }
            return list;
        }

        #endregion
    }
}
