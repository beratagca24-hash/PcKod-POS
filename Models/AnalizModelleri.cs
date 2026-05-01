namespace PcKod.UI.Models
{
    public class SatisAnalizModel
    {
        public string UrunAdi { get; set; }
        public double ToplamMiktar { get; set; }
        public double GrafikGenişligi { get; set; }
        public string MiktarGosterim => ToplamMiktar.ToString("N2");
    }

    public class KritikStokModel
    {
        public string UrunAdi { get; set; }
        public double KalanMiktar { get; set; }
        public string Birim { get; set; }
        public string KalanGosterim => $"{KalanMiktar:N2} {Birim}";
    }
}