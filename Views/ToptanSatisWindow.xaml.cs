using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using Microsoft.Data.Sqlite;
using PcKod.UI.Models;
using PcKod.UI.Data;

namespace PcKod.UI.Views
{
    public partial class ToptanSatisWindow : Window
    {
        public ObservableCollection<SepetUrun> WholesaleCart { get; set; } = new ObservableCollection<SepetUrun>();

        public ToptanSatisWindow()
        {
            InitializeComponent();
            dgToptanSepet.ItemsSource = WholesaleCart;
            InitCompanies();
        }

        private void btnGeri_Click(object sender, RoutedEventArgs e) => this.Close();

        private void InitCompanies()
        {
            var list = new System.Collections.Generic.List<Firma>();
            using (var db = new SqliteConnection(DatabaseHelper.ConnectionString))
            {
                db.Open();
                var rdr = new SqliteCommand("SELECT * FROM Firmalar", db).ExecuteReader();
                while (rdr.Read()) list.Add(new Firma { Id = rdr.GetInt32(0), FirmaAdi = rdr.GetString(1) });
            }
            cmbFirmalar.ItemsSource = list;
        }

        private void txtUrunAra_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = txtUrunAra.Text.Trim();
            if (q.Length < 2) { lstAramaSonuclari.Visibility = Visibility.Collapsed; return; }

            var results = new System.Collections.Generic.List<Urun>();
            using (var db = new SqliteConnection(DatabaseHelper.ConnectionString))
            {
                db.Open();
                var cmd = new SqliteCommand("SELECT Barkod, UrunAdi, SatisFiyati FROM Urunler WHERE UrunAdi LIKE @p", db);
                cmd.Parameters.AddWithValue("@p", $"%{q}%");
                var rdr = cmd.ExecuteReader();
                while (rdr.Read()) results.Add(new Urun { Barkod = rdr.GetString(0), UrunAdi = rdr.GetString(1), SatisFiyati = rdr.GetDecimal(2) });
            }
            lstAramaSonuclari.ItemsSource = results;
            lstAramaSonuclari.Visibility = results.Any() ? Visibility.Visible : Visibility.Collapsed;
        }

        private void lstAramaSonuclari_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lstAramaSonuclari.SelectedItem is Urun item)
            {
                WholesaleCart.Add(new SepetUrun { Barkod = item.Barkod, UrunAdi = item.UrunAdi, BirimFiyat = item.SatisFiyati, Miktar = 1 });
                txtUrunAra.Clear(); lstAramaSonuclari.Visibility = Visibility.Collapsed;
                UpdateWholesaleTotal();
            }
        }

        private void dgToptanSepet_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) => Dispatcher.BeginInvoke(new Action(() => UpdateWholesaleTotal()), System.Windows.Threading.DispatcherPriority.Background);

        private void btnToptanBitir_Click(object sender, RoutedEventArgs e)
        {
            if (cmbFirmalar.SelectedItem == null || !WholesaleCart.Any()) return;

            var firma = (Firma)cmbFirmalar.SelectedItem;
            decimal total = WholesaleCart.Sum(s => s.ToplamTutar);

            using (var db = new SqliteConnection(DatabaseHelper.ConnectionString))
            {
                db.Open();
                using (var tr = db.BeginTransaction())
                {
                    try
                    {
                        var cmd = new SqliteCommand("INSERT INTO Satislar (UrunAdi, Miktar, ToplamTutar, OdemeYontemi, Tarih, FirmaId) VALUES ('Toptan Satış', 1, @a, 'Veresiye', @d, @f)", db, tr);
                        cmd.Parameters.AddWithValue("@a", total.ToString(CultureInfo.InvariantCulture));
                        cmd.Parameters.AddWithValue("@d", DateTime.Now.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@f", firma.Id);
                        cmd.ExecuteNonQuery();

                        var debtCmd = new SqliteCommand("UPDATE Firmalar SET ToplamBorc = ToplamBorc + @a WHERE Id = @f", db, tr);
                        debtCmd.Parameters.AddWithValue("@a", total.ToString(CultureInfo.InvariantCulture));
                        debtCmd.Parameters.AddWithValue("@f", firma.Id);
                        debtCmd.ExecuteNonQuery();

                        tr.Commit();
                        MessageBox.Show("Cari hesaba borç kaydedildi.");
                        this.Close();
                    }
                    catch (Exception ex) { tr.Rollback(); MessageBox.Show(ex.Message); }
                }
            }
        }

        private void UpdateWholesaleTotal()
        {
            decimal total = WholesaleCart.Sum(s => s.ToplamTutar);
            if (txtToptanToplam != null) txtToptanToplam.Text = total.ToString("C2", new CultureInfo("tr-TR"));
        }

        private void btnFirmaEkle_Click(object sender, RoutedEventArgs e) { new FirmaEkleWindow().ShowDialog(); InitCompanies(); }
    }
}