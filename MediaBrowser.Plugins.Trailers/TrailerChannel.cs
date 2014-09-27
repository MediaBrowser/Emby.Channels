using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers
{
    public class TrailerChannel : IChannel, IIndexableChannel, ISupportsLatestMedia
    {
        public string DataVersion
        {
            get
            {
                return "16";
            }
        }

        public string Description
        {
            get { return string.Empty; }
        }

        private DateTime GetDateCreatedForSorting(ChannelItemInfo item)
        {
            var date = item.DateCreated;

            if (date.HasValue)
            {
                // Strip out the time portion in case they were all added at the same time
                // This will allow premiere date to take over
                return date.Value.Date;
            }

            return DateTime.MinValue;
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(query.FolderId))
            {
                return GetTopCategories();
            }

            var idParts = query.FolderId.Split('|');
            var contentType = (ChannelMediaContentType)Enum.Parse(typeof(ChannelMediaContentType), idParts[0], true);

            if (idParts.Length == 1)
            {
                return GetCategories(contentType);
            }

            var trailerType = TrailerType.ComingSoonToTheaters;
            ExtraType extraType;

            if (string.Equals(idParts[1], "TrailerComingSoonToTheaters", StringComparison.OrdinalIgnoreCase))
            {
                extraType = ExtraType.Trailer;
                trailerType = TrailerType.ComingSoonToTheaters;
            }
            else if (string.Equals(idParts[1], "TrailerComingSoonToDvd", StringComparison.OrdinalIgnoreCase))
            {
                extraType = ExtraType.Trailer;
                trailerType = TrailerType.ComingSoonToDvd;
            }
            else if (string.Equals(idParts[1], "TrailerArchive", StringComparison.OrdinalIgnoreCase))
            {
                extraType = ExtraType.Trailer;
                trailerType = TrailerType.Archive;
            }
            else
            {
                extraType = (ExtraType)Enum.Parse(typeof(ExtraType), idParts[1], true);
            }

            var items = await GetChannelItems(contentType, extraType, trailerType, cancellationToken).ConfigureAwait(false);

            if (query.SortBy.HasValue)
            {
                if (query.SortDescending)
                {
                    switch (query.SortBy.Value)
                    {
                        case ChannelItemSortField.Runtime:
                            items = items.OrderByDescending(i => i.RunTimeTicks ?? 0);
                            break;
                        case ChannelItemSortField.PremiereDate:
                            items = items.OrderByDescending(i => i.PremiereDate ?? DateTime.MinValue);
                            break;
                        case ChannelItemSortField.DateCreated:
                            items = items.OrderByDescending(GetDateCreatedForSorting)
                                .ThenByDescending(i => i.PremiereDate ?? DateTime.MinValue);
                            break;
                        case ChannelItemSortField.CommunityRating:
                            items = items.OrderByDescending(i => i.CommunityRating ?? 0);
                            break;
                        default:
                            items = items.OrderByDescending(i => i.Name);
                            break;
                    }
                }
                else
                {
                    switch (query.SortBy.Value)
                    {
                        case ChannelItemSortField.Runtime:
                            items = items.OrderBy(i => i.RunTimeTicks ?? 0);
                            break;
                        case ChannelItemSortField.PremiereDate:
                            items = items.OrderBy(i => i.PremiereDate ?? DateTime.MinValue);
                            break;
                        case ChannelItemSortField.DateCreated:
                            items = items.OrderBy(GetDateCreatedForSorting)
                                .ThenByDescending(i => i.PremiereDate ?? DateTime.MinValue);
                            break;
                        case ChannelItemSortField.CommunityRating:
                            items = items.OrderBy(i => i.CommunityRating ?? 0);
                            break;
                        default:
                            items = items.OrderBy(i => i.Name);
                            break;
                    }
                }
            }

            return new ChannelItemResult
            {
                Items = items.ToList()
            };
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetChannelItems(ChannelMediaContentType contentType, ExtraType extraType, TrailerType trailerType, CancellationToken cancellationToken)
        {
            var providers = EntryPoint.Instance.Providers
                .Where(i => i.ContentType == contentType && i.ExtraType == extraType && i.TrailerType == trailerType)
                .ToList();

            var tasks = providers.Select(async i =>
            {
                try
                {
                    return await i.GetChannelItems(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    return new List<ChannelItemInfo>();
                }
            });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            return results.SelectMany(i => i);
        }

        public ChannelItemResult GetTopCategories()
        {
            var list = new List<ChannelItemInfo>();

            list.Add(new ChannelItemInfo
            {
                FolderType = ChannelFolderType.Container,
                Name = "Movies",
                Type = ChannelItemType.Folder,
                MediaType = ChannelMediaType.Video,
                Id = ChannelMediaContentType.MovieExtra.ToString().ToLower(),
                ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/thumb.jpg"
            });

            return new ChannelItemResult
            {
                Items = list,
                TotalRecordCount = list.Count
            };
        }

        public ChannelItemResult GetCategories(ChannelMediaContentType contentType)
        {
            var list = new List<ChannelItemInfo>();

            list.Add(new ChannelItemInfo
            {
                FolderType = ChannelFolderType.Container,
                Name = "Coming Soon to Theaters",
                Type = ChannelItemType.Folder,
                MediaType = ChannelMediaType.Video,
                Id = contentType.ToString().ToLower() + "|" + "TrailerComingSoonToTheaters",

                ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/thumb.jpg"
            });

            list.Add(new ChannelItemInfo
            {
                FolderType = ChannelFolderType.Container,
                Name = "Coming Soon to Dvd",
                Type = ChannelItemType.Folder,
                MediaType = ChannelMediaType.Video,
                Id = contentType.ToString().ToLower() + "|" + "TrailerComingSoonToDvd",

                ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/thumb.jpg"
            });

            list.Add(new ChannelItemInfo
            {
                FolderType = ChannelFolderType.Container,
                Name = "Archive",
                Type = ChannelItemType.Folder,
                MediaType = ChannelMediaType.Video,
                Id = contentType.ToString().ToLower() + "|" + "TrailerArchive",

                ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/thumb.jpg"
            });

            return new ChannelItemResult
            {
                Items = list,
                TotalRecordCount = list.Count
            };
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Primary:
                case ImageType.Thumb:
                    {
                        var path = GetType().Namespace + ".Images." + type.ToString().ToLower() + ".jpg";

                        return Task.FromResult(new DynamicImageResponse
                        {
                            Format = ImageFormat.Jpg,
                            HasImage = true,
                            Stream = GetType().Assembly.GetManifestResourceStream(path)
                        });
                    }
                default:
                    throw new ArgumentException("Unsupported image type: " + type);
            }
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>
            {
                ImageType.Thumb,
                ImageType.Primary
            };
        }

        public string HomePageUrl
        {
            get { return "http://mediabrowser3.com"; }
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public string Name
        {
            get { return "Trailers"; }
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                ContentTypes = new List<ChannelMediaContentType>
                 {
                     ChannelMediaContentType.MovieExtra
                 },

                MediaTypes = new List<ChannelMediaType>
                  {
                       ChannelMediaType.Video
                  },

                SupportsSortOrderToggle = true,

                DefaultSortFields = new List<ChannelItemSortField>
                   {
                        ChannelItemSortField.CommunityRating,
                        ChannelItemSortField.Name,
                        ChannelItemSortField.DateCreated,
                        ChannelItemSortField.PremiereDate,
                        ChannelItemSortField.Runtime
                   }
            };
        }

        public Task<ChannelItemResult> GetAllMedia(InternalAllChannelMediaQuery query, CancellationToken cancellationToken)
        {
            return GetChannelItems(new InternalChannelItemQuery
            {
                UserId = query.UserId

            }, cancellationToken);
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetLatestMedia(ChannelLatestMediaSearch request, CancellationToken cancellationToken)
        {
            var result = await GetChannelItems(new InternalChannelItemQuery
            {
                SortBy = ChannelItemSortField.DateCreated,
                SortDescending = true,
                UserId = request.UserId,
                FolderId = ChannelMediaContentType.MovieExtra.ToString().ToLower() + "|" + "TrailerComingSoonToTheaters"

            }, cancellationToken).ConfigureAwait(false);

            return result.Items;
        }
    }
}
