using System.Globalization;

namespace PcKod.UI.Helpers
{
    public static class CurrencyHelper
    {
        public static readonly CultureInfo TurkishCulture = new CultureInfo("tr-TR");

        public static string ToLira(this decimal value)
        {
            return value.ToString("C2", TurkishCulture);
        }
    }
}