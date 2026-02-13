using Hafcium.Helpers;
using Hafcium.Models;
using Hafcium.Services;

namespace Hafcium
{

    /// Główne okno aplikacji Hafcium. Łączy wszystkie serwisy (kompozycja)
    /// i obsługuje interakcje użytkownika. Każda operacja biznesowa delegowana
    /// jest do wyspecjalizowanego serwisu (SRP), a formularz odpowiada
    /// wyłącznie za prezentację i obsługę zdarzeń (wzorzec 'thin controller').

    public class MainForm : Form
    {
        // ════════════════════════════════════════════════════════════════
        //  SERWISY (kompozycja — główny formularz 'posiada' serwisy)
        // ════════════════════════════════════════════════════════════════
        private readonly PasswordGenerator       _passwordGenerator;
        private readonly EncryptionService        _encryptionService;
        private readonly FileDataStorageService   _storageService;
        private readonly string                   _masterPassword;

        // ════════════════════════════════════════════════════════════════
        //  DANE
        // ════════════════════════════════════════════════════════════════
        private List<AccountEntry> _accounts = new();

        // ════════════════════════════════════════════════════════════════
        //  KONTROLKI UI — generator haseł
        // ════════════════════════════════════════════════════════════════
        private NumericUpDown _nudLength     = null!;
        private CheckBox _chkLower           = null!;
        private CheckBox _chkUpper           = null!;
        private CheckBox _chkDigits          = null!;
        private CheckBox _chkSpecial         = null!;
        private TextBox _txtGeneratedPassword = null!;
        private Button _btnGenerate          = null!;
        private Button _btnCopyGenerated     = null!;
        private ProgressBar _barStrength     = null!;
        private Label _lblStrength           = null!;

        // ════════════════════════════════════════════════════════════════
        //  KONTROLKI UI — dodawanie konta
        // ════════════════════════════════════════════════════════════════
        private TextBox _txtServiceName      = null!;
        private TextBox _txtLogin            = null!;
        private TextBox _txtAccountPassword  = null!;
        private TextBox _txtNote             = null!;
        private Button _btnInsertFromGen     = null!;
        private Button _btnAddAccount        = null!;

        // ════════════════════════════════════════════════════════════════
        //  KONTROLKI UI — tabela i wyszukiwarka
        // ════════════════════════════════════════════════════════════════
        private DataGridView _gridAccounts   = null!;
        private TextBox _txtSearch           = null!;
        private Button _btnDelete            = null!;
        private CheckBox _chkShowPasswords   = null!;
        private Label _lblStatus             = null!;

        // ════════════════════════════════════════════════════════════════
        //  KONSTRUKTOR
        // ════════════════════════════════════════════════════════════════
        /// Inicjalizuje formularz, tworzy instancje serwisów (Dependency Injection
        /// przez konstruktor) i ładuje dane z zaszyfrowanego pliku.

        public MainForm(string masterPassword)
        {
            _masterPassword    = masterPassword;
            _passwordGenerator = new PasswordGenerator();
            _encryptionService = new EncryptionService();
            _storageService    = new FileDataStorageService(_encryptionService);

            InitializeUI();
            LoadAccounts();
        }

        // ════════════════════════════════════════════════════════════════
        //  INICJALIZACJA UI
        // ════════════════════════════════════════════════════════════════
        /// Buduje cały interfejs użytkownika programowo (bez Designera).
        /// Podział na sekcje: generator haseł | dodawanie konta | tabela kont.

        private void InitializeUI()
        {
            // ── Konfiguracja okna głównego ─────────────────────────────
            Text            = "Hafcium — Generator haseł i menadżer kont";
            Size            = new Size(1050, 750);
            MinimumSize     = new Size(900, 650);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = Color.FromArgb(30, 30, 46);
            Font            = new Font("Segoe UI", 9);

            // ── Panel lewy: generator + dodawanie ──────────────────────
            var panelLeft = new Panel
            {
                Location  = new Point(10, 10),
                Size      = new Size(360, 690),
                BackColor = Color.FromArgb(36, 36, 54),
                Padding   = new Padding(12),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom
            };

            BuildGeneratorSection(panelLeft);
            BuildAddAccountSection(panelLeft);

            // ── Panel prawy: tabela kont ───────────────────────────────
            var panelRight = new Panel
            {
                Location  = new Point(380, 10),
                Size      = new Size(645, 690),
                BackColor = Color.FromArgb(36, 36, 54),
                Padding   = new Padding(12),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            BuildAccountTableSection(panelRight);

            Controls.Add(panelLeft);
            Controls.Add(panelRight);
        }

        // ────────────────────────────────────────────────────────────────
        //  SEKCJA: Generator haseł
        // ────────────────────────────────────────────────────────────────
        private void BuildGeneratorSection(Panel parent)
        {
            int y = 8;

            // Nagłówek sekcji
            parent.Controls.Add(CreateSectionLabel("🔑 Generator haseł", y));
            y += 30;

            // Długość hasła
            parent.Controls.Add(CreateLabel("Długość hasła:", y));
            _nudLength = new NumericUpDown
            {
                Location  = new Point(140, y - 2),
                Size      = new Size(80, 25),
                Minimum   = 4,
                Maximum   = 128,
                Value     = 16,
                BackColor = Color.FromArgb(49, 50, 68),
                ForeColor = Color.FromArgb(205, 214, 244),
                Font      = new Font("Segoe UI", 10)
            };
            _nudLength.ValueChanged += (s, e) => UpdateStrengthIndicator();
            parent.Controls.Add(_nudLength);
            y += 32;

            // Checkboxy zestawów znaków
            _chkLower   = CreateCheckBox("Małe litery (a-z)",     y, true);  parent.Controls.Add(_chkLower);   y += 26;
            _chkUpper   = CreateCheckBox("Wielkie litery (A-Z)",  y, true);  parent.Controls.Add(_chkUpper);   y += 26;
            _chkDigits  = CreateCheckBox("Cyfry (0-9)",           y, true);  parent.Controls.Add(_chkDigits);  y += 26;
            _chkSpecial = CreateCheckBox("Znaki specjalne (!@#)", y, true);  parent.Controls.Add(_chkSpecial); y += 32;

            foreach (var chk in new[] { _chkLower, _chkUpper, _chkDigits, _chkSpecial })
                chk.CheckedChanged += (s, e) => UpdateStrengthIndicator();

            // Przycisk generowania
            _btnGenerate = CreateButton("⚡ Generuj hasło", y, 200, Color.FromArgb(137, 180, 250));
            _btnGenerate.Click += OnGenerateClick;
            parent.Controls.Add(_btnGenerate);
            y += 42;

            // Pole wygenerowanego hasła
            _txtGeneratedPassword = new TextBox
            {
                Location  = new Point(12, y),
                Size      = new Size(258, 28),
                Font      = new Font("Consolas", 11),
                ReadOnly  = true,
                BackColor = Color.FromArgb(49, 50, 68),
                ForeColor = Color.FromArgb(166, 227, 161),
                BorderStyle = BorderStyle.FixedSingle
            };
            parent.Controls.Add(_txtGeneratedPassword);

            // Przycisk kopiowania
            _btnCopyGenerated = new Button
            {
                Text      = "📋",
                Location  = new Point(275, y - 1),
                Size      = new Size(60, 28),
                BackColor = Color.FromArgb(69, 71, 90),
                ForeColor = Color.FromArgb(205, 214, 244),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                Font      = new Font("Segoe UI", 10)
            };
            _btnCopyGenerated.FlatAppearance.BorderSize = 0;
            _btnCopyGenerated.Click += OnCopyGeneratedClick;
            parent.Controls.Add(_btnCopyGenerated);
            y += 35;

            // Wskaźnik siły hasła
            _barStrength = new ProgressBar
            {
                Location = new Point(12, y),
                Size     = new Size(260, 14),
                Style    = ProgressBarStyle.Continuous,
                Value    = 0
            };
            parent.Controls.Add(_barStrength);

            _lblStrength = new Label
            {
                Location  = new Point(278, y - 2),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                Text      = "",
                BackColor = Color.Transparent
            };
            parent.Controls.Add(_lblStrength);

            UpdateStrengthIndicator();
        }

        // ────────────────────────────────────────────────────────────────
        //  SEKCJA: Dodawanie nowego konta
        // ────────────────────────────────────────────────────────────────
        private void BuildAddAccountSection(Panel parent)
        {
            int y = 340;

            parent.Controls.Add(CreateSectionLabel("➕ Dodaj konto", y));
            y += 30;

            // Nazwa serwisu
            parent.Controls.Add(CreateLabel("Serwis:", y + 2));
            _txtServiceName = CreateTextInput(y, 90); parent.Controls.Add(_txtServiceName);
            y += 32;

            // Login
            parent.Controls.Add(CreateLabel("Login / e-mail:", y + 2));
            _txtLogin = CreateTextInput(y, 120); parent.Controls.Add(_txtLogin);
            y += 32;

            // Hasło + przycisk wstawiania z generatora
            parent.Controls.Add(CreateLabel("Hasło:", y + 2));
            _txtAccountPassword = new TextBox
            {
                Location  = new Point(120, y),
                Size      = new Size(152, 25),
                Font      = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(49, 50, 68),
                ForeColor = Color.FromArgb(205, 214, 244),
                BorderStyle = BorderStyle.FixedSingle
            };
            parent.Controls.Add(_txtAccountPassword);

            _btnInsertFromGen = new Button
            {
                Text      = "← Gen",
                Location  = new Point(277, y - 1),
                Size      = new Size(58, 26),
                BackColor = Color.FromArgb(69, 71, 90),
                ForeColor = Color.FromArgb(205, 214, 244),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8),
                Cursor    = Cursors.Hand
            };
            _btnInsertFromGen.FlatAppearance.BorderSize = 0;
            _btnInsertFromGen.Click += OnInsertFromGeneratorClick;
            parent.Controls.Add(_btnInsertFromGen);
            y += 32;

            // Notatka
            parent.Controls.Add(CreateLabel("Notatka:", y + 2));
            _txtNote = CreateTextInput(y, 90); parent.Controls.Add(_txtNote);
            y += 38;

            // Przycisk dodawania
            _btnAddAccount = CreateButton("✅ Dodaj konto", y, 320, Color.FromArgb(166, 227, 161));
            _btnAddAccount.ForeColor = Color.FromArgb(30, 30, 46);
            _btnAddAccount.Click += OnAddAccountClick;
            parent.Controls.Add(_btnAddAccount);
        }

        // ────────────────────────────────────────────────────────────────
        //  SEKCJA: Tabela kont
        // ────────────────────────────────────────────────────────────────
        private void BuildAccountTableSection(Panel parent)
        {
            int y = 8;

            parent.Controls.Add(CreateSectionLabel("📋 Zapisane konta", y));
            y += 30;

            // Wyszukiwarka na żywo
            parent.Controls.Add(new Label
            {
                Text      = "🔍",
                Location  = new Point(12, y + 3),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(166, 173, 200),
                BackColor = Color.Transparent
            });

            _txtSearch = new TextBox
            {
                Location    = new Point(36, y),
                Size        = new Size(240, 25),
                Font        = new Font("Segoe UI", 10),
                BackColor   = Color.FromArgb(49, 50, 68),
                ForeColor   = Color.FromArgb(205, 214, 244),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Szukaj po nazwie serwisu..."
            };
            _txtSearch.TextChanged += OnSearchTextChanged;
            parent.Controls.Add(_txtSearch);

            // Checkbox ukrywania haseł
            _chkShowPasswords = new CheckBox
            {
                Text      = "Pokaż hasła",
                Location  = new Point(290, y + 2),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(166, 173, 200),
                BackColor = Color.Transparent
            };
            _chkShowPasswords.CheckedChanged += (s, e) => RefreshGrid();
            parent.Controls.Add(_chkShowPasswords);

            // Przycisk usuwania
            _btnDelete = CreateButton("🗑 Usuń zaznaczone", y - 2, 150, Color.FromArgb(220, 53, 69));
            _btnDelete.Location = new Point(420, y - 2);
            _btnDelete.Size     = new Size(190, 28);
            _btnDelete.Click   += OnDeleteClick;
            parent.Controls.Add(_btnDelete);
            y += 35;

            // Tabela DataGridView
            _gridAccounts = new DataGridView
            {
                Location            = new Point(12, y),
                Size                = new Size(610, 580),
                Anchor              = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackgroundColor     = Color.FromArgb(30, 30, 46),
                ForeColor           = Color.FromArgb(205, 214, 244),
                GridColor           = Color.FromArgb(69, 71, 90),
                BorderStyle         = BorderStyle.None,
                CellBorderStyle     = DataGridViewCellBorderStyle.SingleHorizontal,
                RowHeadersVisible   = false,
                AllowUserToAddRows  = false,
                AllowUserToDeleteRows = false,
                ReadOnly            = true,
                SelectionMode       = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect         = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font                = new Font("Segoe UI", 9),
                ColumnHeadersHeight = 36,
                RowTemplate         = { Height = 32 }
            };

            // Styl nagłówków
            _gridAccounts.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(49, 50, 68),
                ForeColor = Color.FromArgb(137, 180, 250),
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                SelectionBackColor = Color.FromArgb(49, 50, 68),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            // Styl wierszy
            _gridAccounts.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor          = Color.FromArgb(36, 36, 54),
                ForeColor          = Color.FromArgb(205, 214, 244),
                SelectionBackColor = Color.FromArgb(69, 71, 90),
                SelectionForeColor = Color.FromArgb(205, 214, 244)
            };

            _gridAccounts.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor          = Color.FromArgb(42, 42, 62),
                SelectionBackColor = Color.FromArgb(69, 71, 90)
            };

            _gridAccounts.EnableHeadersVisualStyles = false;

            // Kolumny
            _gridAccounts.Columns.Add("ServiceName", "Serwis");
            _gridAccounts.Columns.Add("Login",       "Login / E-mail");
            _gridAccounts.Columns.Add("Password",    "Hasło");
            _gridAccounts.Columns.Add("Note",        "Notatka");
            _gridAccounts.Columns.Add("CreatedAt",   "Data dodania");

            _gridAccounts.Columns["ServiceName"].FillWeight = 20;
            _gridAccounts.Columns["Login"].FillWeight       = 25;
            _gridAccounts.Columns["Password"].FillWeight    = 20;
            _gridAccounts.Columns["Note"].FillWeight        = 20;
            _gridAccounts.Columns["CreatedAt"].FillWeight   = 15;

            // Zdarzenie podwójnego kliknięcia — kopiowanie hasła do schowka
            _gridAccounts.CellDoubleClick += OnGridDoubleClick;

            parent.Controls.Add(_gridAccounts);

            // Pasek statusu
            _lblStatus = new Label
            {
                Location  = new Point(12, parent.Height - 30),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(108, 112, 134),
                Text      = "Gotowy.",
                BackColor = Color.Transparent,
                Anchor    = AnchorStyles.Left | AnchorStyles.Bottom
            };
            parent.Controls.Add(_lblStatus);
        }

        // ════════════════════════════════════════════════════════════════
        //  OBSŁUGA ZDARZEŃ
        // ════════════════════════════════════════════════════════════════
        /// Funkcjonalność 1: Generowanie hasła z wybranych zestawów znaków.
        /// Deleguje logikę do PasswordGenerator (SRP).

        private void OnGenerateClick(object? sender, EventArgs e)
        {
            try
            {
                bool anyChecked = _chkLower.Checked || _chkUpper.Checked
                               || _chkDigits.Checked || _chkSpecial.Checked;

                if (!anyChecked)
                {
                    ShowWarning("Zaznacz co najmniej jeden rodzaj znaków.");
                    return;
                }

                string password = _passwordGenerator.Generate(
                    (int)_nudLength.Value,
                    _chkLower.Checked, _chkUpper.Checked,
                    _chkDigits.Checked, _chkSpecial.Checked);

                _txtGeneratedPassword.Text = password;
                UpdateStrengthFromPassword(password);
                SetStatus($"Wygenerowano hasło o długości {password.Length} znaków.");
            }
            catch (ArgumentException ex)
            {
                ShowWarning(ex.Message);
            }
        }

        /// Funkcjonalność 2: Kopiowanie wygenerowanego hasła do schowka.
        private void OnCopyGeneratedClick(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_txtGeneratedPassword.Text))
            {
                ShowWarning("Najpierw wygeneruj hasło.");
                return;
            }

            Clipboard.SetText(_txtGeneratedPassword.Text);
            SetStatus("✅ Hasło skopiowane do schowka!");
            ShowInfo("Hasło zostało skopiowane do schowka.");
        }

        /// Funkcjonalność 3 (część): Wstawianie hasła z generatora do pola konta.
        private void OnInsertFromGeneratorClick(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_txtGeneratedPassword.Text))
            {
                ShowWarning("Najpierw wygeneruj hasło.");
                return;
            }

            _txtAccountPassword.Text = _txtGeneratedPassword.Text;
            SetStatus("Hasło wstawione z generatora.");
        }

        /// Funkcjonalność 3: Dodawanie nowego wpisu konta.
        /// Waliduje dane, tworzy obiekt AccountEntry i zapisuje do pliku.
        private void OnAddAccountClick(object? sender, EventArgs e)
        {
            // Walidacja — sprawdzamy wymagane pola
            if (string.IsNullOrWhiteSpace(_txtServiceName.Text))
            {
                ShowWarning("Podaj nazwę serwisu."); return;
            }
            if (string.IsNullOrWhiteSpace(_txtLogin.Text))
            {
                ShowWarning("Podaj login lub e-mail."); return;
            }
            if (string.IsNullOrWhiteSpace(_txtAccountPassword.Text))
            {
                ShowWarning("Podaj hasło (lub wstaw z generatora)."); return;
            }

            try
            {
                var entry = new AccountEntry(
                    _txtServiceName.Text,
                    _txtLogin.Text,
                    _txtAccountPassword.Text,
                    _txtNote.Text);

                _accounts.Add(entry);
                SaveAccounts();   // Funkcjonalność 10: automatyczny zapis
                RefreshGrid();    // Funkcjonalność 4: odświeżenie tabeli

                // Czyszczenie pól po dodaniu
                _txtServiceName.Clear();
                _txtLogin.Clear();
                _txtAccountPassword.Clear();
                _txtNote.Clear();

                SetStatus($"✅ Dodano konto: {entry.ServiceName}");
            }
            catch (Exception ex)
            {
                ShowError($"Błąd podczas dodawania konta: {ex.Message}");
            }
        }

        /// Funkcjonalność 6: Podwójne kliknięcie na wiersz → kopiowanie hasła.
        private void OnGridDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Pobieramy indeks oryginalny z taga wiersza
            if (_gridAccounts.Rows[e.RowIndex].Tag is not int originalIndex) return;

            string password = _accounts[originalIndex].Password;
            Clipboard.SetText(password);
            SetStatus($"✅ Hasło do '{_accounts[originalIndex].ServiceName}' skopiowane do schowka!");
            ShowInfo($"Hasło do '{_accounts[originalIndex].ServiceName}' zostało skopiowane.");
        }

        /// Funkcjonalność 7: Wyszukiwarka na żywo — filtrowanie po nazwie serwisu.
        private void OnSearchTextChanged(object? sender, EventArgs e)
        {
            RefreshGrid();
        }

        /// Funkcjonalność 8: Usuwanie zaznaczonego konta z potwierdzeniem.
        private void OnDeleteClick(object? sender, EventArgs e)
        {
            if (_gridAccounts.SelectedRows.Count == 0)
            {
                ShowWarning("Zaznacz wiersz do usunięcia.");
                return;
            }

            var selectedRow = _gridAccounts.SelectedRows[0];
            if (selectedRow.Tag is not int originalIndex) return;

            string serviceName = _accounts[originalIndex].ServiceName;

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć konto '{serviceName}'?\nTej operacji nie można cofnąć.",
                "Potwierdzenie usunięcia",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _accounts.RemoveAt(originalIndex);
                SaveAccounts();
                RefreshGrid();
                SetStatus($"🗑 Usunięto konto: {serviceName}");
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  METODY POMOCNICZE
        // ════════════════════════════════════════════════════════════════
        /// Funkcjonalność 4 + 5: Odświeżanie tabeli z uwzględnieniem
        /// filtra wyszukiwania i ukrywania haseł.

        private void RefreshGrid()
        {
            _gridAccounts.Rows.Clear();
            string filter = _txtSearch.Text.Trim().ToLowerInvariant();

            for (int i = 0; i < _accounts.Count; i++)
            {
                var acc = _accounts[i];

                // Funkcjonalność 7: filtrowanie po nazwie serwisu
                if (!string.IsNullOrEmpty(filter) &&
                    !acc.ServiceName.ToLowerInvariant().Contains(filter))
                    continue;

                // Funkcjonalność 5: ukrywanie haseł w tabeli
                string displayPassword = _chkShowPasswords.Checked
                    ? acc.Password
                    : new string('•', Math.Min(acc.Password.Length, 16));

                int rowIdx = _gridAccounts.Rows.Add(
                    acc.ServiceName,
                    acc.Login,
                    displayPassword,
                    acc.Note,
                    acc.CreatedAt.ToString("yyyy-MM-dd HH:mm"));

                // Zapisujemy oryginalny indeks w Tag wiersza — potrzebny
                // do prawidłowego usuwania i kopiowania po filtrowaniu.
                _gridAccounts.Rows[rowIdx].Tag = i;
            }

            _lblStatus.Text = $"Kont: {_accounts.Count}" +
                (string.IsNullOrEmpty(filter) ? "" : $" (wyświetlono: {_gridAccounts.Rows.Count})");
        }

        /// Funkcjonalność 10: Automatyczny zapis przy każdej zmianie.
        private void SaveAccounts()
        {
            try
            {
                _storageService.Save(_accounts, _masterPassword);
            }
            catch (Exception ex)
            {
                ShowError($"Błąd zapisu: {ex.Message}");
            }
        }

        /// Funkcjonalność 10: Automatyczne wczytywanie danych przy starcie.
        private void LoadAccounts()
        {
            try
            {
                _accounts = _storageService.Load(_masterPassword);
                RefreshGrid();
                SetStatus($"Wczytano {_accounts.Count} kont z bazy danych.");
            }
            catch (InvalidOperationException ex)
            {
                ShowError(ex.Message);
                // Jeśli hasło nieprawidłowe — zamykamy aplikację
                Application.Exit();
            }
        }

        /// Aktualizuje wskaźnik siły na podstawie ustawień generatora
        /// (przed wygenerowaniem hasła — pokazuje potencjalną siłę).
        private void UpdateStrengthIndicator()
        {
            double entropy = _passwordGenerator.CalculateEntropy(
                (int)_nudLength.Value,
                _chkLower.Checked, _chkUpper.Checked,
                _chkDigits.Checked, _chkSpecial.Checked);

            int percent = Math.Min(100, (int)(entropy / 1.28));
            _barStrength.Value = percent;
            _lblStrength.Text  = $"{entropy:F0} bitów";
        }

        /// Aktualizuje wskaźnik siły na podstawie wygenerowanego hasła.
        /// Funkcjonalność dodatkowa — wizualny feedback.
        private void UpdateStrengthFromPassword(string password)
        {
            var strength = PasswordStrengthAnalyzer.Analyze(password);
            _barStrength.Value = PasswordStrengthAnalyzer.GetStrengthPercent(strength);
            _lblStrength.Text  = PasswordStrengthAnalyzer.GetStrengthLabel(strength);
            _lblStrength.ForeColor = PasswordStrengthAnalyzer.GetStrengthColor(strength);
        }

        // ════════════════════════════════════════════════════════════════
        //  FABRYKI KONTROLEK
        // ════════════════════════════════════════════════════════════════

        private Label CreateSectionLabel(string text, int y) => new()
        {
            Text      = text,
            Location  = new Point(12, y),
            AutoSize  = true,
            Font      = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.FromArgb(137, 180, 250),
            BackColor = Color.Transparent
        };

        private Label CreateLabel(string text, int y) => new()
        {
            Text      = text,
            Location  = new Point(12, y),
            AutoSize  = true,
            Font      = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(166, 173, 200),
            BackColor = Color.Transparent
        };

        private CheckBox CreateCheckBox(string text, int y, bool isChecked) => new()
        {
            Text      = text,
            Location  = new Point(12, y),
            AutoSize  = true,
            Checked   = isChecked,
            Font      = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(205, 214, 244),
            BackColor = Color.Transparent
        };

        private TextBox CreateTextInput(int y, int xOffset) => new()
        {
            Location    = new Point(xOffset, y),
            Size        = new Size(335 - xOffset, 25),
            Font        = new Font("Segoe UI", 10),
            BackColor   = Color.FromArgb(49, 50, 68),
            ForeColor   = Color.FromArgb(205, 214, 244),
            BorderStyle = BorderStyle.FixedSingle
        };

        private Button CreateButton(string text, int y, int width, Color bgColor) => new()
        {
            Text      = text,
            Location  = new Point(12, y),
            Size      = new Size(width, 34),
            Font      = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = bgColor,
            ForeColor = Color.FromArgb(30, 30, 46),
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand
        };

        // ════════════════════════════════════════════════════════════════
        //  KOMUNIKATY — metody pomocnicze dla czytelności
        // ════════════════════════════════════════════════════════════════

        private void SetStatus(string message) => _lblStatus.Text = message;

        private static void ShowWarning(string msg) =>
            MessageBox.Show(msg, "Uwaga", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private static void ShowError(string msg) =>
            MessageBox.Show(msg, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);

        private static void ShowInfo(string msg) =>
            MessageBox.Show(msg, "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

/*
 * ═══════════════════════════════════════════════════════════════════════════
 *  UZASADNIENIE STRUKTURY KLAS
 * ═══════════════════════════════════════════════════════════════════════════
 *
 * Zastosowałem zasadę pojedynczej odpowiedzialności (SRP) — każda klasa ma
 * jedno, jasno zdefiniowane zadanie: AccountEntry przechowuje dane,
 * PasswordGenerator generuje hasła, EncryptionService szyfruje, a
 * FileDataStorageService zarządza persystencją. Dzięki temu zmiana np.
 * algorytmu szyfrowania nie wymaga modyfikacji formularza.
 *
 * Relacje między obiektami oparto na kompozycji ('has-a'), nie dziedziczeniu —
 * MainForm posiada serwisy jako pola, co zgodne jest z zasadą 'Favor
 * composition over inheritance' (Gang of Four). Jedynym miejscem dziedziczenia
 * jest abstrakcyjna klasa DataStorageBase → FileDataStorageService, co realizuje
 * polimorfizm i zasadę Open/Closed (OCP) — dodanie nowego sposobu zapisu
 * (np. do SQLite) wymaga jedynie nowej klasy dziedziczącej po DataStorageBase.
 *
 * Enkapsulacja widoczna jest w klasie AccountEntry (prywatne pola + walidacja
 * w setterach) oraz w serwisach (stałe kryptograficzne jako private const).
 * Abstrakcja realizowana jest przez klasę bazową DataStorageBase, która ukrywa
 * szczegóły implementacyjne za interfejsem publicznym Save/Load/StorageExists.
 *
 * ═══════════════════════════════════════════════════════════════════════════
 */
