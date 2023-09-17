using MG.Sonarr.Functionality;
using MG.Sonarr.Results.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [Serializable]
    public class FieldCollection : ResultListBase<Field>, IEnumerable<IField>
    {
        #region INDEXERS
        /// <summary>
        /// Gets the <see cref="Field"/> whose name matches the specified string.
        /// </summary>
        /// <param name="settingName">The case-insensitive name of the field's name to retrieve.</param>
        /// <returns>The <see cref="Field"/> whose name matches <paramref name="settingName"/>.</returns>
        public Field this[string settingName] => base.InnerList.Find(x => x.Name.Equals(settingName, StringComparison.CurrentCultureIgnoreCase));

        #endregion

        #region CONSTRUCTORS
        [JsonConstructor]
        internal FieldCollection(IEnumerable<Field> items) : base(items) { }

        internal FieldCollection(IEnumerable<IField> items) : base(items.Select(x => new Field(x))) { }

        #endregion

        #region PUBLIC METHODS
        IEnumerator<IField> IEnumerable<IField>.GetEnumerator()
        {
            foreach (IField field in base.InnerList)
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
        public IList<IField> GetSettingByType(params string[] types)
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
        /// <summary>
        /// Copies the fields of the <see cref="FieldCollection"/> to a new single-dimensional array.
        /// </summary>
        /// <returns>An array containing copies of the <see cref="IField"/> of the <see cref="FieldCollection"/>.</returns>
        public IField[] ToArray() => base.InnerList.ToArray();

        #endregion
    }
}
