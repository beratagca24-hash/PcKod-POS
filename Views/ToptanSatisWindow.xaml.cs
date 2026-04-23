using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using PcKod.UI.Models;
using System.Globalization;

namespace PcKod.UI.Views
{
    public partial class ToptanSatisWindow : Window
    {
        public ObservableCollection<SepetUrun> ToptanSepet { get; set; } = new ObservableCollection<SepetUrun>();
        private const string ConnectionString = "Data Source=PcKod.db";

        public ToptanSatisWindow()
        {
            InitializeComponent();
            dgToptanSepet.ItemsSource = ToptanSepet;
            FirmalariYukle();
        }

        private void btnGeri_Click(object sender, RoutedEventArgs e) => this.Close();

        private void FirmalariYukle()
        {
            var firmalar = new List<Firma>();
            using (var db = new SqliteConnection(ConnectionString))
            {
                db.Open();
                var cmd = new SqliteCommand("SELECT * FROM Firmalar", db);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        firmalar.Add(new Firma { Id = reader.GetInt32(0), FirmaAdi = reader.GetString(1) });
                    }
                }
            }
            cmbFirmalar.ItemsSource = firmalar;
        }

        private void txtUrunAra_TextChanged(object sender, TextChangedEventArgs e)
        {
            string ara = txtUrunAra.Text.Trim();
            if (ara.Length < 2) { lstAramaSonuclari.Visibility = Visibility.Collapsed; return; }

            var sonuclar = new List<Urun>();
            using (var db = new SqliteConnection(ConnectionString))
            {
                db.Open();
                var cmd = new SqliteCommand("SELECT * FROM Urunler WHERE UrunAdi LIKE @p", db);
                cmd.Parameters.AddWithValue("@p", "%" + ara + "%");
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        sonuclar.Add(new Urun { Barkod = reader.GetString(1), UrunAdi = reader.GetString(2), SatisFiyati = reader.GetDecimal(4) });
                    }
                }
            }
            if (sonuclar.Count > 0) { lstAramaSonuclari.ItemsSource = sonuclar; lstAramaSonuclari.Visibility = Visibility.Visible; }
            else { lstAramaSonuclari.Visibility = Visibility.Collapsed; }
        }

        private void lstAramaSonuclari_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lstAramaSonuclari.SelectedItem is Urun secili)
            {
                ToptanSepet.Add(new SepetUrun { Barkod = secili.Barkod, UrunAdi = secili.UrunAdi, BirimFiyat = secili.SatisFiyati, Miktar = 1 });
                txtUrunAra.Clear();
                lstAramaSonuclari.Visibility = Visibility.Collapsed;
                ToplamHesapla();
            }
        }

        // Tabloda hücre düzenlemesi bittiğinde genel toplamı anında günceller
        private void dgToptanSepet_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Hücredeki verinin modele işlenmesi için çok kısa bir süre bekleyip hesaplatıyoruz
            Dispatcher.BeginInvoke(new Action(() => ToplamHesapla()), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void btnToptanBitir_Click(object sender, RoutedEventArgs e)
        {
            if (cmbFirmalar.SelectedItem == null || ToptanSepet.Count == 0)
            {
                MessageBox.Show("Lütfen firma ve ürün seçin!");
                return;
            }

            var seciliFirma = (Firma)cmbFirmalar.SelectedItem;
            decimal toplamTutar = ToptanSepet.Sum(s => s.ToplamTutar);

            using (var db = new SqliteConnection(ConnectionString))
            {
                db.Open();
                var tr = db.BeginTransaction();
                try
                {
                    string sqlTutar = toplamTutar.ToString(CultureInfo.InvariantCulture);

                    new SqliteCommand($"INSERT INTO Satislar (UrunAdi, Miktar, ToplamTutar, OdemeYontemi, Tarih, FirmaId) VALUES ('Toptan Satış', 1, {sqlTutar}, 'Veresiye', '{DateTime.Now:yyyy-MM-dd}', {seciliFirma.Id})", db, tr).ExecuteNonQuery();
                    new SqliteCommand($"UPDATE Firmalar SET ToplamBorc = ToplamBorc + {sqlTutar} WHERE Id = {seciliFirma.Id}", db, tr).ExecuteNonQuery();

                    tr.Commit();
                    MessageBox.Show("Satış başarıyla borca kaydedildi!");
                    this.Close();
                }
                catch (Exception ex) { tr.Rollback(); MessageBox.Show("Hata: " + ex.Message); }
            }
        }

        private void ToplamHesapla()
        {
            decimal toplam = ToptanSepet.Sum(s => s.ToplamTutar);
            if (txtToptanToplam != null) txtToptanToplam.Text = toplam.ToString("C2", new CultureInfo("tr-TR"));
        }

        private void btnFirmaEkle_Click(object sender, RoutedEventArgs e)
        {
            var frmEkle = new FirmaEkleWindow();
            frmEkle.ShowDialog();
            FirmalariYukle();
        }
    }
}