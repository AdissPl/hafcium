# 🔐 Hafcium — Generator haseł i menadżer kont

<div align="center">

**Projekt zaliczeniowy — Programowanie Obiektowe w C#**

Autor: **Adrian Zieliński** · Nr albumu: **79565** · Grupa: **59DR2**

MANS — Menadżerska Akademia Nauk Stosowanych w Warszawie

Semestr III, Informatyka

</div>

---

## 📖 O projekcie

**Hafcium** to w pełni lokalna aplikacja desktopowa (Windows Forms, .NET 8) umożliwiająca:

- 🔑 Generowanie bezpiecznych haseł z konfigurowalnymi parametrami
- 📁 Przechowywanie loginów i haseł do różnych serwisów
- 🔒 Szyfrowanie bazy danych algorytmem **AES-256-CBC** z kluczem derywowanym przez PBKDF2
- 💾 Pełną pracę offline — brak wymaganego połączenia z internetem ani zewnętrznych baz danych

Nazwa „Hafcium" nawiązuje do pierwiastka chemicznego **Hf (Hafn)** — twardego, odpornego i stabilnego, podobnie jak hasła generowane przez tę aplikację!

### Wymagania

- **System:** Windows 10/11
- **.NET SDK:** 8.0 lub nowszy ([pobierz tutaj](https://dotnet.microsoft.com/download/dotnet/8.0))

---

## 🎯 Funkcjonalności (12)

| # | Funkcjonalność | Opis |
|---|----------------|------|
| 1 | **Generator haseł** | Generowanie kryptograficznie bezpiecznych haseł z wyborem: małe/wielkie litery, cyfry, znaki specjalne. Minimalna długość 4, maksymalna 128 znaków. |
| 2 | **Kopiuj hasło do schowka** | Przycisk 📋 kopiuje wygenerowane hasło z potwierdzeniem w postaci komunikatu. |
| 3 | **Dodawanie nowego konta** | Formularz: nazwa serwisu, login/e-mail, hasło (ręczne lub z generatora), opcjonalna notatka. |
| 4 | **Tabela kont z odświeżaniem** | Wyświetlanie wszystkich zapisanych kont w DataGridView z automatycznym odświeżaniem po każdej operacji. |
| 5 | **Ukrywanie haseł** | Domyślnie hasła maskowane znakiem `•`. Checkbox „Pokaż hasła" przełącza widoczność. |
| 6 | **Kopiowanie hasła podwójnym kliknięciem** | Podwójne kliknięcie na wiersz w tabeli automatycznie kopiuje hasło danego konta do schowka z komunikatem. |
| 7 | **Wyszukiwarka na żywo** | Filtrowanie listy kont w czasie rzeczywistym po nazwie serwisu (TextChanged event). |
| 8 | **Usuwanie konta** | Przycisk 🗑 z dialogiem potwierdzenia — zabezpieczenie przed przypadkowym usunięciem. |
| 9 | **Szyfrowanie AES-256-CBC** | Cała baza danych szyfrowana algorytmem AES-256 z kluczem derywowanym z hasła głównego przez PBKDF2 (100 000 iteracji, SHA-256). |
| 10 | **Automatyczny zapis/odczyt** | Dane zapisywane do `passwords.dat` przy każdej zmianie. Automatyczne wczytywanie przy starcie. Zapis atomowy (plik tymczasowy + zamiana). |
| 11 | **Wskaźnik siły hasła** | Wizualny ProgressBar z oceną siły hasła (entropia bitowa + analiza różnorodności znaków). |
| 12 | **Hasło główne z logowaniem** | Ekran logowania przy starcie — tworzenie hasła głównego lub odblokowywanie istniejącej bazy. |

---

## 🏗️ Architektura i wzorce OOP

### Struktura projektu

```
Hafcium/
├── Hafcium.csproj          # Plik projektu .NET 8 WinForms
├── Program.cs              # Punkt wejścia aplikacji
├── LoginForm.cs            # Formularz logowania (hasło główne)
├── MainForm.cs             # Główne okno aplikacji
├── Models/
│   └── AccountEntry.cs     # Model danych konta (enkapsulacja)
├── Services/
│   ├── PasswordGenerator.cs      # Generator haseł (SRP)
│   ├── EncryptionService.cs      # Szyfrowanie AES-256-CBC (SRP)
│   └── DataStorageService.cs     # Persystencja danych (abstrakcja + dziedziczenie)
└── Helpers/
    └── PasswordStrengthAnalyzer.cs  # Analiza siły hasła (utility)
```

### Zastosowane aspekty obiektowości

| Aspekt | Gdzie zastosowano |
|--------|-------------------|
| **Abstrakcja** | `DataStorageBase` — abstrakcyjna klasa bazowa ukrywająca szczegóły zapisu/odczytu za interfejsem `Save()/Load()/StorageExists()`. |
| **Enkapsulacja** | `AccountEntry` — prywatne pola (`_serviceName`, `_password` itd.) z publicznymi właściwościami zawierającymi walidację w setterach. `EncryptionService` — stałe kryptograficzne jako `private const`. |
| **Dziedziczenie** | `FileDataStorageService` dziedziczy po `DataStorageBase`, nadpisując metody abstrakcyjne. `LoginForm` i `MainForm` dziedziczą po `Form`. |
| **Polimorfizm** | Metody `Save()`, `Load()`, `StorageExists()` w `DataStorageBase` nadpisane w `FileDataStorageService` — można podmienić implementację bez zmiany reszty kodu (np. na `SqliteDataStorageService`). |

### Zasady SOLID

- **SRP** — Każda klasa ma jedną odpowiedzialność: `PasswordGenerator` → generowanie, `EncryptionService` → szyfrowanie, `FileDataStorageService` → persystencja.
- **OCP** — `DataStorageBase` jako klasa abstrakcyjna pozwala dodać nową implementację (np. SQLite) bez modyfikacji istniejącego kodu.
- **DIP** — `MainForm` zależy od abstrakcji (serwisów), nie od szczegółów implementacyjnych.

### Kompozycja vs Dziedziczenie

Relacje między obiektami oparto na **kompozycji** (`MainForm` „posiada" serwisy jako pola), zgodnie z zasadą *„Favor composition over inheritance"* (Gang of Four). Dziedziczenie stosowane jest tylko tam, gdzie istnieje rzeczywista relacja „jest" (np. `FileDataStorageService` **jest** rodzajem `DataStorageBase`).

---

## 🔒 Bezpieczeństwo

| Element | Rozwiązanie |
|---------|-------------|
| Generator losowy | `System.Security.Cryptography.RandomNumberGenerator` (CSPRNG) |
| Szyfrowanie | AES-256-CBC z PKCS7 padding |
| Derywacja klucza | PBKDF2 (RFC 2898), SHA-256, 100 000 iteracji |
| Sól | 16 bajtów losowych generowanych dla każdej operacji szyfrowania |
| Zapis atomowy | Plik tymczasowy + zamiana — ochrona przed utratą danych |

---

## 🖼️ Zrzuty ekranu

*(Zrzuty ekranu aplikacji zostaną dodane po pierwszym uruchomieniu)*

---

## 📹 Filmy

1. **Film 1 — Prezentacja projektu** — Kontekst, funkcjonalności, demonstracja działania aplikacji.
2. **Film 2 — Omówienie kodu** — Analiza kodu źródłowego, zastosowane wzorce i „smaczki".

Filmy załączone na platformie Moodle.

---

## 📝 Licencja

Projekt edukacyjny wykonany w ramach zajęć z Programowania Obiektowego na MANS.

© 2025/2026 Adrian Zieliński
