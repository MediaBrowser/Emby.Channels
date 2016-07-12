using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.SoundCloud.ClientApi
{
    public class PagingInfo
    {
        public int PageSize { get; private set; }
        public int PageIndex { get; private set; }

        public PagingInfo(int pageSize, int pageIndex)
        {
            this.PageSize = pageSize;
            this.PageIndex = pageIndex;
        }
    }
}
