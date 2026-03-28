namespace PcKod.UI.Models
{
    // Sistemdeki her bir ürünü temsil edecek ana şablonumuz
    public class Urun
    {
        // Veritabanında her ürünün benzersiz bir sıra numarası olmalı
        public int Id { get; set; }

        // Tabancayla okutulacak barkod numarası
        public string Barkod { get; set; }

        // Ekranda ve fişte görünecek isim
        public string UrunAdi { get; set; }

        // YENİ EKLENDİ: Ürünün toptancıdan geliş fiyatı (Maliyet)
        public decimal AlisFiyati { get; set; }

        // GÜNCELLENDİ: Ürünün kasadaki satış fiyatı
        public decimal SatisFiyati { get; set; }

        // Ürünün türü: 0 = Adet, 1 = Kilogram 
        public int BirimTipi { get; set; }
    }
}