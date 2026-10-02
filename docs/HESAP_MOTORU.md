# Hesap Motoru Spesifikasyonu

Tüm formüller `AtolyeRecete.Core` içinde, `decimal` ile. Yuvarlama sadece son sonuçta, `MidpointRounding.AwayFromZero`, 2 basamak.

## Terimler

- **Birim fiyat** = alış fiyatı / alış miktarı (örn. 1000 g = 300 TL, 0.30 TL/g)
- **Kalem maliyeti** = birim fiyat x kullanılan miktar x (1 + kalem fire%)
- **Malzeme maliyeti** = kalem maliyetlerinin toplamı
- **İşçilik** = süre (dk) / 60 x saatlik ücret
- **Genel gider payı** = (malzeme + işçilik) x genel gider% (elektrik, fire payı vb.)
- **Parti maliyeti** = malzeme + işçilik + genel gider payı
- **Birim maliyet (C)** = parti maliyeti / verim (reçetenin kaç adet ürün çıkardığı)

## Fiyat önerisi

Girdiler: C (birim maliyet), F (sabit ücretler: kargo + işlem ücreti), k (komisyon oranı), m (hedef kâr oranı).

**Marj modu (varsayılan), kâr satış fiyatı üzerinden:**

`P = (C + F) / (1 - k - m)`

Geçerlilik: `k + m < 1`, aksi halde fiyat hesaplanamaz (hata/uyarı döndür).

**Markup modu, kâr maliyet üzerinden:**

`P = (C x (1 + m) + F) / (1 - k)`

Geçerlilik: `k < 1`.

Arayüzde mod açıkça seçtirilir; aynı "%40" iki modda çok farklı fiyat verir.

## Test senaryosu 1 (altın örnek)

Hammadde: soya mumu 1000 g = 300 TL (0.30 TL/g), esans 50 ml = 150 TL (3 TL/ml), fitil 2 TL/adet, cam kavanoz 25 TL/adet.

Reçete "200cc aromaterapi mumu", verim = 1:
- 180 g mum = 54.00, 10 ml esans = 30.00, 1 fitil = 2.00, 1 kavanoz = 25.00 → malzeme 111.00
- İşçilik: 20 dk, saatlik 150 TL → 50.00
- Genel gider %10 x (111 + 50) = 16.10
- Parti maliyeti = 177.10, C = 177.10

Beklenen:
- Marj %40, komisyon %20, F = 0 → **442.75**
- Marj %40, komisyon %3.5, F = 0 → **313.45**
- Markup %40, komisyon %20, F = 0 → **309.93** (309.925, AwayFromZero)

## Test senaryosu 2 (verim)

Aynı reçete, ama parti 10 adet çıkarıyorsa ve malzeme/işçilik tüm parti için girilmişse C = parti maliyeti / 10.

## Kenar durumlar (test yazılacak)

- k + m >= 1 → hata
- Verim 0 veya negatif → hata
- Miktar 0 olan kalem → maliyet 0
- Hammadde fiyatı güncellenince tüm reçetelerin C değeri yeniden hesaplanır (hesaplanan değer saklanmaz, her seferinde türetilir)

## Platformlar (`platforms.json`, örnek oranlar, yayın öncesi doğrulanacak)

Alanlar: `name`, `commissionRate`, `fixedFee`, `notes`. Oranlar koda gömülmez.
