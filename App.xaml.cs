using System.Windows;
using PcKod.UI.Data; // Veritabanı yardımcı sınıfına ulaşmak için

namespace PcKod.UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Program başlar başlamaz tabloları (Urunler, Satislar vb.) oluşturur
            DatabaseHelper.InitializeDatabase();
        }
    }
}