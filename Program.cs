// ============================================================================
// Program.cs - Hafcium
// Adrian Zieliński, 79565
// Tu jest cała logika aplikacji - modele, szyfrowanie, generator haseł itd.
// Drugi plik (MainForm.cs) to GUI - żeby nie mieszać logiki z widokiem.
// ============================================================================

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Hafcium;

// ============================================================================
// MAIN - punkt startowy
// ============================================================================

static class Program
{
    [STAThread] // potrzebne żeby WinForms działał poprawnie
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // najpierw pytamy o hasło główne
        string? masterPassword = MasterPasswordDialog.Ask();

        // jak user kliknie anuluj to zamykamy
        if (masterPassword == null)
            return;

        Application.Run(new MainForm(masterPassword));
    }
}

// ============================================================================
// OKNO LOGOWANIA - hasło główne przy starcie
// ============================================================================

// Proste okienko które pyta o hasło główne zanim otworzy się aplikacja.
// Bez tego hasła nie da się odszyfrować bazy - więc jest wymagane.
public static class MasterPasswordDialog
{
    // zwraca hasło albo null jak user anulował
    public static string? Ask()
    {
        // tworzę formularz ręcznie - nie potrzebuję osobnej klasy Form
        var form = new Form
        {
            Text = "Hafcium - Logowanie",
            Size = new Size(420, 260),
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(30, 30, 46),
            ForeColor = Color.FromArgb(205, 214, 244),
            Font = new Font("Segoe UI", 10f)
        };

        // tytuł
        var lblTitle = new Label
        {
            Text = "🔐  Hafcium",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.FromArgb(137, 180, 250),
            Location = new Point(30, 24),
            AutoSize = true
        };

        // opis
        var lblInfo = new Label
        {
            Text = "Podaj hasło główne aby odszyfrować bazę danych.\nPrzy pierwszym uruchomieniu to hasło zostanie ustawione.",
            Location = new Point(30, 65),
            Size = new Size(340, 46),
            ForeColor = Color.FromArgb(166, 173, 200),
            Font = new Font("Segoe UI", 9f)
        };

        // pole na hasło
        var lblPass = new Label
        {
            Text = "Hasło główne:",
            Location = new Point(30, 118),
            AutoSize = true,
            ForeColor = Color.FromArgb(166, 173, 200)
        };

        var txtPass = new TextBox
        {
            Location = new Point(30, 140),
            Width = 340,
            Height = 28,
            UseSystemPasswordChar = true, // gwiazdki zamiast tekstu
            Font = new Font("Consolas", 12f),
            BackColor = Color.FromArgb(49, 50, 68),
            ForeColor = Color.FromArgb(205, 214, 244)
        };

        // przycisk OK
        var btnOk = new Button
        {
            Text = "Odblokuj",
            Location = new Point(30, 178),
            Width = 165,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(49, 50, 68),
            ForeColor = Color.FromArgb(166, 227, 161),
            DialogResult = DialogResult.OK
        };
        btnOk.FlatAppearance.BorderColor = Color.FromArgb(166, 227, 161);

        // przycisk Anuluj
        var btnCancel = new Button
        {
            Text = "Anuluj",
            Location = new Point(205, 178),
            Width = 165,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(49, 50, 68),
            ForeColor = Color.FromArgb(243, 139, 168),
            DialogResult = DialogResult.Cancel
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(243, 139, 168);

        form.Controls.AddRange(new Control[] { lblTitle, lblInfo, lblPass, txtPass, btnOk, btnCancel });
        form.AcceptButton = btnOk;      // enter = OK
        form.CancelButton = btnCancel;  // escape = anuluj

        // walidacja - nie pozwalam na puste hasło
        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtPass.Text))
            {
                MessageBox.Show("Hasło nie może być puste!", "Hafcium",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                form.DialogResult = DialogResult.None; // nie zamykaj formularza
            }
        };

        if (form.ShowDialog() == DialogResult.OK)
            return txtPass.Text;

        return null;
    }
}

// ============================================================================
// MODEL - klasa opisująca jedno konto w bazie
// ============================================================================

// Każde konto w menadżerze ma te dane - serwis, login, hasło itd.
// Używam właściwości (get/set) zamiast publicznych pól, bo tak jest ładniej
// i mamy kontrolę nad tym co się dzieje z danymi (enkapsulacja).
public class AccountEntry
{
    // unikalny identyfikator - generuje się sam jako GUID
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // nazwa serwisu np. "Gmail", "Facebook"
    public string ServiceName { get; set; } = string.Empty;

    // login albo email
    public string Login { get; set; } = string.Empty;

    // hasło w formie jawnej - szyfrowane dopiero przy zapisie do pliku
    public string Password { get; set; } = string.Empty;

    // dodatkowa notatka, nie jest wymagana
    public string Note { get; set; } = string.Empty;

    // kiedy dodano konto
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // zamaskowane hasło do wyświetlania w tabeli (żeby nie było widać od razu)
    // to jest właściwość tylko do odczytu - oblicza się za każdym razem
    public string MaskedPassword => new string('•', Password.Length);
}

// ============================================================================
// INTERFEJS - kontrakt na zapis/odczyt danych
// ============================================================================

// Interfejs mówi CO trzeba zrobić, ale nie JAK.
// Dzięki temu mogę kiedyś podmienić zapis do pliku na np. bazę SQL
// i nie muszę zmieniać reszty kodu - to jest ta zasada DIP.
public interface IDataStorage
{
    List<AccountEntry> Load();  // wczytaj konta
    void Save(List<AccountEntry> entries);  // zapisz konta
}

// ============================================================================
// SZYFROWANY ZAPIS DO PLIKU - implementacja IDataStorage
// ============================================================================

// Ta klasa zajmuje się szyfrowaniem i zapisywaniem kont do pliku.
// Używam AES-256 bo to silne szyfrowanie, a klucz generuję z hasła przez PBKDF2.
// Implementuje interfejs IDataStorage - to jest polimorfizm przez interfejs.
public class EncryptedFileStorage : IDataStorage
{
    // hasło główne podane przez użytkownika przy logowaniu
    private readonly string _masterPassword;

    // ścieżka do pliku z bazą
    private readonly string _filePath;

    // sól do PBKDF2 - dodatkowe zabezpieczenie przy generowaniu klucza
    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("HafciumSalt2025!");

    // konstruktor - wymaga ścieżki do pliku i hasła głównego
    public EncryptedFileStorage(string filePath, string masterPassword)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _masterPassword = masterPassword ?? throw new ArgumentNullException(nameof(masterPassword));
    }

    // wczytuje bazę z pliku - deszyfruje i zamienia JSON na listę kont
    // jak pliku nie ma to po prostu zwraca pustą listę (nowa baza)
    public List<AccountEntry> Load()
    {
        // jeśli plik nie istnieje to znaczy że baza jest nowa
        if (!File.Exists(_filePath))
            return new List<AccountEntry>();

        try
        {
            byte[] encryptedData = File.ReadAllBytes(_filePath);  // czytam surowe bajty
            string json = Decrypt(encryptedData);  // deszyfruję na JSON
            return JsonSerializer.Deserialize<List<AccountEntry>>(json) ?? new List<AccountEntry>();
        }
        catch (CryptographicException)
        {
            // złe hasło - nie da się odszyfrować
            MessageBox.Show(
                "Nieprawidłowe hasło główne! Nie udało się odszyfrować bazy.",
                "Hafcium - Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return new List<AccountEntry>();
        }
        catch (Exception ex)
        {
            // jak plik jest uszkodzony to lepiej zacząć od nowa niż crashować
            Console.WriteLine($"Błąd odczytu bazy: {ex.Message}");
            return new List<AccountEntry>();
        }
    }

    // zapisuje konta do pliku - zamienia na JSON, szyfruje i zapisuje
    public void Save(List<AccountEntry> entries)
    {
        // zamieniam listę kont na tekst JSON
        string json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = false });

        // szyfruję i zapisuję do pliku
        byte[] encrypted = Encrypt(json);
        File.WriteAllBytes(_filePath, encrypted);
    }

    // szyfruje tekst za pomocą AES-256-CBC
    // na początku wyniku doklejam IV (wektor inicjalizujący) - bez niego nie da się odszyfrować
    private byte[] Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = DeriveKey();  // klucz 256-bit z PBKDF2
        aes.GenerateIV();  // losowy IV za każdym razem - tak jest bezpieczniej

        using var encryptor = aes.CreateEncryptor();
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // łączę IV + zaszyfrowane dane w jedną tablicę
        // IV ma 16 bajtów i jest na początku - przy odczycie go wyciągam
        byte[] result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
        return result;
    }

    // deszyfruje dane - wyciąga IV z początku, resztę odszyfrowuje
    private string Decrypt(byte[] cipherData)
    {
        using var aes = Aes.Create();
        aes.Key = DeriveKey();

        // pierwsze 16 bajtów to IV który był dołączony przy szyfrowaniu
        byte[] iv = new byte[16];
        Buffer.BlockCopy(cipherData, 0, iv, 0, 16);
        aes.IV = iv;

        // reszta to zaszyfrowane dane
        using var decryptor = aes.CreateDecryptor();
        byte[] plainBytes = decryptor.TransformFinalBlock(cipherData, 16, cipherData.Length - 16);
        return Encoding.UTF8.GetString(plainBytes);
    }

    // generuje klucz AES z hasła głównego podanego przez użytkownika
    // PBKDF2 ze 100k iteracji - żeby brute-force trwał wieczność
    private byte[] DeriveKey()
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(_masterPassword, Salt, 100_000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32); // 32 bajty = 256 bitów
    }
}

// ============================================================================
// GENERATOR HASEŁ - klasa abstrakcyjna + implementacja
// ============================================================================

// Klasa bazowa generatora - jest abstrakcyjna więc nie można jej użyć bezpośrednio.
// Zrobiłem ją abstrakcyjną (a nie interfejs) bo chciałem współdzielić
// metodę GetSecureRandomIndex między podklasami - żeby nie pisać tego samego dwa razy.
public abstract class PasswordGeneratorBase
{
    // bezpieczny generator losowy - lepszy niż zwykły Random
    // protected bo podklasy muszą mieć do niego dostęp
    protected readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

    // metoda abstrakcyjna - każda podklasa MUSI ją zaimplementować po swojemu
    // to jest polimorfizm - różne klasy, ta sama metoda, różne zachowanie
    public abstract string Generate(int length);

    // losuje indeks z zakresu [0, max) - używane wewnętrznie
    // korzysta z kryptograficznego generatora zamiast Random
    protected int GetSecureRandomIndex(int maxExclusive)
    {
        byte[] data = new byte[4];
        Rng.GetBytes(data);  // 4 losowe bajty
        return (int)(BitConverter.ToUInt32(data, 0) % (uint)maxExclusive);
    }
}

// Właściwy generator haseł - dziedziczy po klasie bazowej.
// Można mu ustawiać jakie znaki mają być w haśle (małe, wielkie, cyfry, specjalne).
public class ConfigurablePasswordGenerator : PasswordGeneratorBase
{
    // pule znaków z których losujemy
    private const string LowerChars = "abcdefghijklmnopqrstuvwxyz";
    private const string UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string DigitChars = "0123456789";
    private const string SpecialChars = "!@#$%^&*()-_=+[]{}|;:,.<>?";

    // flagi - co ma być w haśle (domyślnie wszystko włączone)
    public bool UseLowercase { get; set; } = true;
    public bool UseUppercase { get; set; } = true;
    public bool UseDigits { get; set; } = true;
    public bool UseSpecial { get; set; } = true;

    // nadpisujemy metodę z klasy bazowej (override = polimorfizm)
    // generuje hasło o zadanej długości z wybranych kategorii znaków
    public override string Generate(int length)
    {
        // minimum 4 znaki bo inaczej nie zmieszczę po jednym z każdej kategorii
        if (length < 4)
            throw new ArgumentException("Minimalna długość hasła to 4 znaki.", nameof(length));

        // buduję pulę znaków z zaznaczonych kategorii
        var pool = new StringBuilder();
        var mandatory = new List<char>(); // tu trzymam po jednym znaku z każdej kategorii

        // dla każdej zaznaczonej opcji dodaję znaki do puli i losuję jeden obowiązkowy
        if (UseLowercase) { pool.Append(LowerChars); mandatory.Add(LowerChars[GetSecureRandomIndex(LowerChars.Length)]); }
        if (UseUppercase) { pool.Append(UpperChars); mandatory.Add(UpperChars[GetSecureRandomIndex(UpperChars.Length)]); }
        if (UseDigits) { pool.Append(DigitChars); mandatory.Add(DigitChars[GetSecureRandomIndex(DigitChars.Length)]); }
        if (UseSpecial) { pool.Append(SpecialChars); mandatory.Add(SpecialChars[GetSecureRandomIndex(SpecialChars.Length)]); }

        // musi być zaznaczona chociaż jedna opcja
        if (pool.Length == 0)
            throw new InvalidOperationException("Musisz wybrać co najmniej jedną kategorię znaków.");

        string charPool = pool.ToString();
        var result = new char[length];

        // najpierw wstawiam obowiązkowe znaki na losowe pozycje
        // dzięki temu hasło NA PEWNO ma po jednym znaku z każdej kategorii
        var usedPositions = new HashSet<int>();
        foreach (char c in mandatory)
        {
            int pos;
            do { pos = GetSecureRandomIndex(length); } while (!usedPositions.Add(pos));
            result[pos] = c;
        }

        // resztę pozycji wypełniam losowymi znakami z całej puli
        for (int i = 0; i < length; i++)
        {
            if (!usedPositions.Contains(i))
                result[i] = charPool[GetSecureRandomIndex(charPool.Length)];
        }

        return new string(result);
    }
}

// ============================================================================
// MENADŻER KONT - zarządza listą kont
// ============================================================================

// Ta klasa ogarnia dodawanie, usuwanie, wyszukiwanie kont.
// Ma w sobie IDataStorage (kompozycja, nie dziedziczenie) - dzięki temu
// nie jest sztywno powiązana z konkretnym sposobem zapisu.
// Jedna klasa = jedna odpowiedzialność (SRP).
public class AccountManager
{
    // prywatna lista kont - nikt z zewnątrz nie ma do niej bezpośredniego dostępu
    private readonly List<AccountEntry> _accounts;

    // storage do zapisu/odczytu - zależy od interfejsu a nie od klasy (DIP)
    private readonly IDataStorage _storage;

    // event który odpala się po każdej zmianie - GUI się wtedy odświeża
    public event Action? DataChanged;

    // konstruktor - wczytuje bazę z pliku od razu na starcie
    public AccountManager(IDataStorage storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _accounts = _storage.Load(); // wczytujemy istniejące konta
    }

    // zwraca KOPIĘ listy - żeby ktoś z zewnątrz nie mógł grzebać w oryginale
    public List<AccountEntry> GetAll() => new(_accounts);

    // wyszukiwanie - filtruje po nazwie serwisu (nie zwraca uwagi na wielkość liter)
    // jak query jest puste to zwraca wszystko
    public List<AccountEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return GetAll();

        return _accounts
            .Where(a => a.ServiceName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    // dodaje nowe konto i od razu zapisuje do pliku
    public void Add(AccountEntry entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        _accounts.Add(entry);
        SaveAndNotify(); // zapis + powiadomienie GUI
    }

    // usuwa konto po ID i zapisuje
    public bool Remove(string id)
    {
        var entry = _accounts.FirstOrDefault(a => a.Id == id);
        if (entry == null) return false; // nie znaleziono

        _accounts.Remove(entry);
        SaveAndNotify();
        return true;
    }

    // ile mamy kont w bazie
    public int Count => _accounts.Count;

    // prywatna metoda - zapisuje bazę i odpala event
    // prywatna bo nie chcę żeby ktoś z zewnątrz mógł ją wywołać
    private void SaveAndNotify()
    {
        _storage.Save(_accounts);
        DataChanged?.Invoke(); // powiadamiam formularz że coś się zmieniło
    }
}

// ============================================================================
// UZASADNIENIE STRUKTURY
// ============================================================================
//
// Podzieliłem projekt na dwa pliki - Program.cs (logika) i MainForm.cs (GUI),
// żeby nie mieszać wszystkiego w jednej klasie. To jest zasada pojedynczej
// odpowiedzialności (SRP). Interfejs IDataStorage pozwala łatwo podmienić
// sposób zapisu danych bez ruszania reszty kodu - zasada DIP. Klasa
// PasswordGeneratorBase jest abstrakcyjna i wymusza implementację metody
// Generate() - to jest dziedziczenie i polimorfizm w praktyce.
// AccountManager korzysta z IDataStorage przez kompozycję (ma go jako pole),
// a nie przez dziedziczenie - bo tak jest elastyczniej.
// ============================================================================
