using System.Text.Json.Serialization;

namespace Hafcium.Models
{
    /// <summary>
    /// Reprezentuje pojedynczy wpis konta (serwis + login + hasło + notatka).
    /// Klasa stosuje enkapsulację — pola prywatne z publicznymi właściwościami,
    /// dzięki czemu walidacja danych odbywa się w jednym miejscu (SRP).
    /// </summary>
    public class AccountEntry
    {
        // ── Prywatne pola ──────────────────────────────────────────────
        private string _serviceName = string.Empty;
        private string _login       = string.Empty;
        private string _password    = string.Empty;
        private string _note        = string.Empty;

        // ── Właściwości publiczne z walidacją ──────────────────────────

        /// <summary>Unikalne ID wpisu generowane automatycznie przy tworzeniu.</summary>
        [JsonInclude]
        public Guid Id { get; private set; } = Guid.NewGuid();

        /// <summary>Nazwa serwisu, np. "GitHub", "Gmail".</summary>
        public string ServiceName
        {
            get => _serviceName;
            set => _serviceName = value?.Trim()
                   ?? throw new ArgumentNullException(nameof(value), "Nazwa serwisu nie może być null.");
        }

        /// <summary>Login lub adres e-mail powiązany z kontem.</summary>
        public string Login
        {
            get => _login;
            set => _login = value?.Trim()
                   ?? throw new ArgumentNullException(nameof(value), "Login nie może być null.");
        }

        /// <summary>Hasło — może być wstawione ręcznie lub z generatora.</summary>
        public string Password
        {
            get => _password;
            set => _password = value
                   ?? throw new ArgumentNullException(nameof(value), "Hasło nie może być null.");
        }

        /// <summary>Opcjonalna notatka użytkownika dotycząca konta.</summary>
        public string Note
        {
            get => _note;
            set => _note = value ?? string.Empty;
        }

        /// <summary>Data utworzenia wpisu — ustawiana raz w konstruktorze.</summary>
        [JsonInclude]
        public DateTime CreatedAt { get; private set; } = DateTime.Now;

        // ── Konstruktory ───────────────────────────────────────────────

        /// <summary>
        /// Konstruktor bezparametrowy wymagany przez serializer JSON.
        /// </summary>
        public AccountEntry() { }

        /// <summary>
        /// Tworzy nowy wpis konta z pełnymi danymi.
        /// </summary>
        public AccountEntry(string serviceName, string login, string password, string note = "")
        {
            ServiceName = serviceName;
            Login       = login;
            Password    = password;
            Note        = note;
        }

        // ── Nadpisanie ToString dla czytelnego debugowania ─────────────

        /// <summary>Zwraca skrótowy opis wpisu (bez hasła, dla bezpieczeństwa).</summary>
        public override string ToString()
            => $"[{ServiceName}] {Login} (utworzono: {CreatedAt:yyyy-MM-dd})";
    }
}
