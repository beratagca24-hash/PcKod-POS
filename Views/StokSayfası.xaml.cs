using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using PcKod.UI.Data;

namespace PcKod.UI.Views
{
    public partial class StokSayfasi : Window
    {
        private string _activeBarcode = string.Empty;

        public StokSayfasi()
        {
            InitializeComponent();
            SyncInventory();
        }

        private void SyncInventory(string filter = "")
        {
            var results = new List<StokGorunum>();

            using (var db = new SqliteConnection(DatabaseHelper.ConnectionString))
            {
                db.Open();
                string sql = "SELECT Barkod, UrunAdi, StokMiktari FROM Urunler";
                if (!string.IsNullOrEmpty(filter)) sql += " WHERE UrunAdi LIKE @p OR Barkod LIKE @p";
                sql += " ORDER BY StokMiktari ASC";

                using (var cmd = new SqliteCommand(sql, db))
                {
                    if (!string.IsNullOrEmpty(filter)) cmd.Parameters.AddWithValue("@p", $"%{filter}%");
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            results.Add(new StokGorunum
                            {
                                Barkod = rdr.IsDBNull(0) ? "" : rdr.GetString(0),
                                UrunAdi = rdr.IsDBNull(1) ? "" : rdr.GetString(1),
                                StokMiktari = rdr.IsDBNull(2) ? 0 : rdr.GetDouble(2)
                            });
                        }
                    }
                }
            }
            dgStok.ItemsSource = results;
        }

        private void txtStokAra_TextChanged(object sender, TextChangedEventArgs e) => SyncInventory(txtStokAra.Text.Trim());

        private void dgStok_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgStok.SelectedItem is StokGorunum item)
            {
                _activeBarcode = item.Barkod;
                txtSeciliUrun.Text = item.UrunAdi;
                txtMevcutStok.Text = item.StokMiktari.ToString("N2");
                txtYeniStok.Clear(); txtYeniStok.Focus();
            }
        }

        private void btnStokGuncelle_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_activeBarcode)) { MessageBox.Show("Önce ürünü seçin."); return; }

            if (double.TryParse(txtYeniStok.Text.Replace(".", ","), out double addedAmount))
            {
                using (var db = new SqliteConnection(DatabaseHelper.ConnectionString))
                {
                    db.Open();
                    var cmd = new SqliteCommand("UPDATE Urunler SET StokMiktari = StokMiktari + @m WHERE Barkod = @b", db);
                    cmd.Parameters.AddWithValue("@m", addedAmount);
                    cmd.Parameters.AddWithValue("@b", _activeBarcode);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Stok envanteri güncellendi.", "Başarılı");
                ResetView();
                SyncInventory();
            }
            else MessageBox.Show("Geçerli bir miktar girin.");
        }

        private void ResetView() { txtStokAra.Clear(); txtSeciliUrun.Clear(); txtMevcutStok.Clear(); txtYeniStok.Clear(); _activeBarcode = ""; }

        private void btnAnaliz_Click(object sender, RoutedEventArgs e) => new AnalizWindow().ShowDialog();
    }

    public class StokGorunum
    {
        public string Barkod { get; set; }
        public string UrunAdi { get; set; }
        public double StokMiktari { get; set; }
    }
}