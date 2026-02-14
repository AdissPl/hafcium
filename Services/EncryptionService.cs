using System.Security.Cryptography;
using System.Text;

namespace Hafcium.Services
{
    /// Serwis szyfrowania AES-256-CBC odpowiadający wyłącznie za operacje
    /// kryptograficzne (SRP). Klucz szyfrowania generowany jest z hasła
    /// użytkownika za pomocą PBKDF2 (RFC 2898), co zapobiega atakom słownikowym.
    public class EncryptionService
    {
        // ── Stałe kryptograficzne ──────────────────────────────────────
        /// Liczba iteracji PBKDF2 — wyższe wartości spowalniają brute-force.
        private const int Iterations = 100_000;

        /// Rozmiar klucza AES-256 w bajtach.
        private const int KeySize = 32;

        /// Rozmiar wektora inicjalizacyjnego (IV) dla AES-CBC.
        private const int IvSize = 16;

        /// Rozmiar soli — 16 bajtów wystarczających do unikalności.
        private const int SaltSize = 16;

        /// Szyfruje dane tekstowe algorytmem AES-256-CBC.
        /// Format wyjściowy: [sól 16B][IV 16B][zaszyfrowane dane...]
        /// Sól i IV dołączane na początku umożliwiają deszyfrowanie
        /// bez konieczności przechowywania ich osobno.
        public byte[] Encrypt(string plainText, string masterPassword)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentException("Tekst do zaszyfrowania nie może być pusty.", nameof(plainText));

            // Generujemy losową sól dla każdej operacji szyfrowania,
            // dzięki czemu ten sam tekst z tym samym hasłem daje różne szyfrogramy.
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            // Derywacja klucza z hasła użytkownika — PBKDF2 z SHA256
            using var deriveBytes = new Rfc2898DeriveBytes(
                masterPassword, salt, Iterations, HashAlgorithmName.SHA256);

            byte[] key = deriveBytes.GetBytes(KeySize);

            using var aes = Aes.Create();
            aes.Key     = key;
            aes.Mode    = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes     = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Łączymy: sól + IV + zaszyfrowane dane w jeden ciągły blok
            byte[] result = new byte[SaltSize + IvSize + encryptedBytes.Length];
            Buffer.BlockCopy(salt,           0, result, 0,                        SaltSize);
            Buffer.BlockCopy(aes.IV,         0, result, SaltSize,                 IvSize);
            Buffer.BlockCopy(encryptedBytes, 0, result, SaltSize + IvSize,        encryptedBytes.Length);

            return result;
        }
        public string Decrypt(byte[] encryptedData, string masterPassword)
        {
            if (encryptedData == null || encryptedData.Length < SaltSize + IvSize + 1)
                throw new ArgumentException("Dane zaszyfrowane są zbyt krótkie lub null.", nameof(encryptedData));

            // Wyodrębniamy sól i IV z początku danych
            byte[] salt = new byte[SaltSize];
            byte[] iv   = new byte[IvSize];
            Buffer.BlockCopy(encryptedData, 0,        salt, 0, SaltSize);
            Buffer.BlockCopy(encryptedData, SaltSize, iv,   0, IvSize);

            int cipherLength = encryptedData.Length - SaltSize - IvSize;
            byte[] cipherBytes = new byte[cipherLength];
            Buffer.BlockCopy(encryptedData, SaltSize + IvSize, cipherBytes, 0, cipherLength);

            // Derywacja klucza z tą samą solą — musi dać identyczny klucz
            using var deriveBytes = new Rfc2898DeriveBytes(
                masterPassword, salt, Iterations, HashAlgorithmName.SHA256);

            byte[] key = deriveBytes.GetBytes(KeySize);

            using var aes = Aes.Create();
            aes.Key     = key;
            aes.IV      = iv;
            aes.Mode    = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
