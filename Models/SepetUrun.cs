namespace PcKod.UI.Models
{
    // Satış anında sepetteki her bir satırı temsil eder
    public class SepetUrun
    {
        public int Id { get; set; }
        public string Barkod { get; set; }
        public string UrunAdi { get; set; }
        public decimal BirimFiyat { get; set; }

        // Miktar: 1.5 kg veya 2 adet gibi değerler için double/decimal
        public double Miktar { get; set; }

        // Toplam: Miktar * BirimFiyat (Otomatik hesaplanır)
        public decimal ToplamTutar => (decimal)Miktar * BirimFiyat;

        // Birim Tipi: 0=Adet, 1=Kg (Arayüzde 'adet' mi 'kg' mı yazacağını anlamak için)
        public int BirimTipi { get; set; }
        public string BirimMetni => BirimTipi == 0 ? "Adet" : "Kg";
    }
}