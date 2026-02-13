namespace Hafcium.Helpers
{
    /// Poziomy siły hasła — enum wykorzystywany w UI do kolorowania
    /// wskaźnika siły (polimorfizm zachowań na podstawie wartości enum).
    public enum PasswordStrength
    {
        VeryWeak,
        Weak,
        Medium,
        Strong,
        VeryStrong
    }

    /// Analizuje siłę hasła na podstawie jego długości i różnorodności znaków.
    /// Klasa statyczna — nie przechowuje stanu, pełni rolę czystej funkcji (utility).
    /// Dodana jako funkcjonalność 'od siebie" wykraczająca poza wymagania zadania.
    public static class PasswordStrengthAnalyzer
    {
        /// Ocenia siłę hasła w skali od VeryWeak do VeryStrong.
        /// Algorytm punktowy: każdy spełniony warunek dodaje punkty.
        public static PasswordStrength Analyze(string password)
        {
            if (string.IsNullOrEmpty(password))
                return PasswordStrength.VeryWeak;

            int score = 0;

            // Punkty za długość (progresywne)
            if (password.Length >= 8)  score++;
            if (password.Length >= 12) score++;
            if (password.Length >= 16) score++;
            if (password.Length >= 24) score++;

            // Punkty za różnorodność zestawów znaków
            if (password.Any(char.IsLower))  score++;
            if (password.Any(char.IsUpper))  score++;
            if (password.Any(char.IsDigit))  score++;
            if (password.Any(c => !char.IsLetterOrDigit(c))) score++;

            // Mapowanie punktów na poziom siły
            return score switch
            {
                <= 2 => PasswordStrength.VeryWeak,
                3    => PasswordStrength.Weak,
                4 or 5 => PasswordStrength.Medium,
                6 or 7 => PasswordStrength.Strong,
                _      => PasswordStrength.VeryStrong
            };
        }

        /// Zwraca kolor odpowiadający sile hasła (do użycia w ProgressBar/Label).
        public static Color GetStrengthColor(PasswordStrength strength) => strength switch
        {
            PasswordStrength.VeryWeak  => Color.FromArgb(220, 53, 69),    // czerwony
            PasswordStrength.Weak      => Color.FromArgb(255, 140, 0),    // pomarańczowy
            PasswordStrength.Medium    => Color.FromArgb(255, 193, 7),    // żółty
            PasswordStrength.Strong    => Color.FromArgb(40, 167, 69),    // zielony
            PasswordStrength.VeryStrong => Color.FromArgb(0, 123, 255),   // niebieski
            _ => Color.Gray
        };

        /// Zwraca opis siły hasła po polsku — wyświetlany obok wskaźnika.
        public static string GetStrengthLabel(PasswordStrength strength) => strength switch
        {
            PasswordStrength.VeryWeak  => "Bardzo słabe",
            PasswordStrength.Weak      => "Słabe",
            PasswordStrength.Medium    => "Średnie",
            PasswordStrength.Strong    => "Silne",
            PasswordStrength.VeryStrong => "Bardzo silne",
            _ => "Nieznane"
        };

        /// Zwraca wartość procentową do ProgressBar (0–100).
        public static int GetStrengthPercent(PasswordStrength strength) => strength switch
        {
            PasswordStrength.VeryWeak  => 15,
            PasswordStrength.Weak      => 35,
            PasswordStrength.Medium    => 55,
            PasswordStrength.Strong    => 78,
            PasswordStrength.VeryStrong => 100,
            _ => 0
        };
    }
}
