using Microsoft.Data.Sqlite;

namespace PcKod.UI.Data
{
    /// <summary>
    /// SQLite altyapısını ve kurumsal veritabanı şemasını yönetir.
    /// </summary>
    public static class DatabaseHelper
    {
        private const string DbName = "PcKod.db";
        public static string ConnectionString => $"Data Source={DbName}";

        public static void InitializeDatabase()
        {
            using (var db = new SqliteConnection(ConnectionString))
            {
                db.Open();

                // Sistem Konfigürasyonları
                ExecuteNonQuery("CREATE TABLE IF NOT EXISTS Ayarlar (Id INTEGER PRIMARY KEY, SifreHash TEXT)", db);

                // Envanter Yönetimi
                string urunSql = @"CREATE TABLE IF NOT EXISTS Urunler (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Barkod TEXT UNIQUE,
                    UrunAdi TEXT,
                    AlisFiyati DECIMAL,
                    SatisFiyati DECIMAL,
                    BirimTipi INTEGER,
                    StokMiktari DOUBLE DEFAULT 0)";
                ExecuteNonQuery(urunSql, db);

                // Cari Hesap Yönetimi
                string firmaSql = @"CREATE TABLE IF NOT EXISTS Firmalar (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FirmaAdi TEXT NOT NULL,
                    ToplamBorc DECIMAL DEFAULT 0)";
                ExecuteNonQuery(firmaSql, db);

                // Satış Hareketleri (Analiz ve Raporlama için)
                string satisSql = @"CREATE TABLE IF NOT EXISTS Satislar (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UrunAdi TEXT,
                    Miktar DOUBLE,
                    ToplamTutar DECIMAL,
                    OdemeYontemi TEXT,
                    Tarih TEXT,
                    FirmaId INTEGER NULL)";
                ExecuteNonQuery(satisSql, db);
            }
        }

        private static void ExecuteNonQuery(string sql, SqliteConnection db)
        {
            using (var cmd = new SqliteCommand(sql, db)) { cmd.ExecuteNonQuery(); }
        }
    }
}