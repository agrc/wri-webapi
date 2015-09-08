namespace wri_shared.Models.Response
{
    public class AttributePairs
    {
        public AttributePairs(string key, object value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; set; }
        public object Value { get; set; }
    }
}