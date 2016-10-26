using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Plugins.Trailers.Api
{
    [Route("/Trailers/Dump", "GET")]
    public class GetList : IReturn<List<ChannelItemInfo>>
    {
    }
    
    public class TrailerService : IService
    {
        public async Task<object> Get(GetList request)
        {
            var result = await TrailerChannel.Instance.GetAllItems(true, CancellationToken.None).ConfigureAwait(false);

            return result.ToList();
        }
    }
}
