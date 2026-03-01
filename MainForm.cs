// ============================================================================
// MainForm.cs - Hafcium
// Adrian Zieliński, 79565
// Cały interfejs graficzny aplikacji - przyciski, tabela, wyszukiwarka.
// Tu nie ma żadnej logiki szyfrowania ani zarządzania danymi,
// to wszystko jest w Program.cs (podział według SRP).
// ============================================================================

namespace Hafcium;

// Główne okno aplikacji - dziedziczy po Form (WinForms).
// Realizuje wszystkie 10 funkcjonalności z listy w zadaniu.
public class MainForm : Form
{
    // ========================================================================
    // POLA - prywatne, żeby nikt z zewnątrz nie mieszał
    // ========================================================================

    // serwisy z logiki biznesowej (kompozycja - mamy je jako pola, nie dziedziczymy)
    private readonly AccountManager _manager;
    private readonly ConfigurablePasswordGenerator _generator;

    // czy hasła są widoczne w tabeli (domyślnie ukryte)
    private bool _passwordsVisible = false;

    // kontrolki generatora haseł
    private NumericUpDown _nudLength = null!;
    private CheckBox _chkLower = null!, _chkUpper = null!, _chkDigits = null!, _chkSpecial = null!;
    private TextBox _txtGenerated = null!;
    private Button _btnGenerate = null!, _btnCopyGenerated = null!;

    // kontrolki dodawania kont
    private TextBox _txtService = null!, _txtLogin = null!, _txtPassword = null!, _txtNote = null!;
    private Button _btnAdd = null!, _btnInsertGenerated = null!;

    // kontrolki tabeli i zarządzania
    private DataGridView _dgvAccounts = null!;
    private TextBox _txtSearch = null!;
    private Button _btnDelete = null!, _btnTogglePasswords = null!;
    private Label _lblStatus = null!;

    // ========================================================================
    // KONSTRUKTOR - przyjmuje hasło główne z okna logowania
    // ========================================================================

    public MainForm(string masterPassword)
    {
        // ścieżka do zaszyfrowanej bazy - obok pliku exe
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "passwords.dat");

        // tworzę instancje serwisów - hasło główne idzie do storage
        var storage = new EncryptedFileStorage(dbPath, masterPassword);
        _manager = new AccountManager(storage);
        _generator = new ConfigurablePasswordGenerator();

        // jak dane się zmienią to tabela ma się odświeżyć automatycznie
        _manager.DataChanged += () => RefreshTable(_txtSearch.Text);

        // buduję cały interfejs i pokazuję dane
        InitializeUI();
        RefreshTable(string.Empty);
    }

    // ========================================================================
    // BUDOWANIE GUI
    // ========================================================================

    private void InitializeUI()
    {
        // ustawienia okna
        Text = "Hafcium \u2013 Generator hase\u0142 i menad\u017cer kont";
        Size = new Size(1150, 720);
        MinimumSize = new Size(950, 620);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(30, 30, 46);
        ForeColor = Color.FromArgb(205, 214, 244);
        Font = new Font("Segoe UI", 9.5f);

        // ====== LEWY PANEL - generator + formularz ======
        var panelLeft = new Panel
        {
            Dock = DockStyle.Left,
            Width = 340,
            BackColor = Color.FromArgb(36, 36, 54),
            AutoScroll = true // na wypadek małego ekranu
        };

        int y = 20;
        int x = 18;
        int w = 290; // szerokość kontrolek

        // --- GENERATOR HASEŁ ---
        panelLeft.Controls.Add(MakeLabel("GENERATOR HASE\u0141", x, y, true));
        y += 34;

        panelLeft.Controls.Add(MakeLabel("D\u0142ugo\u015b\u0107:", x, y + 2));
        _nudLength = new NumericUpDown
        {
            Location = new Point(x + 90, y), Width = 80,
            Minimum = 4, Maximum = 128, Value = 16,
            BackColor = Color.FromArgb(49, 50, 68),
            ForeColor = Color.FromArgb(205, 214, 244)
        };
        panelLeft.Controls.Add(_nudLength);
        y += 38;

        _chkLower   = MakeCheckBox("Ma\u0142e litery (a-z)",       x, y, true); panelLeft.Controls.Add(_chkLower);   y += 28;
        _chkUpper   = MakeCheckBox("Wielkie litery (A-Z)",         x, y, true); panelLeft.Controls.Add(_chkUpper);   y += 28;
        _chkDigits  = MakeCheckBox("Cyfry (0-9)",                  x, y, true); panelLeft.Controls.Add(_chkDigits);  y += 28;
        _chkSpecial = MakeCheckBox("Znaki specjalne (!@#...)",     x, y, true); panelLeft.Controls.Add(_chkSpecial); y += 36;

        _btnGenerate = MakeButton("Generuj has\u0142o", x, y, w, Color.FromArgb(137, 180, 250));
        _btnGenerate.Click += OnGeneratePassword;
        panelLeft.Controls.Add(_btnGenerate);
        y += 44;

        _txtGenerated = new TextBox
        {
            Location = new Point(x, y), Width = w,
            ReadOnly = true, Font = new Font("Consolas", 11f),
            BackColor = Color.FromArgb(49, 50, 68),
            ForeColor = Color.FromArgb(166, 227, 161)
        };
        panelLeft.Controls.Add(_txtGenerated);
        y += 34;

        _btnCopyGenerated = MakeButton("Kopiuj do schowka", x, y, w, Color.FromArgb(116, 199, 236));
        _btnCopyGenerated.Click += OnCopyGenerated;
        panelLeft.Controls.Add(_btnCopyGenerated);
        y += 54;

        // --- DODAJ KONTO ---
        panelLeft.Controls.Add(MakeLabel("DODAJ KONTO", x, y, true));
        y += 34;

        panelLeft.Controls.Add(MakeLabel("Serwis:", x, y));
        _txtService = MakeTextBox(x, y + 22, w);
        panelLeft.Controls.Add(_txtService);
        y += 56;

        panelLeft.Controls.Add(MakeLabel("Login / E-mail:", x, y));
        _txtLogin = MakeTextBox(x, y + 22, w);
        panelLeft.Controls.Add(_txtLogin);
        y += 56;

        panelLeft.Controls.Add(MakeLabel("Has\u0142o:", x, y));
        _txtPassword = MakeTextBox(x, y + 22, w - 84);
        panelLeft.Controls.Add(_txtPassword);

        _btnInsertGenerated = MakeButton("Wstaw", x + w - 76, y + 20, 76, Color.FromArgb(249, 226, 175));
        _btnInsertGenerated.Click += (_, _) => _txtPassword.Text = _txtGenerated.Text;
        panelLeft.Controls.Add(_btnInsertGenerated);
        y += 56;

        panelLeft.Controls.Add(MakeLabel("Notatka (opcjonalna):", x, y));
        _txtNote = MakeTextBox(x, y + 22, w);
        panelLeft.Controls.Add(_txtNote);
        y += 56;

        _btnAdd = MakeButton("Dodaj konto", x, y, w, Color.FromArgb(166, 227, 161));
        _btnAdd.Click += OnAddAccount;
        panelLeft.Controls.Add(_btnAdd);

        Controls.Add(panelLeft);

        // ====== PRAWY PANEL - tabela ======
        // Używam zagnieżdżonych paneli zamiast samego Dock żeby mieć pełną kontrolę

        var panelRight = new Panel { Dock = DockStyle.Fill, Padding = new System.Windows.Forms.Padding(340, 0, 0, 0) };

        // -- GÓRNY PASEK: wyszukiwarka + przyciski --
        var topBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = Color.FromArgb(36, 36, 54), // ciemniejszy - wyraźnie oddzielony
            Padding = new Padding(16, 0, 16, 0)
        };

        var lblSearch = MakeLabel("Szukaj:", 16, 18);
        topBar.Controls.Add(lblSearch);

        _txtSearch = new TextBox
        {
            Location = new Point(80, 14), Width = 200, Height = 28,
            Font = new Font("Segoe UI", 10f),
            PlaceholderText = "nazwa serwisu...",
            BackColor = Color.FromArgb(49, 50, 68),
            ForeColor = Color.FromArgb(205, 214, 244),
            BorderStyle = BorderStyle.FixedSingle
        };
        _txtSearch.TextChanged += (_, _) => RefreshTable(_txtSearch.Text);
        topBar.Controls.Add(_txtSearch);

        _btnTogglePasswords = MakeButton("Poka\u017c has\u0142a", 300, 12, 130, Color.FromArgb(180, 190, 254));
        _btnTogglePasswords.Click += OnTogglePasswords;
        topBar.Controls.Add(_btnTogglePasswords);

        _btnDelete = MakeButton("Usu\u0144 zaznaczone", 444, 12, 150, Color.FromArgb(243, 139, 168));
        _btnDelete.Click += OnDeleteAccount;
        topBar.Controls.Add(_btnDelete);

        // -- DOLNY PASEK STATUSU --
        _lblStatus = new Label
        {
            Dock = DockStyle.Bottom, Height = 28,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(127, 132, 156),
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.FromArgb(36, 36, 54),
            Padding = new Padding(12, 0, 0, 0)
        };

        // -- TABELA --
        _dgvAccounts = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.FromArgb(30, 30, 46),
            GridColor = Color.FromArgb(49, 50, 68),
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,

            // KLUCZOWE - wyłączam styl systemu żeby moje kolory działały
            EnableHeadersVisualStyles = false,

            // wymiary
            ColumnHeadersHeight = 40,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            RowTemplate = { Height = 36 },

            // NAGŁÓWKI - jasne na ciemnym tle, wyraźnie widoczne
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(55, 56, 78),
                ForeColor = Color.FromArgb(255, 255, 255), // biały tekst!
                Font = new Font("Segoe UI Semibold", 10f),
                SelectionBackColor = Color.FromArgb(55, 56, 78),
                SelectionForeColor = Color.FromArgb(255, 255, 255),
                Padding = new Padding(10, 0, 0, 0),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            },

            // WIERSZE
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(36, 36, 54),
                ForeColor = Color.FromArgb(205, 214, 244),
                SelectionBackColor = Color.FromArgb(59, 60, 82),
                SelectionForeColor = Color.FromArgb(255, 255, 255),
                Padding = new Padding(10, 0, 0, 0),
                Font = new Font("Segoe UI", 9.5f)
            },

            // co drugi wiersz
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(30, 30, 46),
                ForeColor = Color.FromArgb(205, 214, 244),
                SelectionBackColor = Color.FromArgb(59, 60, 82),
                SelectionForeColor = Color.FromArgb(255, 255, 255)
            }
        };

        // kolumny z konkretnymi proporcjami
        _dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Service", HeaderText = "Serwis", FillWeight = 20, MinimumWidth = 120 });
        _dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Login", HeaderText = "Login / E-mail", FillWeight = 30, MinimumWidth = 150 });
        _dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Password", HeaderText = "Hasło", FillWeight = 20, MinimumWidth = 100 });
        _dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Note", HeaderText = "Notatka", FillWeight = 15, MinimumWidth = 100 });
        _dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Created", HeaderText = "Dodano", Width = 130 });
        _dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", Visible = false });

        _dgvAccounts.CellDoubleClick += OnRowDoubleClick;

        panelRight.Controls.Add(_dgvAccounts);
        panelRight.Controls.Add(topBar);
        panelRight.Controls.Add(_lblStatus);
        topBar.BringToFront();
        _lblStatus.BringToFront();
        _dgvAccounts.SendToBack();
        Controls.Add(panelRight);

        UpdateStatus("Gotowy. Baza zawiera " + _manager.Count + " kont.");
    }

    // ========================================================================
    // OBSŁUGA ZDARZEŃ
    // ========================================================================

    // [1] generuje hasło z ustawionych opcji
    private void OnGeneratePassword(object? sender, EventArgs e)
    {
        try
        {
            _generator.UseLowercase = _chkLower.Checked;
            _generator.UseUppercase = _chkUpper.Checked;
            _generator.UseDigits = _chkDigits.Checked;
            _generator.UseSpecial = _chkSpecial.Checked;

            string password = _generator.Generate((int)_nudLength.Value);
            _txtGenerated.Text = password;
            UpdateStatus("Has\u0142o wygenerowane.");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    // [2] kopiuje wygenerowane hasło do schowka
    private void OnCopyGenerated(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_txtGenerated.Text))
        {
            ShowError("Najpierw wygeneruj has\u0142o!");
            return;
        }
        Clipboard.SetText(_txtGenerated.Text);
        UpdateStatus("\u2714 Has\u0142o skopiowane do schowka!");
    }

    // [3] dodaje nowe konto do bazy
    private void OnAddAccount(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtService.Text))
        {
            ShowError("Podaj nazw\u0119 serwisu!"); return;
        }
        if (string.IsNullOrWhiteSpace(_txtLogin.Text))
        {
            ShowError("Podaj login lub e-mail!"); return;
        }

        var entry = new AccountEntry
        {
            ServiceName = _txtService.Text.Trim(),
            Login = _txtLogin.Text.Trim(),
            Password = _txtPassword.Text,
            Note = _txtNote.Text.Trim()
        };

        _manager.Add(entry);

        // czyszczę formularz
        _txtService.Clear();
        _txtLogin.Clear();
        _txtPassword.Clear();
        _txtNote.Clear();

        UpdateStatus($"\u2714 Dodano konto: {entry.ServiceName}");
    }

    // [5] przełącza widoczność haseł
    private void OnTogglePasswords(object? sender, EventArgs e)
    {
        _passwordsVisible = !_passwordsVisible;
        _btnTogglePasswords.Text = _passwordsVisible ? "Ukryj has\u0142a" : "Poka\u017c has\u0142a";
        RefreshTable(_txtSearch.Text);
    }

    // [6] podwójne kliknięcie = kopiuj hasło
    private void OnRowDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;

        string id = _dgvAccounts.Rows[e.RowIndex].Cells["Id"].Value?.ToString() ?? "";
        var account = _manager.GetAll().FirstOrDefault(a => a.Id == id);

        if (account != null && !string.IsNullOrEmpty(account.Password))
        {
            Clipboard.SetText(account.Password);
            UpdateStatus($"\u2714 Has\u0142o do \"{account.ServiceName}\" skopiowane!");
        }
    }

    // [8] usuwa zaznaczone konto
    private void OnDeleteAccount(object? sender, EventArgs e)
    {
        if (_dgvAccounts.SelectedRows.Count == 0)
        {
            ShowError("Zaznacz konto do usuni\u0119cia!"); return;
        }

        string id = _dgvAccounts.SelectedRows[0].Cells["Id"].Value?.ToString() ?? "";
        string service = _dgvAccounts.SelectedRows[0].Cells["Service"].Value?.ToString() ?? "";

        var result = MessageBox.Show(
            $"Usun\u0105\u0107 konto \"{service}\"?",
            "Potwierdzenie",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            _manager.Remove(id);
            UpdateStatus($"\u2714 Usuni\u0119to: {service}");
        }
    }

    // ========================================================================
    // POMOCNICZE
    // ========================================================================

    // [4, 5, 7] odświeża tabelę z opcjonalnym filtrem
    private void RefreshTable(string searchQuery)
    {
        var accounts = _manager.Search(searchQuery);
        _dgvAccounts.Rows.Clear();

        foreach (var acc in accounts)
        {
            _dgvAccounts.Rows.Add(
                acc.ServiceName,
                acc.Login,
                _passwordsVisible ? acc.Password : acc.MaskedPassword,
                acc.Note,
                acc.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                acc.Id
            );
        }

        _lblStatus.Text = $"  Kont: {accounts.Count} / {_manager.Count}";
    }

    private void UpdateStatus(string msg) => _lblStatus.Text = "  " + msg;

    private void ShowError(string msg)
    {
        MessageBox.Show(msg, "Hafcium", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    // ========================================================================
    // FABRYKI KONTROLEK - żeby nie powtarzać kodu (DRY)
    // ========================================================================

    private Label MakeLabel(string text, int x, int y, bool header = false)
    {
        return new Label
        {
            Text = text, Location = new Point(x, y), AutoSize = true,
            Font = header ? new Font("Segoe UI", 11f, FontStyle.Bold) : new Font("Segoe UI", 9.5f),
            ForeColor = header ? Color.FromArgb(205, 214, 244) : Color.FromArgb(166, 173, 200)
        };
    }

    private TextBox MakeTextBox(int x, int y, int width)
    {
        return new TextBox
        {
            Location = new Point(x, y), Width = width,
            BackColor = Color.FromArgb(49, 50, 68),
            ForeColor = Color.FromArgb(205, 214, 244)
        };
    }

    private CheckBox MakeCheckBox(string text, int x, int y, bool chk)
    {
        return new CheckBox
        {
            Text = text, Location = new Point(x, y), AutoSize = true,
            Checked = chk, ForeColor = Color.FromArgb(205, 214, 244)
        };
    }

    private Button MakeButton(string text, int x, int y, int width, Color accent)
    {
        var btn = new Button
        {
            Text = text, Location = new Point(x, y),
            Width = width, Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(49, 50, 68),
            ForeColor = accent, Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = accent;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(69, 71, 90);
        return btn;
    }
}
