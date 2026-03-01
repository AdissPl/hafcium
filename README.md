# Hafcium

Lokalny menadżer haseł napisany w C# (.NET 8, Windows Forms).

Generuje bezpieczne hasła, przechowuje loginy i hasła do serwisów w zaszyfrowanym pliku na dysku. Nie łączy się z internetem, nie wysyła danych nigdzie — wszystko zostaje na Twoim komputerze.

Nazwa nawiązuje do hafnu (Hf) — pierwiastka który jest stabilny i odporny na korozję. Tak jak dobry menadżer haseł powinien być.

> Projekt zaliczeniowy z Programowania Obiektowego w C#  
> MANS, Warszawa • Adrian Zieliński • 79565 • 59DR A2

---

## Co robi

1. **Generator haseł** — długość 4–128 znaków, wybór kategorii (małe/wielkie litery, cyfry, znaki specjalne). Gwarantuje minimum jeden znak z każdej zaznaczonej kategorii.
2. **Kopiowanie hasła** — przycisk kopiujący wygenerowane hasło do schowka.
3. **Dodawanie kont** — formularz z walidacją: serwis, login, hasło (ręczne lub z generatora), notatka.
4. **Tabela kont** — wszystkie zapisane konta w DataGridView, automatyczne odświeżanie.
5. **Ukrywanie haseł** — domyślnie hasła w tabeli zamaskowane, przycisk do przełączania.
6. **Podwójne kliknięcie** — dwuklik na wiersz kopiuje hasło do schowka (nawet gdy ukryte).
7. **Wyszukiwarka** — filtrowanie po nazwie serwisu w czasie rzeczywistym.
8. **Usuwanie kont** — z potwierdzeniem, żeby nikt nie usunął czegoś przez przypadek.
9. **Szyfrowanie AES-256** — cała baza szyfrowana, klucz derywowany z hasła głównego przez PBKDF2 (100k iteracji).
10. **Hasło główne** — przy każdym uruchomieniu trzeba podać hasło. Przy pierwszym uruchomieniu ustawia się automatycznie.
11. **Automatyczny zapis** — dane zapisywane do `passwords.dat` po każdej zmianie, wczytywane przy starcie.

---

## OOP

Projekt demonstruje 4 filary programowania obiektowego:

**Abstrakcja** — interfejs `IDataStorage` (kontrakt na zapis/odczyt), klasa abstrakcyjna `PasswordGeneratorBase` z metodą `abstract Generate()`.

**Enkapsulacja** — prywatne pola, dostęp przez właściwości, `GetAll()` zwraca kopię listy (nie oryginał).

**Dziedziczenie** — `ConfigurablePasswordGenerator` rozszerza `PasswordGeneratorBase`, współdzieli metodę `GetSecureRandomIndex()`.

**Polimorfizm** — `override Generate()` w klasie pochodnej, `IDataStorage` jako typ parametru w `AccountManager`.

Dodatkowo: SRP (logika i GUI w osobnych plikach), DIP (zależność od interfejsu), kompozycja zamiast dziedziczenia w `AccountManager`.

---

## Struktura

```
Hafcium/
├── Hafcium.csproj   # projekt .NET 8
├── Program.cs       # logika: modele, szyfrowanie, generator, menadżer kont, okno logowania
├── MainForm.cs      # GUI: Windows Forms
├── passwords.dat    # zaszyfrowana baza (tworzy się przy pierwszym zapisie)
└── README.md
```

Dwa pliki `.cs` — zgodnie z wymaganiami.

---

## Uruchomienie

Potrzebny [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) i Windows 10/11.

```bash
git clone https://github.com/[login]/Hafcium.git
cd Hafcium
dotnet run
```

Albo w Visual Studio — otwórz `Hafcium.csproj` i F5.

Żeby zbudować jako samodzielny exe:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## Bezpieczeństwo

- AES-256-CBC
- PBKDF2 ze 100 000 iteracji SHA-256 (klucz derywowany z hasła głównego)
- `RandomNumberGenerator` — kryptograficznie bezpieczne losowanie
- Losowy IV przy każdym zapisie
- Dane wyłącznie lokalnie

---

## Autor

Adrian Zieliński — informatyka, MANS Warszawa, 79565
