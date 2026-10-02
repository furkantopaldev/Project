# AtölyeReçete Kod Rehberi

> Bu rehber, projeyi **ilk kez yazan bir bilgisayar mühendisliği öğrencisi** için hazırlandı. Amaç sadece "kod ne yapıyor"u anlatmak değil; **neden böyle yazdık, hangi alternatifleri düşündük, hangi mühendislik kavramı burada devreye giriyor ve nasıl doğruladık** sorularına da cevap vermek. Tüm rakamlar, dosya adları ve kod parçaları projenin gerçek haliyle uyumludur.

---

## İçindekiler

1. [Büyük resim: ne yapıyor, nasıl katmanlandı](#1-büyük-resim-ne-yapıyor-nasıl-katmanlandı)
2. [Dosya haritası](#2-dosya-haritası)
3. [Core: hesap motoru](#3-core-hesap-motoru)
4. [Data: kalıcılık katmanı (SQLite)](#4-data-kalıcılık-katmanı-sqlite)
5. [Pdf: müşteri fiyat listesi](#5-pdf-müşteri-fiyat-listesi)
6. [App: MAUI uygulaması (MVVM)](#6-app-maui-uygulaması-mvvm)
7. [Uçtan uca bir örnek: "Soya mumunun fiyatını değiştir"](#7-uçtan-uca-bir-örnek-soya-mumunun-fiyatını-değiştir)
8. [Test: ne yaptık, neye karşılık geliyor](#8-test-ne-yaptık-neye-karşılık-geliyor)
9. [Mühendislik terimleri sözlüğü](#9-mühendislik-terimleri-sözlüğü)
10. [Bir bilgisayar mühendisliği öğrencisi için dersler](#10-bir-bilgisayar-mühendisliği-öğrencisi-için-dersler)
11. [Dürüst liste: teknik borç ve bilinen sınırlar](#11-dürüst-liste-teknik-borç-ve-bilinen-sınırlar)
12. [Alıştırmalar](#12-alıştırmalar)
13. [Ek: komutlar, kaynaklar](#13-ek-komutlar-kaynaklar)

---

## Bu rehber nasıl okunur

- **`> Terim — ...`** ile başlayan kutular, o noktada geçen bir mühendislik kavramının kısa tanımıdır. Bölüm 9'da hepsi alfabetik bir sözlükte de toplanmıştır.
- **"Hata hikayesi"** başlıkları, geliştirirken gerçekten yaşadığımız bir hatayı ve bu hatanın kökünü anlatır. Bunlar, ders kitabı örneklerinden daha kalıcı öğretir; bölüm 8'de hepsi tek tabloda toplandı.
- **Sayılar uydurma değil.** Rehberdeki örnek hesaplar (177,10 / 442,75 / 313,45 / 309,93 gibi) hem birim testlerde, hem örnek veride, hem de çalışan uygulamada doğrulandı.
- Önce 1. ve 2. bölümü, sonra 7. bölümü (uçtan uca örnek) oku; geri kalanı ihtiyaca göre dolaş.

### Rakamlarla proje

| Ölçü | Değer |
|---|---|
| Teknoloji | .NET 9, .NET MAUI 9 (Android + Windows), C# 13, SQLite, SkiaSharp |
| Çözümdeki proje | 5 (Core, Data, Pdf, App, Tests) |
| Üretim kodu (C#) | ≈ 2.820 satır (Core ≈ 410, Data ≈ 390, Pdf ≈ 370, App ≈ 1.650) |
| Arayüz (XAML sayfaları) | ≈ 580 satır (+ şablondan gelen stil dosyası) |
| Test kodu | ≈ 1.160 satır, **118 otomatik test** (98 test metodu) |
| Commit | 21 (bu rehber hariç; hepsi aynı gün, her biri bir "kilometre taşı") |
| Platform | Windows (masaüstü), Android (telefon) |

Core + Data + Pdf (≈ 1.170 satır) için ≈ 1.160 satır test yazıldı: **test/üretim oranı yaklaşık 1'e 1.** Bu oran test edilebilir katmanlar için; arayüz katmanı (ViewModel/View) için otomatik test yoktur, bunu bölüm 8 ve 11'de dürüstçe tartışıyoruz.

---

## 1. Büyük resim: ne yapıyor, nasıl katmanlandı

### 1.1 Ürün

**AtölyeReçete**, el yapımı butik üreticiler (ilk dikey: **mum atölyeleri**) için bir **maliyet, fire ve pazaryeri fiyat motorudur**:

1. Hammaddeleri girersin (soya mumu 1000 g = 300 TL).
2. Reçete tanımlarsın: hangi hammaddeden ne kadar, fire yüzdesi, işçilik süresi, genel gider oranı, kaç adet çıkıyor.
3. Uygulama **birim maliyeti** hesaplar (örn. 200cc mum: 177,10 TL).
4. **Simülatör**, "Trendyol'da %40 marjla kaça satmalıyım?" sorusunu cevaplar (442,75 TL), komisyonu ve net kârı gösterir.
5. Bir hammaddenin fiyatı değişince, marjı kritik seviyenin altına düşen reçeteleri **uyarır** ("enflasyon modu").
6. Müşteriye gönderilecek **fiyat listesini PDF** olarak üretir (maliyet ve marj içermez).

Hepsi **telefonda yerel** çalışır: sunucu (backend), hesap, senkronizasyon yoktur. Buna yazılımda **local-first** yaklaşımı denir.

> **Terim — Local-first (yerel öncelikli):** Verinin birincil kopyası kullanıcının cihazındadır; uygulama internet olmadan çalışır. Avantaj: basitlik, hız, gizlilik. Bedel: yedekleme ve cihazlar arası senkronizasyon senin sorunun olur (bu projede henüz yok).

### 1.2 Mimari: katmanlı (layered) yapı

```
┌───────────────────────── AtolyeRecete.App (.NET MAUI) ──────────────────────────┐
│                                                                                 │
│   Views (XAML)  ⇄ veri bağlama ⇄  ViewModels  →  Services (Ayarlar, Açılış, …)  │
│                                                                                 │
└──────────────┬───────────────────────────┬───────────────────────────┬──────────┘
               │ kullanır                  │ kullanır                  │
               ▼                           ▼                           │
      AtolyeRecete.Data            AtolyeRecete.Pdf                    │
      SQLite + Repository          SkiaSharp + yerleşim                │
               │                           │                           │
               └────────────┬──────────────┘                           │
                            ▼                                          │
                   AtolyeRecete.Core   ◄───────────────────────────────┘
        domain modelleri + hesap motoru (kimseye bağımlı DEĞİL)

   AtolyeRecete.Tests ──► Core, Data, Pdf   (xUnit; App'e bağlı değil)
```

**Okun yönü "bağımlılık yönü"dür.** Ok A'dan B'ye gidiyorsa "A, B'yi bilir; B, A'yı bilmez". Kural: **bağımlılıklar hep içeri, Core'a doğru akar.**

> **Terim — Katmanlı mimari (layered architecture):** Kodu, her biri tek bir sorumluluğu olan katmanlara ayırma. Burada: arayüz (App), kalıcılık (Data), çıktı üretimi (Pdf), iş kuralları (Core).
>
> **Terim — Dependency rule (bağımlılık kuralı):** Robert C. Martin'in "Clean Architecture" kitabındaki ilke: iç katmanlar (iş kuralları) dış katmanları (arayüz, veritabanı, çerçeve) tanımaz. Böylece dış detaylar değişince çekirdeğe dokunmazsın.
>
> **Terim — Separation of concerns (ilgilerin ayrılması):** "Hesap nasıl yapılır" ile "ekranda nasıl görünür" ile "veritabanına nasıl yazılır" ayrı yerlerde yaşar.
>
> **Terim — Coupling / Cohesion (bağlaşım / bağdaşım):** İyi tasarım, modüller arası bağlaşımı düşük, modül içi bağdaşımı yüksek tutar. `CostCalculator` yalnızca maliyet bilir (yüksek bağdaşım); ekran, veritabanı ya da PDF'ten haberi yoktur (düşük bağlaşım).

### 1.3 Neden bu kadar katman? (karar günlüğü: `docs/KARARLAR.md`)

| Karar | Neden |
|---|---|
| **Core saf C#**, MAUI/SQLite/PDF bağımlılığı yok | Ürünün değeri **hesabın doğruluğunda**. Hesap, arayüz olmadan **test edilebilmeli**; ileride web/PWA'ya taşınabilmeli. |
| Hesap mantığı **ViewModel'e yazılmaz** | Aksi halde formül ekrana gömülür, test edilemez, kopyalanır. Her formül değişikliği test ile gelir. |
| Data'da ayrı "Row" sınıfları | Core modelleri `[PrimaryKey]` gibi SQLite niteliklerini taşımasın. |
| Pdf ayrı proje | PDF üretimi MAUI'siz test edilebilsin; üretim kütüphanesi değişince (QuestPDF → SkiaSharp'ta yaptık) sadece bir proje etkilensin. |
| Backend yok | Amaç öğrenme + sunum; local-first. Sunucu, yayın öncesi doğrulama sonrası konuşulur. |

> **Terim — ADR (Architecture Decision Record, mimari karar kaydı):** "Hangi kararı, ne zaman, neden aldık" notları. `docs/KARARLAR.md` bunun hafif bir sürümüdür. 6 ay sonra "bunu neden böyle yapmışız?" sorusunun cevabı koddan değil, buradan okunur. Profesyonel ekiplerde çok yaygın bir pratiktir.

---

## 2. Dosya haritası

Aşağıdaki tablo, projedeki **her kaynak dosyayı** ve görevini listeler (satır sayıları yaklaşık).

### 2.1 `AtolyeRecete.Core` (≈ 410 satır): iş kuralları

| Dosya | Görev |
|---|---|
| `Models/UnitOfMeasure.cs` | Birimler: gram, mililitre, adet (enum). |
| `Models/Ingredient.cs` | Hammadde. **Birim fiyat türetilmiş özelliktir** (`UnitPrice`). |
| `Models/RecipeItem.cs` | Reçetedeki bir kalem: hammadde + miktar + fire oranı. |
| `Models/Recipe.cs` | Reçete: kalemler, verim, işçilik süresi, genel gider oranı, **satış fiyatı**. |
| `Models/Platform.cs` | Pazaryeri: komisyon oranı, sabit ücret. |
| `Money.cs` | Para yuvarlama (tek yer). |
| `TextOrder.cs` | Türkçe alfabeye göre sıralama kuralı (tek yer). |
| `CostCalculator.cs` | Maliyet formülleri (malzeme, işçilik, genel gider, parti, birim). |
| `ProfitMode.cs` | Marj / markup seçimi (enum). |
| `PricingCalculator.cs` | Fiyat önerisi, döküm (`Analyze`), satış fiyatındaki marj. |
| `PriceBreakdown.cs` | Önerilen fiyatın dökümü (komisyon, net kâr, marj, markup). |
| `PlatformCatalog.cs` | `platforms.json`'u okur ve **doğrular**. |
| `PriceList.cs` | Müşteri listesine hangi reçeteler girer, nasıl sıralanır. |
| `MarginAlerts.cs` | "Marj düşüş uyarısı" kuralları (önce/sonra karşılaştırma). |

### 2.2 `AtolyeRecete.Data` (≈ 390 satır): kalıcılık

| Dosya | Görev |
|---|---|
| `AtolyeDatabase.cs` | SQLite bağlantısı, tablo kurulumu, test için bellek içi veritabanı. |
| `Rows/IngredientRow.cs`, `RecipeRow.cs`, `RecipeItemRow.cs` | Tablo satırı sınıfları + Core modeline/modelinden çeviri. |
| `Rows/DecimalText.cs` | `decimal` ⇄ metin dönüşümü (kayıpsız saklama). |
| `Repositories/IIngredientRepository.cs`, `IRecipeRepository.cs` | Depo **sözleşmeleri** (arayüz). |
| `Repositories/IngredientRepository.cs`, `RecipeRepository.cs` | Depo gerçekleştirmeleri. |
| `SeedService.cs` | Mum başlangıç paketi (örnek veri) ve "hepsini sil". |

### 2.3 `AtolyeRecete.Pdf` (≈ 370 satır): PDF çıktısı

| Dosya | Görev |
|---|---|
| `PriceListLayout.cs` | **Çizimden bağımsız** satır sarma ve sayfalama algoritmaları. |
| `TextStyle.cs` | Yazı tipi, ölçüm, çizim, eksik harf için yazı tipi yedeği. |
| `PriceListPdf.cs` | Belgeyi kurar: başlık, tablo, altbilgi; SkiaSharp ile PDF'e çizer. |

### 2.4 `AtolyeRecete.App` (≈ 1.650 satır C# + ≈ 580 satır XAML): arayüz

| Dosya / klasör | Görev |
|---|---|
| `MauiProgram.cs` | **Composition root**: bağımlılık enjeksiyonu kayıtları, uygulamanın kurulumu. |
| `App.xaml(.cs)`, `AppShell.xaml(.cs)` | Uygulama kabuğu: alt sekmeler (5), rota kayıtları. |
| `Services/AppSettings.cs` | Ayarlar (saatlik ücret, genel gider, marj eşiği, işletme adı, örnek veri bayrağı). |
| `Services/StartupService.cs` | İlk açılışta veritabanını kurar, örnek veriyi bir kez yükler. |
| `Services/PlatformService.cs` | `platforms.json`'u uygulama paketinden okur. |
| `Services/Display.cs` | **Sunum yardımcıları**: para/yüzde biçimlendirme, sayı okuma. Hesap yok. |
| `Services/AppDefaults.cs` | Sabit kültür (tr-TR). |
| `ViewModels/*.cs` (9 dosya) | Her ekranın durumu ve komutları. |
| `Views/*.xaml(.cs)` (8 sayfa) | Ekranlar (arayüz tanımı, "code-behind" neredeyse boş). |
| `Resources/Raw/platforms.json` | **Komisyon tablosu** (kodda değil, veri olarak). |
| `Resources/Styles/*.xaml` | Renkler (marka rengi) ve stiller (açık/koyu tema). |
| `Resources/AppIcon`, `Splash` | Mum simgesi ve açılış ekranı (SVG). |
| `Platforms/Android/*` | Android'e özgü: tema renkleri, `MainActivity`. |

### 2.5 `AtolyeRecete.Tests` (≈ 1.160 satır): 118 otomatik test

| Dosya | Test sayısı | Neyi doğrular |
|---|---|---|
| `CostCalculatorTests.cs` | 12 | Maliyet formülleri, altın örnek, fire, verim, fiyat değişimi |
| `PricingCalculatorTests.cs` | 19 | Marj/markup fiyatı, sabit ücret, geçersiz oranlar, satış fiyatında marj |
| `PriceBreakdownTests.cs` | 7 | Komisyon, net kâr, gerçekleşen marj/markup |
| `MarginAlertsTests.cs` | 15 | Marj düşüş uyarı kuralı (eşik, sınır, kıyas) |
| `PlatformCatalogTests.cs` | 17 | `platforms.json` okuma ve doğrulama, gerçek dosyanın geçerliliği |
| `PriceListTests.cs` | 7 | Listeye girme, Türkçe sıralama, boş/negatif fiyat |
| `DataTests.cs` | 21 | Gerçek (bellek içi) SQLite ile depolar, örnek veri, kayıpsız decimal, Türkçe sıralama |
| `PriceListLayoutTests.cs` | 15 | Satır sarma, sayfalama, yazı tipi kapsamı |
| `PriceListPdfTests.cs` | 5 | PDF'in üretilmesi (Türkçe harf, boş liste, çok sayfa) |

### 2.6 Dokümanlar (`docs/`, `CLAUDE.md`)

| Dosya | Rol |
|---|---|
| `CLAUDE.md` | Proje kuralları ve komutlar ("projenin anayasası"). |
| `docs/KARARLAR.md` | Karar günlüğü (ADR). |
| `docs/HESAP_MOTORU.md` | **Spesifikasyon**: formüller ve altın örnek. Testler buradan türetildi. |
| `docs/YOL_HARITASI.md` | Fazlar. |
| `docs/SUNUM_AKISI.md` | Demo betiği. |
| `docs/KOD_REHBERI.md` | Bu dosya. |

> **Terim — Spesifikasyon (spec) ve "tek doğruluk kaynağı":** Kod yazmadan önce formülleri ve beklenen sonuçları bir dokümana yazdık (`HESAP_MOTORU.md`). Testler bu dokümandan türedi. Kod ile doküman çelişirse **önce hangisinin yanlış olduğuna** karar verilir. Bu, "önce düşün, sonra kodla" disiplininin somut hali.

---

## 3. Core: hesap motoru

Core, uygulamanın **kalbi**dir ve neredeyse hiçbir şeye bağımlı değildir (sadece .NET'in kendi kütüphanesi, JSON için `System.Text.Json`). Bu yüzden tüm formüller bir telefona ya da veritabanına ihtiyaç duymadan test edilir.

### 3.1 Para neden `decimal`? (ve yuvarlama kuralı)

Projenin ilk "kesin kuralı": **para hesabı `decimal` ile yapılır, `double`/`float` ile asla.**

Neden? Bilgisayar ondalık sayıları ikili (binary) kesir olarak saklar. `0,1` ikili tabanda sonsuz tekrar eden bir sayıdır, tıpkı 1/3'ün onluk tabanda `0,333...` olması gibi. Sonuç:

```csharp
double a = 0.1 + 0.2;     // 0.30000000000000004  (değil 0.3!)
decimal b = 0.1m + 0.2m;  // 0.3                  (tam)
```

`decimal` ise **onluk tabanda** saklanır (96 bitlik tamsayı + 0-28 arası ondalık kaydırma). 0,1 gibi "insan sayıları" tam gösterilir; para için tasarlanmıştır. Ama dikkat: `decimal` de **her sayıyı tam göstermez**. 1/3'ü 28 basamağa kadar tutar. Bu detay aşağıdaki hata hikayesinde karşımıza çıkacak.

> **Terim — IEEE 754 kayan nokta (floating point):** `float`/`double`'ın standardı. Çok geniş aralığı hızlı hesaplar ama ondalık kesirleri yaklaşık tutar. Bilimsel/grafik hesap için uygun, **para için uygun değil**. Klasik kaynak: David Goldberg, *"What Every Computer Scientist Should Know About Floating-Point Arithmetic"*.

**Yuvarlama** tek bir yerde, `Money.cs`'te:

```csharp
public static decimal Round(decimal value) =>
    Math.Round(value, 2, MidpointRounding.AwayFromZero);
```

İki kural var: (1) **2 basamak**, (2) **`AwayFromZero`** ("yarım kuruş yukarı"). .NET'in varsayılanı `ToEven`'dır ("banker's rounding": tam ortadaysa en yakın **çift** sayıya). Fark gerçek bir senaryoda ortaya çıkıyor:

| Değer | `ToEven` (varsayılan) | `AwayFromZero` (bizim seçim) |
|---|---|---|
| 309,925 | 309,92 | **309,93** |
| 442,755 | 442,76 | 442,76 |

Altın örneğimizdeki markup fiyatı tam olarak 309,925; müşteri/satıcı beklentisi "yarım kuruş yukarı"dır, bu yüzden test `309,93` bekler.

Üçüncü kural: **yuvarlama sadece son adımda yapılır.** Ara sonuçları yuvarlarsan hatalar birikir (ör. 3 kalemi tek tek yuvarlayıp toplamak, toplamı yuvarlamaktan farklı çıkabilir). `CostCalculator` hiçbir ara sonucu yuvarlamaz; yuvarlama gösterim anında (`Display.Money`) ya da "önerilen fiyat" gibi nihai sonuçta yapılır.

> **Terim — Magic number / tek sorumluluk noktası:** Yuvarlama kuralı 20 yerde `Math.Round(x, 2, ...)` diye yazılsaydı, biri unutup `ToEven` kullandığında tutarsız para hesapları çıkardı. `Money.Round` bu kuralı **tek yere** toplar (DRY: *Don't Repeat Yourself*).

### 3.2 Modeller (`Models/`)

```csharp
public class Ingredient
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public UnitOfMeasure Unit { get; set; }
    public decimal PurchasePrice { get; set; }      // toplam alış fiyatı (1000 g için 300)
    public decimal PurchaseQuantity { get; set; }   // alış miktarı (1000)

    public decimal UnitPrice =>                     // türetilmiş: 300 / 1000 = 0,30
        PurchaseQuantity > 0
            ? PurchasePrice / PurchaseQuantity
            : throw new InvalidOperationException(
                $"'{Name}' için alış miktarı sıfırdan büyük olmalı.");
}
```

Dikkat edilecek dört nokta:

1. **`UnitPrice` saklanmaz, türetilir.** Veritabanında "birim fiyat" diye bir sütun yoktur. Alış fiyatı veya miktarı değişince birim fiyat **otomatik** doğru olur. (Bkz. 3.3: "türet, saklama".)
2. **Sınıf değişmezini (invariant) korur.** Alış miktarı 0 ise birim fiyat tanımsızdır (sıfıra bölme); sessizce `Infinity`/sıfır dönmek yerine **anlamlı bir hata** fırlatır.
3. **Oranlar kesir olarak tutulur** (`0,10` = %10). Modelde `WasteRate = 0.05m` yazılır; "5" yazıp 100'e bölmek karışıklığı **yalnızca arayüz sınırında** yapılır (`Display.RateFromPercent`).
4. **Anemik model.** Modeller neredeyse sadece veri taşır; davranış (formül) ayrı `static` sınıflardadır.

```csharp
public class RecipeItem
{
    public required Ingredient Ingredient { get; set; }  // "required": oluştururken verilmek ZORUNDA
    public decimal Quantity { get; set; }
    public decimal WasteRate { get; set; }               // 0.05 = %5
}
```

`required` anahtar kelimesi (C# 11), bir kalemin hammaddesiz oluşturulmasını **derleme zamanında** engeller. Hata, çalışırken değil, kodu yazarken yakalanır (*fail early*).

> **Terim — Anemic domain model (anemik alan modeli):** Veri sınıfları + ayrı "servis/hesaplayıcı" sınıfları. Martin Fowler bunu bazen "anti-pattern" olarak eleştirir (davranış verinin yanında olmalı, yani *rich domain model*). Biz bilinçli olarak **fonksiyonel çekirdek** tarzını seçtik: hesaplayıcılar `static` ve **saf fonksiyon**dur; test etmesi ve akıl yürütmesi kolaydır. Trade-off: model sınıfları kendi kurallarını (ör. "verim > 0") zorlayamaz, kuralları hesaplayıcı denetler.
>
> **Terim — Saf fonksiyon (pure function):** Aynı girdiyle her zaman aynı çıktıyı verir ve **yan etkisi yoktur** (dosya yazmaz, global durumu değiştirmez, saate/rastgeleliğe bakmaz). Test etmek en kolay kod türüdür: girdi ver, çıktıya bak. `CostCalculator` ve `PricingCalculator`'ın tüm metotları saftır.

### 3.3 `CostCalculator`: maliyet formülleri

```csharp
public static decimal ItemCost(RecipeItem item) =>
    item.Ingredient.UnitPrice * item.Quantity * (1 + item.WasteRate);

public static decimal MaterialCost(Recipe recipe) => recipe.Items.Sum(ItemCost);

public static decimal LaborCost(Recipe recipe, decimal hourlyRate) =>
    recipe.LaborMinutes * hourlyRate / 60m;                          // ÖNCE çarp, SONRA böl

public static decimal OverheadCost(Recipe recipe, decimal hourlyRate) =>
    (MaterialCost(recipe) + LaborCost(recipe, hourlyRate)) * recipe.OverheadRate;

public static decimal BatchCost(Recipe recipe, decimal hourlyRate) =>
    MaterialCost(recipe) + LaborCost(recipe, hourlyRate) + OverheadCost(recipe, hourlyRate);

public static decimal UnitCost(Recipe recipe, decimal hourlyRate) =>
    recipe.Yield > 0 ? BatchCost(recipe, hourlyRate) / recipe.Yield
                     : throw new InvalidOperationException($"'{recipe.Name}' için verim sıfırdan büyük olmalı.");
```

**Altın örnek** (200cc aromaterapi mumu, saatlik 150 TL), elle:

| Adım | Hesap | Sonuç |
|---|---|---|
| Soya mumu | 180 g × 0,30 | 54,00 |
| Lavanta esansı | 10 ml × 3,00 | 30,00 |
| Pamuk fitil | 1 × 2,00 | 2,00 |
| Cam kavanoz 200cc | 1 × 25,00 | 25,00 |
| **Malzeme** | toplam | **111,00** |
| **İşçilik** | 20 dk × 150 / 60 | **50,00** |
| **Genel gider** | (111 + 50) × %10 | **16,10** |
| **Parti** | 111 + 50 + 16,10 | **177,10** |
| **Birim (verim 1)** | 177,10 / 1 | **177,10** |

Verim 10 olursa birim maliyet 17,71. Kalem fire örneği: 100 g mum, %10 fire → 0,30 × 100 × 1,10 = 33,00.

#### Hata hikayesi: 49,999… (decimal de her şeyi tam tutmaz)

İşçiliği ilk yazdığımda doğal geleni yazdım: `süre / 60 × ücret`. Test **kırmızı** çıktı: beklenen 50,00, bulunan **49,99999999999999999999999999**.

Neden? `20 / 60m` = 1/3'tür ve `decimal` bunu 28 basamakla keser: `0,3333333333333333333333333333`. Bunu 150 ile çarpınca tam 50 değil, 49,9999… olur. `double` hatasına benzer ama **onluk tabanda**: 1/3 hiçbir sonlu onluk kesirle ifade edilemez.

**Çözüm:** sırayı değiştirmek: önce çarp (`20 × 150 = 3000`), sonra böl (`3000 / 60 = 50`). Tamsayıya yakın çarpımlar tam kalır, **bölme en sona** bırakılır. Aynı ilke `PricingCalculator`'da da uygulanır (tek bölme, en sonda).

> **Terim — İşlem sırası ve sayısal kararlılık (numerical stability):** Matematiksel olarak eşit ifadeler, sınırlı hassasiyette farklı sonuç verebilir. Kural: **bölmeyi ve yuvarlamayı mümkün olduğunca geciktir.**
>
> **Terim — Regresyon testi:** Bir hata bulunduktan sonra, o hatayı yakalayan test eklenir; böylece hata sessizce geri dönemez. `Golden_LaborCost` testi tam bu işi yapar.

#### "Türet, saklama" ilkesi (hesaplanan değer saklanmaz)

Reçetenin birim maliyeti **hiçbir yerde saklanmaz**. Her ekran açıldığında `CostCalculator.UnitCost(...)` yeniden hesaplar. Sonuç: bir hammaddenin fiyatı değişince **tüm reçeteler, simülatör, ana sayfa, PDF anında doğru olur**; "reçetedeki maliyet eski kalmış" diye bir hata sınıfı yoktur.

> **Terim — Single Source of Truth (SSOT, tek doğruluk kaynağı):** Her bilginin tek bir otoriter yeri vardır, diğerleri ondan türer.
>
> **Terim — Denormalizasyon ve önbellek geçersizleştirme (cache invalidation):** Hesaplanan değeri saklarsan (denormalizasyon), kaynak değişince onu güncellemeyi **unutabilirsin**. Bilgisayar biliminin meşhur sözü: *"İki zor şey var: önbellek geçersizleştirme ve isimlendirme."* Bizim çözümümüz: saklamamak. Bedel: her seferinde hesaplamak (bu ölçekte ihmal edilebilir; bkz. bölüm 11).

### 3.4 `PricingCalculator`: fiyat nasıl bulunur

İki mod vardır ve **ikisi de ekranda açıkça etiketlenir**; çünkü aynı "%40" iki modda çok farklı fiyat verir.

**Türetme.** `C` birim maliyet, `F` sabit ücretler (kargo + işlem), `k` komisyon oranı, `m` hedef kâr, `P` satış fiyatı.

*Marj modu* (kâr, **satış fiyatı** üzerinden): kâr = `m·P`. Fiyat; maliyeti, sabit ücreti, komisyonu ve kârı karşılar:

```
P = C + F + k·P + m·P
P − k·P − m·P = C + F
P·(1 − k − m) = C + F
P = (C + F) / (1 − k − m)
```

*Markup modu* (kâr, **maliyet** üzerinden): kâr = `m·C`.

```
P = C + m·C + F + k·P
P·(1 − k) = C·(1 + m) + F
P = (C·(1 + m) + F) / (1 − k)
```

Altın örnek (`C = 177,10`, `F = 0`):

| Mod | Komisyon | Hesap | Fiyat |
|---|---|---|---|
| Marj %40 | %20 | 177,10 / (1 − 0,20 − 0,40) | **442,75** |
| Marj %40 | %3,5 | 177,10 / (1 − 0,035 − 0,40) | **313,45** |
| Markup %40 | %20 | 177,10 × 1,40 / 0,80 = 309,925 | **309,93** |

Doğrulama (marj, %20): komisyon = 442,75 × 0,20 = 88,55; net kâr = 442,75 − 88,55 − 177,10 = 177,10 = fiyatın tam **%40**'ı. ✓

**Geçerlilik** (matematiğin kenar durumu): paydalar pozitif olmalı. Marj modunda `k + m < 1`, markup modunda `k < 1`. Aksi halde fiyat tanımsız ya da negatiftir ("%70 komisyon + %40 marj"). Kod bunu iki metotla ele alır:

```csharp
public static bool IsFeasible(decimal commissionRate, decimal profitRate, ProfitMode mode) =>
    mode switch
    {
        ProfitMode.Margin => commissionRate + profitRate < 1m,
        ProfitMode.Markup => commissionRate < 1m,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
```

`SuggestPrice` ise geçersizse `InvalidOperationException` fırlatır. Neden hem bool hem exception? Arayüz, kullanıcı her tuşa bastığında "hesaplanabilir mi?" diye sorar; bunu `try/catch` ile yapmak hem yavaş hem kötü stildir. **Önce soran (guard), sonra hesaplayan** düzeni kullandık.

> **Terim — Exception ile hata yönetimi vs. Result tipi vs. TryXxx deseni:** Üç yaklaşım: (1) istisna fırlat (bizim `SuggestPrice`), (2) sonuç nesnesi döndür (`Result<T>`: başarı/hata), (3) `bool TryParse(out x)` deseni (.NET'in kendisi). İstisnalar "beklenmeyen" durumlar içindir; "kullanıcı yanlış değer girdi" beklenen bir durumdur, bu yüzden arayüz önce `IsFeasible` ile sorar.
>
> **Terim — Pattern matching ve switch ifadesi:** `mode switch { ... }` bir *ifadedir* (değer döndürür) ve `_` ile "diğer her şey" yakalanır. Enum'a yeni bir değer eklenirse, `_` kolu hatayı açığa çıkarır (sessizce yanlış hesap yerine).

**`Analyze` ve `PriceBreakdown`.** Önerilen fiyatla birlikte komisyon, net kâr ve **gerçekleşen** marj/markup'ı döndürür. Önemli ince nokta: döküm, **yuvarlanmış fiyat** üzerinden hesaplanır, çünkü satıcının gerçekten alacağı fiyat kuruşa yuvarlanmış olandır. Bu yüzden markup %40 hedefi, gerçekte %39,99…'dur (testte `InRange(0.3999, 0.4001)`).

```csharp
public record PriceBreakdown(decimal Price, decimal UnitCost, decimal Commission,
                             decimal FixedFee, decimal NetProfit, decimal MarginRate, decimal MarkupRate);
```

> **Terim — `record` (immutable değer nesnesi):** C#'ın `record` türü, otomatik **değer eşitliği** (iki kayıt, alanları eşitse eşittir), `ToString()` ve değiştirilemez (immutable) yapı verir. Hesap sonuçları gibi "veri paketi" için idealdir; başka kod parçası içindeki sonucu yanlışlıkla değiştiremez.

**`MarginAtPrice`:** kullanıcı reçeteye *kendi* satış fiyatını girdiğinde marjı bulur: `(S − C) / S`. `S ≤ 0` ise hata. Maliyetin altında satışta marj **negatif** döner (zararına satış); arayüz bunu ayrıca vurgular.

### 3.5 `MarginAlerts`: "marj düşüş uyarısı"nın kuralları

Kullanıcı bir hammaddenin fiyatını kaydederken, **önce ve sonra** tüm reçetelerin durumunu karşılaştırırız:

```csharp
public static bool IsBelowThreshold(decimal margin, decimal thresholdRate) => margin < thresholdRate;

public static IReadOnlyList<MarginDrop> FindDrops(
    IEnumerable<Recipe> before, IEnumerable<Recipe> after, decimal hourlyRate, decimal thresholdRate)
{
    var beforeById = before.ToDictionary(r => r.Id);
    var drops = new List<MarginDrop>();
    foreach (var recipe in after)
    {
        if (!beforeById.TryGetValue(recipe.Id, out var old)) continue;
        if (old.SalePrice is not { } oldPrice || recipe.SalePrice is not { } newPrice) continue;
        ...
        if (marginAfter < marginBefore && IsBelowThreshold(marginAfter, thresholdRate))
            drops.Add(new MarginDrop(...));
    }
    return drops;
}
```

Bir reçete uyarı listesine girmek için **üç şartı birden** sağlamalıdır: (1) satış fiyatı var, (2) marj **kötüleşti**, (3) yeni marj eşiğin **kesinlikle altında**.

**Neden üç şart?** İki "saf" alternatifin ikisi de kötü:

- Sadece "eşiğin altında" dersek, zaten düşük ve hiç değişmemiş reçeteler **her kayıtta** uyarı üretir (gürültü).
- Sadece "marj düştü" dersek, 56,2'den 55,9'a inen zararsız oynamalar bile uyarır.

**Kesin küçüktür (`<`)** bilinçli bir sınır kararıdır: marj tam eşiğe eşitse "düşük" sayılmaz. Bu karar bir testle kayıt altındadır (`MarginExactlyAtThreshold_IsNotReported`).

Kural tek yerde (`IsBelowThreshold`): ana sayfa, reçete listesi, reçete düzenleme ve uyarı **aynı fonksiyonu** çağırır. Aksi halde dört ekranda dört farklı "düşük" tanımı zamanla ayrışırdı.

> **Terim — Sınır değer analizi (boundary value analysis):** Hatalar çoğunlukla sınırlarda çıkar: `<` mi `≤` mi? Bu yüzden tam sınırı (`0,30`), sınırın hemen altını (`0,29`) ve üstünü (`0,31`) test ederiz.
>
> **Terim — Önce/sonra karşılaştırması (diff / snapshot):** Bir değişikliğin etkisini bulmak için değişiklikten önce ve sonra durumu alıp farkına bakmak. Burada saf bir fonksiyon (`FindDrops`) iki listeyi karşılaştırır; veritabanı bile bilmez, bu yüzden 15 hızlı testle kuşatılabildi.

### 3.6 `PlatformCatalog`: konfigürasyon veri olarak (JSON)

Komisyon oranları **koda gömülü değildir**; `Resources/Raw/platforms.json` içindedir:

```json
{ "version": 1, "note": "ÖRNEK ORANLAR...", "platforms": [
    { "name": "Trendyol", "commissionRate": 0.20, "fixedFee": 0, "notes": "..." }, ... ] }
```

Neden? (1) Oranlar sık değişir; kod değil **veri** değişmeli. (2) İleride uzaktan (sunucudan) güncellenebilir, `"version"` alanı bunun için orada. (3) Yayın öncesi "örnek oranları gerçek kaynaktan doğrula" notu tek dosyada.

`PlatformCatalog.Parse` dosyayı okur ve **doğrular**:

```csharp
private record PlatformDto(string? Name, decimal? CommissionRate, decimal? FixedFee, string? Notes);
```

Alanların **nullable (`?`)** olması kritik bir tasarım kararıdır: JSON'da `commissionRate` unutulursa, `decimal` varsayılan olarak `0` olurdu ve **sessizce %0 komisyonlu** bir platform oluşurdu; fiyatlar yanlış çıkardı. Nullable ile "alan yok" ile "değer 0" ayırt edilir, eksik alan `FormatException` verir. Doğrulamalar: boş ad, oran `[0, 1)` dışında, negatif ücret, yinelenen ad, boş liste, bozuk JSON.

> **Terim — Fail fast (erken ve yüksek sesle başarısız ol):** Hatalı veriyi sessizce kabul edip ileride tuhaf bir yerde patlamasını beklemek yerine, **giriş sınırında** reddet. Hata mesajı da nerede/ne olduğunu söylesin (`platforms[3] ('Etsy'): 'commissionRate' eksik.`).
>
> **Terim — DTO (Data Transfer Object):** Dış dünyadan (JSON) gelen ham veriyi taşıyan, iş kuralı içermeyen basit sınıf. Önce DTO'ya okunur, doğrulanır, sonra **gerçek model**e (`Platform`) çevrilir. Böylece geçersiz bir `Platform` hiç oluşmaz.
>
> **Terim — Veri odaklı tasarım (data-driven) / konfigürasyon:** Davranışı, kod yerine veriyle değiştirebilmek.

### 3.7 `PriceListBuilder`: kim listeye girer, nasıl sıralanır

```csharp
if (recipe.SalePrice is { } price and > 0m) entries.Add(new PriceListEntry(recipe.Name.Trim(), price));
else skipped++;
...
entries.Sort((a, b) => TextOrder.Turkish.Compare(a.Name, b.Name));   // TextOrder.Turkish = tr-TR, büyük/küçük harf fark etmez
```

Kural: **yalnızca satış fiyatı girilmiş (> 0) reçeteler** listeye girer; girmeyenlerin sayısı döndürülür (ekranda "1 reçete listeye girmiyor" demek için).

**Türkçe sıralama** düşündüğünden zor bir konu (kural `Core/TextOrder.cs`'te tek yerde, hammadde ve reçete listeleri de aynı karşılaştırıcıyı kullanır; bkz. 4.9): `ç, ğ, ı, ö, ş, ü` harfleri Türkçe alfabede belirli yerlere oturur. Karşılaştırma **kültüre duyarlı (culture-aware)** olmalı; düz (ordinal, bayt/kod noktası) sıralama `çam`'ı `Zeytin`'den **sonraya** atardı (çünkü `ç` = U+00E7, `Z` = U+005A'dan büyük).

> **Terim — Unicode, kod noktası, kültür:** Metin, "karakter"lerden değil **Unicode kod noktaları**ndan oluşur; alfabetik sıra ise **dil/kültür** kuralıdır, sayısal kod sırası değildir.
>
> **Terim — "Türk I problemi":** Türkçede `i/İ` ve `ı/I` ayrı harf çiftleridir. `"I".ToLower()` Türkçe kültürde `ı`, İngilizcede `i` verir. Bu yüzden kimlik/anahtar karşılaştırmalarında `ToUpper/ToLower` yerine açık karşılaştırıcı kullanılır ve kullanıcıya görünen sıralamada kültür belirtilir. Klasik bir yazılım hatası kaynağıdır.

---

## 4. Data: kalıcılık katmanı (SQLite)

Core "hesap yapar"; Data **"hatırlar"**. Hammadde ve reçeteleri telefonun içindeki bir SQLite veritabanı dosyasında saklar. Backend yoktur; dosya uygulamanın kendi özel klasöründedir.

> **Terim — SQLite:** Sunucu gerektirmeyen, tek dosyalık, gömülü ilişkisel veritabanı. Telefonlarda, tarayıcılarda, uçaklarda... dünyanın en yaygın veritabanı motoru. MySQL/PostgreSQL ile **aynı işi yapan rakip değil**, farklı katmandır: onlar sunucu, SQLite yerel depodur. (Karar günlüğündeki "MySQL'e geçilmeyecek" kararı bu yüzden.)

### 4.1 ORM ve "Row" sınıfları

Kullandığımız kütüphane `sqlite-net-pcl` bir **mikro-ORM**'dir: C# sınıflarını tablolara, nesne özelliklerini sütunlara eşler.

> **Terim — ORM (Object-Relational Mapping):** Nesne (sınıf) dünyası ile tablo (satır/sütun) dünyası arasındaki çeviri katmanı. Entity Framework Core'un hafif, hızlı bir kuzeni gibi düşünebilirsin. İkisi arasındaki yapısal uyumsuzluğa **object-relational impedance mismatch** denir (nesneler iç içe referans tutar; tablolar yabancı anahtar tutar).

**Önemli tasarım kararı:** tabloya karşılık gelen sınıflar (`IngredientRow`, `RecipeRow`, `RecipeItemRow`) Core modellerinden **ayrıdır**:

```csharp
[Table("Ingredients")]
internal class IngredientRow
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Unit { get; set; }                       // enum → int
    public string PurchasePrice { get; set; } = "0";    // decimal → metin (bkz. 4.2)
    public string PurchaseQuantity { get; set; } = "1";

    public static IngredientRow From(Ingredient model) => ...   // Core → tablo satırı
    public Ingredient ToModel() => ...                          // tablo satırı → Core
}
```

Üç şey dikkat çekmeli:

1. **`internal`**: Row sınıfları Data projesinin dışına **sızmaz**. Üst katmanlar yalnızca Core modellerini görür. (*Information hiding, kapsülleme*)
2. **Core'da `[PrimaryKey]` gibi SQLite nitelikleri yok.** Böylece Core, veritabanı seçiminden habersiz kalır (yarın SQLite yerine başka bir şey gelse Core değişmez).
3. **`RecipeItem` bir `Ingredient` *nesnesi* tutar** (referans); tabloda ise yalnızca `IngredientId` (yabancı anahtar) tutulur. Çeviri, `Build` metodunda depoda yapılır.

> **Terim — Data Mapper deseni (Fowler, *PoEAA*):** Domain nesnesi ile veritabanı satırı arasında ayrı bir eşleyici katman. Alternatifi **Active Record**'dur (nesne kendi kendini kaydeder: `ingredient.Save()`). Active Record küçük projelerde hızlıdır ama domain'i veritabanına bağlar; biz Core'u saf tutmak için Data Mapper'a yakın bir yol seçtik.
>
> **Terim — Persistence ignorance (kalıcılıktan habersizlik):** Domain nesnelerinin "nasıl saklandığını" bilmemesi ilkesi.

### 4.2 `decimal` neden metin olarak saklanıyor?

Bu, projenin en ince tuzaklarından biriydi. `sqlite-net`, bir `decimal` özelliği veritabanına yazarken onu **sessizce `double`'a çevirir**. Yani 3. bölümde "para `decimal`'dir" kuralını koyduk ama veritabanı katmanı bu kuralı farkında olmadan çiğneyebilirdi.

Çözüm: Row sınıflarında para ve oran alanlarını **`string`** tutup kültürden bağımsız çevirmek:

```csharp
internal static class DecimalText
{
    public static string ToDb(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    public static decimal FromDb(string text) => decimal.Parse(text, CultureInfo.InvariantCulture);
}
```

`InvariantCulture` kritik: Türk telefonunda `0,35` yazılsa, ingilizce telefonda `0.35`; veritabanı dosyası her cihazda aynı biçimde (`0.35`) yazılmalı. Bunu test ediyoruz: `123456789.123456789m` ve `0.035m` gibi `double`'da bozulacak değerler gidiş-dönüşte **birebir** geri geliyor (`Ingredient_DecimalValuesAreStoredLosslessly`).

**Trade-off (bedel):** metin olarak saklanan sayı SQL'de **sayısal sıralanamaz/toplanamaz** (`"9" > "10"` metinde). Biz hiçbir zaman SQL'de para toplamıyoruz (hesap Core'da), bu yüzden kabul edilebilir. Alternatif: parayı **tamsayı kuruş** olarak saklamak (`45075` = 450,75 TL), ama oranlarımız (`0,035`) ve birim fiyatlarımız (`0,30 TL/g`) daha fazla basamak istiyor; metin en az sürprizli seçimdi.

> **Terim — SQLite'ın dinamik tipleri (type affinity):** SQLite katı tipli değildir; sütunun "eğilimi" vardır ama her değeri saklar. Bu esneklik, `decimal`'in sessizce `REAL`'e (double) çevrilmesine zemin hazırlar.
>
> **Terim — Impedance mismatch örneği:** `decimal` (C#) ↔ SQLite'ta karşılığı yok; `enum` ↔ `int`; `Ingredient` nesne referansı ↔ yabancı anahtar. Her biri elle çevrilir.

### 4.3 Repository (depo) deseni

`IIngredientRepository` ve `IRecipeRepository` **arayüzleri** (sözleşme) ile bunların `IngredientRepository`/`RecipeRepository` **gerçekleştirmeleri** ayrıdır:

```csharp
public interface IIngredientRepository
{
    Task<List<Ingredient>> GetAllAsync();
    Task<Ingredient?> GetAsync(int id);
    Task SaveAsync(Ingredient ingredient);   // Id == 0 ise ekler (Id'yi doldurur), değilse günceller
    Task DeleteAsync(int id);
}
```

ViewModel'ler **arayüze** bağımlıdır, SQLite'a değil. Faydaları: (1) test için sahte (fake) depo yazılabilir (bkz. bölüm 8.6, ViewModel testi alıştırması); (2) yarın SQLite yerine başka bir depolama gelirse ViewModel'ler değişmez; (3) "veri nasıl okunur" bilgisi tek yerde toplanır.

> **Terim — Repository deseni:** Veri erişimini, bellekte bir koleksiyonmuş gibi gösteren soyutlama. Domain "ekle/getir/sil" der; SQL'in nasıl yazıldığını bilmez.
>
> **Terim — Dependency Inversion (DIP, SOLID'in "D"si):** Üst seviye modüller alt seviye modüllere değil, **soyutlamalara (arayüzlere)** bağımlı olmalı. ViewModel → `IRecipeRepository` ← `RecipeRepository`. Ok, somut sınıfa değil arayüze gidiyor.
>
> **Terim — Upsert:** "Varsa güncelle, yoksa ekle". Bizim `SaveAsync(Id == 0 ? ekle : güncelle)` kuralı bir upsert'tür; çağıran taraf "yeni mi eski mi" ayrımıyla uğraşmaz.

### 4.4 `RecipeRepository.SaveAsync`: transaction (işlem bütünlüğü)

Bir reçeteyi kaydetmek aslında **birden çok SQL işlemi**dir: reçete satırı + her kalem için bir satır. Ortada bir hata olursa reçetenin yarısı kayıtlı kalmamalı:

```csharp
await db.Connection.RunInTransactionAsync(conn =>
{
    var row = RecipeRow.From(recipe);
    if (row.Id == 0) { conn.Insert(row); recipe.Id = row.Id; }
    else             { conn.Update(row);
                       conn.Execute("DELETE FROM RecipeItems WHERE RecipeId = ?", recipe.Id); }

    foreach (var item in recipe.Items)
        conn.Insert(RecipeItemRow.From(recipe.Id, item));
});
```

Hepsi ya **tamamen** gerçekleşir ya da hiç (*atomicity*). Güncellemede kalemler "**sil ve baştan ekle**" stratejisiyle yazılır: eski kalemleri tek tek karşılaştırıp (diff) değişenleri güncellemekten çok daha basit ve hata payı daha düşüktür; bu ölçekte (reçete başına birkaç kalem) maliyeti ihmal edilebilir.

> **Terim — ACID:** Veritabanı işlemlerinin dört garantisi: **A**tomicity (hep ya da hiç), **C**onsistency (kurallar bozulmaz), **I**solation (eşzamanlı işlemler birbirini bozmaz), **D**urability (kaydedilen kaybolmaz). Burada özellikle *atomicity* için transaction kullandık.

### 4.5 Okuma: hammaddeleri bir kez yükle, reçeteleri onlarla kur

```csharp
var ingredients  = await LoadIngredientsAsync();                // Dictionary<int, Ingredient>
var recipeRows   = ...ToListAsync();
var itemRows     = await db.Connection.Table<RecipeItemRow>().ToListAsync();
return recipeRows.Select(r => Build(r, itemRows.Where(i => i.RecipeId == r.Id), ingredients)).ToList();
```

Her kalem için ayrı sorgu atmak yerine **üç sorgu**: tüm hammaddeler (bir sözlüğe), tüm reçeteler, tüm kalemler. Sonra bellekte birleştirilir.

> **Terim — N+1 sorgu problemi:** 100 reçete için önce 1 sorguyla reçeteleri, sonra her biri için ayrı sorguyla kalemlerini çekmek: 1 + 100 sorgu. ORM kullanan projelerin en klasik performans hatasıdır. Biz tüm tabloyu bir kerede çekiyoruz (3 sorgu).
>
> **Terim — Identity map:** Aynı kayda ait her referansın **aynı nesne örneğini** göstermesi. `LoadIngredientsAsync` bir sözlük kurduğu için "Soya mumu" kullanan 5 kalemin hepsi **aynı `Ingredient` nesnesini** paylaşır. (Bu, Picker'ların seçili öğeyi referans eşitliğiyle bulmasında da işimize yarıyor; bkz. bölüm 6.)
>
> **Terim — Büyük-O karmaşıklığı:** `itemRows.Where(...)` her reçete için tüm kalemleri tarar: O(R × N). Ondan fazlasını yapmak için kalemleri `RecipeId`'ye göre bir kez **gruplamak** gerekirdi (O(R + N)). Bu ölçekte (onlarca reçete) fark ölçülemez; ama binlerce kayıtta ilk bakılacak yer burasıdır (bkz. bölüm 11).

### 4.6 Referans bütünlüğü: "kullanılan hammadde silinemez"

SQLite yabancı anahtar (FOREIGN KEY) kısıtlarını varsayılan olarak **zorlamaz** ve biz tablolarda tanımlamadık. Bu yüzden kural **uygulama katmanında** korunur:

```csharp
public async Task DeleteAsync(int id)
{
    var usedBy = await db.Connection.Table<RecipeItemRow>().Where(i => i.IngredientId == id).CountAsync();
    if (usedBy > 0)
        throw new InvalidOperationException("Bu hammadde bir reçetede kullanılıyor, silinemez.");
    await db.Connection.DeleteAsync<IngredientRow>(id);
}
```

Bu koruma olmasaydı, bir reçetenin kullandığı hammaddeyi silmek **"yetim" (orphan) kayıt** bırakırdı; reçete yüklenirken sözlükte hammadde bulunamaz ve uygulama çökerdi. Test: `Ingredient_UsedInRecipe_CannotBeDeleted`.

> **Terim — Referential integrity (referans bütünlüğü):** Bir kaydın başka bir kayda işaret ettiği yerde, işaret edilenin var olduğunun garantisi.
>
> **Terim — TOCTOU (time-of-check to time-of-use) yarışı:** "Kullanılıyor mu?" diye kontrol edip sonra silmek arasında başka biri kullanmaya başlayabilir. Tek kullanıcılı yerel uygulamada pratikte sorun değil; çok kullanıcılı sistemde veritabanı kısıtı ya da transaction gerekir.

### 4.7 Asenkron programlama (`async`/`await`)

Veritabanı işleri **I/O**'dur (disk beklemek). Arayüz iş parçacığını (UI thread) bu sürede kilitlemek istemeyiz, yoksa ekran donar. Bu yüzden her depo metodu `Task` döner ve `await` ile çağrılır.

```csharp
protected override async void OnAppearing()
{
    base.OnAppearing();
    await _viewModel.LoadCommand.ExecuteAsync(null);   // veri yüklenirken arayüz donmaz
}
```

> **Terim — `async`/`await`:** "Bu işlem bitene kadar bekle ama iş parçacığını bırak" demenin kolay yolu. `await` satırında metot **duraklar**, kontrolü çağırana geri verir; iş bitince kaldığı yerden devam eder. **Paralellik demek değildir**; bekleme süresini boşa harcamamaktır.
>
> **Terim — UI thread ve `SynchronizationContext`:** Arayüz yalnızca kendi iş parçacığından değiştirilebilir. `await` sonrası kod, MAUI'de otomatik olarak yine UI thread'e döner; bu yüzden `ObservableCollection`'ı doğrudan güncelleyebiliyoruz.
>
> **Terim — `async void`:** Normalde kaçınılır (hatalar yakalanamaz). Yalnızca olay işleyicilerinde (`OnAppearing`) kaçınılmazdır; biz bu yüzden asıl işi `async Task` komutuna (`LoadCommand`) taşıyoruz; `OnAppearing` yalnızca onu çağırır.

#### Hata hikayesi: "izole" testler birbirinin verisini görüyordu

Veri testleri için her teste **boş bir bellek içi veritabanı** (`":memory:"`) verecektik. İlk çalıştırmada 7 test kırmızıydı ve hata mesajları tuhaftı: "boş" bir veritabanında önceki testin 10 örnek hammaddesi görünüyordu.

1. **İlk teşhisim yanlıştı:** "sqlite-net varsayılan `SharedCache` bayrağını açıyor, bellek veritabanları paylaşılıyor." Bayrağı kapattım, **hata sürdü.** (Bu da başlı başına bir ders: **ilk hipotezi doğrulamadan "çözüldü" deme.**)
2. **Gerçek neden:** `SQLiteAsyncConnection`, **aynı bağlantı dizesi** için içeride ortak bir **bağlantı havuzu** kullanıyor. Düz `":memory:"` her seferinde aynı dizeydi, dolayısıyla her test **aynı bağlantıyı (aynı veritabanını)** alıyordu.
3. **Çözüm:** her test için **benzersiz adlı** bir URI: `file:mem-{Guid}?mode=memory` ve `SQLITE_OPEN_URI` (0x40) bayrağı:

```csharp
public static AtolyeDatabase CreateInMemory() =>
    new(new SQLiteAsyncConnection($"file:mem-{Guid.NewGuid():N}?mode=memory",
        SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | OpenUri));
```

> **Terim — Gizli paylaşılan durum (hidden shared state) ve test izolasyonu:** Testler birbirinden **bağımsız** olmalı; sırayla çalışsalar da, paralel çalışsalar da sonuç değişmemeli. Paylaşılan global durum (statik alan, ortak veritabanı, ortak dosya) en sık flaky (kararsız) test sebebidir.
>
> **Terim — Factory (fabrika) metodu:** `AtolyeDatabase.CreateInMemory()`: yapıcıyı (`new`) doğrudan çağırmak yerine, "doğru kurulmuş" nesneyi üreten adlandırılmış metot. Karmaşık kurulumu bir isme saklar.

### 4.8 Şema evrimi (migration)

Reçeteye sonradan `SalePrice` (satış fiyatı) ekledik. Kullanıcının **eski veritabanı** bu sütunu içermiyordu. `CreateTableAsync<RecipeRow>()` mevcut tabloyu inceleyip eksik sütunu **otomatik ekler** (`ALTER TABLE ... ADD COLUMN`), eski satırlarda değer `NULL` olur; bu yüzden alanı `string?` (nullable) yaptık ve `Recipe.SalePrice` `decimal?` oldu.

Bunu sadece teoride bırakmadık: gerçek (eski) veritabanıyla uygulamayı açıp mevcut reçetelerin bozulmadığını, yeni alanın girilip kaydedilebildiğini doğruladık.

> **Terim — Migration (şema göçü) ve geriye dönük uyumluluk (backward compatibility):** Uygulamanın yeni sürümü, eski sürümün oluşturduğu veriyi okuyabilmeli. Basit ekleme (nullable sütun) kolaydır; **yeniden adlandırma, silme, tip değiştirme** otomatik yapılmaz, elle migrasyon kodu gerekir (ör. `PRAGMA user_version` ile şema sürümü tutmak). Bu, gerçek ürünlerde en çok dikkat edilen konulardan biridir; eski sürümden gelen kullanıcıyı bozmak affedilmez.

### 4.9 `SeedService`: örnek veri ve idempotans

`SeedService`, "mum başlangıç paketi"ni (10 hammadde, 2 reçete) yükler. İki metodu var:

- `SeedIfEmptyAsync()`: **yalnızca veritabanı tamamen boşsa** ekler, eklediyse `true` döner.
- `ClearAllAsync()`: üç tabloyu da siler (önce kalemler, sonra reçeteler, sonra hammaddeler, yani **çocuktan ebeveyne**).

> **Terim — İdempotans (idempotency):** Bir işlemi 1 kez de yapsan 10 kez de yapsan sonuç aynıdır. `SeedIfEmptyAsync`'i iki kez çağırmak ikinci seferde **hiçbir şey eklemez** (test: `Seed_SecondCallAddsNothing`). Ağ isteklerinde, kuyruklarda, açılış kodunda aranan çok değerli bir özelliktir.

#### Hata hikayesi: "sil" dedik, uygulama geri getirdi

İlk tasarım: "veritabanı boşsa her açılışta örnek veriyi yükle." Ayarlar ekranını yazarken bir senaryoyu düşündüm: kullanıcı **"Tüm verileri sil"** der, uygulamayı kapatıp açar... ve silinen örnekler **geri gelir**, çünkü veritabanı boştur!

Bu hata hiçbir birim testle yakalanmazdı (her bileşen kendi içinde doğruydu); **bütünün davranışı** hakkında düşünürken yakalandı. Çözüm: kalıcı bir bayrak (`AppSettings.SeedApplied`): örnek veri **yalnızca ilk çalıştırmada** otomatik yüklenir; sonrası kullanıcının elinde ("Örnek verileri yükle" düğmesiyle isterse tekrar yükler).

> **Terim — Durum makinesi (state machine):** Bayrak, uygulamayı iki duruma ayırır: "ilk çalıştırma henüz yapılmadı" → "yapıldı". Geçiş tek yönlüdür. Böyle gizli durumlar tasarım sırasında **açıkça çizilmediğinde** hata kaynağı olur.

#### Hata hikayesi: SQL sıralaması Türkçe harfleri bilmiyordu (kod rehberi yazarken bulundu)

Bu rehberi yazarken, `GetAllAsync`'teki `OrderBy(r => r.Name)` satırına bakıp şunu sordum: *"Bu SQL `ORDER BY` hangi sıralamayı kullanıyor?"* SQLite'ın varsayılanı **BINARY**'dir: baytlara göre, büyük harf **önce**, `ç/ş/ğ` en **sonda**. Kısa bir deneyle doğruladım:

```
SQLite ORDER BY  : Amber, Ardıç, Etiket, Zeytin, bambu, çam, şeker     ← yanlış
Türkçe alfabe    : Amber, Ardıç, bambu, çam, Etiket, şeker, Zeytin     ← doğru
```

Neden bugüne kadar fark edilmedi? **Test verisi gerçek kullanıcıyı temsil etmiyordu:** örnek hammaddelerin hepsi büyük ASCII harfle başlıyor ("Ahşap", "Cam", "Etiket"...). Gerçek bir kullanıcı "çam mumu" ya da "bambu fitil" yazsa listenin sonunda görürdü.

Düzeltme (önce kırmızı test, sonra yeşil): sıralama SQL'den bellekteki Türkçe karşılaştırıcıya (`TextOrder.Turkish`) taşındı; fiyat listesi de aynı karşılaştırıcıyı kullanıyor. İki test eklendi (`..._GetAllUsesTurkishAlphabetOrder`).

> **Terim — Collation (harmanlama):** Veritabanının metinleri nasıl karşılaştırıp sıraladığı kuralı (büyük/küçük harf duyarlılığı, aksanlar, dil). Varsayılan çoğu zaman "dil bilmez".
>
> **Terim — Test verisi temsil gücü:** Testler yalnızca verdiğin girdileri doğrular. Gerçek veride görülen özellikler (Türkçe karakter, çok uzun ad, boşluk, emoji, 0, negatif, çok büyük sayı) test verisinde de **bulunmalıdır**.

---

## 5. Pdf: müşteri fiyat listesi

Müşteriye gönderilen çıktı **maliyet analizi değil, fiyat listesidir** (üretici maliyetini müşteriyle paylaşmaz; ilk planlama kararı). PDF'te yalnızca **ürün adı ve satış fiyatı**, işletme adı, tarih, "Fiyatlar KDV hariçtir." notu ve sayfa numarası bulunur.

Pdf, Core'a bağlı ama MAUI'ye **bağlı olmayan** ayrı bir projedir; bu sayede testlerde gerçekten PDF üretebiliyoruz.

### 5.1 Hata hikayesi: QuestPDF Android'de hiç çalışmıyor

İlk sürümde PDF'i **QuestPDF** ile ürettik: modern, akıcı (fluent) bir yerleşim motoru; Windows'ta çok güzel çalıştı. Telefonu (Android emülatörü) devreye alınca **"PDF oluştur" düğmesine basmak uygulamayı çökertti.**

Kayıtlara (`adb logcat`) bakınca:

```
Shared library 'QuestPdfSkia' not loaded, p/invoke 'questpdf_skia_get_compatibility_version' may fail
FATAL EXCEPTION: ... TypeInitializationException: The type initializer for 'PriceListPdf' threw ...
```

Kütüphanenin kendi hata mesajı daha da açıktı: *"QuestPDF cannot run on the current platform (Android). It depends on native components that are available only on Windows, Linux, and macOS..."* ve PDF üretimini **sunucuya taşımayı** öneriyordu. Bizim "backend yok, yerel çalışır" ilkemizle bu bağdaşmaz.

**Kök neden: yerel (native) bağımlılık.** QuestPDF saf C# değil; içinde **C/C++ ile yazılmış, işletim sistemi/işlemci mimarisine özel derlenmiş kütüphaneler** (`libQuestPdfSkia.so`, `libqpdf.so`) var. NuGet paketi bunları `runtimes/linux-x64/native/` gibi klasörlerde taşıyor. Android Linux çekirdeği kullanır ama C kütüphanesi farklıdır (**bionic**, masaüstü Linux'un **glibc**'si değil); masaüstü Linux için derlenmiş `.so` dosyaları Android'de yüklenemez. Derleme sırasında bunun ipucu zaten vardı: 16 KB sayfa boyutu uyarıları (`XA0141`) tam bu Linux dosyaları için çıkıyordu.

> **Terim — Yönetilen (managed) ve yerel (native) kod:** C# kodu .NET çalışma zamanında çalışır (yönetilen). C/C++ ile derlenmiş kütüphaneler doğrudan işlemcinin makine koduyla çalışır (yerel) ve **her platform/mimari için ayrı derlenmelidir**.
>
> **Terim — P/Invoke:** Yönetilen koddan yerel bir kütüphanedeki fonksiyonu çağırma mekanizması. Kütüphane yüklenemezse, çağrı anında çalışma zamanı hatası alırsın, **derleme sırasında değil**. Bu yüzden bu hata ancak **gerçek cihazda/emülatörde** görülebildi.
>
> **Terim — RID (Runtime Identifier):** `win-x64`, `linux-arm64`, `android-arm64` gibi platform+mimari kimliği. NuGet paketleri, RID'ye göre doğru yerel dosyayı seçer. Bir kütüphane "taşınabilir görünür" ama yerel parçası yalnızca bazı RID'leri destekliyorsa, tuzak buradadır.
>
> **Terim — Çapraz platform ≠ "her yerde derlenir":** Bir paketin NuGet'te kurulabilmesi, hedef platformda **çalışacağı** anlamına gelmez. Yeni bir kütüphane seçerken "desteklediği platformlar" listesi okunmalı.

**Çözüm:** **SkiaSharp**'a geçtik. Skia, Google'ın 2B grafik motorudur (Chrome ve Android'in kendisi kullanır) ve SkiaSharp'ın **Android için ayrı derlenmiş** yerel kitaplıkları vardır. Ücretsiz/MIT lisanslıdır. Bedel: QuestPDF'in hazır yerleşim motorunu kaybettik; başlık, tablo ve sayfa numaralarını **kendimiz** çizeriz (~370 satır). Fiyat listesi basit olduğu için bu iyi bir takas.

Aynı dosyayı hem Windows'ta hem Android'de üretip açtık; iki platformda da Türkçe harfler, ₺ simgesi ve kalın başlıklar doğru çıktı.

#### İkinci ders: yanlış `catch`

Düğmenin çökmesinin **ikinci** sebebi, benim `catch` bloğumun yalnızca `IOException` ve `UnauthorizedAccessException` yakalamasıydı. Beklenmedik bir hata (burada yüklenemeyen yerel kitaplık) yakalanmayınca **uygulama kapandı**. Şimdi genel bir `catch (Exception ex)` var ve kullanıcıya mesaj gösteriyor. Hata **yok olmadı**, ama artık uygulamayı yıkmıyor ve neden olduğu açıkça yazıyor.

> **Terim — Savunmacı programlama (defensive programming) ve zarif bozulma (graceful degradation):** Dış dünyaya (dosya, ağ, yerel kütüphane, kullanıcı girişi) dokunan kod, başarısızlığı **beklemeli**. Amaç hatayı gizlemek değil, kullanıcıya anlaşılır bir mesaj vermek ve geri kalan uygulamayı ayakta tutmak.

### 5.2 PDF üretim hattı (pipeline)

Dört adım ve her biri ayrı sorumluluk:

```
 ┌────────────┐   ┌─────────────┐   ┌──────────────┐   ┌────────────┐
 │  ÖLÇ       │ → │  SAR        │ → │  SAYFALA     │ → │  ÇİZ       │
 │ yazı genişl│   │ uzun adı    │   │ satırları    │   │ Skia canvas│
 │ ve yüksekl.│   │ satırlara   │   │ sayfalara    │   │ üzerine    │
 │ (TextStyle)│   │ böl (Wrap)  │   │ dağıt        │   │ → PDF byte │
 └────────────┘   └─────────────┘   └──────────────┘   └────────────┘
                   PriceListLayout (çizimden bağımsız, testli)     PriceListPdf
```

Neden önce sayfalayıp **sonra** çiziyoruz? Altbilgide **"Sayfa 2 / 3"** yazmak için toplam sayfa sayısını çizimden **önce** bilmemiz gerekir. Bu yüzden iki geçiş (two-pass): önce yerleşimi hesapla (kaç sayfa?), sonra çiz.

```csharp
var available = PageHeight - 2*Margin - headerHeight - tableHeaderHeight - footerHeight;
var pages     = PriceListLayout.Paginate(rows, available);     // adım 3: kaç sayfa?
for (var i = 0; i < pages.Count; i++)                          // adım 4: çiz
{
    var canvas = document.BeginPage(PageWidth, PageHeight);
    ...
    DrawFooter(canvas, fonts, i + 1, pages.Count);             // "Sayfa i / toplam"
    document.EndPage();
}
```

#### Koordinat sistemi: nokta (point) ve baseline

- PDF'in birimi **nokta (pt)**: 1/72 inç. A4 = **595 × 842** nokta. Orijin sol üsttür, **y aşağı doğru artar** (matematikteki gibi yukarı değil).
- Yazı çizerken verdiğin y, harflerin **taban çizgisi (baseline)** konumudur, kutunun üstü değil. Harfler baseline'ın **üstüne** (ascent) uzanır; `g, y, ç` gibi harflerin kuyruğu **altına** (descent) iner. Satır yüksekliği = ascent + descent + satır aralığı (leading). `TextStyle` bu üç değeri `SKFont.Metrics`'ten alır.

> **Terim — Tipografi sözlüğü:** *baseline* (taban çizgisi), *ascent/descent* (yukarı/aşağı uzantı), *leading* (satır aralığı), *advance width* (bir harfin yatayda kapladığı genişlik; yazıyı ölçmek bu genişlikleri toplamaktır), *glyph* (bir harfin çizilmiş şekli).

#### Satır sarma (word wrap) algoritması

```csharp
foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
{
    var candidate = current.Length == 0 ? word : current + " " + word;
    if (measure(candidate) <= maxWidth) { current = candidate; continue; }   // sığıyor: devam
    if (current.Length > 0) { lines.Add(current); current = ""; }            // sığmıyor: satırı bitir
    // tek başına sığmayan çok uzun kelime: harf harf kes
    ...
}
```

**Açgözlü (greedy) algoritma:** her kelimeyi mevcut satıra sığdırmayı dener, sığmazsa yeni satıra geçer. Basit, hızlı, çoğu zaman yeterince güzel. Daha "güzel" paragraflar için Knuth-Plass gibi (TeX'in kullandığı) **global optimizasyon** algoritmaları vardır; fiyat listesi satırları için gereksiz.

Kenar durumlar da kod ve testle ele alınır: boş metin (tek boş satır), fazladan boşluklar (`"  a   b  "` → `"a b"`), hiçbir şekilde sığmayan tek kelime (harf harf böler, **hiçbir harfi kaybetmez**), ve **sonsuz döngü** riski (her turda en az 1 harf ilerlenir).

#### Sayfalama

```csharp
foreach (var row in rows)
{
    var page = pages[^1];
    if (page.Count > 0 && used + row.Height > availableHeight)       // sığmıyor: yeni sayfa
    { page = new List<LayoutRow>(); pages.Add(page); used = 0f; }
    page.Add(row);
    used += row.Height;
}
```

Satırlar sırayla sayfaya konur, doluyorsa yeni sayfaya geçilir (`page.Count > 0` koşulu, **tek başına sayfadan yüksek bir satırın sonsuz yeni sayfa açmasını** engeller).

> **Terim — Açgözlü algoritma (greedy):** Her adımda o anki en iyi görünen seçimi yapar, geri dönmez. Çoğu zaman global olarak en iyi değildir; ama basit ve hızlıdır. Burada sayfalama için gayet yeterli ve **doğruluğu kanıtlanabilir basitlikte.**
>
> **Terim — Bin packing (kutulara yerleştirme) ile ilişkisi:** "Değişken yükseklikli satırları en az sayfaya yerleştir" aslında klasik bir NP-zor probleminin cinsidir; ancak **sıra korunmak zorunda** olduğundan (satırları yer değiştiremeyiz) basit sıralı yerleştirme zaten optimaldir.

### 5.3 Yazı tipleri, Unicode ve "tofu"

`TextStyle`, sistemin varsayılan yazı tipini kullanır (Windows'ta Segoe UI, Android'de Roboto) ve **eksik harf için yedek yazı tipi** arar. Çünkü her yazı tipi her harfi içermez; içermeyen bir harf PDF'te **boş kutu (□)** olarak çıkar (buna *tofu* denir).

```csharp
private SKTypeface TypefaceFor(int codepoint)
{
    if (HasGlyph(_primary, codepoint)) return _primary;                  // birincil yazı tipinde var
    foreach (var fallback in _fallbacks) if (HasGlyph(fallback, codepoint)) return fallback;
    var match = SKFontManager.Default.MatchCharacter(null, style, null, codepoint);   // sistemde ara
    ...
}
```

Metin, aynı yazı tipiyle çizilebilen **parçalara (run)** bölünür; her parça kendi yazı tipiyle ölçülür/çizilir.

Bu kodda iki "bilgisayar mühendisliği" konusu birden var:

> **Terim — Unicode kod noktası (code point) ve UTF-16 vekil çiftleri (surrogate pairs):** C#'ta `string` UTF-16'dır: her `char` 16 bit. Ama Unicode'da 65 bin'den fazla karakter var (emoji gibi); bunlar **iki `char`** (vekil çifti) ile gösterilir. Bu yüzden harf harf dönerken `text[i]` kullanmak emojiyi ikiye böler; doğrusu `char.ConvertToUtf32(text, i)` ve `char.IsSurrogatePair(text, i)` ile **kod noktası** bazında ilerlemektir.
>
> **Terim — Tofu ve glif kapsamı:** Bir yazı tipi Unicode'un yalnızca bir alt kümesini kapsar (ör. `₺` işareti eski yazı tiplerinde yoktur). "Karakter var" ≠ "bu yazı tipinde çizilebilir".

#### Hata hikayesi: kalın yazı kalın değildi

PDF'in ilk görsel kontrolünde başlık ve "Ürün/Fiyat" başlıkları **düz** görünüyordu. Kod `SKTypeface.FromFamilyName(null, SKFontStyle.Bold)` diyordu ama aile adı `null` verilince stil yok sayılıyormuş. Çözüm: aile adı olarak varsayılan yazı tipinin adı verildi. Ardından `IsBold` özelliğiyle bir test eklendi (`Font_BoldStyle_IsReallyBold...`).

Bu hatayı **hiçbir "hata veren" test yakalamazdı**: kod çalışıyordu, PDF üretiliyordu, sadece görünüm yanlıştı. **Gözle bakmak** yakaladı (bölüm 8'de "görsel doğrulama").

> **Terim — Sessiz hata:** Program çökmez, istisna fırlatmaz ama **yanlış sonuç** üretir. En tehlikeli hata türüdür çünkü kimse fark etmez. Altın örnek testleri ve görsel inceleme bu türe karşı savunmadır.

### 5.4 Test edilebilirlik için tasarım

`PriceListLayout`, **yazı tipi bilmeden** çalışır; yazı ölçümünü dışarıdan bir **fonksiyon** olarak alır:

```csharp
public static IReadOnlyList<string> Wrap(string text, float maxWidth, Func<string, float> measure)
```

Gerçek uygulamada `measure`, Skia'nın ölçümüdür. Testlerde ise basit bir **sahte** (fake) ölçüm veririz: "her harf 10 birim":

```csharp
private static float Measure(string text) => text.Length * 10f;
```

Böylece "100 birim genişlikte `aaaa bbbb cccc` iki satıra bölünür" gibi testler **makineden, yazı tipinden, platformdan bağımsız** ve her çalıştırmada **aynı** sonucu verir.

> **Terim — Strategy deseni / bağımlılığı parametre olarak vermek:** Bir davranışı (burada "yazı nasıl ölçülür") sabitlemek yerine dışarıdan vermek. Fonksiyonel dillerde bu doğal bir şeydir (*higher-order function*, yüksek dereceli fonksiyon); nesne yönelimli dillerde `IMeasurer` arayüzü yazardık.
>
> **Terim — Test double (test ikizi):** Gerçek bağımlılığın yerine geçen yapay nesne. Türleri: *dummy* (sadece yer tutar), *stub* (hazır cevap verir), *fake* (basit ama çalışan gerçekleştirme, bizim `Measure` gibi), *mock* (beklenen çağrıları doğrular), *spy* (çağrıları kaydeder).
>
> **Terim — `InternalsVisibleTo`:** `PriceListLayout` bilerek `internal`'dır (dış API'nin parçası değil); test projesine özel izinle görünür kılınır. "Sadece testler için `public` yapmak" yerine bu yol, kapsüllemeyi bozmaz.

**Dürüst not:** PDF'in **görsel doğruluğu** (hizalama, renk, yazı tipi) birim testle doğrulanmıyor. Bunu, üretilen PDF'i **PyMuPDF** ile açıp metnini ve ekran görüntüsünü inceleyerek elle yaptık. Bunun otomatik karşılığı **görsel regresyon testi** (referans görüntüyle piksel karşılaştırması) olurdu; yapmadık (bkz. bölüm 8.7).

### 5.5 PDF'in kullanıcıya ulaşması (platforma göre)

| Platform | Nereye yazılır | "Aç" | "Paylaş" |
|---|---|---|---|
| Windows | `Belgelerim\AtolyeRecete\` (kullanıcı kendi bulabilsin) | varsayılan PDF okuyucu | Windows paylaşım penceresi (denenmedi) |
| Android | uygulamanın **önbellek** klasörü (Belgelerim yok) | Google PDF görüntüleyici | Android paylaşım penceresi (Drive, Mesajlar...) |

Android'de dosya uygulamanın **özel alanında (sandbox)** durur; başka bir uygulama (PDF görüntüleyici, mesajlaşma) onu **doğrudan okuyamaz**. MAUI bunun için bir **FileProvider** kurar: başka uygulamaya dosyanın kendisini değil, **geçici izinli bir `content://` adresini** verir. Test sırasında günlükte şu satırı görmemiz bunun kanıtıydı: `Permission Denial: opening provider ... fileProvider` (paylaşım penceresinin *önizlemesi* izin alamıyor, ama asıl paylaşım çalışıyor).

> **Terim — Sandbox (kum havuzu) ve en az ayrıcalık ilkesi:** Her uygulama yalnızca kendi verisine erişir; başka uygulamayla veri paylaşımı açık izinle, kontrollü yapılır. Mobil güvenlik modelinin temelidir.

---

## 6. App: MAUI uygulaması (MVVM)

**.NET MAUI**, tek C# kod tabanından Android, iOS, Windows ve macOS uygulaması üretmeyi sağlayan çapraz platform çerçevesidir. Biz Android ve Windows'u hedefledik. Arayüz **XAML** (bir XML dili) ile **bildirimsel** (declarative) olarak yazılır: "bir buton olsun, metni şu olsun" dersin, nasıl çizileceğini çerçeve bilir.

### 6.1 MVVM: arayüzü mantıktan ayırmak

```
        ┌──────────────────┐   komut (tıklama)    ┌───────────────────┐   çağırır   ┌──────────────┐
        │  View (XAML)     │ ───────────────────► │   ViewModel       │ ──────────► │ Model/Servis │
        │ "ne görünür"     │                      │ "ekranın durumu   │ ◄────────── │ Core, Data,  │
        │                  │ ◄─────────────────── │  ve davranışı"    │   sonuç     │ Pdf          │
        └──────────────────┘   veri bağlama       └───────────────────┘             └──────────────┘
                              (PropertyChanged)
```

- **View** (`HomePage.xaml`): düğmeler, etiketler, listeler. İçinde **neredeyse hiç C# yok**; "code-behind" dosyası yalnızca `OnAppearing`'de ViewModel'in `LoadCommand`'ını çağırır (20 satır).
- **ViewModel** (`HomeViewModel.cs`): ekranın **durumunu** (sayaçlar, metinler, listeler) ve **davranışını** (komutlar) tutar. Arayüz elemanlarını (`Button`, `Label`) **bilmez**.
- **Model/Servisler**: Core (hesap), Data (depolar), Pdf.

Neden? ViewModel'in arayüzden haberi olmadığı için **ekran olmadan test edilebilir** (bkz. bölüm 8.6'daki fırsat) ve aynı ViewModel farklı arayüzlerle kullanılabilir.

> **Terim — MVVM (Model–View–ViewModel):** Arayüz kodunu üç role ayıran desen. Microsoft'un XAML dünyası (WPF, UWP, MAUI) için popülerleştirdiği, MVC ve MVP'nin kuzeni. Anahtar fikir: View ile ViewModel arasındaki iletişim **veri bağlama** (data binding) ile olur, doğrudan çağrıyla değil.
>
> **Terim — Veri bağlama (data binding):** `Text="{Binding IngredientCount}"` yazısı "bu etiketin metni, ViewModel'deki `IngredientCount` özelliğine **bağlı**" demektir. Özellik değişince etiket **kendiliğinden** güncellenir; sen `label.Text = ...` yazmazsın.

### 6.2 Bağlamanın çalışma prensibi: `INotifyPropertyChanged` (Observer deseni)

Etiketin "özellik değişti" bilgisini alabilmesi için ViewModel bir **olay** yayar. Bu, Observer (gözlemci) desenidir:

```csharp
public class X : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private int _count;
    public int Count
    {
        get => _count;
        set { _count = value; PropertyChanged?.Invoke(this, new(nameof(Count))); }
    }
}
```

Bunu her özellik için elle yazmak (boilerplate) can sıkıcıdır. **CommunityToolkit.Mvvm**'in **kaynak üreteçleri (source generators)** bunu derleme sırasında yazar:

```csharp
// Biz sadece bunu yazıyoruz:
[ObservableProperty] private string _name = "";

// Derleyici arka planda şuna benzer bir kod ÜRETİYOR (özet):
public string Name
{
    get => _name;
    set { if (_name != value) { OnNameChanging(value); _name = value; OnNameChanged(value); OnPropertyChanged(); } }
}
partial void OnNameChanged(string value);   // istersek gövdesini biz yazarız
```

Biz bu "kanca"yı (hook) aktif olarak kullanıyoruz. Örneğin reçete düzenlemede kullanıcı **her tuşa bastığında** maliyet yeniden hesaplanır:

```csharp
partial void OnYieldTextChanged(string value) => Recalculate();
partial void OnLaborMinutesTextChanged(string value) => Recalculate();
```

Aynı şekilde `[RelayCommand] private async Task LoadAsync()` yazınca derleyici `LoadCommand` adında bir `ICommand` üretir ve düğmeler `Command="{Binding LoadCommand}"` ile ona bağlanır.

> **Terim — Source generator (kaynak üreteci):** Derleme sırasında koda bakıp **yeni C# kodu üreten** derleyici eklentisi. Çalışma zamanı maliyeti sıfır (yansıma/reflection yok), tekrarlayan kodu siler. (Bizdeki `MVVMTK0045` uyarısı: alan tabanlı `[ObservableProperty]`, WinRT/AOT senaryosunda "kısmi özellik" ister; paketlenmemiş MAUI uygulamamızı ilgilendirmediği için bilinçli olarak susturuldu; gerekçe `.csproj` içinde yorum olarak ve `docs/KARARLAR.md`'de yazılı.)
>
> **Terim — Observer deseni:** Bir nesne (özne) değişince ona abone olan nesneleri (gözlemciler) haberdar eder. `PropertyChanged` olayı, `ObservableCollection`, C# `event`'leri hep bu desenin örnekleri.
>
> **Terim — Command deseni (`ICommand`):** Bir eylemi ("kaydet") **nesne** olarak temsil etme. Böylece düğme "kaydet nasıl yapılır"ı bilmeden yalnızca "komutu çalıştır" der; komut aynı zamanda "şu an çalıştırılabilir mi?" (`CanExecute`) bilgisini de taşıyabilir (`PDF oluştur` düğmesinin boş listede pasif olması gibi).
>
> **Terim — Derleme zamanı bağlama (compiled bindings):** `x:DataType="vm:HomeViewModel"` yazınca XAML derleyicisi, `{Binding ...}` yollarını **derleme sırasında** kontrol eder. Yanlış özellik adı yazarsan uygulama çalışırken sessizce boş görünmek yerine **derleme hatası** alırsın. (Yansıma ile bulan eski bağlama hem yavaş hem hataya açıktı.)

#### Hata hikayesi: "Her şey yolunda" kutusu neden görünmedi?

Ana sayfada yeşil "Her şey yolunda" kutusunun görünürlüğü **iki** değere bağlıydı:

```csharp
public bool ShowAllGood => HasRecipesFlag && AttentionCount == 0;   // türetilmiş özellik
```

Ama `ShowAllGood` için "değişti" bildirimi yalnızca `AttentionCount` değişince gönderiliyordu. Boş veritabanından örnek veriye geçişte `AttentionCount` 0'dan 0'a gidiyor (değişmiyor) → bildirim gitmiyor → kutu **ekranda güncellenmiyor**. Hesap doğruydu; ekran **eski değeri** gösteriyordu.

Bu hatayı **birim test değil, çalışan uygulamayı otomatik gezerken** yakaladık: ekran metinlerinde kutu yoktu. Düzeltme: bağımlı özelliğe, bağlı olduğu **her** kaynaktan bildirim eklemek:

```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(HasRecipes))]
[NotifyPropertyChangedFor(nameof(ShowAllGood))]     // ← eksik olan satır
private bool _hasRecipesFlag;
```

> **Terim — Türetilmiş özellik bağımlılığı (derived property):** `ShowAllGood` iki kaynağa bağlıysa **ikisi de** değiştiğinde bildirim göndermelidir. Bu, spreadsheet'teki hücre bağımlılıklarına benzer; unutulursa ekran bayat kalır. Tipik, "unutması kolay" bir hata kalıbıdır.

### 6.3 Bağımlılık enjeksiyonu (DI) ve `MauiProgram.cs`

Sınıflar ihtiyaç duydukları bağımlılıkları **kendileri oluşturmaz**; yapıcıya (constructor) **verilir**:

```csharp
public partial class RecipesViewModel(
    StartupService startup,
    AppSettings settings,
    IRecipeRepository repository) : ObservableObject
```

(C# 12 **birincil yapıcı** sözdizimi: parametreler sınıf adının yanında.) Kim sağlıyor? **DI kapsayıcısı**; `MauiProgram.cs` içindeki kayıtlara bakar:

```csharp
builder.Services.AddSingleton(new AtolyeDatabase(dbPath));
builder.Services.AddSingleton<IRecipeRepository, RecipeRepository>();   // arayüz → gerçekleştirme eşlemesi
builder.Services.AddTransient<RecipesViewModel>();
builder.Services.AddTransient<RecipesPage>();
```

Bir sayfa açılırken: sayfa `RecipesViewModel` ister → kapsayıcı onu oluşturur, **onun** bağımlılıklarını (`StartupService`, `AppSettings`, `IRecipeRepository`) da oluşturup/getirip verir. Zincirleme çözülür.

**Ömür (lifetime) seçimi bilinçli:**

| Kayıt | Anlamı | Biz neden |
|---|---|---|
| `AddSingleton` | Uygulama boyunca **tek örnek** | Veritabanı bağlantısı, depolar, ayarlar, açılış servisi: durum/bağlantı paylaşılır, tekrar tekrar oluşturmak israf (hatta yanlış: iki bağlantı). |
| `AddTransient` | Her istekte **yeni örnek** | ViewModel ve sayfalar: her gezinmede **temiz durum** (önceki açılıştan kalan metin kutuları olmasın). |

> **Terim — IoC / DI (Inversion of Control / Dependency Injection):** Nesne yaratma sorumluluğunu sınıfın dışına (kapsayıcıya) vermek. Faydası: (1) sınıflar **gevşek bağlı** olur, (2) testte gerçek bağımlılık yerine sahte verilebilir, (3) tüm "hangi sınıf hangisini kullanıyor" haritası **tek yerde** (`MauiProgram`) toplanır.
>
> **Terim — Composition root (birleştirme kökü):** Nesne grafiğinin kurulduğu **tek nokta**; geri kalan kod `new` demez. `MauiProgram.CreateMauiApp` bizim composition root'umuz.
>
> **Terim — Captive dependency (tutsak bağımlılık):** Uzun ömürlü (singleton) bir nesnenin kısa ömürlü (transient) bir bağımlılığı **ömür boyu tutması** tuzağı. Bizim singleton'larımız (depolar, ayarlar) hiçbir transient'e bağımlı olmadığı için bu hata yok. Yeni servis eklerken kontrol edilecek bir kuraldır.

### 6.4 Gezinme (Shell) ve sayfa yaşam döngüsü

`AppShell.xaml` uygulamanın iskeletidir: alt çubukta **5 sekme** (Ana Sayfa, Hammadde, Reçeteler, Simülatör, Fiyatlar). Sekmelerde olmayan sayfalar (düzenleme, ayarlar) **rota (route)** olarak kaydedilir ve "üstüne itilir" (push):

```csharp
Routing.RegisterRoute("RecipeEditPage", typeof(RecipeEditPage));
...
await Shell.Current.GoToAsync($"RecipeEditPage?id={id}");   // aç, parametre olarak id ver
await Shell.Current.GoToAsync("..");                        // bir geri git
```

Hedef ViewModel parametreyi `IQueryAttributable` ile alır:

```csharp
public void ApplyQueryAttributes(IDictionary<string, object> query)
    => _id = query.TryGetValue("id", out var raw) && int.TryParse(raw?.ToString(), out var id) ? id : 0;
```

Konvansiyon: `id = 0` → "yeni kayıt", `id > 0` → "düzenleme". (Aynı sayfa hem ekleme hem düzenleme için kullanılıyor; `IsExisting` özelliği düğmeleri/başlığı buna göre ayarlıyor.)

**Yaşam döngüsü ve "bayat veri":** Her sayfa `OnAppearing` içinde `LoadCommand`'ını çalıştırır. Yani bir sayfaya **her dönüşte veri yeniden okunur**. Kullanıcı Hammadde sekmesinde fiyatı değiştirip Ana Sayfa'ya dönünce, ana sayfa güncel marjları görür.

> **Terim — Yaşam döngüsü (lifecycle):** Bir sayfanın/bileşenin oluşturulma, görünme, gizlenme, yok edilme aşamaları ve her aşamada çağrılan kancalar (`OnAppearing`, `OnDisappearing`). Mobilde (arka plana atılma, ekran döndürme, bellek baskısıyla öldürülme) yaşam döngüsü masaüstünden çok daha önemlidir.
>
> **Trade-off:** "Her görünüşte yeniden yükle" **basit ve her zaman doğru**, ama gereksiz sorgu demek. Alternatif: bir **mesajlaşma/olay** sistemi ("hammadde değişti" mesajı yayınlansın, ilgili ekranlar dinlesin); daha verimli ama daha karmaşık. Bu ölçekte basit olanı seçtik.

**Listede satıra dokunma hilesi:** `CollectionView`'de `SelectionMode="Single"` ve `SelectedItem` çift yönlü bağlıdır; seçim değişince ViewModel düzenleme sayfasına gider **ve seçimi hemen `null`'a çeker**:

```csharp
partial void OnSelectedItemChanged(IngredientItem? value)
{
    if (value is null) return;
    SelectedItem = null;                 // aynı satıra tekrar dokunulabilsin
    EditCommand.Execute(value.Id);
}
```

Seçimi sıfırlamasaydık, aynı satıra ikinci dokunuş "seçim değişmedi" sayılıp **hiçbir şey yapmazdı**.

### 6.5 Açılış: `StartupService` (tembel ve tek seferlik başlatma)

Kurucular (constructor) `await` yapamaz; ama veritabanı kurulumu asenkron. Bu yüzden ayrı bir servis:

```csharp
private Task? _initialization;
public Task EnsureInitializedAsync() => _initialization ??= InitializeAsync();
```

Her ekran açılışta `await startup.EnsureInitializedAsync()` der. **İlk çağrı** işi başlatır ve `Task`'ı saklar; sonraki çağrılar **aynı `Task`'ı** döner. Sonuç: veritabanı kurulumu ve örnek veri yüklemesi **tam bir kez** olur, kaç ekran çağırırsa çağırsın.

> **Terim — Lazy initialization (tembel başlatma) ve `??=`:** "İhtiyaç olunca, bir kez yap." `a ??= b` "a boşsa b'yi ata" demektir.
>
> **Terim — Task'ı önbelleğe almak (memoization):** Bir hesabı/işi bir kez yapıp **sonucunu (veya Task'ını)** saklamak. Burada saklanan, işin kendisi: "başlatma" bir söz (promise/future).
>
> **Dürüst not (thread-safety):** `??=` atomik değildir. İki iş parçacığı **aynı anda** ilk çağrıyı yapsa iki başlatma çalışabilir. Bizde çağrılar UI iş parçacığından sırayla geldiği için güvenli; çok iş parçacıklı bir ortamda `Lazy<Task>` veya `SemaphoreSlim` gerekirdi.

### 6.6 `AppSettings`: kalıcı tercihler

Saatlik ücret, genel gider varsayılanı, marj eşiği, işletme adı, örnek veri bayrağı: hepsi `Preferences` (MAUI'nin **anahtar-değer deposu**: Android'de `SharedPreferences`, Windows'ta uygulama veri klasöründeki bir dosya) içinde saklanır.

```csharp
public decimal HourlyRate
{
    get => ReadDecimal(HourlyRateKey, DefaultHourlyRate);
    set => WriteDecimal(HourlyRateKey, value);       // decimal → metin (Preferences decimal'i double'a çevirirdi)
}
```

Tek bir `AppSettings` sınıfı sayesinde **hiçbir ekranda "150" sabit değeri kalmadı**. Ücret değişince ana sayfa, reçete listesi, simülatör ve PDF **aynı değeri** kullanır.

> **Terim — Magic number (sihirli sayı):** Koda gömülmüş, anlamı belirsiz sabit (`150m`). Faz 2'de saatlik ücret dört ekranda sabitti; ayarlar eklenince hepsi tek servise bağlandı.
>
> **Terim — Key-value store (anahtar-değer deposu):** `"hourlyRate" → "150"`. Küçük, yapısız ayarlar için ideal; ilişkisel veri için değil (onun için SQLite).

### 6.7 `Display`: sunum yardımcıları (iş mantığı değil)

Core `decimal` döndürür (`177.1000m`). Ekranda **"₺177,10"** olarak göstermek **sunum** sorumluluğudur ve `Display` sınıfında toplanmıştır:

| Metot | Görev |
|---|---|
| `Money(decimal)` | `₺177,10` (tr-TR kültürü, `Money.Round` ile) |
| `UnitPrice(decimal)` | `₺0,30` (en çok 4 basamak; çünkü 0,30 TL/g'ı iki basamağa yuvarlamak yanıltıcı olurdu) |
| `PercentLabel(rate)` | `%60,64` ya da negatifse `-%0,21` |
| `RateFromPercent / PercentText` | Ekranda yüzde (10), modelde kesir (0,10): **sınırda** çeviri |
| `TryParseDecimal(text)` | `"2,5"` (Türkçe) ve `"2.5"` kabul eder |
| `ParseOrZero` | Canlı önizleme için: geçersiz/boş girdi 0 sayılır |

**Kültür (culture) detayı:** `AppDefaults.Culture = new CultureInfo("tr-TR")`. Uygulama, telefon hangi dilde olursa olsun **Türkçe** biçimlendirir (virgül ondalık, nokta binlik, ₺ simgesi). Sayı okurken önce Türkçe (`2,5`), olmazsa kültürden bağımsız (`2.5`) denenir. **Binlik ayracı desteklenmez** ("1.000" → 1 olarak okunur), kasıtlı basitlik; bu kısıtlama dokümanda yazılı.

> **Terim — i18n / l10n (internationalization / localization):** Uygulamayı birden çok dile/kültüre hazırlamak (i18n) ve gerçekten çevirmek (l10n). Biz **yalnızca Türkçe** hedefliyoruz: metinler kodda sabit. Çok dilli olacaksa metinler `.resx` kaynak dosyalarına taşınır ve `CultureInfo.CurrentCulture` kullanılır.
>
> **Terim — Sınırda çevirme (convert at the boundary):** İç dünyada tek bir temsil (kesir), dış dünyada (kullanıcı) başka bir temsil (yüzde); çeviri **sadece sınırda** yapılır. Aksi halde "bu 10 mu 0,10 mu?" sorusu kodun her yerinde belirir (ünlü Mars Climate Orbiter kazası, birim karışıklığının klasik örneğidir).

### 6.8 ViewModel'lerin tek tek gezintisi

| ViewModel | Ne yapar, dikkat çeken fikir |
|---|---|
| `HomeViewModel` | Özet: sayılar, "dikkat gerektirenler", tablo. Tüm kuralları Core'a sorar (`MarginAlerts.IsBelowThreshold`), kendisi hesaplamaz. |
| `IngredientsViewModel` | Liste + dokunarak düzenleme (seçim sıfırlama hilesi). |
| `IngredientEditViewModel` | Form + **sıkı doğrulama** + canlı birim fiyat önizlemesi + **kayıttan önce/sonra marj kontrolü** (bölüm 7). |
| `RecipesViewModel` | Liste: kalem sayısı, satış fiyatı, marj, "⚠ düşük". |
| `RecipeEditViewModel` | En karmaşık olan: kalemler, **canlı maliyet**, satış fiyatı marjı. Aşağıda. |
| `RecipeItemViewModel` | Tek bir kalem satırı: kendi önizleme tutarını bilir, değişince ebeveyni (callback) haberdar eder. |
| `SimulatorViewModel` | Reçete + platform + mod + hedef kâr → döküm ve tüm platformların karşılaştırması. |
| `PriceListViewModel` | PDF oluştur/aç/paylaş; platforma göre klasör seçimi; beklenmedik hatayı mesaja çevirme. |
| `SettingsViewModel` | Ayarlar + örnek veri yükle/sil (onaylı). |

#### `RecipeEditViewModel`: iki farklı doğrulama stratejisi

Aynı ekranda iki ihtiyaç çakışıyor:

1. **Canlı önizleme:** Kullanıcı yazarken ("1" yazıp "12"ye, ya da alanı silip boş bırakırken) maliyet **bozulmadan** güncellenmeli. Yarım yazılmış girdi hata fırlatmamalı. Strateji: **toleranslı**: geçersiz/boş = 0 (`ParseOrZero`), hammaddesiz satır atlanır.
2. **Kaydet:** Veritabanına **bozuk veri** girmemeli. Strateji: **sıkı**: boş ad, verim ≤ 0, kalem yok, miktar ≤ 0, negatif fire → her biri Türkçe hata mesajı ile reddedilir.

Bu "gevşek önizleme / sıkı kayıt" ayrımı, kullanıcı deneyimi ile veri bütünlüğünü birlikte sağlar.

**Yeniden giriş koruması (`_loading` bayrağı):** Veriyi yüklerken alanları tek tek atarız; her atama `OnXChanged` kancasını tetikler ve `Recalculate` çağırır. Yarım yüklenmiş durumda 10 kez gereksiz hesaplamak (ve ekranın titremesi) yerine, yükleme boyunca bayrak açıktır, bittiğinde **bir kez** hesaplanır:

```csharp
_loading = true;
try { ...alanları doldur... }
finally { _loading = false; Recalculate(); }
```

> **Terim — Yeniden giriş koruması (reentrancy guard):** Bir işlemin sürerken kendi tetiklediği olaylarla tekrar çağrılmasını engelleyen bayrak. `try/finally` ile bayrağın **her koşulda** kapanması garanti edilir (istisna olsa bile).
>
> **Terim — Olay fırtınası (event storm) / kademeli güncelleme:** Bir özelliğin değişmesi diğerlerini tetikler, onlar da başkalarını... Yükleme gibi toplu işlerde bunu bastırmak gerekir.

#### `SimulatorViewModel`: değiştirilemezlik (immutability) ve iki küçük ders

Kullanıcı "Kargo / ek sabit ücret" girince, **seçili platformun sabit ücretine eklenir**. Ama platform nesneleri paylaşılan, önbellekteki (`PlatformService._cache`) nesnelerdir. Onları değiştirseydik, bir sonraki hesapta ücret **kümülatif** artardı. Bu yüzden **yeni bir kopya** üretilir:

```csharp
private static Platform WithExtraFee(Platform platform, decimal extraFee) => new()
{
    Name = platform.Name, CommissionRate = platform.CommissionRate,
    FixedFee = platform.FixedFee + extraFee, Notes = platform.Notes
};
```

> **Terim — Değiştirilemezlik (immutability) ve yan etkiden kaçınma:** Paylaşılan bir nesneyi yerinde değiştirmek, başka kodları sessizce etkiler. Yeni bir kopya üretmek güvenlidir.

**Gözden kaçan hata (yazarken fark edilen):** Platform dosyası okunamazsa hata mesajını `LoadAsync` ayarlıyordu ama hemen ardından `Recalculate` mesajı **siliyordu**; kullanıcı hatayı hiç göremezdi. Çözüm: hatayı ayrı bir alanda (`_platformLoadError`) saklayıp `Recalculate`'in onu yeniden göstermesi. **"Hata durumu sonraki bir temizleyici tarafından ezilmesin"** diye düşünmek, kodu yazarken yakalanabilen bir kalıp.

#### `PriceListViewModel` ve `DeviceInfo`: platforma göre dallanma

```csharp
var root = DeviceInfo.Platform == DevicePlatform.WinUI
    ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)   // Windows: kullanıcı bulabilsin
    : FileSystem.CacheDirectory;                                         // Android: uygulamanın önbelleği
```

Platforma özgü davranış, çalışma zamanında `DeviceInfo.Platform` ile ayrılır (alternatif: derleme zamanı `#if ANDROID`). Bu iki satır dışında ViewModel platformdan habersizdir.

### 6.9 View'lar (XAML): bildirimsel arayüz

```xml
<Border Padding="14" StrokeShape="RoundRectangle 12"
        BackgroundColor="{AppThemeBinding Light={StaticResource Secondary}, Dark={StaticResource Gray600}}">
    <VerticalStackLayout>
        <Label Text="{Binding IngredientCount}" FontSize="28" FontAttributes="Bold" />
        <Label Text="Hammadde" FontSize="12" TextColor="Gray" />
    </VerticalStackLayout>
</Border>
```

Dikkat çeken yapı taşları:

- **Yerleşim (layout) kapsayıcıları:** `VerticalStackLayout` (alt alta), `Grid` (satır/sütun ızgarası), `Border` (çerçeve/yuvarlak köşe). `ColumnDefinitions="*,*"` iki **eşit** sütun; `"2*,*,*"` birincisi iki kat geniş. (**Yıldız boyutlandırma**: kalan alanı oranlayarak paylaştırır.)
- **Duyarlı (responsive) düzen:** sayfa sütunu `MaximumWidthRequest="900"`, `HorizontalOptions="Fill"`: telefonda tam genişlik, geniş pencerede ortalanmış ve sınırlı. **Tek tasarım iki cihaz.**
- **`AppThemeBinding`:** açık/koyu temaya göre farklı renk. Cihaz karanlık moda geçince **otomatik** uygulanır.
- **`DataTrigger`:** veri koşuluna göre görünüm: `IsLow == True` olan marj hücresi turuncu.
- **`BindableLayout` ve `CollectionView`:** ikisi de listeleri `DataTemplate` ile çizer. `CollectionView` **sanallaştırır** (ekrandaki satırlar kadar görsel öğe üretir, kaydırdıkça yeniden kullanır; uzun listelerde şart). `BindableLayout` bir `ScrollView` içinde basit listeler içindir ve **sanallaştırmaz** (hepsini çizer). Hammadde/Reçete listelerinde ilkini, kalem satırı/karşılaştırma tablosu gibi kısa listelerde ikincisini kullandık.

> **Terim — Bildirimsel (declarative) vs buyurgan (imperative) arayüz:** "Ne istediğini tarif et" (XAML/HTML/SwiftUI) vs "adım adım nasıl yapılacağını yaz" (`label.Text = ...`). Bildirimsel olan, durumdan arayüzü **türetir**; senkron tutmak zorunda kalmazsın.
>
> **Terim — Sanallaştırma (virtualization) / hücre geri dönüşümü (cell recycling):** 10.000 satırlık listede 10.000 görsel öğe yaratmak yerine ekrana sığan ~12 tanesini üretip kaydırdıkça **yeniden kullanmak**. Mobil listelerin performansının temeli.
>
> **Terim — Dokunma hedefi (touch target):** Parmakla vurulacak alanın erişilebilirlik için en az ≈ 44-48 dp olması önerilir. "150" yazan, 50 piksel genişliğindeki alan bu açıdan zayıftı; telefonda gözle görüp tam genişliğe çektik.

### 6.10 Platforma özgü konular

| Konu | Nasıl |
|---|---|
| Çoklu hedef (multi-targeting) | `.csproj`: `<TargetFrameworks>net9.0-android;net9.0-windows10.0.19041.0</TargetFrameworks>` |
| Android'e özel dosyalar | `Platforms/Android/`: `MainActivity`, `Resources/values/colors.xml` (durum çubuğu ve tema rengi marka rengine çekildi) |
| Windows | `WindowsPackageType=None` (paketlenmemiş, doğrudan `.exe`) |
| Varlıklar (assets) | `Resources/Raw/platforms.json` → uygulama paketine gömülür; `FileSystem.OpenAppPackageFileAsync` ile okunur |
| Simge/açılış | SVG kaynaklarından her platform için PNG/ikon **derleme sırasında üretilir** |
| Veri yeri | `FileSystem.AppDataDirectory` (kalıcı, uygulamaya özel), `FileSystem.CacheDirectory` (sistem temizleyebilir) |
| Geri tuşu (Android) | Shell yönetir; kök sekmede geri = uygulamadan çıkış (test sırasında bu davranışı da gözledik) |
| Yerel kitaplıklar | APK'da `arm64-v8a` (gerçek telefonlar) ve `x86_64` (emülatör) için SQLite ve Skia'nın `.so` dosyaları var |
| Debug / Release | Release'de **trimleme** (kullanılmayan kodu atma; varsayılan kısmi modda yalnızca budanabilir işaretli kütüphaneler budanır) ve **AOT** (önceden makine koduna derleme) açılır |

> **Terim — Trimleme (linker) ve yansıma (reflection) riski:** Release derlemesi, "kullanılmıyor görünen" kodu paketten **siler** (boyut küçülsün diye). Ama **yansıma ile** (adı metinle verip çalışma zamanında bulunan) çağrılan kod "kullanılmıyor" sanılıp silinebilir; Debug'da çalışan uygulama Release'de çökebilir. Bu yüzden **Release sürümünü ayrıca denedik** (SQLite, Skia, PDF, marj uyarısı Release'de çalıştı).
>
> **Terim — AOT / JIT:** *JIT* (Just-In-Time) kodu çalışırken makine koduna çevirir (esnek, açılışı yavaş). *AOT* (Ahead-Of-Time) önceden çevirir (açılış hızlı, paket büyük). Emülatörde Release açılışı Debug'dan belirgin hızlıydı.
>
> **Terim — ABI (Application Binary Interface) ve mimari:** `arm64-v8a` (gerçek telefon işlemcisi) ile `x86_64` (bilgisayar/emülatör) **farklı makine dilleridir**. Emülatörde çalışan APK, ARM telefonda **yerel kitaplık yüzünden** çökebilir; bu yüzden APK'nın içine bakıp her iki mimari için kitaplık olduğunu doğruladık. (Çalıştırmayı yalnızca x86_64'te denedik; bu bir sınırdır.)

---

## 7. Uçtan uca bir örnek: "Soya mumunun fiyatını değiştir"

Şimdiye kadar katmanları tek tek gördük. Şimdi bir kullanıcı eyleminin **bütün katmanlardan** nasıl geçtiğini izleyelim. Senaryo (sunum betiğindeki "enflasyon modu"): kullanıcı **Hammadde** sekmesinde "Soya mumu"nu açar, alış fiyatını `300`'den `1500`'e çıkarır ve **Kaydet**'e basar.

### 7.1 Akış

```
KULLANICI           VIEW                 VIEWMODEL                      DEPO (Data)            CORE
   │                 │                        │                              │                   │
   │ satıra dokun    │                        │                              │                   │
   ├────────────────►│ SelectedItem değişti   │                              │                   │
   │                 ├───────────────────────►│ IngredientsViewModel         │                   │
   │                 │                        │ → GoToAsync("...EditPage?id=1")                   │
   │                 │                        │                              │                   │
   │                 │  sayfa DI'dan kurulur; ApplyQueryAttributes(id=1); OnAppearing → LoadCommand│
   │                 │                        ├─ repository.GetAsync(1) ────►│ SELECT → Row.ToModel()
   │                 │ ◄── alanlar dolar ─────┤◄────────── Ingredient ───────┤                   │
   │                 │                        │                              │                   │
   │ "1500" yaz      │                        │                              │                   │
   ├────────────────►│ PriceText değişti ────►│ UpdatePreview()              │                   │
   │                 │ ◄─ "Birim fiyat ₺1,50" ┤  └─ Display.TryParseDecimal  ·· Ingredient.UnitPrice
   │                 │                        │                              │                   │
   │ Kaydet          │                        │                              │                   │
   ├────────────────►│ SaveCommand ──────────►│ 1) sıkı doğrulama           │                   │
   │                 │                        │ 2) recipesBefore = recipes.GetAllAsync() ───────► (A: eski fiyatlarla)
   │                 │                        │ 3) repository.SaveAsync(yeni Ingredient) ► UPDATE (DecimalText)
   │                 │                        │ 4) recipesAfter  = recipes.GetAllAsync() ───────► (B: yeni fiyatlarla)
   │                 │                        │ 5) MarginAlerts.FindDrops(A, B, saatlikÜcret, eşik) ► CostCalculator.UnitCost
   │                 │                        │                                         PricingCalculator.MarginAtPrice
   │                 │                        │                                         MarginAlerts.IsBelowThreshold
   │                 │ ◄─ DisplayAlert(...) ──┤ 6) uyarı listesi doluysa pencere        │
   │                 │ ◄─ GoToAsync("..") ────┤ 7) sayfayı kapat                        │
   │                 │                        │                              │                   │
   │ Ana Sayfa'ya geç│ OnAppearing → LoadAsync → "Dikkat gerektirenler" listesi güncellenir
```

### 7.2 Adım adım ne oluyor ve sayılar

1. **Doldurma:** `GetAsync(1)` → `IngredientRow.ToModel()` → `PriceText = "300"`, `QuantityText = "1000"`.
2. **Canlı önizleme:** `1500` yazılınca `OnPriceTextChanged` → `UpdatePreview` → `Ingredient { 1500, 1000 }.UnitPrice` = **1,50** → "Birim fiyat: ₺1,50 / g".
3. **Sıkı doğrulama:** ad boş mu? fiyat ≥ 0 mı? miktar > 0 mı?
4. **Anlık görüntü A:** tüm reçeteler **eski** fiyatla yüklenir (yalnızca düzenlemede; yeni hammadde hiçbir reçetede olamaz).
5. **Kayıt:** hammadde güncellenir (`DecimalText` ile kayıpsız).
6. **Anlık görüntü B:** tüm reçeteler **yeni** fiyatla yüklenir. Reçetede saklanan **hiçbir maliyet yoktur**; bu yüzden B'deki maliyetler yeni fiyatı kendiliğinden yansıtır ("türet, saklama").
7. **Karşılaştırma:** `FindDrops(A, B)`:

| Reçete | Maliyet (önce → sonra) | Satış | Marj (önce → sonra) | Uyarı? |
|---|---|---|---|---|
| 200cc aromaterapi mumu | 177,10 → **414,70** | 450,00 | %60,64 → **%7,84** | Evet: düştü ve %30'un altında |
| 100cc mini vanilya mumu | 125,79 → **250,53** | 250,00 | %49,69 → **−%0,21** | Evet: zararına satış |

   (Hesabı elle doğrula: 200cc'de mum kalemi 180 × 1,50 = 270; malzeme 270+30+2+25 = 327; işçilik 50; genel gider (327+50) × %10 = 37,70; toplam **414,70**. Marj = (450 − 414,70) / 450 = **%7,84**.)
8. **Uyarı penceresi:** iki reçete de listelenir, "Simülatör'ü kullanabilirsin" önerisiyle.
9. **Geri dönüş:** `GoToAsync("..")`; Hammadde listesi `OnAppearing` ile yenilenir; Ana Sayfa'da turuncu "Dikkat gerektirenler" kutusu belirir.

### 7.3 Hangi adımı hangi test koruyor?

Bu tablo, **otomatik testin nerede bitip elle doğrulamanın nerede başladığını** gösterir. Önemli bir ders:

| Adım | Otomatik test var mı? | Hangisi |
|---|---|---|
| Satırı okuma, Row ⇄ model çevirisi | **Evet** | `Ingredient_SaveAssignsIdAndRoundTrips`, `Ingredient_DecimalValuesAreStoredLosslessly` |
| `UnitPrice` hesabı | **Evet** | `Ingredient_SaveAssignsIdAndRoundTrips` (0,30), tüm `CostCalculator` testleri |
| **Sayı okuma** (`Display.TryParseDecimal`, `"1500"`, `"2,5"`) | **Hayır** | Yalnızca arayüz üzerinde elle/otomasyonla (emülatörde Türkçe/İngilizce dil) |
| **Sıkı doğrulama kuralları** (`IngredientEditViewModel`) | **Hayır** | Yalnızca arayüz üzerinde |
| Güncelleme kaydı (`SaveAsync` ile `Id != 0`) | **Evet** | `Ingredient_SaveWithExistingIdUpdates` |
| Reçeteleri yükleme, fiyat değişince maliyet değişimi | **Evet** | `Recipe_RoundTripsWithItems`, `IngredientPriceChange_UpdatesRecipeCost_WithoutTouchingRecipe` |
| Maliyet formülleri | **Evet** | `CostCalculatorTests` (altın örnek, fire, verim) |
| Satış fiyatında marj | **Evet** | `PricingCalculatorTests.MarginAtPrice_*` |
| "Düştü ve eşiğin altında mı?" kuralı | **Evet** (en yoğun korunan) | `MarginAlertsTests` (15 test: sınır, eşitlik, kıyas, kaybolan reçete...) |
| Uyarı penceresinin gösterilmesi, `DisplayAlert`, gezinme | **Hayır** | Windows'ta UI Automation, Android'de `adb` ile doğrulandı |
| Sayfaların yenilenmesi (`OnAppearing`) | **Hayır** | Yalnızca arayüz üzerinde |

**Sonuç:** İş mantığının **kalbi** (formüller, kurallar, kalıcılık) güçlü bir ağla korunuyor. **Yapıştırıcı kod** (ViewModel doğrulamaları, sayı okuma, ekran akışı) yalnızca elle/otomasyonla doğrulandı. Bu yaygın bir durumdur ve nedeni teknik bir kısıtlamadır; bkz. bölüm 8.6 (ViewModel'leri test edilebilir kılmak).

---

## 8. Test: ne yaptık, neye karşılık geliyor

Bu projede iki tür doğrulama yaptık: (1) depoda duran **118 otomatik xUnit testi**, (2) uygulamayı gerçek ortamlarda (Windows, Android emülatörü) **çalıştırıp gözlemleme**. İkisi de ayrı değer üretti; ikisinin **neyi yakaladığını** karşılaştırmak, ileride kendi projelerinde strateji kurarken en işine yarayacak kısım olabilir.

### 8.1 Testin yazılım geliştirmedeki yeri

Test yalnızca "hata bulma" değildir. Bir testin aynı anda dört işi vardır:

| Rol | Bu projeden örnek |
|---|---|
| **Spesifikasyon** (beklenen davranışın çalıştırılabilir tanımı) | `Golden_Margin40_Commission20`: "177,10 maliyet, %20 komisyon, %40 marj → 442,75" |
| **Güvenlik ağı** (değiştirirken bozmadığını anlama) | `FindDrops`'u 15 test koruduğu için kuralı sadeleştirirken korkmadık |
| **Canlı dokümantasyon** (bir sınıfın nasıl kullanıldığı) | `PriceListLayoutTests` okuyarak `Wrap`'in kenar durumlarını öğrenirsin |
| **Tasarım geri bildirimi** (zor test edilen kod = kötü tasarım sinyali) | `PriceListLayout`'u test etmek için yazı ölçümünü **parametre** yaptık; ViewModel'leri test edemememiz de bir sinyal (bkz. 8.6) |

> **Terim — Test piramidi (Mike Cohn):** Çok sayıda **hızlı, küçük** birim testi tabanda; daha az **entegrasyon** testi ortada; en az sayıda **uçtan uca (E2E / UI)** test tepede. Alt katman ucuz, hızlı, kararlı; üst katman pahalı, yavaş, kırılgan ama gerçeğe en yakın. Ters piramit ("dondurma külahı": çok UI testi, az birim testi) klasik bir anti-pattern'dir.

#### Bizim piramidimiz

| Katman | Otomatik test sayısı | Nerede | Çalışma süresi |
|---|---|---|---|
| **Birim** (saf fonksiyon, sahte bağımlılıkla) | **89** | `CostCalculator`, `PricingCalculator`, `PriceBreakdown`, `MarginAlerts`, `PlatformCatalog`, `PriceListBuilder`, `PriceListLayout` (sarma/sayfalama) | milisaniyeler |
| **Entegrasyon** (gerçek SQLite / gerçek Skia / gerçek yazı tipi) | **29** | `DataTests` (21), `PriceListPdfTests` (5), `Font_*` testleri (3) | onlarca ms |
| **E2E / UI** | **0 (depoda)** | Elle/komut dosyasıyla yapıldı (8.3) | dakikalar |
| Toplam | **118** | | tüm set ≈ yarım saniye |

Taban geniş, orta dolu, **tepe otomatik değil**. Bu ürün için makul; ama tepe elle olduğu için her değişiklikte yeniden çalıştırılmıyor, bu bir zayıflık (bkz. 8.6).

### 8.2 Hangi teknikleri kullandık? (örneklerle)

#### a) Altın örnek (golden master / test oracle)

**Sorun:** Bir testin "beklenen değeri" nereden geliyor? Kodun ürettiği sayıyı kopyalayıp teste yazarsan, kod **yanlışsa test de yanlışı doğrular** (*kendi kendini doğrulayan döngü*).

**Çözümümüz:** Beklenen değerleri **koddan bağımsız** üretmek, **elle ve spesifikasyonda** (`HESAP_MOTORU.md`): "malzeme 111,00; işçilik 50,00; genel gider 16,10; birim 177,10; marj %40 / %20 komisyon → 442,75". Test bu rakamları doğrular. Rakamlar o kadar önemli ki **üç ayrı katmanda** aynı sayıyı gördük:

1. Birim testte (`CostCalculatorTests.Golden_BatchAndUnitCost` → 177,10),
2. Veri testinde (`Seed_GoldenRecipeMatchesHesapMotoruDoc`: veritabanından yüklenen örnek reçetenin maliyeti de 177,10),
3. Çalışan uygulamada (Windows ve Android ekranında "₺177,10", simülatörde "₺442,75").

> **Terim — Test oracle (kahin) problemi:** Çıktının doğru olup olmadığına **kim karar verecek?** İyi bir oracle koddan bağımsız olmalı: spesifikasyon, elle hesap, başka bir uygulama, fiziksel ölçüm.
>
> **Terim — İzlenebilirlik (traceability):** Gereksinim → test → kod bağı. "Bu formül nereden geliyor?" sorusunun cevabı `HESAP_MOTORU.md`'de, "doğru mu?" sorusunun cevabı testte.

#### b) Sınır değer analizi ve eşdeğerlik sınıfları

Girdi uzayını **sınıflara** bölüp her sınıftan temsilci ve **sınır noktalarını** test ederiz.

```csharp
[Theory]
[InlineData(0.29, true)]
[InlineData(0.30, false)]    // tam eşik: "düşük" sayılmaz  ← bilinçli sınır kararı
[InlineData(0.31, false)]
[InlineData(-0.50, true)]    // zararına satış
public void IsBelowThreshold_IsStrict(double margin, bool expected) => ...
```

Başka sınırlar: `k + m = 1` (hata) ve `k + m > 1` (hata); verim `0` ve `-1`; satış fiyatı `0` ve `-5`; komisyon `0` (geçerli) ve `1` (geçersiz); marj `%200` markup (geçerli, çünkü o modda yalnızca `k < 1` aranır).

> **Terim — Parametreli (veri güdümlü) test:** `[Theory]` + `[InlineData(...)]`: aynı test gövdesi, çok sayıda girdiyle çalışır. Her satır ayrı test sayılır (bu yüzden 118 test, 98 test *metodundan* oluşuyor: 91 `[Fact]` + 7 `[Theory]`'nin toplam 27 veri satırı).
>
> **Terim — Sınır değer analizi (BVA):** Hatalar büyük ölçüde `<`/`≤`, `0`, `-1`, "boş", "tam sınır" gibi uç noktalarda olur; en yüksek hata yoğunluğu orada.

#### c) Olumsuz (negatif) ve hata yolu testleri

Mutlu yolu test etmek kolaydır; ürünleri **bozuk girdide** neler olduğu ayırır. `PlatformCatalogTests` bir `[Theory]` ile **12 farklı bozuk JSON'u** dener (boş dosya, eksik alan, negatif ücret, yinelenen ad...) ve her birinde `FormatException` bekler. `Assert.Throws<InvalidOperationException>(...)` ile verim 0, geçersiz oran gibi durumlar test edilir.

#### d) Test odaklı geliştirme (TDD): kırmızı → yeşil → düzenle

Yeni bir şey eklerken sırayı şöyle tuttuk:

1. **Önce imza + test** yaz; gövdeyi `throw new NotImplementedException()` bırak.
2. Testi çalıştır: **kırmızı** olmalı. (Neden önemli: **testin kendisini test eder.** Kırmızı görmeden yeşile döndürdüğün test, hiçbir şeyi doğrulamıyor olabilir.)
3. Gövdeyi yaz → **yeşil.**
4. Gerekirse düzenle (refactor), testler güvenlik ağı olur.

Bunu birkaç kez gerçekten yaptık: `CostCalculator` (11 kırmızı → yeşil), `PricingCalculator` (13), `Analyze`+`PlatformCatalog` (24 kırmızı), `MarginAlerts` (11), `PriceListBuilder` (7), Türkçe sıralama (2).

> **Terim — TDD (Test-Driven Development, Kent Beck):** Kırmızı-Yeşil-Refactor döngüsü. Fayda: (1) her satır kod bir **ihtiyaca** dayanır, (2) test edilebilir tasarım doğal olarak ortaya çıkar, (3) kapsam yüksek olur. Dürüstlük notu: her yerde katı TDD uygulamadık (ör. `Money.Round`, dökümantasyon örneği, bazı Data testleri kodla birlikte yazıldı); fakat Core formüllerinde uyguladık, çünkü orada beklenen değerler **önceden** biliniyordu.

#### e) Regresyon testleri: bulunan hatanın mezar taşı

Bir hata bulunca **önce** onu yakalayan testi yazıp kırmızı görmek, sonra düzeltmek: hata geri gelirse test uyarır. Projeden örnekler: `Golden_LaborCost` (49,999… hatası), `Font_BoldStyle_IsReallyBold...` (kalın yazı), `..._GetAllUsesTurkishAlphabetOrder` (SQL sıralaması), `MarginExactlyAtThreshold_IsNotReported` (eşit sınır).

#### f) Entegrasyon testleri: sahte değil, gerçek veritabanı

`DataTests`, depoları **mock'lamaz**; gerçek SQLite'ı (bellek içi) kullanır:

```csharp
private static async Task<(IIngredientRepository, IRecipeRepository, SeedService)> CreateAsync()
{
    var db = AtolyeDatabase.CreateInMemory();      // her test kendi boş veritabanı
    await db.InitializeAsync();
    ...
}
```

Neden mock değil? Depo kodunun **asıl işi SQL'dir** (çeviri, transaction, sorgular). Mock'lasan, tam da riskli parçayı test dışı bırakırdın. Gerçek (ama bellek içi) veritabanı hem hızlıdır hem gerçek davranışı sınar: `decimal`'in `double`'a **sessizce çevrilmesi** (bölüm 4.2), sıralama (4.9), kayıpsız gidiş-dönüş **sadece gerçek veritabanıyla** yakalanır.

Ne test ediliyor: **gidiş-dönüş (round-trip)** (yaz → oku → aynı), **güncelleme**, **silme**, **silme koruması**, **kalem değişimi**, **null satış fiyatı**, **idempotans** (örnek veri iki kez), **türetilmiş veri** (fiyat değişince maliyet değişir: reçeteye dokunmadan).

> **Terim — Mock'a karşı gerçek bağımlılık (Fowler: "Mocks Aren't Stubs"):** Mock/stub, bağımlılığın **yerine geçer** ve davranış doğrulaması yapar. Entegrasyon testi gerçek bağımlılıkla çalışır. Kural: **kendi sahip olmadığın/kontrol edemediğin (ağ, saat, ödeme API'si) şeyleri** mock'la; **kendi veri katmanını** mümkünse gerçekle test et.
>
> **Terim — Test izolasyonu ve deterministik test:** Test sıradan, saatten, makineden, önceki testten **bağımsız** aynı sonucu vermeli. Bağlantı havuzu hatası (bölüm 4.7) tam bunu bozuyordu.

#### g) Konfigürasyon / sözleşme testi

```csharp
[Fact]
public void ShippedPlatformsJson_IsValid()
{
    var path = FindRepoFile("AtolyeRecete.App", "Resources", "Raw", "platforms.json");
    var list = PlatformCatalog.Parse(File.ReadAllText(path));
    Assert.NotEmpty(list);
    Assert.All(list, p => Assert.InRange(p.CommissionRate, 0m, 0.99m));
}
```

Bu test, **uygulamaya gömülen gerçek JSON dosyasını** okur. Biri dosyayı yanlışlıkla bozarsa (virgül, negatif oran) uygulama yayına çıkmadan **test** kırılır. Çözüm kökünü (`.sln`'i) yukarı doğru arayarak bulur; yani test hangi klasörden çalıştırılırsa çalıştırılsın dosyayı bulur.

> **Terim — Sözleşme testi (contract test):** İki parçanın (burada "veri dosyası" ile "okuyucu kod") birbirinin beklentisini karşıladığını doğrular. Sürüm, şema, API değişikliklerine karşı erken uyarı.

#### h) Test double: sahte ölçüm (fake)

`PriceListLayoutTests` yazı tipini hiç kullanmaz: `Measure = text.Length * 10f`. Böylece "100 birimde 10 harf sığar" gibi **deterministik** sayılar vardır (bölüm 5.4).

#### i) Görünmeyen bağımlılık: makineye bağlı test (dürüst not)

`Font_CanRenderTurkishLettersAndLiraSign`, **o makinede yüklü yazı tiplerine** bağlıdır. Çok eski/boş bir CI imajında `₺`'yi içeren yazı tipi olmayabilir ve test (haklı olarak) kırılabilir; yani bu test **çevreyi** de test eder. Bilerek tuttuk (gerçek bir riski yakalar: tofu) ama bu **kararsız (flaky) olabilecek** türden bir testtir.

> **Terim — Flaky (kararsız) test:** Aynı kodda bazen geçen bazen kalan test. Sebepler: zaman, rastgelelik, paylaşılan durum, ağ, makine farkı. Flaky testler ekibin test **güvenini** aşındırır; kaçınılmalı ya da izole edilmeli.

### 8.3 xUnit dışı doğrulama: uygulamayı gerçek ortamlarda çalıştırmak

Birim testler "kod doğru mu?"yu cevaplar; **"kullanıcı için doğru çalışıyor mu?"** sorusu için uygulamayı gerçekten çalıştırıp gözlemledik. Kullandığımız araçların sektördeki karşılığı da yanında:

| Yaptığımız | Araç | Sektördeki muadili |
|---|---|---|
| Windows uygulamasını sürüp ekrandaki **metinleri** okumak, tıklamak, yazmak | **UI Automation** (erişilebilirlik ağacı, PowerShell) | WinAppDriver, FlaUI, Appium |
| Ekran görüntüsü alıp **gözle** incelemek | `PrintWindow`, `adb screencap` | Görsel regresyon araçları (Percy, Applitools) |
| Android'de dokunmak, yazmak, ekran hiyerarşisini okumak | **`adb`** (`input tap/text`, `uiautomator dump`) | Espresso, UI Automator, Appium |
| Çökmeleri ve hatayı bulmak | `adb logcat` (sistem günlüğü) | Crashlytics, Sentry |
| Açılış süresini ölçmek | `am start -W` | Android Vitals, profil çıkarıcılar |
| Farklı cihaz koşulları | Emülatör + `wm size/density`, `uimode night`, `font_scale`, uygulama dili | Cihaz çiftlikleri (Firebase Test Lab, BrowserStack) |

**Nasıl çalışıyor (UI Automation örneği):** Uygulamanın arayüzü bir **erişilebilirlik ağacı** olarak sunulur (ekran okuyucuların kullandığı yapı). Her öğenin adı, türü, konumu vardır. Betik, "Hammadde" adlı sekmeyi bulur ve `SelectionItemPattern.Select()` çağırır; metin kutusunu bulur ve `ValuePattern.SetValue("1500")` der; "Kaydet" düğmesini `InvokePattern.Invoke()` ile tıklar. Sonra ekrandaki **tüm metinleri** döker ve beklenenle karşılaştırırız.

> **Terim — E2E / sistem testi:** Uygulamayı kullanıcının gördüğü uçtan (arayüz) sürüp tüm katmanları birlikte sınamak. En gerçekçi, en pahalı, en kırılgan test türü.
>
> **Terim — Erişilebilirlik ağacı (accessibility tree):** Ekran okuyucular (TalkBack, Narrator) için uygulamanın anlamsal betimi. UI otomasyonu aynı yapıyı kullanır; bu yüzden **erişilebilir uygulama aynı zamanda test edilebilir uygulamadır.**
>
> **Terim — Duman testi (smoke test):** "Uygulama açılıyor mu, ana ekranlar gelip çökmeden dolaşılıyor mu?" gibi hızlı, geniş ama sığ kontrol.

#### Test ortamı hijyeni

Otomasyon gerçek veritabanına yazdığı için (fiyat değiştirmek, silmek...) her koşudan önce **tüm veri klasörünü yedekledik**, sonunda `finally` bloğunda **geri yükledik** ve geri yüklemeyi doğruladık. Test verisinin kullanıcının gerçek verisine **sızmaması** bir mühendislik sorumluluğudur.

> **Terim — Test verisi yönetimi / ortam sıfırlama:** Her test koşusunun bilinen bir başlangıç durumundan başlaması ve bittiğinde ortamı **temizlemesi**. Aksi halde testler birbirini ve gerçek veriyi kirletir.

#### Çevre/koşul matrisi

Aynı uygulamayı şu koşullarda denedik; her biri **farklı bir hata sınıfını** açığa çıkardı:

| Koşul | Nasıl | Neyi açığa çıkardı |
|---|---|---|
| **Debug ve Release** | iki ayrı derleme | Release'de trimleme/AOT davranışı; açılış süresi farkı (~13 sn → ~8 sn emülatörde) |
| **Windows ve Android** | iki platform | QuestPDF'in Android'de çalışmaması; kaydetme klasörü farkı |
| **Uygulama dili Türkçe / İngilizce** | `cmd locale set-app-locales` | **Virgül süzülmesi** ("2,5" → 25): Android sayı klavyesi dil ayarına göre |
| **Küçük ekran** (360 dp) | `wm size 720x1280`, `density 320` | **Kırpılan sekme adları**, kesilen seçici metni |
| **Büyük yazı** (%130) | `font_scale 1.3` | Metin sarma, taşma (sorun çıkmadı) |
| **Karanlık mod** | `cmd uimode night yes` | **Düşük kontrast** (kartlar, başlık rengi) |
| **Yatay ekran** | döndürme | Düzen sağlam (planlı değildi, tesadüfen denendi) |
| **Eski veritabanı** | gerçek eski dosyayla açma | Şema göçü (`SalePrice`) sorunsuz |

> **Terim — Uyumluluk testi (compatibility / matrix testing):** Aynı davranışı farklı platform, ekran, dil, sürüm, tema kombinasyonlarında doğrulamak. Mobilde "benim telefonumda çalışıyor" yetmez; çünkü cihaz çeşitliliği (ekran boyutu, üretici klavyesi, dil, Android sürümü) hata kaynağıdır.
>
> **Terim — Geriye dönük uyumluluk testi:** Eski sürümün verisini yeni sürümün açabildiğini doğrulamak (şema göçü).

#### Testin testi: yanlış alarmlar

Bazen **test aracının kendisi** yanlış sonuç verdi. Bu da başlı başına önemli bir ders:

- **"Maliyet etiketleri görünmüyor!"** Ekran görüntüsünde sağ taraf boş görünüyordu; ben "arayüz hatası" sandım. Konum verisine bakınca etiketlerin pencere içinde olduğu, görüntü alma betiğimin **ekran ölçekleme (DPI)** yüzünden görüntüyü sağdan **kestiği** ortaya çıktı. Uygulamada hata yoktu; **önce ölçümü doğruladım, kodu değiştirmedim.**
- Arka planda başlatılan betiklerde pencere **küçültülmüş** açılıyor, ekran görüntüsü 200×30 piksellik boş şerit çıkıyordu; betiği "pencereyi geri yükle" diye düzelttim.
- Emülatörün klavyesi **yüzen bir araç çubuğu** olarak "Kaydet" düğmesinin üstüne biniyordu; dokunuşum düğmeye değil araç çubuğuna gidiyordu. Ortam sorunuydu, uygulama sorunu değil.
- Bazı betikler Türkçe karakter kodlaması (BOM'suz UTF-8) yüzünden derlenmedi; bazıları `\U` kaçış karakteri yüzünden çalışmadı.

> **Terim — Yanlış pozitif (false positive) ve araç güvenilirliği:** Test "hata var" dediğinde hata test aracında olabilir. Kural: **hata raporlamadan önce şüphelen:** aracı (ve önce **ölçümü**) doğrula; hipotezi **kanıtla** (konum verisi, günlük, ikinci bir yöntem). İlk akla gelen açıklama çoğu zaman yanlıştır.
>
> **Terim — "Bekleme (sleep)" ile senkronizasyon:** Betiklerde "2 saniye bekle" kullandım. Bu basit ama **kırılgan**: yavaş emülatörde kısa gelir (nitekim bir testte düğme henüz pasifken basmışım). Daha iyisi: öğe belirene/etkinleşene kadar **koşula bağlı bekleme (polling)**.

### 8.4 Bulunan hataların tam kataloğu

Aşağıdaki tablo, geliştirme boyunca **bulduğumuz ve düzelttiğimiz** her hatayı, **hangi teknikle** yakalandığıyla birlikte listeler. Bu tablo, "hangi test türü hangi hatayı bulur?" sorusunun somut cevabıdır.

| # | Hata | Yakalayan teknik | Kök neden | Ders |
|---|---|---|---|---|
| 1 | İşçilik 50,00 yerine **49,999…** | **Birim test** (altın örnek, kırmızı) | `20/60` ondalıkta sonsuz | İşlem sırası; bölmeyi en sona bırak |
| 2 | Testler **birbirinin verisini** görüyordu | **Entegrasyon testi** (7 kırmızı) | Bağlantı havuzu aynı `:memory:` dizesini paylaşıyordu | Gizli paylaşılan durum; **ilk teşhis yanlıştı** (SharedCache), doğrulayınca anlaşıldı |
| 3 | Silinen örnek veri **geri geliyordu** | **Tasarım incelemesi** (ayarlar yazarken) | Durumsuz "boşsa yükle" kuralı | Kalıcı durum (bayrak); bütünün davranışını düşün |
| 4 | "Her şey yolunda" kutusu **görünmüyordu** | **UI otomasyonu** (ekran metin dökümü) | Bağımlı özellik için bildirim eksik | `NotifyPropertyChangedFor`; birim test bunu göremezdi |
| 5 | PDF düğmesi Android'de **çöküyordu** | **Emülatörde çalıştırma** + `logcat` | QuestPDF'in yerel kitaplığı Android'de yok | Çapraz platform ≠ "derlenir"; yerel bağımlılık |
| 6 | Beklenmedik PDF hatası **uygulamayı kapatıyordu** | Aynı koşu | `catch` yalnızca IO hatalarını yakalıyordu | Savunmacı programlama; mesaj göster, yıkma |
| 7 | **"2,5" yazılınca "25"** | **Dil değiştirerek test** (İngilizce emülatör) | Android sayı klavyesi cihaz diline göre virgülü süzüyor | Yerelleştirme testi; Türkçe'de doğrulandı |
| 8 | Sekme adları **kırpılıyordu** | **Küçük ekran testi** (360 dp) | Uzun etiketler | Dar ekranı da tasarla: "Hammadde", "Fiyatlar" |
| 9 | Koyu modda kartlar **ayrışmıyor**, başlık silik | **Karanlık mod turu** | Açık tema renkleri koyuya taşınmamış | Her tema için kontrast |
| 10 | Giriş alanları **çok dar** (≈ 50 px) | **Telefon ekran görüntüsü** | `Start` + `MaximumWidth` ile içerik kadar daralma | Dokunma hedefi boyutu |
| 11 | **Kalın yazı kalın değil** (PDF) | **Görsel inceleme** (PyMuPDF görüntüsü) | `FromFamilyName(null, Bold)` stili yok sayıyor | **Sessiz hata**; ardından `IsBold` testi |
| 12 | Para biçimi tutarsız (₺ önde / arkada), "Sil" yazısı okunmuyor, Picker başlığı hizayı bozuyor | **Windows ekran görüntüleri** | Stil/biçim ayrıntıları | Gözle bakmak, "çalışıyor"dan fazlasını gösterir |
| 13 | Düzeltilen hata mesajı **ekranda kalıyor** | **UI akış testi** | Mesaj hiçbir yerde temizlenmiyordu | Durum temizleme |
| 14 | Platform dosyası hatası **hemen siliniyordu** | **Kod gözden geçirme** (yazarken) | Sonraki `Recalculate` mesajı eziyordu | Hata durumunu başkası ezmesin |
| 15 | **Listeler Türkçe sıralanmıyordu** | **Şüphe + doğrulama** (rehber yazarken) | SQLite BINARY sıralaması | **Test verisi temsil gücü**; testle düzeltildi |
| 16 | `Platform` adı **MAUI'nin kendi sınıfıyla çakıştı** | **Derleyici** | Ad uzayı çakışması | `using` takma adı (alias) |
| 17 | `CommunityToolkit.Maui` 15.x **kurulmadı** | **Paket yöneticisi** (NU1202) | Yeni sürüm .NET 10 istiyor | Sürüm uyumu; 12.3.0'a sabitledik |
| 18 | `SKTypeface.ContainsGlyph` **kullanımdan kalktı** uyarısı | **Derleyici uyarısı** | Kütüphane sürüm yükseltmesi | Uyarıları ciddiye al, kaynak/yeni yol |

**Örüntü:** Birim testler **mantık ve sayı** hatalarını (1, 2, 15-sıralama, 11-regresyon) yakaladı. **Çalıştırıp gözlemleme** ise **entegrasyon/platform/görünüm** hatalarını (4-14) yakaladı; bunların birim test karşılığı yazılması zor ya da imkânsız. İkisi birbirinin yerini **tutmuyor**.

> **Terim — Shift-left (sola kaydır):** Hatayı geliştirme zincirinin **ne kadar erken** noktasında yakalarsan o kadar ucuz. Derleyici hatası (saniyeler) < birim test (saniyeler) < entegrasyon testi (dakikalar) < manuel/UI testi (dakikalar-saatler) < kullanıcı şikâyeti (günler) < veri kaybı/itibar kaybı. Bu tablodaki 16-18 derleyiciyle, 1-2 testle, 5-10 emülatörle yakalandı (15 ise şüpheyle bulundu, sonra teste dönüştü); **hiçbirini kullanıcı yakalamadı.**

### 8.5 Test sayıları neye "tekabül ediyor"? (dosya dosya)

| Test dosyası | Test | Koruduğu şey | Olmasa ne olurdu? |
|---|---|---|---|
| `CostCalculatorTests` | 12 | **Para hesabının kalbi**: kalem, malzeme, işçilik, genel gider, parti, birim; fire; verim; fiyat değişimi | Formüldeki sessiz yanlışlık, **yanlış fiyata** satış demek; kullanıcının parası |
| `PricingCalculatorTests` | 19 | Marj/markup formülleri, sabit ücret, **geçersiz oranlar**, `MarginAtPrice` | `k+m ≥ 1`'de negatif/sonsuz fiyat gösterilirdi |
| `PriceBreakdownTests` | 7 | Komisyon/net kâr/gerçekleşen marj ve markup, sıfıra bölme koruması | Dökümde tutarsız rakamlar |
| `MarginAlertsTests` | 15 | **Uyarı kuralı**: eşik, eşitlik, iyileşme, kaybolan reçete, farklı fiyat anlık görüntüsü | Gürültülü ya da **eksik** uyarılar |
| `PlatformCatalogTests` | 17 | Veri dosyası doğrulaması (12 bozuk girdi) + gerçek dosya | Bozuk bir JSON, sessizce %0 komisyonlu platform yaratırdı |
| `PriceListTests` | 7 | Kim listeye girer, Türkçe sıralama, kırpma | Müşteriye fiyatsız/yanlış sıralı liste gider |
| `DataTests` | 21 | Gerçek SQLite: gidiş-dönüş, silme koruması, **kayıpsız decimal**, örnek veri, Türkçe sıralama | Veri kaybı, bozuk kayıt, "double" yuvarlama hatası |
| `PriceListLayoutTests` | 15 | Satır sarma, sayfalama, yazı tipi kapsamı | Uzun ürün adı taşar, sayfa sınırında satır kaybolur |
| `PriceListPdfTests` | 5 | PDF'in üretilebilmesi (Türkçe harf, boş, çok sayfa) | Belirli girdide PDF üretimi **çöker** |

**Test/üretim kod oranı**, Core + Data + Pdf için ≈ 1'e 1'dir. Bu oran tek başına kaliteyi ölçmez, ama "formül ve veri katmanlarının neredeyse her dalı bir testle karşılanıyor" demektir. (Gerçek kapsam ölçülmedi; bkz. 8.7.)

### 8.6 Test edilmeyenler ve nasıl tamamlanır (dürüst bölüm)

**Neden ViewModel'lerimiz için otomatik test yok?** Teknik nedeni önemli bir tasarım dersi içeriyor:

1. Test projesi `net9.0` hedefler; **MAUI projesine (`net9.0-android`/`windows`) başvuramaz.**
2. Ayrıca ViewModel'ler **MAUI'nin statik API'lerine** dokunuyor: `Shell.Current.GoToAsync(...)`, `Shell.Current.DisplayAlert(...)`, `Preferences`, `DeviceInfo`, `Launcher`, `Share`. Bunlar test ortamında yoktur/çalışmaz.

> **Terim — Dikiş (seam) ve statik bağımlılık:** Testte bir davranışı **değiştirebildiğin nokta** "seam"dir (Michael Feathers, *Working Effectively with Legacy Code*). `IRecipeRepository` arayüzü iyi bir seam; `Shell.Current` (statik global) ise seam **yok**: değiştiremezsin, dolayısıyla test edemezsin.

**Nasıl çözülür?** Üç adım:

```csharp
// 1) Statik çağrıları arayüzün arkasına sakla (seam yarat)
public interface INavigationService { Task GoToAsync(string route); }
public interface IDialogService     { Task<bool> ConfirmAsync(string title, string message, string ok, string cancel);
                                      Task AlertAsync(string title, string message); }
public interface ISettings          { decimal HourlyRate { get; set; } ... }   // Preferences'ın üstüne

// 2) ViewModel'leri MAUI'siz bir sınıf kütüphanesine taşı (AtolyeRecete.Presentation, net9.0)
// 3) Testte sahte (fake) gerçekleştirmelerle çalıştır:
var vm = new IngredientEditViewModel(new FakeIngredientRepo(), new FakeRecipeRepo(),
                                     new FakeSettings(), new FakeNavigation(), new FakeDialog());
vm.Name = "";  vm.SaveCommand.Execute(null);
Assert.Equal("Hammadde adı boş olamaz.", vm.ErrorMessage);
```

O zaman şunlar **hızlı birim testi** olabilirdi: sıkı doğrulama kuralları, virgül/nokta sayı okuma, `_loading` bayrağının davranışı, `ShowAllGood` bildirimi (bölüm 6.2'deki hata **testle** yakalanırdı!), marj uyarısının **ne zaman** gösterildiği.

Bu, bir sonraki doğal iyileştirmedir ve bölüm 12'de **alıştırma** olarak bırakıldı.

> **Terim — Test edilebilirlik için tasarım (design for testability):** Test edilmesi zor kod, çoğu zaman **fazla bağımlı** koddur (statik, global, `new` ile yaratılmış). "Bunu nasıl test ederim?" sorusu tasarım sorusudur, test sorusu değil.

### 8.7 Yapmadıklarımız (ve neden önemli oldukları)

| Teknik | Ne olurdu? | Neden yapmadık |
|---|---|---|
| **Kod kapsamı (coverage)** ölçümü | `coverlet` ile hangi satır/dal test edilmedi görünürdü | Kurulmadı; "yüzde" yerine dosya dosya mantık yürüttük. Not: **kapsam ≠ kalite** (bir satırı çalıştırmak, onu *doğruladığın* anlamına gelmez). Satır (line) kapsamı ile **dal (branch)** kapsamı farklı şeylerdir. |
| **Mutasyon testi** | `Stryker.NET` kodu bilerek bozar (`<` → `<=`); testler kırılmazsa test zayıf demektir | Bu bir testi **test etme** tekniği; ileri seviye |
| **Özellik tabanlı test (property-based)** | FsCheck rastgele yüzlerce girdi üretip "her zaman şu özellik doğru mu?" der (ör. `SuggestPrice` marj modunda her zaman komisyon+kâr+maliyet = fiyat) | Elle seçilmiş örnekler yetti; ama bu formüller için çok uygun |
| **Görsel regresyon testi** | PDF/ekran görüntüsünü referansla piksel karşılaştırır | PDF'i elle PyMuPDF ile inceledik |
| **Otomatik UI testleri depoda** | Betikler `scratchpad`'te kaldı, repoya **konmadı** | Geçici araç gibi kullanıldı; kalıcı olması için temizlenmeli |
| **CI (sürekli entegrasyon)** | Her `git push`'ta testler otomatik koşardı (GitHub Actions) | Kurulmadı; testleri elle çalıştırdık |
| **Gerçek cihaz** | Gerçek ARM telefon, üretici klavyesi, düşük donanım | Emülatörle yetindik; bilinen sınır |
| **Performans / yük** | Çok sayıda hammadde ile hız | Ölçek küçük; yalnızca açılış süresi gözlendi |
| **Erişilebilirlik** (TalkBack) | Ekran okuyucu ile kullanım | Yapılmadı (UI otomasyonu aynı ağacı kullandığından kısmen dolaylı) |
| **Güvenlik** | Veri şifreleme, yedek | Yerel, kişisel veri riski düşük; yedek/şifreleme yok |

### 8.8 Kendi projelerin için test stratejisi kontrol listesi

1. **İş mantığını saf fonksiyonlara ayır.** En ucuz, en değerli testler orada. (Bu projenin en önemli kararı.)
2. **Beklenen değerleri koddan bağımsız üret** (spesifikasyon, elle hesap). Kodun çıktısını kopyalama.
3. **Sınırları test et:** `<` / `≤`, 0, -1, boş, çok büyük, Türkçe karakter, çok uzun metin.
4. **Hata bulunca önce test yaz** (kırmızı gör), sonra düzelt; hata mezar taşı olarak testte kalır.
5. **Kenarı (I/O) ince tut, çekirdeği kalın.** Veritabanı, ağ, dosya, arayüz: ince ve **seam'li**; iş kuralı saf.
6. **Kendi veri katmanını mock'lama**, gerçek (bellek içi) veritabanıyla test et.
7. **Test izolasyonu:** her test kendi durumunu kursun; paylaşılan global durumdan kaçın.
8. **Çalıştır ve gözle:** özellikle arayüz, platform, tema, dil, ekran boyutu. Otomatik test **sessiz hataları** yakalamaz.
9. **Gerçek veriyi temsil et:** Türkçe harf, küçük harf, 0, boş, negatif.
10. **Aracı da doğrula;** "test hata verdi" ile "ürün hatalı" aynı şey değildir.
11. **Her hatayı "neden yakalanmadı" diye sorgula** (bölüm 8.4 tablosu gibi bir kayıt tut): hangi testin eksikliği, hangi tasarım zayıflığı?
12. **Test yazmak için kodu esnet** (arayüz, parametre, `internal`+`InternalsVisibleTo`); test zorsa tasarım sorusudur.

---

## 9. Mühendislik terimleri sözlüğü

Rehber boyunca geçen kavramlar, **alfabetik** ve "projede nerede" bilgisiyle. (Sağdaki bölüm numarası, kavramın anlatıldığı yerdir.)

| Terim | Kısa tanım | Projede nerede |
|---|---|---|
| **ABI / mimari** | İşlemci "dili" (`arm64-v8a`, `x86_64`); yerel kod her biri için ayrı derlenir | APK'da iki mimari var (6.10) |
| **ACID** | İşlemlerin 4 garantisi: atomiklik, tutarlılık, izolasyon, kalıcılık | `RunInTransactionAsync` (4.4) |
| **ADR (karar kaydı)** | "Neden böyle?" notları | `docs/KARARLAR.md` (1.3) |
| **Anemik model** | Veri sınıfları + ayrı davranış sınıfları | Core modelleri + `CostCalculator` (3.2) |
| **AOT / JIT** | Önceden / çalışırken makine koduna çevirme | Release APK'da `libaot-*.so` (6.10) |
| **async/await** | Beklerken iş parçacığını bırakan asenkron model | Tüm depo ve komutlar (4.7) |
| **Atomiklik** | Bir işlem ya tamamen olur ya hiç | Reçete + kalemler kaydı (4.4) |
| **Backward compatibility** | Yeni sürüm eski veriyi okur | `SalePrice` sütunu eklenince (4.8) |
| **Boundary value analysis** | Sınır noktalarını test etme | `0,30` eşiği, `k+m=1` (3.5, 8.2) |
| **Bin packing / greedy** | Sıralı yerleştirme; açgözlü seçim | Sayfalama (5.2) |
| **Bağlama (data binding)** | Arayüzü veriye otomatik bağlama | `{Binding ...}` (6.1) |
| **Cache invalidation** | Önbellek/türetilmiş veriyi güncel tutma zorluğu | "Türet, saklama" (3.3) |
| **Captive dependency** | Singleton'ın transient'i hapsetmesi | DI ömürleri (6.3) |
| **Code coverage** | Testlerin çalıştırdığı satır/dal oranı | Ölçülmedi (8.7) |
| **Collation** | Metin karşılaştırma/sıralama kuralı | SQLite BINARY hatası (4.9) |
| **Command deseni** | Eylemi nesne yapma (`ICommand`) | `[RelayCommand]` (6.2) |
| **Composition root** | Nesne grafiğinin kurulduğu tek yer | `MauiProgram.cs` (6.3) |
| **Contract test** | İki parçanın beklentisini sınama | `ShippedPlatformsJson_IsValid` (8.2) |
| **Coupling / Cohesion** | Bağlaşım (az olsun) / bağdaşım (çok olsun) | Katmanlar (1.2) |
| **Data Mapper** | Domain ile tablo arası eşleyici | `Row` sınıfları (4.1) |
| **Defensive programming** | Dış dünyaya karşı hazırlıklı kod | PDF `catch (Exception)` (5.1) |
| **Denormalizasyon** | Türetilebilir veriyi saklamak | Bilerek yapmadık (3.3) |
| **DI / IoC** | Bağımlılıkları dışarıdan verme | Yapıcı enjeksiyonu (6.3) |
| **DIP** | Soyutlamaya bağımlı ol, somuta değil | `IRecipeRepository` (4.3) |
| **DRY** | Aynı bilgiyi tek yerde tut | `Money.Round`, `IsBelowThreshold` (3.1, 3.5) |
| **DTO** | Dış veriyi taşıyan basit sınıf | `PlatformDto` (3.6) |
| **E2E test** | Uygulamayı arayüzden uçtan uca sınama | UI otomasyonu, `adb` (8.3) |
| **Fail fast** | Hatayı giriş sınırında yakala | `PlatformCatalog` (3.6) |
| **Fake / Stub / Mock / Spy** | Test ikizleri | `Measure = len × 10` (5.4) |
| **Flaky test** | Bazen geçip bazen kalan test | Font testi riski (8.2) |
| **Floating point (IEEE 754)** | İkili kesirli sayı; para için uygunsuz | `decimal` kararı (3.1) |
| **Golden master / oracle** | Koddan bağımsız beklenen değer | `HESAP_MOTORU.md` altın örneği (8.2) |
| **Graceful degradation** | Hata olunca zarifçe devam etme | PDF hata mesajı (5.1) |
| **Identity map** | Aynı kayıt = aynı nesne örneği | `LoadIngredientsAsync` (4.5) |
| **Idempotans** | Tekrar çalıştırmak sonucu değiştirmez | `SeedIfEmptyAsync` (4.9) |
| **Immutability** | Nesne değiştirilemez; kopya üret | `record`, `WithExtraFee` (3.4, 6.8) |
| **Information hiding** | İç ayrıntıyı gizleme | `internal` Row sınıfları (4.1) |
| **Invariant** | Her zaman doğru olması gereken kural | `UnitPrice` miktar > 0 (3.2) |
| **KISS / YAGNI** | Basit tut / ihtiyaç olmadan yapma | Backend yok, birim dönüşümü yok |
| **Lazy initialization** | Gerektiğinde, bir kez başlat | `StartupService` (6.5) |
| **Lifecycle** | Oluşma-görünme-yok olma aşamaları | `OnAppearing` (6.4) |
| **Local-first** | Veri cihazda, internet şart değil | Genel mimari (1.1) |
| **Magic number** | Gömülü anlamsız sabit | `150m` → `AppSettings` (6.6) |
| **Memoization** | Sonucu/Task'ı saklayıp yeniden kullanma | `_initialization ??=` (6.5) |
| **Migration** | Şema/veri sürüm geçişi | `SalePrice` (4.8) |
| **Mutation testing** | Kodu bozup testlerin yakalamasına bakma | Yapılmadı (8.7) |
| **MVVM** | Model–View–ViewModel deseni | App (6.1) |
| **N+1 problemi** | Döngüde sorgu atma | Üç sorguyla çözdük (4.5) |
| **Native / managed** | Yerel (C/C++) / yönetilen (.NET) kod | QuestPDF sorunu (5.1) |
| **Observer** | Değişiklikte abonelere haber | `PropertyChanged` (6.2) |
| **ORM** | Nesne–tablo eşleme | `sqlite-net` (4.1) |
| **P/Invoke** | Yönetilen koddan yerel fonksiyon çağırma | QuestPDF yerel kitaplığı (5.1) |
| **Persistence ignorance** | Domain "nasıl saklanır"ı bilmez | Core (4.1) |
| **Property-based test** | Rastgele girdiyle "özellik" doğrulama | Yapılmadı (8.7) |
| **Pure function** | Aynı girdi → aynı çıktı, yan etkisiz | Hesaplayıcılar (3.2) |
| **Reentrancy guard** | Kendi tetiklediği olaya karşı bayrak | `_loading` (6.8) |
| **Referential integrity** | Gösterilen kayıt var olmalı | Silme koruması (4.6) |
| **Regression test** | Düzeltilen hatanın geri gelmesini önleyen test | `Golden_LaborCost` (3.3) |
| **Repository** | Veri erişimini koleksiyon gibi gösterme | Depolar (4.3) |
| **RID** | Platform+mimari kimliği (`android-arm64`) | NuGet yerel dosya seçimi (5.1) |
| **Sandbox** | Uygulamanın yalıtılmış alanı | Android dosya paylaşımı (5.5) |
| **Seam (dikiş)** | Testte davranışı değiştirebildiğin nokta | `Shell.Current` yok (8.6) |
| **SoC** | İlgilerin ayrılması | Katmanlar (1.2) |
| **Shift-left** | Hatayı erken yakalamak ucuz | Hata kataloğu (8.4) |
| **SOLID** | Beş tasarım ilkesi (aşağıda) | Genel |
| **Source generator** | Derlemede kod üreten eklenti | `[ObservableProperty]` (6.2) |
| **SSOT** | Tek doğruluk kaynağı | `HESAP_MOTORU.md`, "türet, saklama" |
| **State machine** | Durumlar ve geçişler | `SeedApplied` (4.9) |
| **Strategy** | Davranışı dışarıdan verme | `measure` fonksiyonu (5.4) |
| **Technical debt** | Bilerek/bilmeyerek biriken iyileştirme borcu | Bölüm 11 |
| **Test double** | Gerçek bağımlılığın yerine geçen yapay nesne | Bölüm 5.4, 8.2 |
| **Test pyramid** | Çok birim, az E2E | 8.1 |
| **TDD** | Kırmızı → yeşil → düzenle | 8.2 |
| **TOCTOU** | Kontrol ile kullanım arası yarış | Silme koruması (4.6) |
| **Tofu / glif** | Yazı tipinde olmayan harfin boş kutusu | `TextStyle` (5.3) |
| **Transaction** | Birden çok işlemi tek bütün yapma | `SaveAsync` (4.4) |
| **Trimleme** | Release'de kullanılmayan kodu silme | Release testi (6.10) |
| **Unicode kod noktası** | Karakterin evrensel numarası | `ConvertToUtf32` (5.3) |
| **UI thread** | Arayüzü değiştirebilen tek iş parçacığı | `await` sonrası (4.7) |
| **Upsert** | Varsa güncelle, yoksa ekle | `SaveAsync` (4.3) |
| **Virtualization** | Yalnızca görünen satırları üretme | `CollectionView` (6.9) |

### SOLID, projeden örneklerle

| Harf | İlke | Projedeki karşılığı |
|---|---|---|
| **S**: Single Responsibility | Bir sınıfın değişmesi için **tek** neden olsun | `CostCalculator` yalnız maliyet; `Display` yalnız biçim; `PriceListLayout` yalnız yerleşim |
| **O**: Open/Closed | Genişlemeye **açık**, değişikliğe **kapalı** | Yeni pazaryeri = `platforms.json`'a satır eklemek; **koda dokunmadan** |
| **L**: Liskov Substitution | Alt tür, üst türün yerine geçebilmeli | Gerçek depo ve (test için) sahte depo aynı arayüzü sağlar |
| **I**: Interface Segregation | Küçük, odaklı arayüzler | `IIngredientRepository` ve `IRecipeRepository` ayrı, tek dev `IRepository` yok |
| **D**: Dependency Inversion | Soyutlamaya bağımlı ol | ViewModel → `IRecipeRepository` |

> **Not:** SOLID bir dogma değil, **gerekçeli rehberlerdir.** Küçük projede hepsini zorlamak aşırı mühendislik (over-engineering) olur; biz yalnızca **fayda sağladığı** yerlerde uyguladık (ör. depo arayüzleri var, ama her şeyin arayüzü yok).

---

## 10. Bir bilgisayar mühendisliği öğrencisi için dersler

Bu bölüm kodun dışına çıkıyor: bu projeden **ileride her projede** işine yarayacağını düşündüğüm şeyler.

### 10.1 Süreç dersleri

- **Önce düşün, sonra kodla.** Formülleri `HESAP_MOTORU.md`'ye yazdık, altın örneği elle hesapladık, **sonra** kodladık. Spesifikasyon yazarken bulunan bir tutarsızlık, kodlarken bulunandan **çok daha ucuzdur.**
- **Küçük adımlar, sık kayıt.** 21 commit, her biri **çalışan** bir kilometre taşı; mesajlar "neden"i anlatıyor. Bir şey bozulunca "son çalışan hale" dönmek kolaydır. (Not: Git'in "LF will be replaced by CRLF" uyarısı, Windows ile Unix satır sonu farkıdır; zararsızdır, `.gitattributes`/`core.autocrlf` ile yönetilir.)
- **Karar günlüğü tut.** `KARARLAR.md`: tarih + karar + **neden**. "Neden decimal? Neden Row sınıfları? Neden QuestPDF'ten vazgeçtik?" cevapları 6 ay sonra sende de olmaz.
- **Kapsamı koru.** MVP'de "yok" listesi (stok, sipariş, çoklu para birimi, hesap/senkron/abonelik) **bilinçli** bir karardır. Her "güzel olur" özellik teslim süresini ve hata yüzeyini büyütür (YAGNI).
- **Her faz bittiğinde çalışır durumda.** Testler yeşil, uygulama açılıyor, dokümanlar güncel. "Yarım ama her şey var" yerine "tam ama küçük".

### 10.2 Hata ayıklama zihniyeti

1. **Hipotez → kanıt → doğrulama.** "Sanırım SharedCache bayrağı" dedim, değiştirdim, **düzelmedi.** İkinci teşhis kanıta (kayıt, deney) dayandı. İlk akla gelen açıklama çoğu zaman yanlıştır; düzelttiğini **gözlemeden** "çözüldü" deme.
2. **Hata mesajını oku.** QuestPDF'in mesajı sorunu **kelimesi kelimesine** söylüyordu ("cannot run on ... Android").
3. **Günlük (log) yaz/oku.** `adb logcat`, çökmenin **neden**ini söyler. Çalışma zamanı hatası için en değerli kaynak.
4. **Ölçümü şüphe et.** DPI hatası: "etiket yok" sanıldı; ölçüm aracı kesiyordu. Önce **aracı**, sonra kodu sorgula.
5. **Küçült.** Karmaşık hatayı en küçük tekrarlanabilir örneğe indir (tek test, tek ekran).
6. **"Neden daha önce yakalanmadı?"** Her hata sonrası bu soru, test ve tasarım eksiğini gösterir (bölüm 8.4).

### 10.3 Doğruluk, kesinlik ve dürüstlük

Bu projede sürekli ayırdığımız iki cümle var:

- **"Derleniyor / testler geçiyor"** ≠ **"doğru çalışıyor."** (Para hesabında doğru formül, ama ekranda eski değer görünüyorsa kullanıcı için yanlıştır.)
- **"Doğruladım"** ≠ **"gerçek koşulda doğruladım."** Emülatör gerçek telefon değildir; Windows'ta "Paylaş" denenmedi; ARM'ı çalıştırmadık. **Bildiğini ve bilmediğini ayırmak** mühendisliğin çekirdeğidir. Raporunda "doğrulayamadıklarım" listesi bulunmalı.

Bir mühendisin değeri, "hatasız yazdım" demesinde değil, **hatayı nerede arayacağını ve neyi garanti etmediğini bilmesindedir.**

### 10.4 Sistem düşüncesi

- **Çekirdeği saf, kenarları ince tut.** İş kuralları saf fonksiyonlar (kolay test); I/O (veritabanı, dosya, arayüz) kenarda, ince. Bu tek ilke, test edilebilirliği, taşınabilirliği ve anlaşılırlığı birlikte getirdi.
- **Her soyutlamanın bedeli var.** QuestPDF'in yerleşim motoru bize zaman kazandırdı; **ama** platform kısıtını gizledi. SkiaSharp'a geçince kontrol kazandık, kod yazdık. "Hazır kütüphane her zaman kazançtır" değildir; **kısıtlarını** (platform, lisans, bakım) öğren.
- **Varsayımların üstüne inşa etme; doğrula.** "decimal veritabanında da decimal kalır" varsayımı yanlıştı (4.2); "SQL sıralaması Türkçe bilir" varsayımı yanlıştı (4.9).
- **Çapraz platform gerçekleri:** yerel bağımlılıklar, mimari (ABI), sandbox, yaşam döngüsü, ekran çeşitliliği: masaüstünde görünmeyen hata sınıfları. **Hedef platformda çalıştırmadan "çalışıyor" deme.**

### 10.5 Veri ve sayı bilgisi (her mühendisin bilmesi gerekenler)

- **Kayan nokta** ve **decimal** farkı; **yuvarlama modları**; işlem sırasının sonucu değiştirmesi.
- **Unicode:** karakter ≠ bayt ≠ `char`; vekil çiftleri; kültür; "Türk I" problemi; sıralama bir **dil** kuralıdır.
- **Zaman ve yerel ayar:** (bu projede saat kullanılmadı ama) saat dilimi, yaz saati, takvim: bir sonraki büyük hata kaynağı.
- **Para:** hesap, saklama, gösterim ayrı katmanlardır ve **her birinde** farklı bir yanlış yapılabilir (bölüm 3.1, 4.2, 6.7).

### 10.6 Ürün ve mühendislik dengesi

Bu projenin amacı "öğrenme + sunum". `KARARLAR.md`'de **açık riskler** da yazılı: (1) **retention**: fiyatı bir kez hesaplayan kullanıcı uygulamayı bırakır mı? (2) **ödeme isteği**: hedef kitle ödeme yapar mı? Bunlar **mühendislikle** çözülmez; kullanıcılarla konuşarak (Excel prototipi, landing page) doğrulanır.

> **Ders:** "Doğru yapıyor muyuz?" (mühendislik) ile "doğru şeyi mi yapıyoruz?" (ürün) farklı sorulardır. İkincisini cevaplamadan birincisine aşırı yatırım yapmak, klasik bir girişim hatasıdır.

### 10.7 Lisans ve bağımlılık yönetimi

- **QuestPDF** Community lisansı (gelir sınırı) ile geldi; **SkiaSharp (MIT)** o kısıtı kaldırdı. Bir kütüphaneyi seçerken **lisansı** da oku (ticari kullanım, gelir sınırı, kaynak açma zorunluluğu).
- **Sürüm uyumu:** `CommunityToolkit.Maui` 15.x .NET 10 ister; biz .NET 9'dayız → 12.3.0'a **sabitledik** ve nedenini yazdık. Bağımlılık yükseltmek bir **karar**dır, otomatik değil.
- **Uyarıları ciddiye al:** `ContainsGlyph` kullanımdan kalkma uyarısı, ileride kırılacak bir çağrıyı **bugünden** gösterdi.
- **Yerel (native) bağımlılık = platform riski:** her yeni kütüphane için "hangi platformlarda çalışır?" cevabını ara.

### 10.8 Araç okuryazarlığı

Bu projede kullandığımız ve bir mühendisin tanıması gereken araçlar: **`dotnet` CLI** (build/test/run/package), **MSBuild/NuGet** (`.csproj`, hedef çerçeve, paket sürümü), **Git** (commit, push, `.gitignore`), **`adb`** (Android cihaz/emülatör), **UI Automation / erişilebilirlik ağacı**, **PowerShell/Bash/Python ile otomasyon**, **PyMuPDF** (PDF inceleme), **hata günlüğü okuma**. "Araç bilmek" derken kısayol değil, **ne yaptığını anlamak** kastediliyor.

### 10.9 Bu projeyi anlatma rehberi (mülakat / portföy)

**Üç cümlelik özet:** "Mum atölyeleri için, maliyet-fire-pazaryeri komisyonunu hesaplayıp satış fiyatı öneren **local-first** bir .NET MAUI uygulaması yaptım. İş mantığı saf, test edilebilir bir çekirdekte (118 otomatik test), uygulama Windows ve Android'de çalışıyor. Hammadde fiyatı değişince marjı düşen ürünleri uyarıyor ve müşteriye PDF fiyat listesi üretiyor."

**Sorulabilecek sorular ve ipucu cevaplar:**

| Soru | Cevap iskeleti |
|---|---|
| Neden para için `decimal`? | İkili kayan nokta ondalık kesirleri tam tutamaz (`0,1+0,2`); `decimal` onluk tabanda; yuvarlamayı tek yerde (`AwayFromZero`) ve **son adımda** yaptık. |
| Core'u neden bağımsız tuttun? | Bağımlılık kuralı: iş kuralları arayüz/veritabanından habersiz; test edilebilir, taşınabilir. |
| `Row` sınıfları neden ayrı? | Persistence ignorance; Core'da SQLite nitelikleri olmasın; `decimal`'in `double`'a çevrilme tuzağını Row'da çözdük. |
| Singleton ve Transient farkı? | Singleton: durum/bağlantı paylaşan servisler (DB, depo, ayarlar); Transient: her gezinmede temiz durum isteyen ViewModel/sayfa. |
| Bir hata hikayesi anlat. | QuestPDF/Android: yerel kitaplık, emülatörde bulundu, SkiaSharp'a geçtik; ayrıca `catch` çok dardı. |
| Test stratejin? | Test piramidi: 89 birim, 29 entegrasyon (gerçek SQLite); arayüz elle/otomasyonla; **neyin test edilmediğini** biliyorum (ViewModel), nedeni (statik MAUI API'leri) ve çözümü (seam'ler). |
| Ölçeklenseydi ne değişirdi? | N+1/O(R×N) sorguları, sayfalama, backend/senkron, `PRAGMA user_version` göçleri, CI/kapsam, yedekleme. |
| En çok neyi öğrendin? | "Çalışıyor"u "doğrulandı"dan ayırmak; ölçümü şüphe etmek; test verisinin gerçeği temsil etmesi. |

---

## 11. Dürüst liste: teknik borç ve bilinen sınırlar

> **Terim — Teknik borç (technical debt, Ward Cunningham):** Bugün hızlı ilerlemek için bilerek (ya da bilmeden) bıraktığımız iyileştirme işleri. Finansal borç gibi: kullandıkça **faiz** öder (sonraki değişiklikler zorlaşır). Mühendislik, borcu **görünür** tutup bilinçli yönetmektir; sıfır borç hedefi gerçekçi değildir.

Aşağıdaki liste, bu projede **bildiğimiz** eksikleri, **önem** sırasıyla verir. Bunları saklamamak bilinçli bir tercihtir (bkz. 10.3).

| # | Konu | Neden önemli | Öncelik |
|---|---|---|---|
| 1 | **ViewModel'ler otomatik test edilemiyor** (statik MAUI API'leri) | Doğrulama, sayı okuma, marj uyarısı akışı yalnızca elle/otomasyonla doğrulandı; ShowAllGood gibi hatalar testle yakalanabilirdi | **Yüksek** |
| 2 | **`Display` (sayı okuma/biçimleme) birim testsiz** | `"1.000"` yazan kullanıcı **1** olarak okunur (binlik ayracı desteklenmiyor); sessiz yanlış veri girişi riski | **Yüksek** |
| 3 | **Yedekleme/dışa aktarma yok** | Veri tek cihazda; cihaz kaybı = tüm reçeteler gider | **Yüksek** (ürün için) |
| 4 | Otomatik UI/duman testleri **depoda yok** | Betikler geçici klasörde kaldı; her değişiklikte yeniden çalıştırılmıyor | Orta |
| 5 | **CI yok** | Testler yalnızca elle çalışıyor; bir "push" bozuk olsa kimse otomatik fark etmez | Orta |
| 6 | Yükleme akışlarında (`OnAppearing → LoadCommand`) **genel hata yönetimi yok** | Veritabanı bozulursa kullanıcı anlamsız bir çökme görür (PDF ve platform dosyası ele alındı, yükleme değil) | Orta |
| 7 | Önizleme modelinin kurulması `RecipeEditViewModel`'de **iki yerde** (önizleme ve kayıt) | `ParseOrZero` ile `TryParseDecimal` iki yol; ileride ayrışabilir | Orta |
| 8 | Release APK **debug anahtarıyla imzalı** | Mağazaya yüklemek için kendi imzalama anahtarı ve paket biçimi (AAB) gerekir | Orta (yayın için) |
| 9 | Gerçek (ARM) telefonda, Windows'ta "Paylaş" **denenmedi** | Emülatör ≠ cihaz | Orta |
| 10 | Veritabanı **şema sürümü yok** (`PRAGMA user_version`) | Şu an yalnızca "sütun ekleme" göçü çalışır; yeniden adlandırma/silme için altyapı gerekir | Orta |
| 11 | `Font_*` testi **makineye bağlı** | Boş bir CI imajında kırılabilir (flaky riski) | Düşük |
| 12 | `StartupService` **iş parçacığı güvenli değil** (`??=`) | Çok iş parçacıklı çağrıda çift başlatma; bugün tek iş parçacığı | Düşük |
| 13 | `RecipeRepository.GetAllAsync` **O(R×N)** ve her çağrıda tüm hammaddeler yüklenir; marj kontrolü iki kez yükler | Yüzlerce reçetede fark edilir; bugün ihmal edilebilir | Düşük |
| 14 | `AppSettings` statik `Preferences`'a bağlı | Test edilemez (madde 1 ile aynı kök) | Düşük |
| 15 | Yalnızca **Türkçe** (`tr-TR` sabit), metinler kodda; para birimi ₺ sabit, **KDV yok**, fiyat **geçmişi yok** | Ürün kararları (karar günlüğünde); çok dilliye geçmek `.resx` ister | Ürün kararı |
| 16 | **Eşzamanlılık:** iki cihaz/pencere aynı veriyi değiştirirse kontrol yok | Tek kullanıcılı yerel uygulama varsayımı | Düşük |
| 17 | Ölçülmemiş: **kod kapsamı**, performans, erişilebilirlik (TalkBack), güvenlik (şifreleme) | Bölüm 8.7 | Değişken |

**Şu an en yüksek getirili üç iş:** (1) `Display`'i test edilebilir yapıp testlerini yazmak ve **binlik ayracı** davranışına karar vermek, (2) ViewModel'ler için seam'leri (`INavigationService`, `IDialogService`, `ISettings`) çıkarıp testlemek, (3) veri **dışa/içe aktarma** (JSON yedek).

---

## 12. Alıştırmalar

Aşağıdakiler gerçek, bu kod tabanında yapılabilecek işlerdir; her biri yukarıdaki bir kavrama bağlı. Kolaydan zora doğru.

| # | Alıştırma | Yapılacak | Öğreneceğin kavram | İpucu |
|---|---|---|---|---|
| 1 | **Pazaryeri ekle** | `platforms.json`'a "Amazon" ekle (kodu değiştirmeden). | Veri güdümlü tasarım, açık/kapalı ilkesi | `ShippedPlatformsJson_IsValid` testi hâlâ geçmeli. Geçersiz bir oran yazıp testin kırıldığını gör |
| 2 | **Sayı okuma testleri** | `Display.TryParseDecimal` için testler yaz: `"2,5"`, `"2.5"`, `""`, `"abc"`, `"1.000"`. `"1.000"` ne olmalı? | Sınır değerler, belirsizlik, **davranışı karara bağlamak** | Önce karar ver (hata mı? binlik mi? tek nokta ondalık mı?), sonra testi yaz (TDD). Karar günlüğüne ekle |
| 3 | **Hata avı** | Reçete adına `"Çam"`, `"çam"`, `"Çiçek"` ekle; Türkçe sıralamayı uygulamada gör. Sonra **sıralamayı** bozmak için `TextOrder.Turkish`'i `StringComparer.Ordinal` yap ve hangi testlerin kırıldığına bak. | Mutasyon testi mantığı, test gücü | Kırılan testler = o davranışı koruyanlar. Kırılmayan olsaydı, test boşluğu demekti |
| 4 | **KDV desteği** | Satış fiyatı KDV dahil/hariç; Core'da `VatCalculator` + testler; ayar; PDF notunu ("KDV hariçtir") duruma göre değiştir. | Spesifikasyon → test → kod → arayüz akışı | Önce `HESAP_MOTORU.md`'ye formülü ve altın örneği yaz |
| 5 | **ViewModel'i test edilebilir yap** | `INavigationService`, `IDialogService`, `ISettings` çıkar; `IngredientEditViewModel`'i **MAUI'siz** bir kütüphaneye taşı; sahte (fake) bağımlılıklarla "boş ad → hata mesajı" testi yaz. | **Seam**, DI, test double, bağımlılık yönü | Bölüm 8.6'daki kod iskeleti. Sonra `ShowAllGood` hatasını testle yakalamayı dene |
| 6 | **N+1 / Büyük-O** | `RecipeRepository.GetAllAsync`'te kalemleri `RecipeId`'ye göre bir kez grupla (`ToLookup`). **Önce** 5.000 kalemlik bir performans testi yaz ve ölç. | Karmaşıklık analizi, **ölçmeden optimize etme** | Optimizasyondan önce ve sonra ölç; fark yoksa değişikliği geri al |
| 7 | **Özellik tabanlı test** | `PriceListLayout.Wrap` için FsCheck: "sarılmış satırların birleşimi (boşluklar hariç) orijinal metnin harflerini taşır ve hiçbir satır `maxWidth`'i aşmaz." | Property-based testing | Rastgele metinler boş, uzun, emojili olabilir (vekil çifti!) |
| 8 | **Fiyat geçmişi** | Hammadde fiyat değişimini tarihli bir tabloya yaz (yeni `PriceHistoryRow`); Hammadde sayfasında son değişimleri göster. | Veri modelleme, **şema göçü** (yeni tablo) | `CreateTableAsync` yeni tabloyu ekler; eski veritabanıyla test et |
| 9 | **CI + kapsam** | GitHub Actions ile her `push`'ta `dotnet test`; `coverlet` ile kapsam raporu. | CI/CD, kapsam yorumlama | Kapsamın **yüzdesine** değil, **kapsanmayan dallara** bak |
| 10 | **Yedekleme (dışa/içe aktarma)** | Tüm hammadde+reçeteleri JSON'a yaz/oku. Okurken `PlatformCatalog` gibi **doğrula**; içe aktarmayı **tek transaction**'da yap. | Serileştirme, doğrulama, atomiklik, sürüm alanı | Bozuk yedek dosyası yarım kayıt bırakmamalı |
| 11 | **Mutasyon testi** | `Stryker.NET` ile `MarginAlerts` testlerinin gücünü ölç; hayatta kalan mutantlar için test yaz. | Test kalitesinin ölçümü | Hayatta kalan mutant = testin görmediği davranış |
| 12 | **Şema sürümü** | `PRAGMA user_version` ile şema sürümü tut; bir "göç" (migration) listesi yaz ("v1→v2: SalePrice ekle"). | Migration altyapısı | Eski veritabanı dosyalarıyla her sürümden her sürüme test |
| 13 | **Çok dil** | Metinleri `.resx`'e taşı, `tr` ve `en` yap; `Display`'in sabit `tr-TR` kültürünü kullanıcı seçimine bağla. | i18n/l10n | Sayı biçimi ve metin ayrı kavramlardır |

---

## 13. Ek: komutlar, kaynaklar

### 13.1 Sık kullanılan komutlar

```bash
# Testler (tüm çözüm; ≈ yarım saniye)
dotnet test

# Tek bir test sınıfı / adı
dotnet test --filter "FullyQualifiedName~MarginAlertsTests"

# Uygulamayı Windows'ta çalıştır
dotnet run --project AtolyeRecete.App -f net9.0-windows10.0.19041.0

# Android: önce emülatörü aç (ya da telefonu USB hata ayıklamayla bağla), sonra:
dotnet build AtolyeRecete.App -f net9.0-android -t:Run

# Release APK (telefona kurulabilir; debug anahtarıyla imzalı, yalnızca test için)
dotnet build AtolyeRecete.App -c Release -f net9.0-android -p:AndroidPackageFormat=apk
#   → AtolyeRecete.App/bin/Release/net9.0-android/com.atolyerecete.app-Signed.apk
adb install -r <apk>

# Android çökme günlüğü
adb logcat -d | grep -E "FATAL|AndroidRuntime"

# Android açılış süresi
adb shell am start -W -n com.atolyerecete.app/<MainActivity>
```

### 13.2 "Şu belirtiyi görünce nereye bakılır?"

| Belirti | İlk bakılacak yer |
|---|---|
| Maliyet / fiyat **sayısı yanlış** | `Core/CostCalculator.cs`, `PricingCalculator.cs` + ilgili testler; `HESAP_MOTORU.md`'deki altın örnekle karşılaştır |
| Sayı **ekranda eski** (güncellenmiyor) | ViewModel'de `[NotifyPropertyChangedFor]` eksik mi? (6.2); `OnAppearing`'de yeniden yükleme var mı? |
| Liste **sıralaması** garip | `Core/TextOrder.cs`; sıralama SQL'de mi bellekte mi? (4.9) |
| Veritabanında **sayı bozulmuş** / yuvarlanmış | `Data/Rows/DecimalText.cs`; `decimal` hâlâ metin mi? (4.2) |
| PDF'te **boş kutu (□)** | `Pdf/TextStyle.cs` yazı tipi yedeği; `Font_*` testi (5.3) |
| PDF'te **kalın/düz** yazı sorunu | `TextStyle` yazı tipi seçimi, `IsBold` (5.3) |
| Android'de **çökme** | `adb logcat -d | grep FATAL`; yerel kitaplık mı? (5.1); Release'de trimleme mi? (6.10) |
| "2,5" yazınca **25** | Cihaz/uygulama dili; Android sayı klavyesi yerel ayarı (8.3) |
| Sekme adı **kırpılıyor** / ekran taşıyor | Dar ekran (360 dp) testi; etiket uzunluğu (8.4) |
| Test **sırayla geçiyor, paralelde kırılıyor** | Paylaşılan durum (4.7); `CreateInMemory()` kullanılıyor mu? |
| Hammadde **silinemiyor** | `IngredientRepository.DeleteAsync` kullanım kontrolü (4.6) |
| Örnek veri **geri geliyor / gelmiyor** | `AppSettings.SeedApplied`, `StartupService` (4.9) |

### 13.2b Proje dokümanları

`CLAUDE.md` (kurallar, komutlar), `docs/KARARLAR.md` (kararlar ve **nedenleri**), `docs/HESAP_MOTORU.md` (formüller, altın örnek), `docs/YOL_HARITASI.md` (fazlar), `docs/SUNUM_AKISI.md` (demo betiği).

### 13.3 Okuma önerileri

(Hepsi bilinen, yerleşik kaynaklar.)

| Konu | Kaynak |
|---|---|
| Mimari, bağımlılık kuralı, SOLID | Robert C. Martin, *Clean Architecture* |
| Alan modeli, ortak dil | Eric Evans, *Domain-Driven Design* |
| Repository, Data Mapper, Unit of Work, anemik model | Martin Fowler, *Patterns of Enterprise Application Architecture*; fowler'ın "AnemicDomainModel" yazısı |
| TDD | Kent Beck, *Test-Driven Development: By Example* |
| Test desenleri, test double'lar | Gerard Meszaros, *xUnit Test Patterns*; Martin Fowler, "Mocks Aren't Stubs" |
| Test piramidi | Martin Fowler'ın sitesindeki "The Practical Test Pyramid" (Ham Vocke) |
| Eski/zor test edilen kodu kurtarma, "seam" | Michael Feathers, *Working Effectively with Legacy Code* |
| Kayan nokta sayılar | David Goldberg, *"What Every Computer Scientist Should Know About Floating-Point Arithmetic"* |
| Unicode | Joel Spolsky, "The Absolute Minimum Every Software Developer Absolutely, Positively Must Know About Unicode and Character Sets" |
| Veri sistemleri, işlemler, tutarlılık | Martin Kleppmann, *Designing Data-Intensive Applications* |
| Pragmatik mühendislik alışkanlıkları | Hunt & Thomas, *The Pragmatic Programmer* |
| .NET MAUI, MVVM, Shell | Microsoft Learn: ".NET MAUI" ve "MVVM Community Toolkit" belgeleri |

---

> **Son söz.** Bu projeden en değerli çıktı, çalışan bir uygulamadan çok **çalışma biçimi**: önce spesifikasyon, küçük adımlar, kırmızı-yeşil, hataları kataloglamak, bilmediğini söylemek ve ölçümü şüphe etmek. Uygulama büyüyüp değişir; bu alışkanlıklar her projede seninle gelir.
