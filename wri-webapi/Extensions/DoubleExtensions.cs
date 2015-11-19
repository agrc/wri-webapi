using System;
using System.Globalization;

namespace wri_webapi.Extensions
{
    public static class DoubleExtensions
    {
        public static string InAcres(this double value)
        {
            try
            {
                return string.Format("{0:#,###0.####} ac", Math.Round(value * 0.00024710538187021526, 4));
            }
            catch (Exception)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static string InFeet(this double value)
        {
            try
            {
                return string.Format("{0:#,###0.####} ft", Math.Round(value * 3.2808333328119184, 4));
            }
            catch (Exception)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}