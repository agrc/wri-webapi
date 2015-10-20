using System.Collections.Generic;
using wri_webapi.Models.Database;

namespace wri_webapi.Models.Response
{
    public class SpatialFeatureResponse
    {
        public IEnumerable<RelatedDetails> County { get; set; }
        public IEnumerable<RelatedDetails> LandOwnership { get; set; }
        public IEnumerable<RelatedDetails> FocusArea { get; set; }
        public IEnumerable<RelatedDetails> SageGrouse { get; set; }
    }
}