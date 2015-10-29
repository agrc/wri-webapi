using System.Linq;
using wri_webapi.Extensions;

namespace wri_webapi.Models.Database
{
    public class RelatedDetails
    {
        private string _size;
        public string Origin { get; set; }
        public string Name { get; set; }
        public string Extra { get; set; }
        public string Table { get; set; }
        public string Space 
        {
            get
            {
                if (Origin.ToLower() == "nhd" && Table.ToLower() != "point")
                {
                    return _size.InMiles();
                }

                if (Origin.ToLower() != "nhd")
                {
                    if (Table.ToLower() == "poly")
                    {
                        return _size.InAcres();
                    }
                    
                    if (Table.ToLower() == "line")
                    {
                        return _size.InFeet();
                    }
                }

                if (_size == "0")
                {
                    return null;
                }

                return _size;
            }
            set { _size = value; }
        }
    }
}