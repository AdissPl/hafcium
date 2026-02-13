using System.Text.Json.Serialization;

namespace Hafcium.Models
{
    /// Reprezentuje pojedynczy wpis konta (serwis + login + hasło + notatka).
    /// Klasa stosuje enkapsulację — pola prywatne z publicznymi właściwościami,
    /// dzięki czemu walidacja danych odbywa się w jednym miejscu (SRP).
    public class AccountEntry
    {
        // ── Prywatne pola ──────────────────────────────────────────────
        private string _serviceName = string.Empty;
        private string _login       = string.Empty;
        private string _password    = string.Empty;
        private string _note        = string.Empty;

        // ── Właściwości publiczne z walidacją ──────────────────────────

        /// Unikalne ID wpisu generowane automatycznie przy tworzeniu.
        [JsonInclude]
        public Guid Id { get; private set; } = Guid.NewGuid();

        /// Nazwa serwisu, np. "GitHub", "Gmail".
        public string ServiceName
        {
            get => _serviceName;
            set => _serviceName = value?.Trim()
                   ?? throw new ArgumentNullException(nameof(value), "Nazwa serwisu nie może być null.");
        }

        /// Login lub adres e-mail powiązany z kontem.
        public string Login
        {
            get => _login;
            set => _login = value?.Trim()
                   ?? throw new ArgumentNullException(nameof(value), "Login nie może być null.");
        }

        /// Hasło — może być wstawione ręcznie lub z generatora.
        public string Password
        {
            get => _password;
            set => _password = value
                   ?? throw new ArgumentNullException(nameof(value), "Hasło nie może być null.");
        }

        /// Opcjonalna notatka użytkownika dotycząca konta.
        public string Note
        {
            get => _note;
            set => _note = value ?? string.Empty;
        }

        /// Data utworzenia wpisu — ustawiana raz w konstruktorze.
        [JsonInclude]
        public DateTime CreatedAt { get; private set; } = DateTime.Now;

        // ── Konstruktory ───────────────────────────────────────────────

        /// Konstruktor bezparametrowy wymagany przez serializer JSON.
        public AccountEntry() { }

        /// Tworzy nowy wpis konta z pełnymi danymi.
        public AccountEntry(string serviceName, string login, string password, string note = "")
        {
            ServiceName = serviceName;
            Login       = login;
            Password    = password;
            Note        = note;
        }

        // ── Nadpisanie ToString dla czytelnego debugowania ─────────────

        /// Zwraca skrótowy opis wpisu (bez hasła, dla bezpieczeństwa).
        public override string ToString()
            => $"[{ServiceName}] {Login} (utworzono: {CreatedAt:yyyy-MM-dd})";
    }
}
