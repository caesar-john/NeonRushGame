# 📘 Panduan Tim — NeonRushGame

Panduan ini untuk semua anggota tim yang mau mengambil versi terbaru project dari GitHub.

**Status repo saat panduan ini dibuat:**

| Item | Nilai |
|---|---|
| Repository | https://github.com/caesar-john/NeonRushGame |
| Branch resmi | `main` |
| Commit terakhir | `78a06c2` |
| Unity version | **6000.3.27f1** (Unity 6.3) |
| Render pipeline | URP 17.3.0 |
| Input handler | Both (Input System + Input Manager lama) |

> ✅ **Semua branch fitur sudah digabung ke `main`.** Jadi kamu cukup ambil `main`, tidak perlu ambil branch lain.

---

## 1. Prasyarat

Pastikan sudah terpasang:

- **Git** → cek dengan `git --version`
- **Unity Hub** → https://unity.com/download
- **Unity Editor 6000.3.27f1** (install lewat Unity Hub, tab *Installs* → *Install Editor* → pilih versi ini)
- **Ruang disk minimal 3 GB** (Assets ± 905 MB + Library hasil import bisa 1–2 GB)

> ⚠️ Jangan pakai versi Unity lain. Beda versi bisa merusak file scene/prefab.

---

## 2. Pertama kali: Clone Project

```bash
git clone https://github.com/caesar-john/NeonRushGame.git
cd NeonRushGame
git checkout main
```

**Catatan penting soal lokasi project:**
Folder hasil clone bernama `NeonRushGame`, dan **project Unity-nya ada di dalam folder bernama `NeonRushGame` lagi** (nested):

```
NeonRushGame/          ← folder hasil git clone (root repo)
├── .git/
├── INTEGRASI_PROGRAMMER2.md
├── PANDUAN_TIM.md
└── NeonRushGame/      ← INI yang dibuka di Unity Hub
    ├── Assets/
    ├── Packages/
    └── ProjectSettings/
```

Kalau clone terasa lambat (repo ± 1 GB), boleh pakai clone ringkas:

```bash
git clone --depth 1 https://github.com/caesar-john/NeonRushGame.git
```

---

## 3. Kalau Sudah Pernah Clone: Cukup Pull

```bash
cd NeonRushGame
git checkout main
git pull origin main
```

Kalau ada perubahan lokal yang belum di-commit dan pull ditolak, simpan dulu:

```bash
git stash            # simpan perubahan sementara
git pull origin main
git stash pop        # kembalikan perubahan
```

---

## 4. Membuka Project di Unity

1. Buka **Unity Hub** → tab **Projects** → tombol **Add** → **Add project from disk**
2. Pilih folder: **`NeonRushGame/NeonRushGame`** (yang ada `Assets` di dalamnya)
3. Pastikan versi editor di kolom kanan tertulis **6000.3.27f1**
4. Klik project untuk membuka

**Saat pertama kali dibuka:**

- Unity akan **import semua asset** — bisa **10–30 menit**. Jangan ditutup, jangan dikira hang.
- Package akan di-download otomatis dari internet. Butuh koneksi.
- Setelah selesai, buka scene lewat `Assets/Scenes/`.

---

## 5. Scene yang Tersedia

| Scene | Fungsi |
|---|---|
| `Assets/Scenes/MainMenu.unity` | Menu utama (PLAY / EXIT, best score) — **scene awal build** |
| `Assets/Scenes/Game.unity` | Gameplay utama (runner) |
| `Assets/Scenes/Sandbox_PLayer.unity` | Scene uji coba player |
| `Assets/NeonCity.unity` | Scene kota (backdrop visual) |

Cara jalanin: buka scene → tekan **Play** di atas. Untuk alur lengkap, mulai dari `MainMenu`.

**Kontrol permainan:**

| Tombol | Aksi |
|---|---|
| `A` / `←` | Pindah jalur kiri |
| `D` / `→` | Pindah jalur kanan |
| `Spasi` / `↑` | Lompat |
| `S` / `↓` | Slide |
| `Esc` | Pause |

---

## 6. Alur Kerja Git untuk Tim

**Aturan utama: jangan kerja langsung di `main`.** Buat branch sendiri, lalu ajukan Pull Request.

### Mulai kerja (setiap hari)

```bash
git checkout main
git pull origin main          # ambil versi terbaru dulu
git checkout -b feature/nama-kamu-fitur
```

Contoh nama branch: `feature/imfarhann-sfx`, `fix/tyas-ui-menu`

### Setelah selesai ngoding

```bash
git status                    # cek file apa saja yang berubah
git add .
git commit -m "feat: tambah efek suara slide"
git push -u origin feature/nama-kamu-fitur
```

Lalu buka GitHub → **Compare & pull request** → minta review → merge ke `main`.

### Sebelum push, selalu cek

```bash
git status                    # pastikan tidak ada Library/ atau Temp/ yang ikut
git diff --stat               # pastikan file yang berubah sesuai yang kamu kerjakan
```

---

## 7. Aturan Wajib (biar repo tidak rusak)

1. ❌ **Jangan pernah commit** folder/file berikut (sudah di-`.gitignore`, tapi tetap waspada):
   `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Obj/`, `*.csproj`, `*.sln`, `*.slnx`
2. ✅ **File `.meta` wajib ikut** saat menambah asset baru. Jangan pernah hapus/di-ignore.
3. 🚫 **Jangan edit scene yang sama bersamaan.** `MainMenu.unity` dan `Game.unity` sangat rawan konflik. Koordinasi dulu di grup.
4. 🚫 **Jangan ganti nama / pindah / hapus file** milik orang lain tanpa bilang.
5. ✅ **Pull dulu sebelum mulai kerja**, jangan kerja di versi basi berhari-hari.
6. ✅ Simpan perubahan kecil & sering commit, jangan numpuk seminggu sekali.

---

## 8. Troubleshooting

| Masalah | Solusi |
|---|---|
| **"Project has been opened in another version"** | Buka lewat Unity Hub, pilih versi **6000.3.27f1**. Kalau belum ada, install dulu. |
| **Import lama / Unity seperti hang** | Normal untuk pertama kali (asset besar). Tunggu, jangan Force Quit. |
| **Missing script / komponen hilang** | Pastikan `git pull` sukses dan file `.meta` ikut ter-commit. Kalau tetap, minta yang punya script untuk cek. |
| **Error `CS...` di Console** | Screenshot Console, kirim ke grup. Jangan asal hapus script. |
| **Teks UI tidak muncul / kotak-kotak** | `Window` → `TextMeshPro` → `Import TMP Essential Resources`. |
| **Konflik saat `git pull` (file `.unity`/`.prefab`)** | **Jangan diselesaikan sendiri.** Hubungi pemilik scene. Bisa dibatalkan dengan `git merge --abort`. |
| **`error: invalid path` / `filename too long` (Windows)** | Jalankan: `git config --global core.longpaths true` lalu ulangi `git clone`. |
| **Clone sangat lambat** | Pakai `--depth 1` (lihat bagian 2). |
| **File `.csproj` muncul terus di `git status`** | Abaikan / jangan di-add. File itu hasil generate Unity, bukan source code. |

---

## 9. Kalau Bingung

1. Cek dulu kamu ada di branch yang benar: `git branch --show-current` → harus `main` (atau branch kamu sendiri, bukan `main` orang lain)
2. Cek kamu sudah dapat versi terbaru: `git log --oneline -3` lalu bandingkan dengan commit di halaman GitHub
3. Masih bingung → tanya di grup dengan menyertakan: **output `git status`**, **screenshot Console**, dan **langkah yang kamu lakukan**

---

## 10. Quick Reference

```bash
# Ambil versi terbaru
git checkout main && git pull origin main

# Mulai fitur baru
git checkout -b feature/nama-fitur

# Simpan pekerjaan
git add . && git commit -m "feat: deskripsi singkat" && git push -u origin feature/nama-fitur

# Batalkan merge yang berantakan
git merge --abort

# Lihat branch aktif
git branch --show-current

# Lihat commit terakhir
git log --oneline -5
```
