# Yol Haritası

## Faz 1: Core hesap motoru

- `AtolyeRecete.Core` ve `AtolyeRecete.Tests` projeleri
- Modeller: Ingredient, Recipe, RecipeItem, Platform
- `CostCalculator`, `PricingCalculator` (marj/markup)
- `HESAP_MOTORU.md`'deki senaryolar xUnit testi olarak
- Çıkış kriteri: tüm testler yeşil

## Faz 2: Data ve ilk ekranlar

- SQLite şeması, repository'ler
- `SeedService`: mum başlangıç paketi (8-10 hammadde, 2 reçete)
- Hammadde listesi/ekleme/düzenleme (CollectionView)
- Reçete listesi ve detay, canlı maliyet gösterimi (MVVM binding)

## Faz 3: Pazaryeri simülatörü

- `platforms.json` okuma
- Picker ile platform, marj/markup seçimi, hedef kâr girişi
- Önerilen satış fiyatı ekranı

## Faz 4: Paylaşım ve sunum cilası

- Müşteriye fiyat listesi PDF'i (QuestPDF) + Share API
- Hammadde fiyat güncelleme, tüm reçetelerin otomatik yenilenmesi, marj düşüş uyarısı
- Ayarlar (Preferences): saatlik işçilik, genel gider %, para birimi
- Sunum demosu: seed data ile baştan sona akış

## Sunum sonrası (onay gelirse)

- Excel prototipiyle retention testi, landing page ile ödeme isteği testi
- Backend, hesap/yedek, abonelik (RevenueCat)
- Stok bildirimi, sipariş kuyruğu, ikinci dikey (butik pasta)
