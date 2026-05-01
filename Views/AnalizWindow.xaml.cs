using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace PcKod.UI.Views
{
    public partial class AnalizWindow : Window
    {
        private const string ConnectionString = "Data Source=PcKod.db";

        public AnalizWindow()
        {
            InitializeComponent();
            GrafikVerileriniYukle();
            KritikStoklariYukle();
        }

        private void GrafikVerileriniYukle()
        {
            var grafikListesi = new List<SatisGrafikModel>();
            double enYuksekMiktar = 1; // Sıfıra bölme hatasını önlemek için 1

            // Son 30 günün tarihini alıyoruz
            string otuzGunOnce = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");

            using (var db = new SqliteConnection(ConnectionString))
            {
                db.Open();
                // Son 30 günde satılan ürünleri grupla ve toplam miktarlarına göre çoktan aza sırala
                var cmd = new SqliteCommand(@"SELECT UrunAdi, SUM(Miktar) as ToplamSatis 
                                              FROM Satislar 
                                              WHERE Tarih >= @t AND UrunAdi != 'Kasa İndirimi / Yuvarlama' AND UrunAdi != 'Ek Ücret / Fark'
                                              GROUP BY UrunAdi 
                                              ORDER BY ToplamSatis DESC 
                                              LIMIT 10", db);
                cmd.Parameters.AddWithValue("@t", otuzGunOnce);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model = new SatisGrafikModel
                        {
                            UrunAdi = reader.GetString(0),
                            ToplamSatis = reader.GetDouble(1)
                        };

                        // Listenin en tepesindeki ürün en yüksek satandır (animasyon barı onun üzerinden hesaplanacak)
                        if (model.ToplamSatis > enYuksekMiktar && grafikListesi.Count == 0)
                            enYuksekMiktar = model.ToplamSatis;

                        grafikListesi.Add(model);
                    }
                }
            }

            // Arayüzdeki kutunun maksimum genişliği (yaklaşık değer)
            double maksimumPiksel = 450;

            // Çubukların (barların) genişliğini yüzde hesabıyla piksele çeviriyoruz
            foreach (var item in grafikListesi)
            {
                item.GrafikGenişligi = (item.ToplamSatis / enYuksekMiktar) * maksimumPiksel;
            }

            icGrafik.ItemsSource = grafikListesi;
        }

        private void KritikStoklariYukle()
        {
            var kritikListesi = new List<KritikStokModel>();

            using (var db = new SqliteConnection(ConnectionString))
            {
                db.Open();
                var cmd = new SqliteCommand("SELECT UrunAdi, StokMiktari, BirimTipi FROM Urunler", db);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string urunAdi = reader.GetString(0);
                        double stok = reader.GetDouble(1);
                        int birimTipi = reader.GetInt32(2); // 0: Adet, 1: Kg

                        double kritikSinir = 5; // Varsayılan: Adet ürünler için 5

                        if (birimTipi == 1) // Eğer ürün Kilogram ise
                        {
                            // Ürün adında Tavuk veya Piliç geçiyorsa sınır 10 kg
                            if (urunAdi.IndexOf("Tavuk", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                urunAdi.IndexOf("Piliç", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                kritikSinir = 10;
                            }
                            else // Dana eti, kuzu eti vs. için sınır 15 kg
                            {
                                kritikSinir = 15;
                            }
                        }

                        // Eğer kalan stok, hesapladığımız kritik sınırın altındaysa listeye ekle
                        if (stok <= kritikSinir)
                        {
                            kritikListesi.Add(new KritikStokModel
                            {
                                UrunAdi = urunAdi,
                                KalanGosterim = stok.ToString("0.##") + (birimTipi == 1 ? " Kg" : " Adet")
                            });
                        }
                    }
                }
            }

            dgKritikStok.ItemsSource = kritikListesi;
        }
    }

    // Arayüze veri bağlamak için kullanılan yardımcı modeller
    public class SatisGrafikModel
    {
        public string UrunAdi { get; set; }
        public double ToplamSatis { get; set; }
        public double GrafikGenişligi { get; set; }
        public string MiktarGosterim => ToplamSatis.ToString("0.##");
    }

    public class KritikStokModel
    {
        public string UrunAdi { get; set; }
        public string KalanGosterim { get; set; }
    }
}