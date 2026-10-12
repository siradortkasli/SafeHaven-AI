# SafeHaven AI — Yapay Zekâ Destekli Afet Koordinasyon Platformu

**SafeHaven AI**, afet anında deprem, bina ve güvenli toplanma alanlarına ilişkin bilgileri tek bir masaüstü arayüzünde sunmayı amaçlayan bir hackathon projesidir.

Bu proje, **Avrupa Birliği finansmanıyla, UNDP ve Türkiye Belediyeler Birliği (TBB) iş birliğiyle düzenlenen Kamuda Dijital Dönüşüm ve Yapay Zekâ Hackathonu** kapsamında ekip çalışması olarak geliştirilmiştir.

## Öne çıkan özellikler

- **Afet izleme paneli:** Deprem bilgilerinin arayüzde görüntülenmesi ve harita üzerinde incelenmesi.
- **Bina verileri:** Binalarla ilişkili bilgilerin ve değerlendirme çıktılarının görüntülenmesine yönelik bileşenler.
- **Güvenli alanlar:** Toplanma alanlarının harita üzerinde gösterimi ve kapasite/doluluk bilgisinin işlenmesine yönelik modeller ve yardımcı sınıflar.
- **Yapay zekâ destekli değerlendirme:** Python backend üzerinde yapay zekâ ajanı ve analiz uç noktaları; uygun yapılandırmada haricî model servisiyle çalışma, aksi durumda alternatif çıktılar.
- **Masaüstü ve servis entegrasyonu:** C# WPF arayüzün FastAPI backend servisleriyle iletişimi.

## Kullanılan teknolojiler

| Katman | Teknolojiler |
| --- | --- |
| Masaüstü uygulaması | C#, .NET 8, WPF, XAML |
| Backend / API | Python, FastAPI, Uvicorn |
| Veri işleme | Pandas, SQLAlchemy, SQLite |
| Harita bileşenleri | GMap.NET, WebView2, HTML, JavaScript |
| Yapay zekâ entegrasyonu | OpenAI Python istemcisi (isteğe bağlı API anahtarıyla) |

## Proje yapısı

```text
safehaven-ai/
├── backend/
│   ├── main.py              # FastAPI servisleri
│   ├── ai_agent.py          # Yapay zekâ ajanı ve değerlendirme mantığı
│   ├── data_sync.py         # Veri toplama / eşitleme
│   ├── init_db.py           # Veritabanı başlangıç işlemleri
│   ├── requirements.txt
│   ├── .env.example
│   └── start.ps1
├── frontend/
│   └── SafeHaven/
│       ├── SafeHaven.sln
│       └── SafeHaven/
│           ├── MainWindow.xaml
│           ├── MainWindow.xaml.cs
│           ├── Models/
│           ├── Helpers/
│           └── Services/
└── README.md
```

## Yerel kurulum

**Gereksinimler:** Windows, Python, .NET 8 SDK ve WPF geliştirme desteğine sahip Visual Studio.

### 1. Backend

```powershell
cd backend
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
Copy-Item .env.example .env
python init_db.py
python -m uvicorn main:app --reload --host 127.0.0.1 --port 8000
```

İsteğe bağlı yapay zekâ ve adres servisi anahtarlarını `backend/.env` dosyasına yalnızca yerel bilgisayarınızda ekleyin. Kullanılan dış veri servisleri ağ erişimi gerektirebilir; bazı özellikler API anahtarı veya güncel veri olmadan sınırlı çalışabilir.

### 2. Masaüstü arayüz

`frontend/SafeHaven/SafeHaven.sln` dosyasını Visual Studio ile açın, NuGet paketlerini geri yükleyin ve projeyi çalıştırın. Backend'in `http://127.0.0.1:8000` adresinde çalıştığından emin olun.

## Projedeki görevim

Hackathon ekibinde **C# WPF masaüstü arayüzü, harita ve veri görselleştirme bileşenleri ile backend servis entegrasyonu** üzerinde çalıştım. Deprem ve bina verilerinin, güvenli alanların ve analiz sonuçlarının operatöre sunulmasına yönelik ekranların geliştirilmesine katkıda bulundum.

## Güvenlik ve yayınlama notu

Bu depoya sanal ortam (`venv`), yerel veritabanı (`*.db`), derleme çıktıları, gerçek `.env` dosyaları veya API anahtarları eklenmemelidir. Örnek yapılandırma olarak yalnızca `.env.example` paylaşılır.

## Lisans

Bu depoda açık kaynak lisansı belirtilmemiştir. Kaynak kodun yeniden kullanımı için proje ekibinin hakları ve izinleri dikkate alınmalıdır.
