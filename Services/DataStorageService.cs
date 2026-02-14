using System.Text.Json;
using Hafcium.Models;

namespace Hafcium.Services
{
    /// Abstrakcyjna klasa bazowa definiująca kontrakt zapisu/odczytu danych.
    /// Zastosowano abstrakcję, aby umożliwić łatwą wymianę sposobu
    /// persystencji (np. plik → baza danych) bez modyfikacji reszty kodu
    /// (zasada Open/Closed — OCP).
    public abstract class DataStorageBase
    {
        /// Zapisuje listę kont do trwałego magazynu.
        public abstract void Save(List<AccountEntry> accounts, string masterPassword);

        /// Wczytuje listę kont z trwałego magazynu.
        public abstract List<AccountEntry> Load(string masterPassword);

        /// Sprawdza, czy magazyn danych istnieje (plik/baza/etc.).
        public abstract bool StorageExists();
    }

    /// Konkretna implementacja persystencji opartej na zaszyfrowanym pliku lokalnym.
    /// Dziedziczy po <see cref="DataStorageBase"/> (dziedziczenie) i nadpisuje
    /// metody wirtualne (polimorfizm), realizując zapis/odczyt do pliku passwords.dat.
    /// Schemat działania:
    /// 1. Serializacja listy kont do JSON.
    /// 2. Szyfrowanie JSON-a za pomocą <see cref="EncryptionService"/>.
    /// 3. Zapis zaszyfrowanych bajtów do pliku.
    public class FileDataStorageService : DataStorageBase
    {
        // ── Zależności (kompozycja — 'has-a", nie dziedziczenie) ────────
        private readonly EncryptionService _encryptionService;
        private readonly string _filePath;

        /// Opcje serializera JSON — ładne formatowanie i polskie znaki.
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// Inicjalizuje serwis persystencji z serwisem szyfrowania i ścieżką do pliku.
        /// Domyślna ścieżka to 'passwords.dat" w katalogu obok exe.
        public FileDataStorageService(EncryptionService encryptionService, string? filePath = null)
        {
            _encryptionService = encryptionService
                ?? throw new ArgumentNullException(nameof(encryptionService));

            _filePath = filePath ?? Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "passwords.dat");
        }

        /// Ścieżka do pliku bazy danych (readonly — enkapsulacja).
        public string FilePath => _filePath;

        /// Zapisuje listę kont: serializacja → szyfrowanie → zapis binarny.
        /// Operacja jest atomowa — najpierw piszemy do pliku tymczasowego,
        /// a potem podmieniamy oryginalny, co chroni przed utratą danych
        /// w razie przerwania operacji (np. brak prądu).
        public override void Save(List<AccountEntry> accounts, string masterPassword)
        {
            if (accounts == null)
                throw new ArgumentNullException(nameof(accounts));
            if (string.IsNullOrEmpty(masterPassword))
                throw new ArgumentException("Hasło główne nie może być puste.", nameof(masterPassword));

            string json = JsonSerializer.Serialize(accounts, JsonOptions);
            byte[] encrypted = _encryptionService.Encrypt(json, masterPassword);

            // Zapis atomowy — plik tymczasowy + zamiana
            string tempPath = _filePath + ".tmp";
            File.WriteAllBytes(tempPath, encrypted);

            // Jeśli oryginalny plik istnieje, usuwamy go przed zamianą
            if (File.Exists(_filePath))
                File.Delete(_filePath);

            File.Move(tempPath, _filePath);
        }

        /// Wczytuje listę kont: odczyt binarny → deszyfrowanie → deserializacja.
        /// Zwraca pustą listę jeśli plik nie istnieje (pierwszy uruchomienie).
        public override List<AccountEntry> Load(string masterPassword)
        {
            if (!File.Exists(_filePath))
                return new List<AccountEntry>();

            if (string.IsNullOrEmpty(masterPassword))
                throw new ArgumentException("Hasło główne nie może być puste.", nameof(masterPassword));

            try
            {
                byte[] encrypted = File.ReadAllBytes(_filePath);
                string json = _encryptionService.Decrypt(encrypted, masterPassword);
                return JsonSerializer.Deserialize<List<AccountEntry>>(json, JsonOptions)
                       ?? new List<AccountEntry>();
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                throw new InvalidOperationException(
                    "Nie udało się odszyfrować bazy danych. Sprawdź hasło główne.");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Plik bazy danych jest uszkodzony: {ex.Message}");
            }
        }

        /// Sprawdza, czy plik bazy danych istnieje na dysku.
        public override bool StorageExists() => File.Exists(_filePath);
    }
}
