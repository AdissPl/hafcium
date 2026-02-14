using Hafcium.Services;

namespace Hafcium
{
    /// Punkt wejścia aplikacji Hafcium.
    /// Schemat uruchomienia:
    /// 1. Sprawdzenie, czy plik bazy danych istnieje.
    /// 2. Wyświetlenie LoginForm (tworzenie nowego hasła lub logowanie).
    /// 3. Weryfikacja hasła PRZED otwarciem MainForm — próba odszyfrowania.
    /// 4. Jeśli hasło złe — blokada dostępu, brak nadpisania danych.
    /// 5. Uruchomienie MainForm dopiero po pomyślnej weryfikacji.
    internal static class Program
    {
        /// Maksymalna liczba prób wpisania hasła głównego.</summary>
        private const int MaxAttempts = 3;

        [STAThread]
        static void Main()
        {
            // Konfiguracja aplikacji WinForms
            ApplicationConfiguration.Initialize();

            // Sprawdzamy, czy plik bazy danych już istnieje
            var encryptionService = new EncryptionService();
            var storageService = new FileDataStorageService(encryptionService);
            bool isNewDatabase = !storageService.StorageExists();

            // ── Ścieżka A: Tworzenie nowej bazy danych ──────────────────
            if (isNewDatabase)
            {
                using var loginForm = new LoginForm(isNewDatabase: true);
                if (loginForm.ShowDialog() != DialogResult.OK)
                    return;

                string masterPassword = loginForm.MasterPassword;

                try
                {
                    storageService.Save(new List<Models.AccountEntry>(), masterPassword);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Nie udało się utworzyć bazy danych: {ex.Message}",
                        "Błąd krytyczny",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                Application.Run(new MainForm(masterPassword));
                return;
            }

            // ── Ścieżka B: Logowanie do istniejącej bazy ────────────────
            // Użytkownik ma maksymalnie 3 próby wpisania hasła.
            // Przy złym haśle — komunikat i ponowna próba.
            // Po 3 nieudanych próbach — blokada i zamknięcie aplikacji.
            // Dane NIE są nigdy nadpisywane przy błędnym haśle.
            int attempts = 0;

            while (attempts < MaxAttempts)
            {
                using var loginForm = new LoginForm(isNewDatabase: false);
                if (loginForm.ShowDialog() != DialogResult.OK)
                    return; // Użytkownik zamknął okno — wychodzimy

                string masterPassword = loginForm.MasterPassword;
                attempts++;

                try
                {
                    // Próba odszyfrowania bazy — weryfikacja hasła
                    var accounts = storageService.Load(masterPassword);

                    // Hasło prawidłowe — uruchamiamy główne okno
                    Application.Run(new MainForm(masterPassword));
                    return;
                }
                catch (InvalidOperationException)
                {
                    int remaining = MaxAttempts - attempts;

                    if (remaining > 0)
                    {
                        MessageBox.Show(
                            $"Nieprawidłowe hasło główne!\n\n"
                            + $"Pozostało prób: {remaining}",
                            "Błąd logowania",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Nieprawidłowe hasło główne!\n\n"
                            + "Wykorzystano wszystkie 3 próby.\n"
                            + "Aplikacja zostanie zamknięta w celu ochrony danych.",
                            "Dostęp zablokowany",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
