using System;
using System.Collections.Generic;

namespace MediaBrowser.Plugins.Trailers.Listings
{
    public class ListingResults
    {
        public Dictionary<string, ListingResult> TestResults { get; set; }

        public ListingResults()
        {
            TestResults = new Dictionary<string, ListingResult>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public class ListingResult
    {
        public DateTime DateTested { get; set; }
        public bool IsValid { get; set; }
    }
}
