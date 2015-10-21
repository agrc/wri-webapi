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
                if (Table.ToLower() == "poly")
                {
                    return _size.InAcres();
                }

                if (Table.ToLower() == "line")
                {
                    return _size.InFeet();
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