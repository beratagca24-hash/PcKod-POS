using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PcKod.UI.Models;
using Microsoft.Data.Sqlite;
using System.Globalization; // Olası fiyat işlemleri için eklendi

namespace PcKod.UI.Views
{
    public partial class UrunListesiWindow : Window
    {
        private const string ConnectionString = "Data Source=PcKod.db";

        public UrunListesiWindow()
        {
            InitializeComponent();
            UrunleriYukle();
        }

        private void btnGeri_Click(object sender, RoutedEventArgs e) => this.Close();

        private void UrunleriYukle(string arama = "")
        {
            var urunListesi = new List<Urun>();
            using (var db = new SqliteConnection(ConnectionString))
            {
                db.Open();
                string sql = "SELECT * FROM Urunler";
                if (!string.IsNullOrEmpty(arama)) sql += " WHERE UrunAdi LIKE @p OR Barkod LIKE @p";
                var cmd = new SqliteCommand(sql, db);
                if (!string.IsNullOrEmpty(arama)) cmd.Parameters.AddWithValue("@p", "%" + arama + "%");
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) urunListesi.Add(new Urun { Id = reader.GetInt32(0), Barkod = reader.GetString(1), UrunAdi = reader.GetString(2), AlisFiyati = reader.GetDecimal(3), SatisFiyati = reader.GetDecimal(4), BirimTipi = reader.GetInt32(5) });
                }
            }
            dgUrunler.ItemsSource = urunListesi;
        }

        private void txtHizliAra_TextChanged(object sender, TextChangedEventArgs e) => UrunleriYukle(txtHizliAra.Text.Trim());

        private void dgUrunler_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgUrunler.SelectedItem is Urun s)
            {
                txtBarkod.Text = s.Barkod; txtUrunAdi.Text = s.UrunAdi;
                txtAlisFiyati.Text = s.AlisFiyati.ToString("N2", new CultureInfo("tr-TR"));
                txtSatisFiyati.Text = s.SatisFiyati.ToString("N2", new CultureInfo("tr-TR"));
                cmbBirim.SelectedIndex = s.BirimTipi;
            }
        }

        private void btnKaydet_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBarkod.Text) || string.IsNullOrWhiteSpace(txtUrunAdi.Text)) return;
            try
            {
                using (var db = new SqliteConnection(ConnectionString))
                {
                    db.Open();
                    new SqliteCommand($"INSERT OR REPLACE INTO Urunler (Barkod, UrunAdi, AlisFiyati, SatisFiyati, BirimTipi) VALUES ('{txtBarkod.Text}', '{txtUrunAdi.Text}', {txtAlisFiyati.Text.Replace(',', '.')}, {txtSatisFiyati.Text.Replace(',', '.')}, {cmbBirim.SelectedIndex})", db).ExecuteNonQuery();
                }
                UrunleriYukle(); Temizle(); MessageBox.Show("Kaydedildi.");
            }
            catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
        }

        private void btnSilIlk_Click(object sender, RoutedEventArgs e) { if (dgUrunler.SelectedItem != null) pnlSilOnay.Visibility = Visibility.Visible; }

        private void btnSilOnay_Click(object sender, RoutedEventArgs e)
        {
            if (txtSilSifre.Password == "1234")
            {
                var s = (Urun)dgUrunler.SelectedItem;
                using (var db = new SqliteConnection(ConnectionString)) { db.Open(); new SqliteCommand($"DELETE FROM Urunler WHERE Id = {s.Id}", db).ExecuteNonQuery(); }
                UrunleriYukle(); btnSilIptal_Click(null, null);
            }
        }

        private void btnSilIptal_Click(object sender, RoutedEventArgs e) { pnlSilOnay.Visibility = Visibility.Collapsed; txtSilSifre.Clear(); }
        private void Temizle() { txtBarkod.Clear(); txtUrunAdi.Clear(); txtAlisFiyati.Clear(); txtSatisFiyati.Clear(); cmbBirim.SelectedIndex = 0; }
    }
}