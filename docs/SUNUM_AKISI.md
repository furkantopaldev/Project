# Sunum Akışı (Demo Betiği)

Süre: yaklaşık 8-10 dakika. Tüm rakamlar örnek veri paketiyle uygulamada doğrulanmıştır.

## Hazırlık (sunumdan önce, bir kez)

1. Uygulamayı aç: `dotnet run --project AtolyeRecete.App -f net9.0-windows10.0.19041.0`
2. **Ayarlar** sekmesi:
   - "Tüm verileri sil", ardından "Örnek verileri yükle" (başlangıç durumuna döner).
   - İşletme adı: kendi (veya örnek) atölye adını yaz. Saatlik ücret `150`, genel gider `10`, marj uyarı eşiği `30` olmalı. **Kaydet**.
3. **Ana Sayfa**'da şunları görmelisin: 10 hammadde, 2 reçete, "2 / 2" fiyat listesinde, yeşil "Her şey yolunda" kutusu.

Başlangıç rakamları:

| Reçete | Maliyet | Satış | Marj |
|---|---|---|---|
| 100cc mini vanilya mumu | ₺125,79 | ₺250,00 | %49,69 |
| 200cc aromaterapi mumu | ₺177,10 | ₺450,00 | %60,64 |

## Akış

### 1. Sorun (30 sn)
El yapımı üreticiler fiyatı genelde "komşu ne kadar satıyor"a bakarak koyuyor. Maliyet (fire, işçilik, genel gider) ve pazaryeri komisyonu hesaba girmiyor. Hammadde fiyatı her ay değişiyor ama fiyatlar güncellenmiyor.

### 2. Hammaddeler (1 dk)
**Hammaddeler** sekmesi: soya mumu 1000 g = ₺300, yani birim fiyat **₺0,30/g**. Tek birimde (g, ml, adet) tutuluyor.

### 3. Reçete ve canlı maliyet (2 dk)
**Reçeteler** → "200cc aromaterapi mumu":
- Malzeme **₺111,00**, işçilik (20 dk x 150 TL/saat) **₺50,00**, genel gider (%10) **₺16,10**, parti **₺177,10**.
- Satış fiyatı ₺450 → "Bu fiyatla marj: **%60,64**".
- Canlı değişiklik: **Verim**'i 10 yap, birim maliyet anında **₺17,71** olur. Sonra 1'e geri al (kaydetmeden çık).
- Vurgu: hesap her seferinde türetilir, saklanmaz; hiçbir yerde "eski fiyatla kalmış" reçete olmaz.

### 4. Pazaryeri simülatörü (2 dk)
**Simülatör** → reçete: 200cc, hedef kâr `40`:
- **Trendyol, marj %40: ₺442,75** (komisyon ₺88,55, net kâr ₺177,10).
- **Kendi sitem: ₺313,45** (komisyon %3,5). Alt tablo aynı ürünün tüm platformlardaki fiyatını gösterir.
- Modu **Markup**'a çevir, Trendyol: **₺309,93**. Vurgu: "aynı %40 iki modda çok farklı fiyat verir"; bu yüzden mod her zaman açıkça seçilir ve etiketlidir.
- Kargo/ek sabit ücret kutusuna `20`: marj modunda Trendyol **₺492,75**.

### 5. Enflasyon modu: hammadde zammı (2 dk)
**Hammaddeler** → "Soya mumu" → fiyatı `300`'den `1500`'e çıkar → Kaydet:
- "Marj uyarısı" penceresi çıkar:
  - 200cc: %60,64 → **%7,84** (maliyet ₺177,10 → ₺414,70)
  - 100cc: %49,69 → **-%0,21** (zararına satış, maliyet ₺125,79 → ₺250,53)
- **Ana Sayfa**'da "Dikkat gerektirenler" turuncu görünür.
- Geri al: soya mumu fiyatını `300`'e çevir (bu sefer uyarı çıkmaz, marj yükseldi).
- İsteğe bağlı: **Ayarlar**'da saatlik ücreti `200` yap: 200cc maliyeti **₺195,43**, simülatörde Trendyol %40 marj **₺488,58**. Sonra `150`'ye geri al.

### 6. Müşteri fiyat listesi PDF'i (1 dk)
**Fiyat listesi** → "PDF oluştur" → "Aç". Vurgu: müşteriye **yalnızca ürün adı ve fiyat** gider; maliyet ve marj paylaşılmaz. Fiyatı girilmemiş reçeteler listeye girmez.

### 7. Sonraki adım: doğrulama planı (1 dk)
Bu bir demo; ürün değil. Henüz doğrulanmayan iki risk:
- **Retention:** Fiyatı bir kez hesaplayan kullanıcı bırakabilir. Plan: Excel prototipiyle 10-15 üretici, 1 ay sonra kullanım ölçümü.
- **Ödeme isteği:** Hedef kitlenin ödeme gücü düşük olabilir. Plan: landing page + fiyat gösterip ön kayıt ölçümü.

## Sunum sonrası sıfırlama
**Ayarlar** → "Tüm verileri sil" → "Örnek verileri yükle"; saatlik ücret 150, eşik 30.

## Bilinen sınırlar (sorulursa dürüstçe söyle)
- **Komisyon oranları örnektir** (`platforms.json`), yayın öncesi güncel platform tablolarından doğrulanmalıdır.
- Fiyatlar **KDV hariç**; KDV ayarı yok.
- Şu an yalnızca **Windows**; Android sonraki adım.
- Hammadde fiyat geçmişi tutulmuyor, yalnızca güncel fiyat var.
- Backend, hesap, senkron ve abonelik yok (veriler yalnızca bu bilgisayarda).
- PDF "Paylaş" düğmesi Windows'ta (paketlenmemiş uygulama) denenmedi; "Aç" ile açıp oradan gönderilebilir.

## Olası zor sorular
- **"Komisyon oranları doğru mu?"** Örnek; tablo `platforms.json` içinde, koda gömülü değil. İleride uzaktan güncellenebilir.
- **"Neden sadece mum?"** Basit reçete yapısı ve net birimler; pasta gibi alt reçeteli ürünler sonraki dikey.
- **"Fire nasıl hesaplanıyor?"** Kalem bazında: birim fiyat x miktar x (1 + fire %). Genel gider fireyi içermez, çift sayım olmasın diye.
- **"Marj ile markup farkı?"** Marj: kâr satış fiyatı üzerinden. Markup: kâr maliyet üzerinden. %40 marj ile %40 markup farklı fiyat verir; uygulama ikisini de etiketleyerek gösterir.
