using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Channels.IPTV.Configuration;
using ServiceStack;


namespace MediaBrowser.Channels.IPTV
{
    [Route("/Notification/NotifyMyAndroid/Test/{UserID}", "POST", Summary = "Tests NotifyMyAndroid")]
    public class VideoSend : IReturnVoid
    {
        [ApiMember(Name = "Name", Description = "Name", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Name { get; set; }

        [ApiMember(Name = "Path", Description = "Path", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Path { get; set; }

        [ApiMember(Name = "ImagePath", Description = "ImagePath", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string ImagePath { get; set; }
    }

    class ServerApiEndpoints : IRestfulService
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public ServerApiEndpoints(ILogManager logManager, IHttpClient httpClient)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _httpClient = httpClient;
        }

        public object Post(VideoSend request)
        {
            Plugin.Instance.Configuration.streams.Add(new Streams
            {
                Name = request.Name,
                Image = request.ImagePath,
                URL = request.Path,
                Type = "HTTP"
            });

            

            return "added";
        }
    }
}
