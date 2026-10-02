# Kararlar

Format: tarih, karar, neden. Yeni karar en üste eklenir.

## 2026-10-02 Faz 1 öncesi netleştirmeler

**Fire sadece kalem bazında.** Genel gider payı fireyi içermez (elektrik, ambalaj vb. için).
Neden: aynı kayıp iki kez sayılmasın. Sorun çıkarsa yeniden değerlendirilir.

**Demo hedefi: Windows (maui-windows).** Android sonra, ilerleyen aşamada eklenecek.
Neden: makinede hazır, emülatör kurulumu gerekmez.

**Hedef çatı: net9.0** (makinede SDK 9.0.318). CLAUDE.md komutları buna göre güncellendi.

**Hammadde tek birimde tutulur (g, ml, adet); birim dönüşümü MVP'de yok.**

**Seed "tek tuşla silme" = tüm veriyi silme (ClearAllAsync).** Sadece örnekleri silmek (IsSeed bayrağı) ertelendi.
Neden: kullanıcı örnek hammaddeyi düzenleyince/kendi reçetesinde kullanınca bayrak ve silme koruması çakışır; demo için fazla karmaşık.

**Data katmanı: tablo sınıfları (Row) Data'da internal, Core modelleri saf kalır; decimal alanlar metin saklanır.**
Neden: sqlite-net decimal'i double yazar, para kuralını bozar. Testlerde kayıpsız gidiş-dönüş doğrulanır.

**Testler Data katmanını da kapsar (bellek içi SQLite).** Not: SQLiteAsyncConnection aynı bağlantı dizesi için ortak havuz kullanır; testlerde her veritabanı benzersiz adlı URI ile açılır (AtolyeDatabase.CreateInMemory).

**CommunityToolkit.Maui 12.3.0'da sabit.** Güncel 15.x .NET 10 gerektiriyor; biz net9.0'dayız.
Neden: NU1202 uyumsuzluk hatası. .NET 10'a geçilirse birlikte yükseltilir.

**MAUI hedefleri: net9.0-windows10.0.19041.0 ve net9.0-android.** iOS/Mac yok (Mac gerektirir, kapsam dışı).
Neden: demo Windows'ta, ürün mobil. Android emülatör ve derleme makinede hazır; emülatör `adb` ile sürülüp doğrulanabiliyor.

**platforms.json: `{ "version": 1, "platforms": [...] }`, MAUI asset olarak uygulamayla gelir (Resources/Raw).** Okuma ve doğrulama Core'da (`PlatformCatalog`), dosyaya erişim App'te (`PlatformService`).
Neden: oranlar koda gömülmez, ileride uzaktan çekilebilir (sürüm alanı bunun için). Eksik veya geçersiz alan sessizce 0 sayılmaz, `FormatException` verir.

**Fiyat dökümü (`PriceBreakdown`) yuvarlanmış önerilen fiyat üzerinden hesaplanır.** Komisyon, net kâr ve gerçekleşen marj/markup yuvarlanmaz, gösterimde yuvarlanır.
Neden: satıcının gerçekten alacağı fiyat kuruşa yuvarlanmış fiyattır; hedef orandan birkaç kuruş sapma doğaldır.

**Simülatörde "ek sabit ücret" (kargo vb.) platformun sabit ücretine eklenir.** Simülatör seçili platformun dökümünü ve aynı reçetenin tüm platformlardaki fiyatını gösterir.

**App'te `Platform` adı MAUI'nin `Microsoft.Maui.ApplicationModel.Platform` sınıfıyla çakışır; ilgili dosyalarda `using Platform = AtolyeRecete.Core.Models.Platform;` takma adı kullanılır.** Modeli yeniden adlandırmak Core, testler ve dokümanlara dokunurdu.

**Ayarlar MAUI Preferences'ta: saatlik işçilik (varsayılan 150 TL), yeni reçete için genel gider % (varsayılan 10), örnek veri bayrağı.** Hesaplayan tüm ekranlar ücreti `AppSettings`'ten okur; sabit değer kalmadı.
Neden: ücret değişince tüm reçeteler, simülatör ve (Faz 4) PDF/uyarılar aynı değeri kullansın. Decimal ayarlar metin saklanır (Preferences decimal'i double'a çevirirdi).

**Örnek veri yalnızca ilk çalıştırmada otomatik yüklenir (`SeedApplied` bayrağı).** Kullanıcı "Tüm verileri sil" derse bir sonraki açılışta geri gelmez; Ayarlar'dan "Örnek verileri yükle" ile (yalnızca veritabanı boşken) yeniden yüklenebilir.
Neden: önceki davranışta (boşsa her açılışta yükle) silme işlemi bir sonraki açılışta geri alınıyordu.

**Genel gider % ayarı yalnızca yeni reçetelerin başlangıç değeridir; mevcut reçeteleri değiştirmez.** Her reçete kendi oranını saklar.

**Reçeteye isteğe bağlı `SalePrice` (satış fiyatı, KDV hariç) eklendi; reçete başına tek fiyat, platform bazlı değil.**
Neden: "marj düştü" uyarısı ve müşteri fiyat listesi PDF'i (Faz 4) için "ürünü şu an kaça satıyorum" bilgisi gerekiyor. Simülatör sadece öneri veriyordu, kaydetmiyordu. Sütun nullable; eski veritabanlarında sqlite-net otomatik ekler (gerçek eski veritabanında doğrulandı).

**Marj tanımı (satış fiyatı için): (fiyat - birim maliyet) / fiyat, komisyon öncesi.** Platform bazlı marjı simülatör gösterir.
**Marj uyarı eşiği Ayarlar'da tek bir genel değer (varsayılan %30); reçete bazlı eşik yok.** Eşiğin altında reçete düzenlemede uyarı, listede "⚠ düşük" görünür.

**Marj düşüş uyarısı, hammadde kaydedilirken çalışır (`MarginAlerts.FindDrops`, Core).** Kayıttan önce ve sonra tüm reçeteler yüklenir, karşılaştırılır. Reçete uyarı listesine girer ancak ve ancak: satış fiyatı varsa, marj kötüleştiyse ve yeni marj eşiğin kesinlikle altındaysa.
Neden: sadece "eşiğin altında" demek, zaten düşük ve hiç değişmemiş reçeteler için her kayıtta gürültü üretirdi; sadece "düştü" demek de küçük, zararsız oynamalar için uyarırdı. Yeni hammadde eklemek uyarı üretmez (hiçbir reçetede kullanılamaz). Hammadde silme zaten reçetede kullanılıyorsa engelli.

**PDF üretimi ayrı proje: `AtolyeRecete.Pdf` (SkiaSharp, yalnızca Core'a bağlı).** Hangi reçetelerin listeye gireceği ve sıralama Core'da (`PriceListBuilder`, testli); sayfalama ve satır sarma `PriceListLayout`'ta (çizimden bağımsız, ölçüm fonksiyonuyla testli); çizim `PriceListPdf`/`TextStyle`'da.
Neden: Core saf kalsın, MAUI'siz test edilebilsin. Yazı tipi kapsamı (ç ğ ı İ ö ş ü ve ₺) ve kalın yazı testle doğrulanır; bir harf sistem yazı tipinde yoksa başka bir yazı tipine düşülür.

**Fiyat listesi PDF'i: yalnızca satış fiyatı girilmiş (> 0) reçeteler, Türkçe alfabeye göre sıralı; yalnızca ürün adı ve fiyat içerir (maliyet/marj yok), "Fiyatlar KDV hariçtir." notu taşır.** Fiyatı olmayanlar listeye girmez, ekranda kaç tane atlandığı yazar.
Neden: müşteriye maliyet gösterilmez (ilk plan kararı); fiyatsız satırlar müşteri listesinde boş görünürdü.

**PDF Belgelerim\AtolyeRecete\ altına `Fiyat-Listesi-YYYY-AA-GG.pdf` olarak kaydedilir (aynı gün üzerine yazar); "Aç" varsayılan PDF okuyucuyla açar, "Paylaş" MAUI Share API'sini kullanır.** İşletme adı Ayarlar'dan girilir, boşsa başlık yalnızca "Fiyat Listesi".
Android'de PDF uygulamanın önbellek klasörüne yazılır (Belgelerim yok); "Paylaş" birincil yoldur. Android'de doğrulandı: PDF üretimi, "Aç" (Google PDF görüntüleyici) ve "Paylaş" (paylaşım penceresi) çalışıyor. Windows'ta "Paylaş" denenmedi; çalışmazsa kullanıcıya "Aç ile açıp oradan gönder" mesajı gösterilir. PDF üretimindeki beklenmedik hatalar uygulamayı çökertmez, ekranda mesaj olarak gösterilir.

**PDF için QuestPDF bırakıldı, SkiaSharp (MIT) kullanılıyor.** QuestPDF mobilde (Android) hiç çalışmıyor: yerel bileşenleri yalnızca Windows/Linux/macOS için, kendi hata mesajı PDF üretimini sunucuya taşımayı öneriyor. Bu, "backend yok, yerel" ilkemizle bağdaşmaz. Android'de "PDF oluştur" uygulamayı çökertti, bu yüzden değişiklik yapıldı.
Neden: tek kod yolu, iki platformda çalışır, lisans kısıtı ve QuestPDF'in 16 KB sayfa boyutu uyarıları ortadan kalktı. Bedeli: QuestPDF'in yerleşim motoru gitti, başlık/tablo/sayfa numarası elle çiziliyor (fiyat listesi basit olduğu için makul).

**"Marj eşiğin altında mı" kuralı tek yerde: `MarginAlerts.IsBelowThreshold` (Core, kesin küçüktür; eşit marj düşük sayılmaz).** Reçete listesi, reçete düzenleme, Ana Sayfa ve marj uyarısı aynı kuralı kullanır.
Neden: kural dört ekranda ayrı ayrı yazılırsa zamanla birbirinden ayrışır.

**Ana Sayfa özet ekranıdır:** hammadde/reçete sayısı, "fiyat listesinde x / y", saatlik ücret, "dikkat gerektirenler" (marjı eşiğin altında olan ve satış fiyatı girilmemiş reçeteler) ve maliyet/satış/marj tablosu. Hepsi güvende ise yeşil "Her şey yolunda" kutusu görünür.

**Marka: ana renk yanık turuncu `#A8501C`, mum simgesi ve açılış ekranı.** Şablonun mor rengi, macenta sekme rengi ve .NET logosu kaldırıldı.

**Sunum betiği `docs/SUNUM_AKISI.md`:** hazırlık adımları, rakamlarıyla demo akışı, bilinen sınırlar ve olası zor sorular. Rakamlar uygulamada doğrulandı.

**Telefon düzeni (tek tasarım, hem telefon hem masaüstü): alt çubukta 5 sekme (Ana Sayfa, Hammaddeler, Reçeteler, Simülatör, Fiyat listesi); Ayarlar, Ana Sayfa'nın sağ üstündeki "⚙ Ayarlar" ile açılan ayrı bir sayfadır.**
Neden: Android alt çubuğu en fazla 5 sekme gösterir, fazlası "More" menüsüne düşüyordu. Ana Sayfa kartları 2x2, reçete kalemleri tek satır yerine kart (hammadde üstte, miktar/fire/tutar altta): 6 sütunlu satır telefonda sığmıyordu. Android durum çubuğu ve tema renkleri marka rengine (`colors.xml`) çekildi.

**Sayı girişi hem virgül hem nokta kabul eder.** Android sayı klavyesi cihaz diline göre ondalık için virgül veya nokta gösterir (İngilizce emülatörde virgül süzülüyor, Türkçe telefonda gösterilir); okuma ikisini de destekler, binlik ayracı desteklenmez.

**Alt sekme adları kısa: Ana Sayfa, Hammadde, Reçeteler, Simülatör, Fiyatlar** (sayfa başlıkları uzun hâliyle kalır: "Hammaddeler", "Fiyat listesi").
Neden: 360 dp genişliğindeki telefonda "Hammaddeler" ve "Fiyat listesi" sekmede kırpılıyordu ("Hamma…", "Fiyat list…").

**Mobil doğrulama (gerçek telefon yerine emülatör, ayarlarla zorlanarak):** Android 16 emülatörde Debug ve Release (trimleme + AOT) sürümleri denendi: açılış, tüm sekmeler, SQLite, PDF üretimi/Aç/Paylaş, marj uyarısı çalışıyor. Türkçe uygulama diliyle virgüllü sayı girişi çalışıyor (İngilizce dilde Android sayı klavyesi virgülü süzüyordu). 360 dp ekran, %130 yazı boyutu, karanlık mod ve yatay mod zorlandı; düzen bozulmadı. Karanlık modda kartlar `Gray600`, marka başlığı `PrimaryDark` ile okunur hâle getirildi.
Bilinen sınırlar: gerçek telefonda (özellikle düşük donanım, farklı klavye) denenmedi; Release APK `arm64-v8a` ve `x86_64` yerel kitaplık içerir, 32 bit `armeabi-v7a` yok; Windows'ta çok dar pencerede sekme çubuğu "Diğer" menüsüne taşar (hedef kullanım değil).

**Çalışma şekli: faz faz, adım adım ilerlenir. Proje bitince dosya dosya anlatan bir .md (ör. docs/KOD_REHBERI.md) hazırlanacak.**

## 2026-10-02 İlk planlama oturumu

**Hedef:** Şu anki amaç öğrenme ve sunum. Sunum onaylanırsa üretici görüşmeleri ve doğrulama sonraki aşama.
Neden: kullanıcı kendi kararı olarak belirtti; pazar riskleri (aşağıda) ertelendi, kapsam dışı değil.

**Teknoloji: .NET MAUI + MVVM + SQLite, backend yok.**
Neden: hedef kurs/öğrenme ve sunum; MVP'de backend zaman kaybı. Local-first, backend (Postgres) sonra: hesap/yedek, uzaktan komisyon tablosu, abonelik doğrulama (RevenueCat, mağaza faturalandırması).

**SQLite kalıyor, MySQL'e geçilmeyecek.**
Neden: SQLite yerel katman, MySQL/Postgres sunucu katmanı; birbirinin yerine geçmezler.

**İlk dikey: mum atölyeleri.**
Neden: basit reçete yapısı, net birimler (g, ml, adet), alt reçete yok. Butik pasta (alt reçete, verim, raf ömrü) sonraki dikey.

**Hesap motoru ayrı proje (Core), xUnit testli.**
Neden: ürünün değeri hesabın doğruluğunda; UI'dan bağımsız test edilebilir, ileride web/PWA'ya taşınabilir.

**Çıkarılanlar:** ZXing barkod (MVP'de gereksiz), Syncfusion (ağır, lisans sınırları; CommunityToolkit yeterli).

**Müşteriye giden çıktı = fiyat listesi PDF'i, maliyet analizi değil.**
Neden: üretici maliyetini müşteriyle paylaşmaz.

**Öne çıkan özellik: hammadde fiyatı güncellenince tüm reçeteler ve marjlar otomatik yenilenir (enflasyon modu).**
Neden: Türkiye'de hammadde fiyatları sürekli değişiyor; hem pazar farkı hem MVVM için iyi demo.

**Boş ekran çözümü: mum başlangıç paketi (seed data), tek tuşla silinebilir.**

**Ücretsiz sürüm limiti (ileride):** hammadde sayısıyla değil reçete sayısıyla sınırla. 5 hammadde, bir mum reçetesi için bile az.

## Açık riskler (ertelendi, sunum sonrası)

- Retention: fiyatı bir kez hesaplayan kullanıcı uygulamayı bırakabilir. Doğrulama: Excel prototipiyle 10-15 üretici, 1 ay sonra kullanım.
- Ödeme isteği: hedef kitlenin ödeme gücü düşük olabilir. Doğrulama: landing page + fiyat gösterip ön kayıt ölçme.
- Sunumda "Sonraki adım: doğrulama planı" slaytı olacak.

## Açık teknik sorular

- KDV: MVP'de fiyatlar KDV hariç hesaplanır, KDV ayarı sonra. (Değişirse burayı güncelle.)
- Hammadde fiyat geçmişi tutulacak mı (MVP'de hayır, sadece güncel fiyat).
