using System;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace PcKod.UI.Views
{
    public partial class FirmaEkleWindow : Window
    {
        public FirmaEkleWindow()
        {
            InitializeComponent();
        }

        // ANA SAYFAYA DÖNÜŞ
        private void btnGeri_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // FİRMA KAYDETME
        private void btnFirmaKaydet_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFirmaAdi.Text))
            {
                MessageBox.Show("Lütfen bir isim girin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new SqliteConnection("Data Source=PcKod.db"))
                {
                    db.Open();
                    var cmd = new SqliteCommand("INSERT INTO Firmalar (FirmaAdi, ToplamBorc) VALUES (@ad, 0)", db);
                    cmd.Parameters.AddWithValue("@ad", txtFirmaAdi.Text.Trim());
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Firma başarıyla eklendi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt Hatası: " + ex.Message);
            }
        }
    }
}