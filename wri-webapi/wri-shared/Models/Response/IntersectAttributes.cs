using System.Collections.Generic;
using System.Linq;

namespace wri_shared.Models.Response
{
    public class IntersectAttributes
    {
        public IntersectAttributes(IEnumerable<KeyValuePair<string, object>> values)
        {
            if (values != null && values.Any())
            {
                Attributes = values.Select(x => x.Value);
            }
        }

        public IEnumerable<object> Attributes { get; set; }

        public double Intersect { get; set; }
    }
}