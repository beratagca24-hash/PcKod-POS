using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PcKod.UI.Models;
using Microsoft.Data.Sqlite;
using PcKod.UI.Views;
using ClosedXML.Excel;
using System.IO;

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
            if (barkod.StartsWith("27") && barkod.Length == 13)
            {
                arananBarkod = barkod.Substring(2, 5);
                double gramaj = double.Parse(barkod.Substring(7, 5));
                miktar = gramaj / 1000.0;
            }

            using (var db = new SqliteConnection(ConnectionString))
            {
                db.Open();
                var komut = new SqliteCommand("SELECT * FROM Urunler WHERE Barkod = @b", db);
                komut.Parameters.AddWithValue("@b", arananBarkod);
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
                        if (mevcut != null && yeniUrun.BirimTipi == 0) { mevcut.Miktar += 1; dgSepet.Items.Refresh(); }
                        else { Sepet.Add(yeniUrun); }
                        ToplamHesapla();
                    }
                    else { MessageBox.Show("Ürün Kayıtlı Değil!"); }
                }
            }
        }

        private void SatisiTamamla(string odemeTuru)
        {
            if (Sepet.Count == 0) return;
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

                        var cmdStok = new SqliteCommand("UPDATE Urunler SET StokMiktari = StokMiktari - @m WHERE Barkod = @b", db, tr);
                        cmdStok.Parameters.AddWithValue("@m", kalem.Miktar);
                        cmdStok.Parameters.AddWithValue("@b", kalem.Barkod);
                        cmdStok.ExecuteNonQuery();
                    }
                    tr.Commit();
                    MessageBox.Show($"{odemeTuru} satışı tamamlandı!");
                    Sepet.Clear(); ToplamHesapla();
                }
                catch (Exception ex) { tr.Rollback(); MessageBox.Show("Hata: " + ex.Message); }
            }
        }

        private void btnRaporHazirlik_Click(object sender, RoutedEventArgs e) { pnlRaporSifre.Visibility = Visibility.Visible; btnRaporHazirlik.Visibility = Visibility.Collapsed; txtRaporSifre.Focus(); }

        private void txtRaporSifre_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) btnRaporOnay_Click(null, null);
        }

        private void btnRaporOnay_Click(object sender, RoutedEventArgs e)
        {
            if (SifreKontrol(txtRaporSifre.Password)) { GerçekRaporuAl(); btnRaporIptal_Click(null, null); }
            else { MessageBox.Show("Hatalı Şifre!"); txtRaporSifre.Clear(); }
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
                    ws.Cell(1, 1).Value = "Ürün"; ws.Cell(1, 2).Value = "Miktar"; ws.Cell(1, 3).Value = "Tutar"; ws.Cell(1, 4).Value = "Tür";
                    using (var db = new SqliteConnection(ConnectionString))
                    {
                        db.Open();
                        var cmd = new SqliteCommand("SELECT UrunAdi, SUM(Miktar), SUM(ToplamTutar), OdemeYontemi FROM Satislar WHERE Tarih = @t GROUP BY UrunAdi, OdemeYontemi", db);
                        cmd.Parameters.AddWithValue("@t", bugun);
                        int r = 2;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read()) { ws.Cell(r, 1).Value = reader.GetString(0); ws.Cell(r, 2).Value = reader.GetDouble(1); ws.Cell(r, 3).Value = reader.GetDecimal(2); ws.Cell(r, 4).Value = reader.GetString(3); r++; }
                        }
                    }
                    workbook.SaveAs(dosyaYolu);
                    MessageBox.Show("Rapor Masaüstüne Kaydedildi.");
                }
            }
            catch (Exception ex) { MessageBox.Show("Excel Hatası: " + ex.Message); }
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
                MessageBox.Show("Şifre Değişti!");
            }
        }

        private void btnNakit_Click(object sender, RoutedEventArgs e) => SatisiTamamla("Nakit");
        private void btnKart_Click(object sender, RoutedEventArgs e) => SatisiTamamla("Kart");
        private void btnRaporIptal_Click(object sender, RoutedEventArgs e) { pnlRaporSifre.Visibility = Visibility.Collapsed; btnRaporHazirlik.Visibility = Visibility.Visible; txtRaporSifre.Clear(); }
        private void ToplamHesapla() { txtGenelToplam.Text = Sepet.Sum(s => s.ToplamTutar).ToString("C2"); }
        private void btnUrunListesi_Click(object sender, RoutedEventArgs e) => new UrunListesiWindow().ShowDialog();
        private void btnFirmaEkle_Click(object sender, RoutedEventArgs e) => new FirmaEkleWindow().ShowDialog();
        private void btnToptanSatis_Click(object sender, RoutedEventArgs e) => new ToptanSatisWindow().ShowDialog();
        private void btnSepetiTemizle_Click(object sender, RoutedEventArgs e) { Sepet.Clear(); ToplamHesapla(); }
    }
}