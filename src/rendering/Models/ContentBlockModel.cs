using Sitecore.LayoutService.Client.Response.Model.Fields;

namespace sitecore_netcore_auth.Models
{
    /// <summary>
    /// An example of binding to component fields (usually a serialized datasource item).
    /// </summary>
    public class ContentBlockModel
    {
        public TextField Title { get; set; }

        public RichTextField Text { get; set; }
    }
}
