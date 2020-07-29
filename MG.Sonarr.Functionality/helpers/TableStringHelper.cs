using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality.Helpers
{
    public abstract class TableStringHelper
    {
        //public IReadOnlyList<object> AllValues { get; private set; }
        //public IDictionary<string, object> 
        protected abstract string[] Keys { get; set; }

        protected TableStringHelper(object[] input)
        {

        }

        //protected virtual void ModifyObject<T>(ISet<T> set)
        protected virtual void ProcessObject(params object[] o)
        {

        }
    }
}
