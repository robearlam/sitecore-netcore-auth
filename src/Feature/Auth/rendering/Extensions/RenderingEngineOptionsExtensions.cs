using Sitecore.AspNet.RenderingEngine.Configuration;
using Sitecore.AspNet.RenderingEngine.Extensions;

namespace Feature.Auth.Rendering.Extensions
{
    public static class RenderingEngineOptionsExtensions
    {
        public static RenderingEngineOptions AddFeatureAuth(this RenderingEngineOptions options)
        {
            options.AddModelBoundView<Models.Login>("Login");
            return options;
        }
    }
}