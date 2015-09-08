using System.Collections.Generic;
using System.Linq;
using wri_webapi.Models.Request;

namespace wri_webapi.Configuration
{
    public class AttributeValidator : IAttributeValidator
    {
        public bool ValidAttributesFor(string table, string featureType, IEnumerable<FeatureActions> actions)
        {
            if (string.IsNullOrEmpty(featureType))
            {
                return false;
            }

            featureType = featureType.ToLower();
            
            if (featureType == "affected area" && actions == null)
            {
                return true;
            }

            if (string.IsNullOrEmpty(table))
            {
                return false;
            }

            table = table.ToLower();

            if (actions == null || actions.Count() != 1)
            {
                return false;
            }

            switch (table)
            {
                case "poly":
                    return
                        actions.All(
                            x =>
                                !string.IsNullOrEmpty(x.Action) &&
                                x.Treatments.All(t => !string.IsNullOrEmpty(t.Treatment)));
                case "point":
                    if (new[] {"guzzler", "trough", "fish passage structure"}.Contains(featureType))
                    {
                        return actions.All(x => !string.IsNullOrEmpty(x.Action) && 
                                                 !string.IsNullOrEmpty(x.Type));
                    }
                    
                    return actions.All(x => !string.IsNullOrEmpty(x.Description));
                case "line":
                    return actions.All(x => !string.IsNullOrEmpty(x.Action) &&
                                                 !string.IsNullOrEmpty(x.Type));
                
            }

            return false;
        }
    }
}