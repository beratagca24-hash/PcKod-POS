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
        private string _seciliBarkod = ""; // Güncellenecek ürünün barkodunu tutmak için

        public StokSayfasi()
        {
            InitializeComponent();
            StoklariYukle();
        }

        // Arama özelliği eklendi
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
                txtYeniStok.Text = secili.StokMiktari.ToString();
            }
        }

        private void btnStokGuncelle_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_seciliBarkod))
            {
                MessageBox.Show("Lütfen listeden bir ürün seçin.");
                return;
            }

            // Noktalı veya virgüllü girişleri doğru okuması için Replace
            string girilenStok = txtYeniStok.Text.Replace(".", ",");

            if (double.TryParse(girilenStok, out double yeniStok))
            {
                using (var db = new SqliteConnection(ConnectionString))
                {
                    db.Open();
                    var cmd = new SqliteCommand("UPDATE Urunler SET StokMiktari = @m WHERE Barkod = @b", db);
                    cmd.Parameters.AddWithValue("@m", yeniStok);
                    cmd.Parameters.AddWithValue("@b", _seciliBarkod);
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Stok başarıyla güncellendi.");

                // Ekranı temizle ve listeyi yenile
                txtStokAra.Clear();
                txtSeciliUrun.Clear();
                txtMevcutStok.Clear();
                txtYeniStok.Clear();
                _seciliBarkod = "";

                StoklariYukle();
            }
            else
            {
                MessageBox.Show("Lütfen geçerli bir miktar girin.");
            }
        }
    }

    public class StokGorunum
    {
        public string Barkod { get; set; }
        public string UrunAdi { get; set; }
        public double StokMiktari { get; set; }
    }
}