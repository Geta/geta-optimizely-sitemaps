using EPiServer.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Wangkanai.Detection;

namespace AlloyTemplates.Business.Channels
{
    /// <summary>
    /// Defines the 'Web' content channel
    /// </summary>
    public class WebChannel : DisplayChannel
    {
        public override string ChannelName
        {
            get
            {
                return "web";
            }
        }

        //CMS-16684: ASPNET Core doesn't natively support checking device, we need to reimplement this
        public override bool IsActive(HttpContext context)
        {
            var detection = context.RequestServices.GetRequiredService<IDetection>();
            return detection.Device.Type == DeviceType.Desktop;
        }
}
}
