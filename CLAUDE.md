# AtölyeReçete

El yapımı butik üreticiler (ilk dikey: **mum atölyeleri**) için maliyet, fire ve pazaryeri fiyat motoru. Mobil uygulama, local-first.

Amaç şu an: öğrenme + sunum için çalışan, güzel bir MVP. Pazar doğrulaması (retention, ödeme isteği) sunum onaylanırsa sonraki aşama. Detay: `docs/KARARLAR.md`.

## Teknoloji

- .NET MAUI (.NET 9), MVVM, `CommunityToolkit.Mvvm` (source generators), `CommunityToolkit.Maui`
- Yerel veri: `sqlite-net-pcl` (backend YOK, MVP'de eklenmeyecek)
- PDF: QuestPDF (müşteriye giden fiyat listesi)
- Testler: xUnit (Core hesap motoru + Data katmanı, bellek içi SQLite ile)

## Çözüm yapısı

```
AtolyeRecete.Core   -> domain modelleri + hesap motoru. Saf C#, MAUI/SQLite bağımlılığı YOK.
AtolyeRecete.Data   -> SQLite repository'leri, SeedService
AtolyeRecete.App    -> MAUI: Views, ViewModels, Shell
AtolyeRecete.Tests  -> Core ve Data için xUnit testleri
```

## Kesin kurallar

- Para hesabı `decimal`. `double`/`float` ile para hesabı yapma.
- Yuvarlama: `MidpointRounding.AwayFromZero`, 2 basamak, sadece son gösterim/sonuç adımında.
- Hesap mantığı ViewModel'e veya View'a YAZILMAZ; sadece `Core`'da yaşar. Her formül değişikliği test ile gelir.
- "Kâr" her zaman açıkça etiketlenir: **marj** (satış fiyatı üzerinden) veya **markup** (maliyet üzerinden). UI'da seçtirilir, varsayılan marj.
- Komisyon oranları koda gömülmez; `platforms.json` içinde tutulur (ileride uzaktan çekilebilsin). Oranlar örnektir, yayın öncesi güncel kaynaktan doğrulanır.
- Arayüz metinleri Türkçe, kod/sınıf/metot adları İngilizce.

## MVP kapsamı

Var: hammadde CRUD, reçete (verim, işçilik, genel gider payı), pazaryeri simülatörü, fiyat listesi PDF, hammadde fiyatı değişince reçetelerin otomatik güncellenmesi, mum başlangıç paketi (seed data).

Yok (sonra): stok bildirimi, sipariş kuyruğu, çoklu para birimi, barkod, hesap/senkron/abonelik, alt reçeteler.

## Çalışma şekli

- Bu proje claude.ai sohbetinde planlandı. Mimari kararlar ve nedenleri `docs/KARARLAR.md`, fazlar `docs/YOL_HARITASI.md`, formüller ve test senaryoları `docs/HESAP_MOTORU.md` içinde.
- Yeni önemli bir karar alındığında `docs/KARARLAR.md` güncellenir (tarih + karar + neden).
- Bir göreve başlamadan önce ilgili docs dosyasını oku; formül değiştirmeden önce `HESAP_MOTORU.md`'deki test senaryolarını kontrol et.

## Komutlar

```
dotnet test
dotnet build AtolyeRecete.App -f net9.0-windows10.0.19041.0
```

Uygulama şu an sadece Windows hedefli (Android sonra eklenecek). Çalıştırma: `dotnet run --project AtolyeRecete.App -f net9.0-windows10.0.19041.0`.
Veritabanı: `%LOCALAPPDATA%\User Name\com.atolyerecete.app\Data\atolyerecete.db3` (silinirse seed yeniden yüklenir).

## Durum

- [x] Faz 1: Core hesap motoru + testler (25 test yeşil)
- [x] Faz 2: Data katmanı + hammadde/reçete ekranları
- [ ] Faz 3: Pazaryeri simülatörü
- [ ] Faz 4: PDF + fiyat güncelleme + sunum cilası
