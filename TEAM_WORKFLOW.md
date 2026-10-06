# 🌾 FarmASU - Ekip Geliştirme & Git İş Akışı Protokolü

Bu doküman, **Tamer** ve **Faruk**'un FarmASU projesinde kodların, sahnelerin ve varlıkların (asset) çakışmasını önlemek ve profesyonel bir stüdyo disipliniyle geliştirmek için uyması gereken kuralları içerir.

---

## 1. 🛑 Altın Kural: Sahne (Scene) Çakışmalarını Önleme

Unity'de `.unity` sahne dosyaları iki kişi tarafından aynı anda düzenlendiğinde Git bunları birleştiremez. Sahne bozulur veya birinizin çalışması kaybolur.

### Çözüm Yöntemleri:
1. **Prefab Odaklı Çalışma (Tavsiye Edilen):**
   - Sahnede yer alan tüm ana sistemler prefab olmalıdır (`Player.prefab`, `HotbarHUD.prefab`, `FarmGrid.prefab` vb.).
   - Yeni bir özellik geliştirirken `MainScene`'i düzenleyip kaydetmek yerine, doğrudan Prefab moduna (`Open Prefab`) girin ve geliştirmeyi prefab içinde yapın.
   - Böylece sadece ilgili `.prefab` dosyası değişir; `MainScene.unity` tertemiz kalır.
2. **Kişisel Test Sahneleri (Sandbox):**
   - Her geliştirici `Assets/Scenes/Test/` klasörü altına kendi sahnesini açabilir (örn. `Assets/Scenes/Test/Tamer_Dev.unity`, `Faruk_Dev.unity`).
   - Mekanikler önce burada geliştirilir, prefab yapılır.
   - Ana sahneye (`MainScene`) ekleme yapılacağı zaman ekip arkadaşına haber verilir ("Sahneye prefableri bağlıyorum") ve tek kişi commit atar.
3. **Sahne Kilidi Anlaşması (Scene Lock):**
   - Arazi (Terrain), ışıklandırma veya sahne tasarımı gibi kaçınılmaz sahne düzenlemelerinde Discord üzerinden haberleşin: *"1 saat MainScene üzerindeyim, sahneyi açıp kaydetme."*

---

## 2. 🌿 Git Branch (Dal) & Pull Request (PR) Akışı

**`main` dalına asla doğrudan `commit` / `push` yapılmaz!**

### Günlük Adım Adım İş Akışı:
```bash
# 1. Güne başlarken her zaman ana daldan en son kodu çekin:
git checkout main
git pull origin main

# 2. Üzerinde çalışacağınız iş için yeni bir dal açın:
# Format: feature/ozellik-adi veya fix/hata-adi
git checkout -b feature/tarla-sulama-sistemi

# 3. Geliştirmenizi yapın, test edin ve sadece ilgili dosyaları commit'leyin:
git add .
git commit -m "feat(farming): implement soil hydration and watering mechanic"

# 4. Dalınızı GitHub'a gönderin:
git push origin feature/tarla-sulama-sistemi
```
5. **GitHub'da Pull Request (PR) Açın:**
   - Değişen dosyaları kontrol edin. Yanlışlıkla demo sahneleri veya geçici dosyalar eklenmediğinden emin olun.
   - Arkadaşınız PR'ı inceleyip onaylasın (`Squash and merge` veya `Merge pull request`).
   - Birleşince yerelde tekrar `git checkout main` ve `git pull` yapın.

---

## 3. ⚠️ `.meta` Dosyaları Kuralı

Unity'deki tüm script, prefab, materyal ve görsellerin yanında `.meta` dosyaları bulunur (GUID referansları).
- **Asla Windows Gezgini'nden dosya silmeyin, taşımayın veya isim değiştirmeyin!** Her zaman Unity Editor içindeki Project penceresinden yapın.
- Yeni bir dosya eklediğinizde Git'e hem asıl dosyayı hem de `.meta` dosyasını dahil ettiğinizden emin olun.
- Bir `.meta` dosyası eksik olursa diğer geliştiricinin sahnesinde objeler **"Missing Script"** veya pembe renge (Missing Material) döner.

---

## 4. 🔄 Unity Açıkken Git Kullanım Protokolü

Unity arka planda açıkken dışarıdan `git pull` veya branch değişimi yapılırsa:
- Unity sahneyi RAM belleğinde tuttuğu için eski haliyle diske geri yazabilir ve yeni çekilen değişiklikleri silebilir.
- **En Güvenli Yol:**
  1. Çekme işleminden önce Unity'deki sahnenizi kaydedin.
  2. `git pull` yapmadan önce `git status` ile gereksiz değişen sahne varsa `git restore` ile geri alın.
  3. Çekme işleminden sonra Unity'e döndüğünüzde Unity'nin varlıkları yeniden içe aktarmasını (re-import) bekleyin. Gerekirse `File -> Open Scene -> MainScene` diyerek sahneyi tazeleyin.

---

## 5. 🛠️ Unity SmartMerge (UnityYAMLMerge)

Projedeki `.gitattributes` ve `.git/config` dosyalarına Unity'nin resmi akıllı birleştirme aracı entegre edilmiştir.
- Bir `.prefab` veya `.asset` dosyasında Git çakışması yaşanırsa, `UnityYAMLMerge.exe` otomatik devreye girerek YAML hiyerarşisini akıllıca birleştirir.
- Faruk da kendi bilgisayarında terminalde şu komutu çalıştırarak bu özelliği aktif etmelidir:
  ```powershell
  git config merge.unityyamlmerge.name "Unity SmartMerge (UnityYamlMerge)"
  git config merge.unityyamlmerge.driver "'C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data\Tools\UnityYAMLMerge.exe' merge -h -p -- %O %B %A %A"
  git config merge.unityyamlmerge.recursive "binary"
  ```
  *(Not: Unity yolu farklı bir dizindeyse kendi `UnityYAMLMerge.exe` yolunu yazmalıdır).*

---

## 6. 📋 Hızlı Kontrol Listesi (Cheat Sheet)

| Senaryo | Ne Yapmalısın? |
|---|---|
| Güne başlarken | `git checkout main` ➔ `git pull origin main` |
| Yeni bir özelliğe başlarken | `git checkout -b feature/gorev-adi` |
| Sahne kaydedildi ama değişiklik yapmadım | `git restore Assets/Scenes/...` |
| İşim bittiğinde | `git push origin feature/...` ➔ GitHub'da PR aç |
| Arkadaşım PR birleştirdiğinde | `git checkout main` ➔ `git pull` |
