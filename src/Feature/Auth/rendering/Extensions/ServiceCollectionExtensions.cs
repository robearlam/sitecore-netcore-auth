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
                        Dictionary<string, string[]> dictionary;
                        if (layoutRequest.TryGetHeadersCollection(out dictionary) && 
                            dictionary.ContainsKey("Cookie") && 
                            dictionary["Cookie"].Any(x => x.StartsWith(".AspNet.Cookies=")))
                        {
                            var authCookie = dictionary["Cookie"].FirstOrDefault(x => x.StartsWith(".AspNet.Cookies="));
                            var cookies = dictionary["Cookie"].Where(x => !x.StartsWith(".AspNet.Cookies=")).ToList();
                            cookies.Add(System.Net.WebUtility.UrlDecode(authCookie));
                            cookies.Reverse();      //auth cookie needs to be the first one ¯\_(ツ)_/¯
                            dictionary["Cookie"] = cookies.ToArray();
                        }
                    });
                }));

                services.AddSingleton<ForwardAuthCookiesMarkerService>();
            }
            return serviceBuilder;
        }
    }
}