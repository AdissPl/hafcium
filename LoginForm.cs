namespace Hafcium
{
    /// Formularz logowania — wyświetlany przy starcie aplikacji.
    /// Użytkownik podaje hasło główne, które deszyfruje bazę danych.
    /// Jeśli plik bazy nie istnieje, użytkownik tworzy nowe hasło główne.
    /// Stosuje enkapsulację — hasło dostępne tylko przez właściwość readonly.
    public class LoginForm : Form
    {
        // ── Kontrolki UI ───────────────────────────────────────────────
        private readonly TextBox _txtPassword;
        private readonly TextBox _txtConfirm;
        private readonly Label _lblTitle;
        private readonly Label _lblInfo;
        private readonly Label _lblCont;
        private readonly Label _lblConfirm;
        private readonly Button _btnLogin;
        private readonly CheckBox _chkShowPassword;
        private readonly Panel _panelMain;

        /// Hasło główne wprowadzone przez użytkownika (readonly).
        public string MasterPassword { get; private set; } = string.Empty;

        /// Czy baza danych już istnieje (tryb logowania vs tworzenia).
        private readonly bool _isNewDatabase;

        public LoginForm(bool isNewDatabase)
        {
            _isNewDatabase = isNewDatabase;

            // ── Konfiguracja okna ──────────────────────────────────────
            Text = "Hafcium — Logowanie";
            Size = new Size(420, 340);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(30, 30, 46);

            // ── Panel główny ───────────────────────────────────────────
            _panelMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30)
            };

            // ── Tytuł ──────────────────────────────────────────────────
            _lblTitle = new Label
            {
                Text = "🔐 HAFCIUM",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.FromArgb(137, 180, 250),
                AutoSize = true,
                Location = new Point(100, 20),
                BackColor = Color.Transparent
            };

            // ── Informacja ─────────────────────────────────────────────
            _lblInfo = new Label
            {
                Text = isNewDatabase
                    ? "Witaj! Ustaw hasło główne do bazy danych:"
                    : "Podaj hasło główne, aby odblokować bazę:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(205, 214, 244),
                AutoSize = true,
                Location = new Point(30, 65),
                BackColor = Color.Transparent
            };

            // ── Pole hasła ─────────────────────────────────────────────
            _txtPassword = new TextBox
            {
                Location = new Point(30, 95),
                Size = new Size(340, 30),
                Font = new Font("Segoe UI", 12),
                UseSystemPasswordChar = true,
                BackColor = Color.FromArgb(49, 50, 68),
                ForeColor = Color.FromArgb(205, 214, 244),
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) OnLoginClick(s!, e);
            };

            // ── Potwierdzenie hasła (tylko dla nowej bazy) ─────────────
            _lblConfirm = new Label
            {
                Text = "Powtórz hasło:",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(205, 214, 244),
                AutoSize = true,
                Location = new Point(30, 135),
                Visible = isNewDatabase,
                BackColor = Color.Transparent
            };

            _txtConfirm = new TextBox
            {
                Location = new Point(30, 158),
                Size = new Size(340, 30),
                Font = new Font("Segoe UI", 12),
                UseSystemPasswordChar = true,
                BackColor = Color.FromArgb(49, 50, 68),
                ForeColor = Color.FromArgb(205, 214, 244),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = isNewDatabase
            };
            _txtConfirm.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) OnLoginClick(s!, e);
            };

            // ── Checkbox pokaż hasło ───────────────────────────────────
            _chkShowPassword = new CheckBox
            {
                Text = "Pokaż hasło",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(166, 173, 200),
                AutoSize = true,
                Location = new Point(30, isNewDatabase ? 195 : 135),
                BackColor = Color.Transparent
            };
            _chkShowPassword.CheckedChanged += (s, e) =>
            {
                _txtPassword.UseSystemPasswordChar = !_chkShowPassword.Checked;
                _txtConfirm.UseSystemPasswordChar = !_chkShowPassword.Checked;
            };

            // ── Przycisk logowania ─────────────────────────────────────
            _btnLogin = new Button
            {
                Text = isNewDatabase ? "Utwórz bazę danych" : "Odblokuj",
                Location = new Point(30, isNewDatabase ? 230 : 170),
                Size = new Size(340, 40),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(137, 180, 250),
                ForeColor = Color.FromArgb(30, 30, 46),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnLogin.FlatAppearance.BorderSize = 0;
            _btnLogin.Click += OnLoginClick;

            // ── Dodanie kontrolek ──────────────────────────────────────
            Controls.AddRange(new Control[]
            {
                _lblTitle, _lblInfo, _lblCont, _txtPassword, _lblConfirm, _txtConfirm, _chkShowPassword, _btnLogin
                
            });
        }

        /// Obsługa kliknięcia przycisku logowania — walidacja hasła.
        private void OnLoginClick(object sender, EventArgs e)
        {
            string password = _txtPassword.Text;

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Hasło nie może być puste.");
                return;
            }

            if (password.Length < 4)
            {
                ShowError("Hasło główne musi mieć co najmniej 4 znaki.");
                return;
            }

            if (_isNewDatabase)
            {
                if (password != _txtConfirm.Text)
                {
                    ShowError("Hasła nie są identyczne.");
                    return;
                }
            }

            MasterPassword = password;
            DialogResult = DialogResult.OK;
            Close();
        }

        /// Wyświetla komunikat błędu w stylu ciemnego motywu.
        private void ShowError(string message)
        {
            MessageBox.Show(message, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// Ustawienie focusu na pole hasła przy otwarciu formularza.
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _txtPassword.Focus();
        }
    }
}
