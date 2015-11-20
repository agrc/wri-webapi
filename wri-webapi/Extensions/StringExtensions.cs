using System;

namespace wri_webapi.Extensions
{
    public static class StringExtensions
    {
        public static string InAcres(this string value)
        {
            if (Empty(value))
            {
                return null;
            }
            
            try
            {
                var meters = Convert.ToDouble(value);
                return string.Format("{0:#,###0.####} ac", Math.Round(meters * 0.00024710538187021526, 4));
            }
            catch (Exception)
            {
                return value;
            }
        }

        public static string InSquaredMiles(this string value)
        {
            if (Empty(value))
            {
                return null;
            }

            try
            {
                var meters = Convert.ToDouble(value);
                return string.Format("{0:#,###0.####} mi²", Math.Round(meters * 0.00000038610214678498217, 4));
            }
            catch (Exception)
            {
                return value;
            }
        }
        public static string InMiles(this string value)
        {
            if (Empty(value))
            {
                return null;
            }

            try
            {
                var meters = Convert.ToDouble(value);
                return string.Format("{0:#,###0.####} mi", Math.Round(meters * 0.00062137004149730017, 4));
            }
            catch (Exception)
            {
                return value;
            }
        }

        public static string InFeet(this string value)
        {
            if (Empty(value))
            {
                return null;
            }

            try
            {
                var meters = Convert.ToDouble(value);
                return string.Format("{0:#,###0.####} ft", Math.Round(meters * 3.2808333328119184, 4));
            }
            catch (Exception)
            {
                return value;
            } 
        }

        public static string AsPoint(this string value)
        {
            if (Empty(value))
            {
                return null;
            }

            try
            {
                var count = Convert.ToDouble(value);
                return string.Format("{0:#,###0}", count);
            }
            catch (Exception)
            {
                return value;
            }  
        }

        private static bool Empty(string value)
        {
            if (string.IsNullOrEmpty(value) || value.StartsWith("0.0000"))
            {
                return true;
            }

            return false;
        }
 
    }
}