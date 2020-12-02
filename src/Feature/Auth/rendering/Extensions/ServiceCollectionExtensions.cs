using Microsoft.Extensions.DependencyInjection;
using Sitecore.AspNet.RenderingEngine;
using Sitecore.AspNet.RenderingEngine.Configuration;
using Sitecore.AspNet.RenderingEngine.Extensions;
using Sitecore.LayoutService.Client.Request;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Feature.Auth.Rendering.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ISitecoreRenderingEngineBuilder ForwardAuthCookies(this ISitecoreRenderingEngineBuilder serviceBuilder , Action<ForwardHeadersOptions> options = null )
        {
            IServiceCollection services = serviceBuilder.Services;
            if (services.All(s => s.ServiceType != typeof(ForwardAuthCookiesMarkerService)))
            {
                services.Configure((Action<RenderingEngineOptions>)(renderingOptions => {
                    renderingOptions.MapToRequest((httpRequest, layoutRequest) =>
                    {
                        string authCookie = GetAuthCookieValue();
                        Dictionary<string, string[]> dictionary;
                        if (!layoutRequest.TryGetHeadersCollection(out dictionary))
                        {
                            dictionary = new Dictionary<string, string[]>()
                            {
                                { "Cookie", new string [] { authCookie } }
                            };
                            layoutRequest.Add("sc_request_headers_key", dictionary);
                        }
                        if (!dictionary.ContainsKey("Cookie"))
                        {
                            dictionary.Add("Cookie", new string[] { authCookie });
                        }
                        else
                        {
                            var cookies = dictionary["Cookie"].ToList();
                            cookies.Add(authCookie);
                            cookies.Reverse();      //auth cookie needs to be the first one ¯\_(ツ)_/¯
                            dictionary["Cookie"] = cookies.ToArray();
                        }
                    });
                }));

                services.AddSingleton<ForwardAuthCookiesMarkerService>();
            }
            return serviceBuilder;
        }

        private static string GetAuthCookieValue()
        {
            return ".AspNet.Cookies=7Ij19ARLqxD2XfIJGdVittgbtKs-1msPGvAN9iVg2j1AwnyGuG947sDxBvM6U1HX2HQfwKldwMur5PtResHdnCdxyNrJO8-qNIkN7r6usJl4HYgyid7REkZXsrfY31OogbflD2nGpR0lpSZtxw1WnnVBOjK_UrNFt0XThGRwbRa21XyZUlrC7YNyo0EqDKAHgoNdi8Mwz7LkSPwNmOdKzsZHXMZInoed3iwRRm38_SPg3OjeDO5oNyeKsJVDRON-LPB84UxZ9Ww70tTanVRSsTy1mbk_z9Glt5AOn7sDSKM-qUcY0ebc_R6dc2TptZNVGxY-sogq4TR4K0DWbbsJXROCf7SXYI0YKgWJhDkjYpUCIttz0vo3WqnmR0BhSiDB7J-31bP_md0d8Fu7A8McgotPDppyS9PM7DZMb_wAaIlCj90mthZY66nSlfLdxJAa3wSeCTfdECDUevR4XkmtCQ; Expires=Wed, 02 Dec 2020 03:46:21 GMT; Path=/; Secure; HttpOnly; SameSite=None; Domain=cm.sitecore_netcore_auth.localhost";
        }
    }
}