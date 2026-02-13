using Hafcium.Services;

namespace Hafcium
{
    /// <summary>
    /// Punkt wejścia aplikacji Hafcium.
    /// Schemat uruchomienia:
    /// 1. Sprawdzenie, czy plik bazy danych istnieje.
    /// 2. Wyświetlenie LoginForm (tworzenie nowego hasła lub logowanie).
    /// 3. Uruchomienie MainForm z podanym hasłem głównym.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Konfiguracja aplikacji WinForms
            ApplicationConfiguration.Initialize();

            // Sprawdzamy, czy plik bazy danych już istnieje
            var encryptionService = new EncryptionService();
            var storageService = new FileDataStorageService(encryptionService);
            bool isNewDatabase = !storageService.StorageExists();

            // Wyświetlamy formularz logowania
            using var loginForm = new LoginForm(isNewDatabase);
            if (loginForm.ShowDialog() != DialogResult.OK)
            {
                // Użytkownik zamknął okno logowania — kończymy aplikację
                return;
            }

            string masterPassword = loginForm.MasterPassword;

            // Jeśli to nowa baza, tworzymy pusty plik zaszyfrowany,
            // aby kolejne uruchomienie wykryło istniejącą bazę.
            if (isNewDatabase)
            {
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
            }

            // Uruchamiamy główne okno aplikacji
            Application.Run(new MainForm(masterPassword));
        }
    }
}
