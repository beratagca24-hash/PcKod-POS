using Microsoft.Data.Sqlite;

namespace PcKod.UI.Data
{
    public static class DatabaseHelper
    {
        private const string DbName = "PcKod.db";

        public static void InitializeDatabase()
        {
            using (var db = new SqliteConnection($"Filename={DbName}"))
            {
                db.Open();

                // 1. Ayarlar (Giriş şifresi vb. için)
                new SqliteCommand("CREATE TABLE IF NOT EXISTS Ayarlar (Id INTEGER PRIMARY KEY, SifreHash TEXT)", db).ExecuteNonQuery();

                // 2. Ürünler (Maliyet, Satış ve Stok takibi)
                string urunSql = @"CREATE TABLE IF NOT EXISTS Urunler (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Barkod TEXT UNIQUE,
                    UrunAdi TEXT,
                    AlisFiyati DECIMAL,
                    SatisFiyati DECIMAL,
                    BirimTipi INTEGER,
                    StokMiktari DOUBLE DEFAULT 0)";
                new SqliteCommand(urunSql, db).ExecuteNonQuery();

                // 3. Firmalar (Toptan satış yapılacak yerler)
                string firmaSql = @"CREATE TABLE IF NOT EXISTS Firmalar (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FirmaAdi TEXT NOT NULL,
                    ToplamBorc DECIMAL DEFAULT 0)";
                new SqliteCommand(firmaSql, db).ExecuteNonQuery();

                // 4. GÜNCELLENMİŞ: Satışlar (Excel raporu için ürün bazlı detay tutar)
                // Veresiye kaldırıldı, sadece Nakit/Kart ve ürün detayları eklendi
                string satisSql = @"CREATE TABLE IF NOT EXISTS Satislar (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UrunAdi TEXT,          -- Excel'de görünmesi için
                    Miktar DOUBLE,         -- Kaç adet/kg satıldı
                    ToplamTutar DECIMAL,   -- O satırın toplam cirosu
                    OdemeYontemi TEXT,     -- Nakit veya Kart
                    Tarih TEXT,            -- Excel filtreleme için YYYY-MM-DD formatında
                    FirmaId INTEGER NULL   -- Toptan satışlarda hangi firmaya gittiği
                )";
                new SqliteCommand(satisSql, db).ExecuteNonQuery();
            }
        }
    }
}