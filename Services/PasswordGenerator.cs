using System.Security.Cryptography;
using System.Text;

namespace Hafcium.Services
{
    /// <summary>
    /// Odpowiada wyłącznie za generowanie haseł (zasada SRP).
    /// Używa kryptograficznie bezpiecznego generatora liczb losowych
    /// (<see cref="RandomNumberGenerator"/>) zamiast <see cref="Random"/>,
    /// co eliminuje przewidywalność sekwencji.
    /// </summary>
    public class PasswordGenerator
    {
        // ── Stałe zestawy znaków ──────────────────────────────────────
        private const string LowercaseChars  = "abcdefghijklmnopqrstuvwxyz";
        private const string UppercaseChars  = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string DigitChars      = "0123456789";
        private const string SpecialChars    = "!@#$%^&*()-_=+[]{}|;:',.<>?/~`";

        /// <summary>
        /// Generuje hasło o zadanej długości z wybranych zestawów znaków.
        /// Gwarantuje, że co najmniej jeden znak z każdego wybranego zestawu
        /// będzie obecny w wygenerowanym haśle (wymóg wielu polityk bezpieczeństwa).
        /// </summary>
        /// <param name="length">Długość hasła (minimum 4).</param>
        /// <param name="useLowercase">Czy uwzględnić małe litery.</param>
        /// <param name="useUppercase">Czy uwzględnić wielkie litery.</param>
        /// <param name="useDigits">Czy uwzględnić cyfry.</param>
        /// <param name="useSpecial">Czy uwzględnić znaki specjalne.</param>
        /// <returns>Wygenerowane hasło jako <see cref="string"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Rzucany gdy długość &lt; 4 lub nie wybrano żadnego zestawu znaków.
        /// </exception>
        public string Generate(int length, bool useLowercase, bool useUppercase,
                               bool useDigits, bool useSpecial)
        {
            // ── Walidacja danych wejściowych ───────────────────────────
            if (length < 4)
                throw new ArgumentException("Minimalna długość hasła to 4 znaki.", nameof(length));

            // Budowanie puli znaków na podstawie wyboru użytkownika
            var charPool = new StringBuilder();
            var requiredSets = new List<string>();

            if (useLowercase)  { charPool.Append(LowercaseChars);  requiredSets.Add(LowercaseChars); }
            if (useUppercase)  { charPool.Append(UppercaseChars);   requiredSets.Add(UppercaseChars); }
            if (useDigits)     { charPool.Append(DigitChars);       requiredSets.Add(DigitChars); }
            if (useSpecial)    { charPool.Append(SpecialChars);     requiredSets.Add(SpecialChars); }

            if (charPool.Length == 0)
                throw new ArgumentException("Należy wybrać co najmniej jeden zestaw znaków.");

            string pool = charPool.ToString();

            // ── Generowanie hasła ──────────────────────────────────────
            // Krok 1: Losujemy po jednym znaku z każdego wybranego zestawu,
            //         aby spełnić wymaganie 'minimum 1 znak z każdego rodzaju".
            var passwordChars = new char[length];
            int index = 0;

            foreach (var set in requiredSets)
            {
                passwordChars[index++] = set[RandomNumberGenerator.GetInt32(set.Length)];
            }

            // Krok 2: Resztę pozycji wypełniamy losowo z pełnej puli.
            for (int i = index; i < length; i++)
            {
                passwordChars[i] = pool[RandomNumberGenerator.GetInt32(pool.Length)];
            }

            // Krok 3: Tasujemy tablicę (Fisher-Yates), aby wymuszone znaki
            //         nie zawsze trafiały na początek hasła.
            Shuffle(passwordChars);

            return new string(passwordChars);
        }

        /// <summary>
        /// Oblicza przybliżoną entropię hasła w bitach.
        /// Entropia = log2(rozmiar_puli ^ długość).
        /// Wynik służy do wizualnego wskaźnika siły hasła w UI.
        /// </summary>
        public double CalculateEntropy(int length, bool lower, bool upper, bool digits, bool special)
        {
            int poolSize = 0;
            if (lower)   poolSize += LowercaseChars.Length;
            if (upper)   poolSize += UppercaseChars.Length;
            if (digits)  poolSize += DigitChars.Length;
            if (special) poolSize += SpecialChars.Length;

            return poolSize > 0 ? length * Math.Log2(poolSize) : 0;
        }

        /// <summary>
        /// Algorytm tasowania Fisher-Yates z kryptograficznie bezpiecznym RNG.
        /// Zapewnia równomierny rozkład permutacji.
        /// </summary>
        private static void Shuffle(char[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }
}
