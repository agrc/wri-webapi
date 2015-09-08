using System.Collections.Generic;

namespace wri_shared.Models.Response
{
    public class IntersectResponse
    {
        public IntersectResponse(Dictionary<string, IList<IntersectAttributes>> attributes)
        {
            Attributes = attributes;
        }

        public Dictionary<string, IList<IntersectAttributes>> Attributes { get; set; }
    }
}