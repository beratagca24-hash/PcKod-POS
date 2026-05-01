using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;

namespace PcKod.UI.Views
{
    public partial class StokSayfasi : Window
    {
        private const string ConnectionString = "Data Source=PcKod.db";
        private string _seciliBarkod = "";

        public StokSayfasi()
        {
            InitializeComponent();
            StoklariYukle();
        }

        private void StoklariYukle(string arama = "")
        {
            var stokListesi = new List<StokGorunum>();

            using (var db = new SqliteConnection(ConnectionString))
            {
                db.Open();
                string sql = "SELECT Barkod, UrunAdi, StokMiktari FROM Urunler";

                if (!string.IsNullOrEmpty(arama))
                    sql += " WHERE UrunAdi LIKE @p OR Barkod LIKE @p";

                sql += " ORDER BY StokMiktari ASC";

                var cmd = new SqliteCommand(sql, db);

                if (!string.IsNullOrEmpty(arama))
                    cmd.Parameters.AddWithValue("@p", "%" + arama + "%");

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stokListesi.Add(new StokGorunum
                        {
                            Barkod = reader.IsDBNull(0) ? "" : reader.GetString(0),
                            UrunAdi = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            StokMiktari = reader.IsDBNull(2) ? 0 : reader.GetDouble(2)
                        });
                    }
                }
            }
            dgStok.ItemsSource = stokListesi;
        }

        private void txtStokAra_TextChanged(object sender, TextChangedEventArgs e)
        {
            StoklariYukle(txtStokAra.Text.Trim());
        }

        private void dgStok_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgStok.SelectedItem is StokGorunum secili)
            {
                _seciliBarkod = secili.Barkod;
                txtSeciliUrun.Text = secili.UrunAdi;
                txtMevcutStok.Text = secili.StokMiktari.ToString();
                txtYeniStok.Clear(); // Önceden mevcudu yazıyordu, şimdi boş geliyor ki kullanıcı sadece geleni yazsın
                txtYeniStok.Focus();
            }
        }

        private void btnStokGuncelle_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_seciliBarkod))
            {
                MessageBox.Show("Lütfen listeden bir ürün seçin.");
                return;
            }

            string girilenStok = txtYeniStok.Text.Replace(".", ",");

            if (double.TryParse(girilenStok, out double eklenenMal))
            {
                using (var db = new SqliteConnection(ConnectionString))
                {
                    db.Open();
                    // ÖNEMLİ: Eşittir değil, mevcut stoğa ekleme (+) yapıyoruz
                    var cmd = new SqliteCommand("UPDATE Urunler SET StokMiktari = StokMiktari + @m WHERE Barkod = @b", db);
                    cmd.Parameters.AddWithValue("@m", eklenenMal);
                    cmd.Parameters.AddWithValue("@b", _seciliBarkod);
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show($"{eklenenMal} miktar/kg stoğa başarıyla eklendi.");

                txtStokAra.Clear();
                txtSeciliUrun.Clear();
                txtMevcutStok.Clear();
                txtYeniStok.Clear();
                _seciliBarkod = "";

                StoklariYukle();
            }
            else
            {
                MessageBox.Show("Lütfen geçerli bir sayı girin.");
            }
        }

        // YENİ: Analiz Sayfasını Aç
        private void btnAnaliz_Click(object sender, RoutedEventArgs e)
        {
            var analiz = new AnalizWindow();
            analiz.ShowDialog();
        }
    }

    public class StokGorunum
    {
        public string Barkod { get; set; }
        public string UrunAdi { get; set; }
        public double StokMiktari { get; set; }
    }
}