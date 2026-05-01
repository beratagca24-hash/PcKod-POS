<h1 align="center">
  PcKod POS | Perakende ve Envanter Yönetim Sistemi
</h1>

<div align="center">

![WPF](https://img.shields.io/badge/WPF-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/.NET_8-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-003B57?style=for-the-badge&logo=sqlite&logoColor=white)
![Status](https://img.shields.io/badge/Status-Completed-success?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge)

<br/>

**.NET 8 & WPF tabanlı, barkod okuyucu uyumlu, entegre terazi desteği sunan ve çevrimdışı (offline-first) çalışabilen modern masaüstü satış sistemidir.**

</div>

---

## 🚀 Projenin Amacı

**PcKod POS**, perakende sektöründeki işletmelerin operasyonel süreçlerini merkezileştirmeyi hedefler. Temel amacı; donanım entegrasyonu (terazi, barkod okuyucu), satış takibi, envanter yönetimi ve cari hesap kayıtlarını tek bir platformda birleştirerek veri tutarlılığını sağlamak ve işlem sürelerini optimize etmektir.

---

## ✨ Temel Özellikler

* 🏷️ **Gelişmiş Barkod ve Terazi Entegrasyonu**
  Standart ticari barkodların yanı sıra, endüstriyel ağırlık terazilerinden üretilen EAN-13 formatındaki barkodları (27, 28, 29 önekleri) dinamik olarak ayrıştırır ve net gramaj bilgisini anında işler.

* 🛒 **Esnek Tahsilat ve Kasa Yönetimi**
  Çoklu ödeme tiplerini destekler. İşlem anında uygulanabilen manuel indirim ve yuvarlama farklarını, finansal kayıtların doğruluğu için veritabanında ayrı bir işlem kalemi olarak raporlar.

* 🚚 **Cari Hesap Takibi (B2B)**
  Kurumsal müşteriler veya düzenli alıcılar için toptan satış ve açık hesap (veresiye) yönetim altyapısı sunar. Firma bazlı borç-alacak dengesini takip eder.

* 📊 **Finansal ve Stok Analitiği**
  Satış verilerini analiz ederek son 30 günlük işlem hacmini grafiksel olarak görselleştirir. Ürün tiplerine (adet/kg) göre özel olarak belirlenen eşik değerleri üzerinden kritik stok uyarıları üretir.

* 📑 **Dışa Aktarım ve Raporlama**
  Gün sonu finansal verilerini, uygulanan iskontoları ve net hasılat dökümünü `ClosedXML` altyapısı ile Excel (.xlsx) formatında raporlar.

---

## 🛠️ Mimari ve Teknolojik Altyapı

Proje, kodun ölçeklenebilirliğini ve bakım kolaylığını sağlamak adına Separation of Concerns (Sorumlulukların Ayrılması) prensibine uygun olarak katmanlı bir yapıda geliştirilmiştir.

| Katman / Bileşen | Teknoloji & Standart |
| :--- | :--- |
| **Arayüz (UI)** | WPF (Windows Presentation Foundation) |
| **Çekirdek Dil** | C# 12 (.NET 8.0) |
| **Veritabanı** | SQLite (Yerel Depolama) |
| **Raporlama Motoru**| ClosedXML |
| **Sistem Mimarisi** | Data, Models, Helpers ve Views izolasyonu |

---

## 📂 Dosya Organizasyonu

```text
PcKod.UI/
 ├── Data/         # DatabaseHelper.cs (Merkezi bağlantı ve veritabanı inşası)
 ├── Helpers/      # BarcodeParser.cs, CurrencyHelper.cs (Bağımsız iş mantığı)
 ├── Models/       # Urun.cs, Firma.cs, SepetUrun.cs (Veri modelleri)
 ├── Views/        # XAML arayüzleri ve UI arka plan kodları
 └── App.xaml      # Uygulama yaşam döngüsü
```

---

## ⚙️ Kurulum ve Derleme

Sistemi yerel ortamda çalıştırmak için aşağıdaki adımları izleyebilirsiniz:

```bash
# 1. Depoyu yerel makinenize klonlayın
git clone [https://github.com/beratagca24-hash/PcKod.UI.git](https://github.com/beratagca24-hash/PcKod.UI.git)

# 2. Proje dizinine gidin
cd PcKod.UI

# 3. .sln dosyasını Visual Studio ile açın.
# 4. Eksik NuGet paketlerini (Microsoft.Data.Sqlite, ClosedXML) geri yükleyin.
# 5. Projeyi 'Release' konfigürasyonunda derleyin ve çalıştırın.
```

> 💡 **Erişim Notu:** 
> İlk kurulum aşamasında uygulamanın Kritik Ayarlar ve Raporlama bölümlerine erişim için varsayılan yetkili şifresi **`1234`** olarak yapılandırılmıştır.

---

## 👨‍💻 Geliştirici

* **Berat Ağca**
  * **GitHub:** [@beratagca24-hash](https://github.com/beratagca24-hash)
  * **LinkedIn:** [Berat Ağca](https://www.linkedin.com/in/berat-ağca-0a61a5342/)

*(Not: Sistemin terazi entegrasyonu ve kararlılık testleri, pilot bölge olarak Öz Biga Et ve Şarküteri altyapısında gerçekleştirilmiştir.)*

---

## 📝 Lisans

Bu proje **MIT Lisansı** altında lisanslanmıştır. Kullanım hakları ve dağıtım detayları için `LICENSE` dosyasını inceleyebilirsiniz.
