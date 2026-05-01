using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Globalization;
using Microsoft.Data.Sqlite;
using ClosedXML.Excel;
using PcKod.UI.Models;
using PcKod.UI.Views;
using PcKod.UI.Data;
using PcKod.UI.Helpers;

namespace PcKod.UI
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<SepetUrun> Sepet { get; set; } = new ObservableCollection<SepetUrun>();

        public MainWindow()
        {
            InitializeComponent();
            dgSepet.ItemsSource = Sepet;
            txtBarkodOkuyucu.Focus();
        }

        #region Barkod ve Satış İşlemleri

        private void txtBarkodOkuyucu_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            string input = txtBarkodOkuyucu.Text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            ProcessScannedInput(input);

            txtBarkodOkuyucu.Clear();
            txtBarkodOkuyucu.Focus();
        }

        private void ProcessScannedInput(string rawInput)
        {
            // Helpers katmanındaki BarcodeParser kullanılarak terazi barkodu ayrıştırılıyor
            var (urunKodu, miktar) = BarcodeParser.Parse(rawInput);

            using (var db = new SqliteConnection(DatabaseHelper.ConnectionString))
            {
                db.Open();
                var cmd = new SqliteCommand("SELECT * FROM Urunler WHERE Barkod = @b OR Barkod = @kisa", db);
                cmd.Parameters.AddWithValue("@b", rawInput);
                cmd.Parameters.AddWithValue("@kisa", urunKodu);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var product = new SepetUrun
                        {
                            Barkod = reader.GetString(1),
                            UrunAdi = reader.GetString(2),
                            BirimFiyat = reader.GetDecimal(4),
                            Miktar = miktar,
                            BirimTipi = reader.GetInt32(5)
                        };
                        UpdateCart(product);
                    }
                    else
                    {
                        MessageBox.Show($"'{urunKodu}' barkoduna ait bir kayıt bulunamadı.", "Sistem Uyarısı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void UpdateCart(SepetUrun item)
        {
            var existing = Sepet.FirstOrDefault(s => s.Barkod == item.Barkod);

            // Ürün sepette varsa ve "Adet" bazlıysa miktarını 1 artır, aksi halde (Terazi) yeni satır olarak ekle
            if (existing != null && item.BirimTipi == 0)
            {
                existing.Miktar += 1;
                dgSepet.Items.Refresh();
            }
            else
            {
                Sepet.Add(item);
            }

            CalculateTotal();
        }

        private void SatisiTamamla(string paymentMethod)
        {
            if (Sepet.Count == 0) return;

            decimal calculatedTotal = Sepet.Sum(s => s.ToplamTutar);
            string inputTotal = txtGenelToplam.Text.Replace("₺", "").Replace(" ", "").Trim();

            if (!decimal.TryParse(inputTotal, out decimal finalTotal))
            {
                finalTotal = calculatedTotal;
            }

            decimal difference = finalTotal - calculatedTotal;

            using (var db = new SqliteConnection(DatabaseHelper.ConnectionString))
            {
                db.Open();
                using (var tr = db.BeginTransaction())
                {
                    try
                    {
                        foreach (var s in Sepet)
                        {
                            InsertSaleRecord(s.UrunAdi, s.Miktar, s.ToplamTutar, paymentMethod, db, tr);

                            if (s.Barkod != "MANUEL")
                            {
                                UpdateStockRecord(s.Barkod, s.Miktar, db, tr);
                            }
                        }

                        // İndirim veya Kasa Yuvarlama tespiti yapıldıysa ayrı bir fiş satırı olarak ekle
                        if (difference != 0)
                        {
                            string diffTitle = difference < 0 ? "Kasa İndirimi / Yuvarlama" : "Ek Ücret / Fark";
                            InsertSaleRecord(diffTitle, 1, difference, paymentMethod, db, tr);
                        }

                        tr.Commit();
                        MessageBox.Show($"{paymentMethod} tahsilatı başarıyla gerçekleştirildi.", "İşlem Tamam", MessageBoxButton.OK, MessageBoxImage.Information);

                        Sepet.Clear();
                        CalculateTotal();
                        txtBarkodOkuyucu.Focus();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        MessageBox.Show($"Kritik Veritabanı Hatası: {ex.Message}", "Sistem Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        #endregion

        #region Veritabanı Primitifleri (Satış ve Stok Güncelleme)

        private void InsertSaleRecord(string urunAdi, double miktar, decimal tutar, string odemeYontemi, SqliteConnection db, SqliteTransaction tr)
        {
            var cmd = new SqliteCommand("INSERT INTO Satislar (UrunAdi, Miktar, ToplamTutar, OdemeYontemi, Tarih) VALUES (@a, @m, @t, @y, @d)", db, tr);
            cmd.Parameters.AddWithValue("@a", urunAdi);
            cmd.Parameters.AddWithValue("@m", miktar);
            cmd.Parameters.AddWithValue("@t", tutar);
            cmd.Parameters.AddWithValue("@y", odemeYontemi);
            cmd.Parameters.AddWithValue("@d", DateTime.Now.ToString("yyyy-MM-dd"));
            cmd.ExecuteNonQuery();
        }

        private void UpdateStockRecord(string barkod, double miktar, SqliteConnection db, SqliteTransaction tr)
        {
            var cmd = new SqliteCommand("UPDATE Urunler SET StokMiktari = StokMiktari - @m WHERE Barkod = @b", db, tr);
            cmd.Parameters.AddWithValue("@m", miktar);
            cmd.Parameters.AddWithValue("@b", barkod);
            cmd.ExecuteNonQuery();
        }

        #endregion

        #region UI Eventleri ve Kontrolleri

        private void CalculateTotal()
        {
            txtGenelToplam.Text = Sepet.Sum(s => s.ToplamTutar).ToString("C2", new CultureInfo("tr-TR"));
        }

        private void dgSepet_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => CalculateTotal()), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void txtGenelToplam_GotFocus(object sender, RoutedEventArgs e)
        {
            txtGenelToplam.Text = txtGenelToplam.Text.Replace("₺", "").Replace(".", "").Trim();
        }

        private void txtGenelToplam_LostFocus(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(txtGenelToplam.Text, out decimal val))
            {
                txtGenelToplam.Text = val.ToString("C2", new CultureInfo("tr-TR"));
            }
            else
            {
                CalculateTotal();
            }
        }

        private void btnNakit_Click(object sender, RoutedEventArgs e) => SatisiTamamla("Nakit");

        private void btnKart_Click(object sender, RoutedEventArgs e) => SatisiTamamla("Kart");

        private void btnStokSayfasi_Click(object sender, RoutedEventArgs e) => new StokSayfasi().ShowDialog();

        private void btnUrunListesi_Click(object sender, RoutedEventArgs e) => new UrunListesiWindow().ShowDialog();

        private void btnToptanSatis_Click(object sender, RoutedEventArgs e) => new ToptanSatisWindow().ShowDialog();

        private void btnSepetiTemizle_Click(object sender, RoutedEventArgs e)
        {
            Sepet.Clear();
            CalculateTotal();
            txtBarkodOkuyucu.Focus();
        }

        private void btnManuelEkle_Click(object sender, RoutedEventArgs e)
        {
            string urunAdi = txtManuelUrunAdi.Text.Trim();
            string fiyatMetni = txtManuelFiyat.Text.Replace(".", ",");

            if (string.IsNullOrEmpty(urunAdi) || !decimal.TryParse(fiyatMetni, out decimal price))
            {
                MessageBox.Show("Ürün adı ve fiyat bilgisi zorunludur.", "Eksik Giriş", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Sepet.Add(new SepetUrun
            {
                Barkod = "MANUEL",
                UrunAdi = urunAdi,
                BirimFiyat = price,
                Miktar = 1,
                BirimTipi = 0
            });

            txtManuelUrunAdi.Clear();
            txtManuelFiyat.Clear();
            CalculateTotal();
        }

        #endregion

        #region Raporlama ve Güvenlik İşlemleri

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
            if (ValidatePassword(txtRaporSifre.Password))
            {
                ExportDailyReport();
                btnRaporIptal_Click(null, null);
            }
            else
            {
                MessageBox.Show("Giriş yetkiniz bulunmuyor. Hatalı şifre tuşladınız.", "Erişim Reddedildi", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtRaporSifre.Clear();
            }
        }

        private void btnRaporIptal_Click(object sender, RoutedEventArgs e)
        {
            pnlRaporSifre.Visibility = Visibility.Collapsed;
            btnRaporHazirlik.Visibility = Visibility.Visible;
            txtRaporSifre.Clear();
        }

        private void ExportDailyReport()
        {
            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Rapor_{currentDate}.xlsx");

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Satışlar");
                    worksheet.Cell(1, 1).Value = "Ürün";
                    worksheet.Cell(1, 2).Value = "Miktar";
                    worksheet.Cell(1, 3).Value = "Tutar";
                    worksheet.Cell(1, 4).Value = "Ödeme Türü";

                    using (var db = new SqliteConnection(DatabaseHelper.ConnectionString))
                    {
                        db.Open();
                        var cmd = new SqliteCommand("SELECT UrunAdi, SUM(Miktar), SUM(ToplamTutar), OdemeYontemi FROM Satislar WHERE Tarih = @t GROUP BY UrunAdi, OdemeYontemi", db);
                        cmd.Parameters.AddWithValue("@t", currentDate);

                        int row = 2;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                worksheet.Cell(row, 1).Value = reader.GetString(0);
                                worksheet.Cell(row, 2).Value = reader.GetDouble(1);
                                worksheet.Cell(row, 3).Value = reader.GetDecimal(2);
                                worksheet.Cell(row, 4).Value = reader.GetString(3);
                                row++;
                            }
                        }
                    }

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(filePath);
                    MessageBox.Show("Gün sonu raporu masaüstüne başarıyla aktarıldı.", "Rapor Oluşturuldu", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rapor oluşturulurken bir hata meydana geldi:\n{ex.Message}", "Sistem Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidatePassword(string inputPassword)
        {
            using (var db = new SqliteConnection(DatabaseHelper.ConnectionString))
            {
                db.Open();
                var cmd = new SqliteCommand("SELECT SifreHash FROM Ayarlar WHERE Id = 1", db);
                var result = cmd.ExecuteScalar();
                return (result == null || result == DBNull.Value) ? inputPassword == "1234" : result.ToString() == inputPassword;
            }
        }

        private void btnSifreDegistir_Click(object sender, RoutedEventArgs e)
        {
            string newPassword = Microsoft.VisualBasic.Interaction.InputBox("Lütfen yeni 4 haneli erişim şifresini giriniz:", "Güvenlik Ayarları", "");
            if (newPassword.Length == 4)
            {
                using (var db = new SqliteConnection(DatabaseHelper.ConnectionString))
                {
                    db.Open();
                    new SqliteCommand($"INSERT OR REPLACE INTO Ayarlar (Id, SifreHash) VALUES (1, '{newPassword}')", db).ExecuteNonQuery();
                }
                MessageBox.Show("Sistem erişim şifresi güncellenmiştir.", "İşlem Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion
    }
}