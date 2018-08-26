using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Endpoints
{
    public class Parameter : IEquatable<Parameter>
    {
        private readonly string _name;
        private readonly Type _type;
        private readonly object _val;

        public string Name => _name;
        public Type Type => _type;
        public string Value => Convert.ToString(_val);

        public bool Equals(Parameter other)
        {
            var peq = new ParameterEquality();
            return peq.Equals(this, other) ? true : false;
        }

        public Parameter(string name, Type type, object value)
        {
            _name = name;
            _type = type;
            _val = value;
        }
    }

    internal class ParameterEquality : EqualityComparer<Parameter>
    {
        public override bool Equals(Parameter x, Parameter y) =>
            x.Name == y.Name && x.Type.Equals(y.Type) ? true : false;
        public override int GetHashCode(Parameter obj) => throw new NotImplementedException();
    }
}
