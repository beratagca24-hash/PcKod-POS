using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PcKod.UI.Models;
using Microsoft.Data.Sqlite;
using PcKod.UI.Views;
using ClosedXML.Excel;
using System.IO;
using System.Globalization;

namespace PcKod.UI
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<SepetUrun> Sepet { get; set; } = new ObservableCollection<SepetUrun>();
        private const string ConnectionString = "Data Source=PcKod.db";

        public MainWindow()
        {
            InitializeComponent();
            dgSepet.ItemsSource = Sepet;
            txtBarkodOkuyucu.Focus();
        }

        private void txtBarkodOkuyucu_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string okunanBarkod = txtBarkodOkuyucu.Text.Trim();
                if (string.IsNullOrEmpty(okunanBarkod)) return;

                UrunBulVeSepeteEkle(okunanBarkod);

                txtBarkodOkuyucu.Clear();
                txtBarkodOkuyucu.Focus();
            }
        }

        private void UrunBulVeSepeteEkle(string barkod)
        {
            string arananBarkod = barkod;
            double miktar = 1.0;

            // PROFESYONEL TERAZİ BARKODU AYRIŞTIRMA (EAN-13)
            // Türkiye'deki teraziler genelde 27, 28 veya 29 ile başlar ve 13 hanelidir.
            if (barkod.Length == 13 && (barkod.StartsWith("27") || barkod.StartsWith("28") || barkod.StartsWith("29")))
            {
                // Örn: 2700001012504 -> Ürün Kodu: 00001
                arananBarkod = barkod.Substring(2, 5);

                // Ağırlık kısmı: 01250 -> 1.250 kg
                if (double.TryParse(barkod.Substring(7, 5), out double gramaj))
                {
                    miktar = gramaj / 1000.0;
                }
            }

            using (var db = new SqliteConnection(ConnectionString))
            {
                db.Open();
                // Ürünü tam okunan barkoddan VEYA teraziden ayıklanan kısa koddan arar
                var komut = new SqliteCommand("SELECT * FROM Urunler WHERE Barkod = @b OR Barkod = @kisa", db);
                komut.Parameters.AddWithValue("@b", barkod);
                komut.Parameters.AddWithValue("@kisa", arananBarkod);

                using (var reader = komut.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var yeniUrun = new SepetUrun
                        {
                            Barkod = reader.GetString(1),
                            UrunAdi = reader.GetString(2),
                            BirimFiyat = reader.GetDecimal(4),
                            Miktar = miktar,
                            BirimTipi = reader.GetInt32(5)
                        };

                        var mevcut = Sepet.FirstOrDefault(s => s.Barkod == yeniUrun.Barkod);

                        // Ürün sepette varsa ve ADET bazlıysa miktarını 1 artır, KG bazlıysa yeni satır olarak ekle
                        if (mevcut != null && yeniUrun.BirimTipi == 0)
                        {
                            mevcut.Miktar += 1;
                            dgSepet.Items.Refresh();
                        }
                        else
                        {
                            Sepet.Add(yeniUrun);
                        }

                        ToplamHesapla();
                    }
                    else
                    {
                        MessageBox.Show($"'{arananBarkod}' kodlu ürün sistemde bulunamadı!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        // TABLODA HÜCRE DEĞİŞİNCE ANLIK TOPLAM HESAPLAMA
        private void dgSepet_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => ToplamHesapla()), System.Windows.Threading.DispatcherPriority.Background);
        }

        // MANUEL GENEL TOPLAM DEĞİŞTİRME - KUTUYA TIKLAYINCA "₺" TEMİZLE
        private void txtGenelToplam_GotFocus(object sender, RoutedEventArgs e)
        {
            txtGenelToplam.Text = txtGenelToplam.Text.Replace("₺", "").Replace(".", "").Trim();
        }

        // MANUEL GENEL TOPLAM DEĞİŞTİRME - KUTUDAN ÇIKINCA TEKRAR FORMATLA
        private void txtGenelToplam_LostFocus(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(txtGenelToplam.Text, out decimal girilenDeger))
            {
                txtGenelToplam.Text = girilenDeger.ToString("C2", new CultureInfo("tr-TR"));
            }
            else
            {
                ToplamHesapla();
            }
        }

        private void ToplamHesapla()
        {
            txtGenelToplam.Text = Sepet.Sum(s => s.ToplamTutar).ToString("C2", new CultureInfo("tr-TR"));
        }

        private void SatisiTamamla(string odemeTuru)
        {
            if (Sepet.Count == 0) return;

            decimal sistemToplami = Sepet.Sum(s => s.ToplamTutar);
            string temizTutar = txtGenelToplam.Text.Replace("₺", "").Replace(" ", "").Trim();

            if (!decimal.TryParse(temizTutar, out decimal alinanToplam))
            {
                alinanToplam = sistemToplami;
            }

            decimal fiyatFarki = alinanToplam - sistemToplami;

            using (var db = new SqliteConnection(ConnectionString))
            {
                db.Open();
                var tr = db.BeginTransaction();
                try
                {
                    foreach (var kalem in Sepet)
                    {
                        var cmdSatis = new SqliteCommand(@"INSERT INTO Satislar (UrunAdi, Miktar, ToplamTutar, OdemeYontemi, Tarih) VALUES (@ad, @mik, @top, @yontem, @tarih)", db, tr);
                        cmdSatis.Parameters.AddWithValue("@ad", kalem.UrunAdi);
                        cmdSatis.Parameters.AddWithValue("@mik", kalem.Miktar);
                        cmdSatis.Parameters.AddWithValue("@top", kalem.ToplamTutar);
                        cmdSatis.Parameters.AddWithValue("@yontem", odemeTuru);
                        cmdSatis.Parameters.AddWithValue("@tarih", DateTime.Now.ToString("yyyy-MM-dd"));
                        cmdSatis.ExecuteNonQuery();

                        if (kalem.Barkod != "MANUEL")
                        {
                            var cmdStok = new SqliteCommand("UPDATE Urunler SET StokMiktari = StokMiktari - @m WHERE Barkod = @b", db, tr);
                            cmdStok.Parameters.AddWithValue("@m", kalem.Miktar);
                            cmdStok.Parameters.AddWithValue("@b", kalem.Barkod);
                            cmdStok.ExecuteNonQuery();
                        }
                    }

                    if (fiyatFarki != 0)
                    {
                        string islemAdi = fiyatFarki < 0 ? "Kasa İndirimi / Yuvarlama" : "Ek Ücret / Fark";
                        var cmdIndirim = new SqliteCommand(@"INSERT INTO Satislar (UrunAdi, Miktar, ToplamTutar, OdemeYontemi, Tarih) VALUES (@ad, 1, @top, @yontem, @tarih)", db, tr);
                        cmdIndirim.Parameters.AddWithValue("@ad", islemAdi);
                        cmdIndirim.Parameters.AddWithValue("@top", fiyatFarki);
                        cmdIndirim.Parameters.AddWithValue("@yontem", odemeTuru);
                        cmdIndirim.Parameters.AddWithValue("@tarih", DateTime.Now.ToString("yyyy-MM-dd"));
                        cmdIndirim.ExecuteNonQuery();
                    }

                    tr.Commit();
                    MessageBox.Show($"{odemeTuru} satışı başarıyla tamamlandı!", "Satış Bitti", MessageBoxButton.OK, MessageBoxImage.Information);

                    Sepet.Clear();
                    ToplamHesapla();
                    txtBarkodOkuyucu.Focus();
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    MessageBox.Show("Veritabanı Kayıt Hatası: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnNakit_Click(object sender, RoutedEventArgs e) => SatisiTamamla("Nakit");
        private void btnKart_Click(object sender, RoutedEventArgs e) => SatisiTamamla("Kart");

        private void btnRaporHazirlik_Click(object sender, RoutedEventArgs e)
        {
            pnlRaporSifre.Visibility = Visibility.Visible;
            btnRaporHazirlik.Visibility = Visibility.Collapsed;
            txtRaporSifre.Focus();
        }

        private void txtRaporSifre_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) btnRaporOnay_Click(null, null);
        }

        private void btnRaporOnay_Click(object sender, RoutedEventArgs e)
        {
            if (SifreKontrol(txtRaporSifre.Password))
            {
                GerçekRaporuAl();
                btnRaporIptal_Click(null, null);
            }
            else
            {
                MessageBox.Show("Hatalı Şifre!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtRaporSifre.Clear();
            }
        }

        private void GerçekRaporuAl()
        {
            string bugun = DateTime.Now.ToString("yyyy-MM-dd");
            string dosyaYolu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Rapor_{bugun}.xlsx");

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Satışlar");
                    ws.Cell(1, 1).Value = "Ürün";
                    ws.Cell(1, 2).Value = "Miktar";
                    ws.Cell(1, 3).Value = "Tutar";
                    ws.Cell(1, 4).Value = "Tür";

                    using (var db = new SqliteConnection(ConnectionString))
                    {
                        db.Open();
                        var cmd = new SqliteCommand("SELECT UrunAdi, SUM(Miktar), SUM(ToplamTutar), OdemeYontemi FROM Satislar WHERE Tarih = @t GROUP BY UrunAdi, OdemeYontemi", db);
                        cmd.Parameters.AddWithValue("@t", bugun);

                        int r = 2;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ws.Cell(r, 1).Value = reader.GetString(0);
                                ws.Cell(r, 2).Value = reader.GetDouble(1);
                                ws.Cell(r, 3).Value = reader.GetDecimal(2);
                                ws.Cell(r, 4).Value = reader.GetString(3);
                                r++;
                            }
                        }
                    }
                    workbook.SaveAs(dosyaYolu);
                    MessageBox.Show("Rapor Masaüstüne Kaydedildi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Excel Hatası: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool SifreKontrol(string girilen)
        {
            using (var db = new SqliteConnection(ConnectionString))
            {
                db.Open();
                var cmd = new SqliteCommand("SELECT SifreHash FROM Ayarlar WHERE Id = 1", db);
                var s = cmd.ExecuteScalar();
                return (s == null || s == DBNull.Value) ? girilen == "1234" : s.ToString() == girilen;
            }
        }

        private void btnSifreDegistir_Click(object sender, RoutedEventArgs e)
        {
            string n = Microsoft.VisualBasic.Interaction.InputBox("Yeni 4 Haneli Şifre:", "Ayarlar", "");
            if (n.Length == 4)
            {
                using (var db = new SqliteConnection(ConnectionString))
                {
                    db.Open();
                    new SqliteCommand($"INSERT OR REPLACE INTO Ayarlar (Id, SifreHash) VALUES (1, '{n}')", db).ExecuteNonQuery();
                }
                MessageBox.Show("Şifre başarıyla değiştirildi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnRaporIptal_Click(object sender, RoutedEventArgs e)
        {
            pnlRaporSifre.Visibility = Visibility.Collapsed;
            btnRaporHazirlik.Visibility = Visibility.Visible;
            txtRaporSifre.Clear();
        }

        private void btnUrunListesi_Click(object sender, RoutedEventArgs e) => new UrunListesiWindow().ShowDialog();
        private void btnStokSayfasi_Click(object sender, RoutedEventArgs e) => new StokSayfasi().ShowDialog();
        private void btnToptanSatis_Click(object sender, RoutedEventArgs e) => new ToptanSatisWindow().ShowDialog();

        private void btnSepetiTemizle_Click(object sender, RoutedEventArgs e)
        {
            Sepet.Clear();
            ToplamHesapla();
            txtBarkodOkuyucu.Focus();
        }

        private void btnManuelEkle_Click(object sender, RoutedEventArgs e)
        {
            string urunAdi = txtManuelUrunAdi.Text.Trim();
            string fiyatMetni = txtManuelFiyat.Text.Replace(".", ",");

            if (string.IsNullOrEmpty(urunAdi) || !decimal.TryParse(fiyatMetni, out decimal fiyat))
            {
                MessageBox.Show("Lütfen geçerli bir ürün adı ve fiyat (örn: 50,50) giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Sepet.Add(new SepetUrun
            {
                Barkod = "MANUEL",
                UrunAdi = urunAdi,
                BirimFiyat = fiyat,
                Miktar = 1,
                BirimTipi = 0
            });

            txtManuelUrunAdi.Clear();
            txtManuelFiyat.Clear();
            ToplamHesapla();
        }
    }
}