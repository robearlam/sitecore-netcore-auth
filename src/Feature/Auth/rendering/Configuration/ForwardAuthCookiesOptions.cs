using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace Feature.Auth.Rendering.Configuration
{
    public class ForwardAuthCookiesOptions
    {
        public IList<Action<HttpRequest, IDictionary<string, string[]>>> RequestCookies { get; set; }

        public ForwardAuthCookiesOptions()
        {
        }
    }
}
