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

**MAUI hedefi şimdilik sadece net9.0-windows10.0.19041.0.** Android/iOS/Mac hedefleri csproj'dan çıkarıldı.
Neden: demo Windows'ta; gereksiz iş yükü derlenmesin. Android için csproj'a net9.0-android eklenir.

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
