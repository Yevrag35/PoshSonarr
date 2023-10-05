using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr
{
    public class NameOrId
    {
        #region FIELDS/CONSTANTS
        private string _name;
        private long _id;

        #endregion

        #region PROPERTIES
        public bool IsId { get; }
        public bool IsName { get; }

        #endregion

        #region CONSTRUCTORS
        private NameOrId(string name)
        {
            if (long.TryParse(name, out long result))
            {
                _id = result;
                this.IsId = true;
            }
            else
            {
                _name = name;
                this.IsName = true;
            }
        }
        private NameOrId(long id)
        {
            this.IsId = true;
            _id = id;
        }
        private NameOrId(int id)
            : this(Convert.ToInt64(id)) { }

        #endregion

        #region PUBLIC METHODS
        public static implicit operator NameOrId(string s) => new NameOrId(s);
        public static implicit operator NameOrId(int id) => new NameOrId(id);
        public static implicit operator NameOrId(long id) => new NameOrId(id);
        public static explicit operator string(NameOrId noi) => noi._name;
        public static explicit operator long(NameOrId noi) => noi._id;

        #endregion
    }
}