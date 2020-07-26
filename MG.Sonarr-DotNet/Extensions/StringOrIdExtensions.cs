using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;
using System.Reflection;

namespace MG.Sonarr.Extensions
{
    public static class StringOrIdExtensions
    {
        private const string REGEX_PATTERN = "^.+\\s+(?:\\-{0}|\\\"|\\')";

        private static string GetInvocation(PSCmdlet psCmdlet)
        {
            return psCmdlet.MyInvocation.Line.Substring(psCmdlet.MyInvocation.OffsetInLine - 1);
        }
        private static bool Matches(PSCmdlet psCmdlet)
        {
            Type bpt = psCmdlet.MyInvocation.BoundParameters.GetType();
            PropertyInfo pi = bpt.GetProperty("BoundPositionally");
            if (pi?.GetValue(psCmdlet.MyInvocation.BoundParameters) is List<string> positionParams && positionParams.Count > 0)
            {
                return false;
            }
            else
                return true;
        }
        public static void ProcessStringOrIdParameter<T>(this T psCmdlet, object[] objs,
            ISet<string> names, ISet<int> ids)
            where T : PSCmdlet
        {
            if (objs == null)
                return;

            else if (Matches(psCmdlet))
            {
                foreach (IConvertible ic in objs)
                {
                    names.Add(Convert.ToString(ic));
                }
            }
            else
            {
                for (int i = 0; i < objs.Length; i++)
                {
                    object o = objs[i];
                    if (o is IConvertible icon 
                        && 
                        int.TryParse(Convert.ToString(icon), out int outInt))
                    {
                        ids.Add(outInt);
                    }
                    else if (o is string oStr)
                        names.Add(oStr);
                }
            }
        }
    }
}
