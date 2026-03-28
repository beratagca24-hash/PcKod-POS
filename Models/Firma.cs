namespace PcKod.UI.Models
{
    // Toptan satış yapılan restoran, otel veya şahıslar
    public class Firma
    {
        public int Id { get; set; }
        public string FirmaAdi { get; set; }

        // Firmanın dükkana olan toplam borcu
        public decimal ToplamBorc { get; set; }
    }
}