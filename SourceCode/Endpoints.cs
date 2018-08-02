using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Sonarr.Api
{
    namespace Endpoints
    {
        public interface IGet
        {
            //WebRequest Get();
        }

        public interface IPost
        {
            //WebRequest Post();
        }

        public interface IDelete
        {
            //WebRequest Delete();
        }

        public interface IEndpoint
        {
            string Definition { get; }
            string[] SupportedMethods { get; }
        }

        public class Calendar : IEndpoint, IGet
        {
            private const string _ep = "/api/calendar";
            private string[] _sms = new[] { WebRequestMethods.Http.Get };
            public string Definition { get { return _ep; } }
            public string[] SupportedMethods { get { return _sms; } }

            //public Calendar(Auth
            //{

            //}

            // Methods for Calendar
            //public WebRequest Get()
            //{

            //}
        }

        public class Command : IEndpoint, IGet, IPost
        {
            private const string _ep = "/api/command";
            private string[] _sms = new[] { WebRequestMethods.Http.Get, WebRequestMethods.Http.Post };
            public string Definition { get { return _ep; } }
            public string[] SupportedMethods { get { return _sms; } }

            //public WebRequest Get() { }
        }
    }
}
