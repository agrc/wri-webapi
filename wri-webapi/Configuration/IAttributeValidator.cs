using System.Collections.Generic;
using wri_webapi.Models.Request;

namespace wri_webapi.Configuration
{
    public interface IAttributeValidator
    {
        bool ValidAttributesFor(string table, string featureType, IEnumerable<FeatureActions> actions);
    }
}