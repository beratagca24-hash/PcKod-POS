using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PcKod.UI.Models
{
    // INotifyPropertyChanged eklendi: Bir değer değiştiğinde ekranı anında günceller.
    public class SepetUrun : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Barkod { get; set; }
        public string UrunAdi { get; set; }

        private decimal _birimFiyat;
        public decimal BirimFiyat
        {
            get => _birimFiyat;
            set { _birimFiyat = value; OnPropertyChanged(); OnPropertyChanged(nameof(ToplamTutar)); }
        }

        private double _miktar;
        public double Miktar
        {
            get => _miktar;
            set { _miktar = value; OnPropertyChanged(); OnPropertyChanged(nameof(ToplamTutar)); }
        }

        public decimal ToplamTutar => (decimal)Miktar * BirimFiyat;

        public int BirimTipi { get; set; }
        public string BirimMetni => BirimTipi == 0 ? "Adet" : "Kg";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}