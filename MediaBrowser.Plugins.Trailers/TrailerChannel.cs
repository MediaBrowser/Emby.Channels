using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.Trailers.Listings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers
{
    public class TrailerChannel : IChannel, IIndexableChannel, ISupportsLatestMedia, IHasCacheKey
    {
        public static TrailerChannel Instance;
        private readonly IJsonSerializer _json;
        private readonly IApplicationPaths _appPaths;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;

        public TrailerChannel(IJsonSerializer json, IApplicationPaths appPaths, IHttpClient httpClient, ILogger logger, IProviderManager providerManager)
        {
            _json = json;
            _appPaths = appPaths;
            _httpClient = httpClient;
            _logger = logger;
            _providerManager = providerManager;
            Instance = this;
        }

        public string DataVersion
        {
            get
            {
                return "66";
            }
        }

        public string GetCacheKey(string userId)
        {
            return Plugin.Instance.Configuration.EnableMovieArchive + "-" +
                   Plugin.Instance.Configuration.EnableDvd + "-" +
                   Plugin.Instance.Configuration.EnableNetflix + "-" +
                   Plugin.Instance.Configuration.EnableTheaters + "-" +
                   Plugin.Instance.Configuration.ExcludeUnIdentifiedContent;
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

        private ChannelItemResult GetNonSupporterItems()
        {
            var list = new List<ChannelItemInfo>
                {
                    new ChannelItemInfo
                    {
                         Id = "notregistered",
                         Name = "Supporter membership required",
                         Type = ChannelItemType.Folder
                    }
                };

            return new ChannelItemResult
            {
                Items = list,
                TotalRecordCount = list.Count
            };
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            if (!RegistrationInfo.Instance.IsRegistered)
            {
                return GetNonSupporterItems();
            }

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

            var extraType = (ExtraType)Enum.Parse(typeof(ExtraType), idParts[1], true);
            var trailerType = (TrailerType)Enum.Parse(typeof(TrailerType), idParts[2], true);

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
            var items = await GetAllItems(false, cancellationToken).ConfigureAwait(false);

            return items
                .Where(i => i.ContentType == contentType && i.ExtraType == extraType && i.TrailerTypes.Contains(trailerType));
        }

        private IEnumerable<ChannelItemInfo> RemoveDuplicates(IEnumerable<ChannelItemInfo> items)
        {
            items = RemoveDuplicatesById(items);
            items = RemoveDuplicatesByName(items);

            return items;
        }

        private IEnumerable<ChannelItemInfo> RemoveDuplicatesById(IEnumerable<ChannelItemInfo> items)
        {
            var dictionary = new Dictionary<string, ChannelItemInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                var key = item.Id;

                if (dictionary.ContainsKey(key))
                {
                    var current = dictionary[key];

                    var types = current.TrailerTypes.ToList();
                    types.AddRange(item.TrailerTypes);
                    item.TrailerTypes = types.Distinct().ToList();
                }

                dictionary[key] = item;
            }

            return dictionary.Values;
        }

        private IEnumerable<ChannelItemInfo> RemoveDuplicatesByName(IEnumerable<ChannelItemInfo> items)
        {
            var dictionary = new Dictionary<string, ChannelItemInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                var key = item.Name;

                if (dictionary.ContainsKey(key))
                {
                    var current = dictionary[key];

                    var types = current.TrailerTypes.ToList();
                    types.AddRange(item.TrailerTypes);
                    item.TrailerTypes = types.Distinct().ToList();

                    var sources = current.MediaSources.ToList();
                    sources.AddRange(item.MediaSources.Where(i => !sources.Any(s => string.Equals(s.Path, i.Path, StringComparison.OrdinalIgnoreCase))));
                    item.MediaSources = sources;
                }

                dictionary[key] = item;
            }

            return dictionary.Values;
        }

        private async Task<IEnumerable<ChannelItemInfo>> GetChannelItems(IEnumerable<IExtraProvider> providers, CancellationToken cancellationToken)
        {
            var tasks = providers.Select(async i =>
            {
                try
                {
                    var items = await i.GetChannelItems(cancellationToken).ConfigureAwait(false);

                    items = items.Where(t => !string.IsNullOrWhiteSpace(t.Name) && t.MediaSources.Count > 0);

                    return items;
                }
                catch (Exception ex)
                {
                    return new List<ChannelItemInfo>();
                }
            });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            return results.SelectMany(i => i.ToList());
        }

        public async Task<IEnumerable<ChannelItemInfo>> GetAllItems(bool direct, CancellationToken cancellationToken)
        {
            if (!RegistrationInfo.Instance.IsRegistered)
            {
                return GetNonSupporterItems().Items;
            }

            if (direct)
            {
                return await GetDirectListings(cancellationToken).ConfigureAwait(false);
            }

            var json = await EntryPoint.Instance.GetAndCacheResponse("https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Listings/listingswithmetadata.txt?v=" + DataVersion,
                        TimeSpan.FromDays(3), cancellationToken);

            var items = _json.DeserializeFromString<List<ChannelItemInfo>>(json);

            if (!Plugin.Instance.Configuration.EnableMovieArchive)
            {
                items = items.Where(i => i.TrailerTypes.Count != 1 || i.TrailerTypes[0] != TrailerType.Archive)
                    .ToList();
            }

            if (Plugin.Instance.Configuration.ExcludeUnIdentifiedContent)
            {
                items = items.Where(i => !string.IsNullOrWhiteSpace(i.GetProviderId(MetadataProviders.Imdb)) || !string.IsNullOrWhiteSpace(i.GetProviderId(MetadataProviders.Tmdb)))
                    .ToList();
            }

            return items;
        }

        private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        private async Task<IEnumerable<ChannelItemInfo>> GetDirectListings(CancellationToken cancellationToken)
        {
            var items = await GetChannelItems(EntryPoint.Instance.Providers, cancellationToken).ConfigureAwait(false);

            items = RemoveDuplicates(items);

            var list = items.ToList();

            var savePath = _appPaths.ProgramDataPath;

            await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                _json.SerializeToFile(list, Path.Combine(savePath, "alllistings.txt"));
            }
            finally
            {
                _fileLock.Release();
            }

            var results = await TestResults(list, savePath, cancellationToken).ConfigureAwait(false);

            var filteredItems = CreateFilteredListings(list, results);

            await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                _json.SerializeToFile(filteredItems, Path.Combine(savePath, "filteredlistings.txt"));
            }
            finally
            {
                _fileLock.Release();
            }

            await FillMetadata(filteredItems, savePath, cancellationToken).ConfigureAwait(false);

            await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                _json.SerializeToFile(filteredItems, Path.Combine(savePath, "listingswithmetadata.txt"));
            }
            finally
            {
                _fileLock.Release();
            }

            return filteredItems;
        }

        private async Task FillMetadata(IEnumerable<ChannelItemInfo> items, string savePath, CancellationToken cancellationToken)
        {
            List<TrailerMetadata> results = null;

            try
            {
                using (var stream = GetType().Assembly.GetManifestResourceStream(GetType().Namespace + ".Listings.metadata.txt"))
                {
                    if (stream != null)
                    {
                        results = _json.DeserializeFromStream<List<TrailerMetadata>>(stream);
                    }
                }
            }
            catch
            {
            }

            if (results == null)
            {
                results = new List<TrailerMetadata>();
            }

            await FillMetadata(items, results, cancellationToken).ConfigureAwait(false);

            await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                _json.SerializeToFile(results, Path.Combine(savePath, "metadata.txt"));
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task FillMetadata(IEnumerable<ChannelItemInfo> items, List<TrailerMetadata> metadata, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                try
                {
                    await FillMetadata(item, metadata, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error filling metadata for {0}", ex, item.Name);
                }
            }
        }

        private async Task FillMetadata(ChannelItemInfo item, List<TrailerMetadata> metadataList, CancellationToken cancellationToken)
        {
            var imdbId = item.GetProviderId(MetadataProviders.Imdb);
            TrailerMetadata metadata = null;

            if (!string.IsNullOrWhiteSpace(imdbId))
            {
                metadata = metadataList.FirstOrDefault(i => string.Equals(imdbId, i.GetProviderId(MetadataProviders.Imdb), StringComparison.OrdinalIgnoreCase));
            }

            if (metadata == null)
            {
                var tmdbId = item.GetProviderId(MetadataProviders.Tmdb);

                if (!string.IsNullOrWhiteSpace(tmdbId))
                {
                    metadata = metadataList.FirstOrDefault(i => string.Equals(tmdbId, i.GetProviderId(MetadataProviders.Tmdb), StringComparison.OrdinalIgnoreCase));
                }
            }

            if (metadata == null)
            {
                var searchResults =
                    await _providerManager.GetRemoteSearchResults<Movie, MovieInfo>(new RemoteSearchQuery<MovieInfo>
                    {
                        IncludeDisabledProviders = true,
                        SearchInfo = new MovieInfo
                        {
                            Name = item.Name,
                            Year = item.ProductionYear,
                            ProviderIds = item.ProviderIds
                        }

                    }, cancellationToken).ConfigureAwait(false);

                var result = searchResults.FirstOrDefault();

                if (result != null)
                {
                    metadata = new TrailerMetadata
                    {
                        Name = result.Name,
                        PremiereDate = result.PremiereDate,
                        ProductionYear = result.ProductionYear,
                        ProviderIds = result.ProviderIds
                    };

                    metadataList.Add(metadata);
                }
            }

            if (metadata != null)
            {
                item.Name = metadata.Name ?? item.Name;
                item.ProductionYear = metadata.ProductionYear ?? item.ProductionYear;
                item.PremiereDate = metadata.PremiereDate ?? item.PremiereDate;

                // Merge provider id's
                foreach (var id in metadata.ProviderIds)
                {
                    item.SetProviderId(id.Key, id.Value);
                }
            }
        }

        private List<ChannelItemInfo> CreateFilteredListings(IEnumerable<ChannelItemInfo> items, ListingResults results)
        {
            return items.Where(i => Filter(i, results)).ToList();
        }

        private bool Filter(ChannelItemInfo item, ListingResults results)
        {
            item.MediaSources = item.MediaSources
                .Where(i => Filter(i, results))
                .ToList();

            return item.MediaSources.Count > 0;
        }

        private bool Filter(ChannelMediaInfo item, ListingResults results)
        {
            var key = item.Path;

            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            ListingResult result;
            if (results.TestResults.TryGetValue(key, out result))
            {
                return result.IsValid;
            }

            // Should never get here. return false
            return false;
        }

        private async Task<ListingResults> TestResults(IEnumerable<ChannelItemInfo> items, string savePath, CancellationToken cancellationToken)
        {
            ListingResults results = null;

            try
            {
                using (var stream = GetType().Assembly.GetManifestResourceStream(GetType().Namespace + ".Listings.testresults.txt"))
                {
                    if (stream != null)
                    {
                        results = _json.DeserializeFromStream<ListingResults>(stream);
                    }
                }
            }
            catch
            {
            }

            if (results == null)
            {
                results = new ListingResults();
            }

            await TestItems(items, results, cancellationToken).ConfigureAwait(false);

            await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                _json.SerializeToFile(results, Path.Combine(savePath, "testresults.txt"));
            }
            finally
            {
                _fileLock.Release();
            }

            return results;
        }

        private async Task TestItems(IEnumerable<ChannelItemInfo> items, ListingResults results, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                await TestItem(item, results, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task TestItem(ChannelItemInfo item, ListingResults results, CancellationToken cancellationToken)
        {
            foreach (var media in (item.MediaSources ?? new List<ChannelMediaInfo>()))
            {
                try
                {
                    await TestMediaInfo(media, results, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error testing media info", ex);
                }
            }
        }

        private async Task TestMediaInfo(ChannelMediaInfo item, ListingResults results, CancellationToken cancellationToken)
        {
            var key = item.Path;

            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            ListingResult result;
            if (results.TestResults.TryGetValue(key, out result))
            {
                // Was validated recently
                if (result.IsValid && (DateTime.UtcNow - result.DateTested).TotalDays <= 30)
                {
                    return;
                }

                // Already known to be bad
                if (!result.IsValid)
                {
                    return;
                }
            }

            var options = new HttpRequestOptions
            {
                Url = item.Path,
                CancellationToken = cancellationToken,
                BufferContent = false
            };

            foreach (var header in item.RequiredHttpHeaders)
            {
                options.RequestHeaders[header.Key] = header.Value;
            }

            var isOk = false;

            try
            {
                var response = await _httpClient.SendAsync(options, "HEAD").ConfigureAwait(false);

                try
                {
                    if (response.ContentType.StartsWith("video", StringComparison.OrdinalIgnoreCase))
                    {
                        isOk = true;
                    }
                }
                finally
                {
                    response.Content.Dispose();
                }
            }
            catch (HttpException ex)
            {
                if (!ex.StatusCode.HasValue)
                {
                    throw;
                }

                if (ex.StatusCode.Value != HttpStatusCode.NotFound &&
                    ex.StatusCode.Value != HttpStatusCode.Forbidden)
                {
                    throw;
                }
            }

            results.TestResults[key] = new ListingResult
            {
                IsValid = isOk,
                DateTested = DateTime.UtcNow
            };
        }

        public ChannelItemResult GetTopCategories()
        {
            var list = new List<ChannelItemInfo>();

            if (Plugin.Instance.Configuration.EnableTheaters)
            {
                list.Add(new ChannelItemInfo
                {
                    FolderType = ChannelFolderType.Container,
                    Name = "New and Upcoming in Theaters",
                    Type = ChannelItemType.Folder,
                    MediaType = ChannelMediaType.Video,
                    Id = ChannelMediaContentType.MovieExtra.ToString().ToLower() + "|" + ExtraType.Trailer + "|" + TrailerType.ComingSoonToTheaters,

                    ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/thumb.jpg"
                });
            }

            if (Plugin.Instance.Configuration.EnableDvd)
            {
                list.Add(new ChannelItemInfo
                {
                    FolderType = ChannelFolderType.Container,
                    Name = "New and Upcoming Movies on Dvd & Blu-ray",
                    Type = ChannelItemType.Folder,
                    MediaType = ChannelMediaType.Video,
                    Id = ChannelMediaContentType.MovieExtra.ToString().ToLower() + "|" + ExtraType.Trailer + "|" + TrailerType.ComingSoonToDvd,

                    ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/bluray.jpg"
                });
            }

            if (Plugin.Instance.Configuration.EnableNetflix)
            {
                list.Add(new ChannelItemInfo
                {
                    FolderType = ChannelFolderType.Container,
                    Name = "New and Upcoming Movies on Netflix",
                    Type = ChannelItemType.Folder,
                    MediaType = ChannelMediaType.Video,
                    Id = ChannelMediaContentType.MovieExtra.ToString().ToLower() + "|" + ExtraType.Trailer + "|" + TrailerType.ComingSoonToStreaming,

                    ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/netflix.png"
                });
            }

            if (Plugin.Instance.Configuration.EnableMovieArchive)
            {
                list.Add(new ChannelItemInfo
                {
                    FolderType = ChannelFolderType.Container,
                    Name = "Movie Trailer Archive",
                    Type = ChannelItemType.Folder,
                    MediaType = ChannelMediaType.Video,
                    Id = ChannelMediaContentType.MovieExtra.ToString().ToLower() + "|" + ExtraType.Trailer + "|" + TrailerType.Archive,

                    ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/reel.jpg"
                });
            }

            return new ChannelItemResult
            {
                Items = list,
                TotalRecordCount = list.Count
            };
        }

        public ChannelItemResult GetCategories(ChannelMediaContentType contentType)
        {
            var list = new List<ChannelItemInfo>();

            //list.Add(new ChannelItemInfo
            //{
            //    FolderType = ChannelFolderType.Container,
            //    Name = "New and coming soon to theaters",
            //    Type = ChannelItemType.Folder,
            //    MediaType = ChannelMediaType.Video,
            //    Id = contentType.ToString().ToLower() + "|" + "TrailerComingSoonToTheaters",

            //    ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/thumb.jpg"
            //});

            //list.Add(new ChannelItemInfo
            //{
            //    FolderType = ChannelFolderType.Container,
            //    Name = "New and coming soon to Dvd",
            //    Type = ChannelItemType.Folder,
            //    MediaType = ChannelMediaType.Video,
            //    Id = contentType.ToString().ToLower() + "|" + "TrailerComingSoonToDvd",

            //    ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/bluray.jpg"
            //});

            //list.Add(new ChannelItemInfo
            //{
            //    FolderType = ChannelFolderType.Container,
            //    Name = "Archive",
            //    Type = ChannelItemType.Folder,
            //    MediaType = ChannelMediaType.Video,
            //    Id = contentType.ToString().ToLower() + "|" + "TrailerArchive",

            //    ImageUrl = "https://raw.githubusercontent.com/MediaBrowser/MediaBrowser.Channels/master/MediaBrowser.Plugins.Trailers/Images/reel.jpg"
            //});

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
            get { return "http://emby.media"; }
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
                   },

                AutoRefreshLevels = 3
            };
        }

        public async Task<ChannelItemResult> GetAllMedia(InternalAllChannelMediaQuery query, CancellationToken cancellationToken)
        {
            var items = await GetAllItems(false, cancellationToken).ConfigureAwait(false);

            if (query.ContentTypes.Length > 0)
            {
                items = items.Where(i => query.ContentTypes.Contains(i.ContentType));
            }
            if (query.ExtraTypes.Length > 0)
            {
                items = items.Where(i => query.ExtraTypes.Contains(i.ExtraType));
            }
            if (query.TrailerTypes.Length > 0)
            {
                items = items.Where(i => i.TrailerTypes.Any(t => query.TrailerTypes.Contains(t)));
            }

            var list = items.ToList();

            return new ChannelItemResult
            {
                Items = list,
                TotalRecordCount = list.Count
            };
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
                FolderId = ChannelMediaContentType.MovieExtra.ToString().ToLower() + "|" + ExtraType.Trailer + "|" + TrailerType.ComingSoonToTheaters

            }, cancellationToken).ConfigureAwait(false);

            return result.Items;
        }
    }
}
