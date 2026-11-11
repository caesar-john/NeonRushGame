# Integrasi Programmer 2 — NeonRushGame

Dokumen ini menjelaskan cara pekerjaan **Programmer 2** (spawner, skor, koin,
kecepatan, UI, scene manager, audio) dipasang ke repo kelompok **NeonRushGame**
tanpa menghapus atau menimpa pekerjaan Programmer 1 maupun Game Designer.

Status: **selesai dan lolos smoke test** di Unity 6000.3.27f1.

---

## 1. Prinsip Integrasi

| Aturan | Penerapan |
|--------|-----------|
| Tidak mengubah script Programmer 1 | `PlayerMovement.cs`, `CoinWork.cs`, `FollowPlayer.cs` tidak disentuh |
| Tidak mengubah prefab/scene tim | `Player.prefab`, `CoinPlaceHolder.prefab`, `Sandbox_PLayer.unity` tidak disentuh |
| Tidak menimpa aset Game Designer | Material, scene, dan setting tim tetap ada |
| Sistem Programmer 2 self-contained | Semua script baru berada di `Assets/Scripts/Programmer2` |

Semua penyesuaian dilakukan dari sisi script Programmer 2, lewat komponen
jembatan dan referensi objek di scene baru (`Game.unity`).

---

## 2. File yang Ditambahkan

```
Assets/Scripts/Programmer2/
├── Core
│   ├── GameManager.cs
│   ├── DifficultyManager.cs
│   ├── ScoreManager.cs
│   ├── PoolManager.cs
│   ├── PooledObject.cs
│   └── PlayerSpeedBridge.cs   <- jembatan ke PlayerMovement milik Programmer 1
├── Spawning
│   ├── TileSpawner.cs
│   ├── ObstacleSpawner.cs
│   └── CoinSpawner.cs
├── Gameplay
│   ├── Coin.cs
│   └── Hazard.cs
├── UI
│   ├── HUDController.cs
│   └── UIManager.cs
├── Audio
│   └── AudioManager.cs
├── Debug
│   └── PlayerAutoRunner.cs    <- cadangan bila Player.prefab tidak ada
└── Editor
    ├── NeonRushSceneBuilder.cs
    └── NeonRushSmokeTest.cs

Assets/Scenes/Game.unity          (baru)
Assets/Scenes/MainMenu.unity      (baru)
Assets/Prefabs/RoadTile.prefab
Assets/Prefabs/Obstacle_Laser.prefab
Assets/Prefabs/Obstacle_Barrier.prefab
Assets/Prefabs/Coin.prefab
Assets/Materials/*.mat            (URP Lit + emissive neon)
Assets/Resources/Audio/*.wav      (BGM + 5 SFX, dibuat prosedural)
Assets/TextMesh Pro/              (TMP Essential Resources untuk UI)
```

`Assets/Scripts/Programmer2/Editor` adalah folder khusus Editor, jadi builder
dan smoke test tidak ikut terbangun ke dalam game.

---

## 3. File yang Berubah (bukan milik P1/GD)

| File | Perubahan |
|------|-----------|
| `ProjectSettings/EditorBuildSettings.asset` | Menambahkan `MainMenu` dan `Game`; `Sandbox_PLayer` tetap ada |
| `ProjectSettings/TagManager.asset` | Menambahkan layer baru `Obstacle` (untuk pengecekan koin vs rintangan) |
| `Assembly-CSharp*.csproj`, `NeonRushGame.slnx` | Regenerasi otomatis Unity karena ada script baru |

Tidak ada perubahan pada script, prefab, atau scene milik Programmer 1 /
Game Designer (sudah diverifikasi dengan `git status`).

---

## 4. Cara Menyesuaikan dengan Pekerjaan Tim

### 4.1 Player Programmer 1 dipakai apa adanya

Builder scene `Game` meng-instance `Assets/Prefabs/Player/Player.prefab`
milik tim. Prefab tidak diubah. Komponen `PlayerMovement` tetap yang
menggerakkan karakter.

### 4.2 Kecepatan: `PlayerSpeedBridge`

`PlayerMovement` milik Programmer 1 memakai `forwardSpeed` tetap. Agar sistem
kesulitan Programmer 2 bekerja tanpa mengedit script itu, ditambahkan
komponen **`PlayerSpeedBridge`** pada instance Player di scene Game:

- Setiap frame menyalin `GameManager.Instance.Speed` ke field `forwardSpeed`
  milik `PlayerMovement` (lewat reflection, field privat tetap aman).
- Saat state bukan `Playing` (menu, pause, game over), nilainya dibuat `0`
  sehingga karakter berhenti.
- Jika `GameManager` tidak ada (misalnya di scene `Sandbox_PLayer`), bridge
  tidak melakukan apa-apa, jadi scene sandbox tetap berjalan seperti biasa.

Reflection dipakai supaya script Programmer 1 tidak perlu diubah dan tidak
terikat compile-time. Jika nanti Programmer 1 ingin membaca `GameManager.Speed`
langsung, bridge ini boleh dihapus.

### 4.3 Koin dan rintangan tidak bergantung pada tag

`Coin.cs` dan `Hazard.cs` mengenali karakter lewat referensi
`GameManager.Instance.Player`, bukan `CompareTag("Player")`. Karena
`Player.prefab` milik tim masih `Untagged`, ini membuat sistem Programmer 2
tetap berfungsi tanpa mengubah prefab.

### 4.4 Kamera memakai `FollowPlayer` milik tim

Kamera scene Game memakai komponen **`FollowPlayer`** (script tim) dengan
`player` diarahkan ke Player dan `offset = (0, 4.5, -8.5)`. Tidak ada camera
script baru yang menduplikasi pekerjaan tim.

### 4.5 URP

Repo memakai Universal Render Pipeline 17.3.0. Builder otomatis:

- memilih shader `Universal Render Pipeline/Lit` (fallback ke Standard),
- mengisi `_BaseColor`, `_Metallic`, `_Smoothness`, dan `_EmissionColor`
  supaya material neon tidak berwarna pink,
- menambahkan `UniversalAdditionalCameraData` pada kamera.

### 4.6 Lantai fisik

`Player.prefab` memakai Rigidbody + gravity. Scene Game menambahkan objek
`FloorCollider` (BoxCollider tanpa renderer) di bawah jalan agar karakter
tidak jatuh, tanpa mengubah Rigidbody atau prefab tim.

### 4.7 Input

`ProjectSettings` tim memakai `activeInputHandler: 2` (Both), sehingga
`Input.GetKeyDown` di `PlayerMovement` dan Input System baru sama-sama aktif.
Tidak ada perubahan setting input.

---

## 5. Cara Membuka dan Menjalankan

1. Buka **Unity Hub** → **Add project from disk** →
   `C:\Users\Acer\NeonRushGameExtract\NeonRushGame\NeonRushGame`
   (atau folder repo kalian setelah merge).
2. Gunakan **Unity 6000.3.27f1**.
3. Buka `Assets/Scenes/MainMenu.unity`, tekan **Play**.
4. Klik **PLAY** untuk masuk ke `Game`.
5. Scene `Sandbox_PLayer` tetap bisa dibuka dan dipakai tim.

Build Settings sudah berisi:

```
MainMenu
Game
Sandbox_PLayer
```

### Kontrol

| Aksi | Tombol |
|------|--------|
| Pindah kiri | `A` / `←` |
| Pindah kanan | `D` / `→` |
| Pause | `Esc` |
| Tombol UI | Mouse |

> Lompat/slide tidak dipakai di scene Game karena `PlayerMovement` tim baru
> mendukung perpindahan jalur. Rintangan dirancang maksimal 2 dari 3 jalur
> terblokir, jadi selalu ada jalur aman.

---

## 6. Rebuild Scene dan Smoke Test

### Lewat menu Unity

- **Neon Rush > 2. Build Scenes & Prefabs**

Perintah ini membuat ulang material, prefab, audio, `Game.unity`, dan
`MainMenu.unity`. `Sandbox_PLayer` tidak disentuh dan tetap masuk Build Settings.

### Lewat command line

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe"
$repo  = "C:\Users\Acer\NeonRushGameExtract\NeonRushGame\NeonRushGame"

# build scene + prefab
& $unity -batchmode -quit -projectPath $repo `
  -executeMethod NeonRushSceneBuilder.BuildFromCommandLine `
  -logFile "$env:TEMP\neonrush_build.log"

# smoke test play mode
& $unity -batchmode -projectPath $repo `
  -executeMethod NeonRushSmokeTest.Run `
  -logFile "$env:TEMP\neonrush_smoke.log"
```

### Hasil smoke test terakhir

```
[SMOKE][OK] GameManager.Instance tersedia
[SMOKE][OK] State = Playing saat mulai
[SMOKE][OK] Kecepatan naik (Speed > 8): 8.29972
[SMOKE][OK] Jarak bertambah: 14.98598
[SMOKE][OK] Skor bertambah dari jarak: 14
[SMOKE][OK] Koin ter-spawn di lintasan
[SMOKE][OK] Rintangan ter-spawn di lintasan
[SMOKE][OK] TileSpawner aktif
[SMOKE][OK] Tile jalan ter-spawn
[SMOKE][OK] CollectCoin menambah 10 koin
[SMOKE][OK] State = Paused setelah Pause()
[SMOKE][OK] State = Playing setelah Resume()
[SMOKE][OK] Jarak berhenti selama pause
[SMOKE][OK] State = GameOver setelah EndGame()
[SMOKE][OK] Best score tersimpan
SMOKE TEST LULUS
```

Catatan: baris `ArgumentOutOfRangeException` dari `UnityEditor.Search` adalah
bug indexer bawaan Unity saat batchmode, bukan dari game, dan tidak
menggagalkan tes.

---

## 7. API untuk Programmer 1 / Sistem Lain

```csharp
GameManager gm = GameManager.Instance;

if (!gm.IsPlaying) return;          // hormati menu / pause / game over
float speed = gm.Speed;             // kecepatan dari DifficultyManager
gm.EndGame();                       // saat tabrakan
gm.CollectCoin(10);                 // saat ambil koin (Coin.cs sudah otomatis)

AudioManager.PlaySfx(SfxId.Jump);
AudioManager.PlaySfx(SfxId.Slide);
AudioManager.PlaySfx(SfxId.Crash);
```

`PlayerMovement` tidak perlu diubah. Jika ingin, Programmer 1 bisa membaca
`gm.Speed` langsung dan menghapus `PlayerSpeedBridge` dari Player di scene Game.

---

## 8. Langkah Merge ke Repo Kelompok

Repo hasil integrasi ada di:

```
C:\Users\Acer\NeonRushGameExtract\NeonRushGame
```

Branch yang dipakai: `bagian-programmer2` (origin:
`https://github.com/caesar-john/NeonRushGame.git`).

```powershell
cd C:\Users\Acer\NeonRushGameExtract\NeonRushGame

git status
git add NeonRushGame/Assets/Scripts/Programmer2 `
        NeonRushGame/Assets/Scripts/Programmer2.meta `
        NeonRushGame/Assets/Scenes/Game.unity `
        NeonRushGame/Assets/Scenes/Game.unity.meta `
        NeonRushGame/Assets/Scenes/MainMenu.unity `
        NeonRushGame/Assets/Scenes/MainMenu.unity.meta `
        NeonRushGame/Assets/Prefabs `
        NeonRushGame/Assets/Materials `
        NeonRushGame/Assets/Materials.meta `
        NeonRushGame/Assets/Resources `
        NeonRushGame/Assets/Resources.meta `
        "NeonRushGame/Assets/TextMesh Pro" `
        "NeonRushGame/Assets/TextMesh Pro.meta" `
        NeonRushGame/ProjectSettings/EditorBuildSettings.asset `
        NeonRushGame/ProjectSettings/TagManager.asset

git commit -m "Integrasi Programmer 2: spawner, skor, koin, kecepatan, UI, audio, scene Game/MainMenu"
git push origin bagian-programmer2
```

Jangan commit folder `Library/`, `Temp/`, `Logs/`, `obj/`, `.vs/`, atau `*.csproj`
hasil generate bila tim tidak ingin konflik IDE. File `Assembly-CSharp.csproj`
dan `NeonRushGame.slnx` memang ikut berubah karena Unity menambahkan script baru;
koordinasikan dengan tim sebelum commit.

### Kalau ingin menerapkan ke repo asli secara manual

Cukup salin dari folder hasil integrasi ke repo kalian:

1. `Assets/Scripts/Programmer2` beserta `.meta`-nya.
2. `Assets/Scenes/Game.unity`, `MainMenu.unity` + `.meta`.
3. `Assets/Prefabs` (RoadTile, Obstacle_Laser, Obstacle_Barrier, Coin) + `.meta`.
4. `Assets/Materials` + `.meta`.
5. `Assets/Resources/Audio` + `.meta`.
6. `Assets/TextMesh Pro` + `.meta`.
7. Tambahkan `MainMenu` dan `Game` ke Build Settings (Sandbox tetap).
8. Buka scene `Game`, pastikan Player memakai `Player.prefab` tim dan memiliki
   komponen `PlayerSpeedBridge`, lalu Play.

---

## 9. Catatan untuk Tim

- `CoinWork.cs` milik tim masih mengecek `CompareTag("Player")`, sedangkan
  `Player.prefab` saat ini `Untagged`. Artinya koin di scene `Sandbox_PLayer`
  belum bisa diambil. Ini kondisi awal repo, bukan akibat integrasi. Perbaikan
  yang disarankan: beri tag `Player` pada prefab, atau ubah `CoinWork` memakai
  referensi Player seperti `Coin.cs`. Prefab sengaja tidak diubah agar tidak
  menimpa pekerjaan Programmer 1.
- `PlayerSpeedBridge` dapat dihapus kapan saja bila `PlayerMovement` sudah
  diubah sendiri oleh Programmer 1 untuk membaca `GameManager.Speed`.
- Semua spawn memakai pooling; tidak ada `Instantiate/Destroy` berulang saat
  gameplay.
- Audio dibuat prosedural (bebas hak cipta) dan bisa diganti lewat komponen
  `AudioManager` di scene Game/MainMenu.
