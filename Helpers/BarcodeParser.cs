namespace PcKod.UI.Helpers
{
    public static class BarcodeParser
    {
        /// <summary>
        /// Türkiye terazi standartlarına (27,28,29 prefix) göre barkodu parçalar.
        /// </summary>
        public static (string ProductCode, double Weight) Parse(string rawBarcode)
        {
            if (string.IsNullOrWhiteSpace(rawBarcode)) return (string.Empty, 1.0);

            if (rawBarcode.Length == 13 && (rawBarcode.StartsWith("27") || rawBarcode.StartsWith("28") || rawBarcode.StartsWith("29")))
            {
                string code = rawBarcode.Substring(2, 5); // Ürün PLU kodu
                if (double.TryParse(rawBarcode.Substring(7, 5), out double gram))
                {
                    return (code, gram / 1000.0); // Kg cinsinden döner
                }
            }
            return (rawBarcode, 1.0);
        }
    }
}