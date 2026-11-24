#nullable disable
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.Net.NetworkInformation;
using LibreHardwareMonitor.Hardware;

namespace AppManutenzione;

public partial class Form1 : Form
{
    private RichTextBox consoleOutput;
    private Label lblTemps, lblRam, lblTopRam, lblDischi, lblPing;
    private System.Windows.Forms.Timer timerDashboard; 
    private Computer monitorHardware;

    private bool trascinamento = false;
    private Point puntoInizioCursore;
    private Point puntoInizioFinestra;

    public Form1()
    {
        InitializeComponent();
        this.Size = new Size(1150, 880); 
        this.BackColor = Color.FromArgb(235, 235, 240);
        this.FormBorderStyle = FormBorderStyle.None; 
        
        ArrotondaControllo(this, 20); 

        // Sensore CPU disabilitato
        monitorHardware = new Computer { IsCpuEnabled = false, IsGpuEnabled = true, IsMotherboardEnabled = true, IsControllerEnabled = true };
        monitorHardware.Open();

        // === BARRA DEL TITOLO PERSONALIZZATA ===
        Panel barraTitolo = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(20, 20, 25) };
        Label lblTitoloApp = new Label { Text = "Manutenzione PC v14.1", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.WhiteSmoke, Location = new Point(15, 10), AutoSize = true };
        
        Button btnRiduci = new Button { Text = "—", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Gray, BackColor = Color.Transparent, FlatStyle = FlatStyle.Flat, Size = new Size(45, 40), Location = new Point(this.Width - 90, 0), Cursor = Cursors.Hand };
        btnRiduci.FlatAppearance.BorderSize = 0;
        btnRiduci.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

        Button btnChiudi = new Button { Text = "✕", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Gray, BackColor = Color.Transparent, FlatStyle = FlatStyle.Flat, Size = new Size(45, 40), Location = new Point(this.Width - 45, 0), Cursor = Cursors.Hand };
        btnChiudi.FlatAppearance.BorderSize = 0;
        btnChiudi.MouseEnter += (s, e) => { btnChiudi.BackColor = Color.Crimson; btnChiudi.ForeColor = Color.White; };
        btnChiudi.MouseLeave += (s, e) => { btnChiudi.BackColor = Color.Transparent; btnChiudi.ForeColor = Color.Gray; };
        btnChiudi.Click += (s, e) => Application.Exit();

        barraTitolo.MouseDown += (s, e) => { trascinamento = true; puntoInizioCursore = Cursor.Position; puntoInizioFinestra = this.Location; };
        barraTitolo.MouseMove += (s, e) => { if (trascinamento) { Point dif = Point.Subtract(Cursor.Position, new Size(puntoInizioCursore)); this.Location = Point.Add(puntoInizioFinestra, new Size(dif)); } };
        barraTitolo.MouseUp += (s, e) => trascinamento = false;

        lblTitoloApp.MouseDown += (s, e) => { trascinamento = true; puntoInizioCursore = Cursor.Position; puntoInizioFinestra = this.Location; };
        lblTitoloApp.MouseMove += (s, e) => { if (trascinamento) { Point dif = Point.Subtract(Cursor.Position, new Size(puntoInizioCursore)); this.Location = Point.Add(puntoInizioFinestra, new Size(dif)); } };
        lblTitoloApp.MouseUp += (s, e) => trascinamento = false;

        barraTitolo.Controls.Add(lblTitoloApp);
        barraTitolo.Controls.Add(btnRiduci);
        barraTitolo.Controls.Add(btnChiudi);
        this.Controls.Add(barraTitolo);


        // === COLONNA 1: PULIZIA (X=20) ===
        this.Controls.Add(new Label { Text = "🧹 PULIZIA", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(20, 60), AutoSize = true });
        
        CreaBottone("Analizza Spazio Spreco\nCalcola i MB liberabili", 20, 90, Color.Teal).Click += async (s, e) => {
            ScriviConsole("\n=========================================\n>>> ANALISI SPAZIO IN CORSO...");
            double mbSprecati = await CalcolaSpazioSprecatoAsync();
            if (mbSprecati > 0)
                ScriviConsole($">>> 📊 ANALISI COMPLETATA: Puoi liberare fino a {mbSprecati:F1} MB svuotando i Temp e il Cestino.");
            else
                ScriviConsole(">>> 📊 ANALISI COMPLETATA: Il sistema è già pulito (0 MB da liberare).");
        };

        CreaBottone("Pulizia File Temporanei\nSvuota Temp e Cestino", 20, 150, Color.DarkOrange).Click += async (s, e) => {
            DriveInfo discoC = new DriveInfo("C");
            long spazioPrima = discoC.AvailableFreeSpace;
            await EsecutoreComandi.EseguiAsync("cmd.exe", "/c powershell.exe -NoProfile -Command \"Clear-RecycleBin -Force -ErrorAction SilentlyContinue\" & del /q /f /s \"%TEMP%\\*\" 2>nul & del /q /f /s \"%WINDIR%\\Temp\\*\" 2>nul", "PULIZIA TEMPORANEI", ScriviConsole);
            long spazioDopo = new DriveInfo("C").AvailableFreeSpace;
            double mbLiberati = Math.Max(0, (spazioDopo - spazioPrima) / 1048576.0);
            ScriviConsole($"\n>>> 🧹 RISULTATO: Pulizia completata! Hai liberato {mbLiberati:F1} MB.");
        };

        CreaBottone("Pulisci Cache Rete (DNS)\nRisolve errori navigazione", 20, 210, Color.Black).Click += async (s, e) => 
            await EsecutoreComandi.EseguiAsync("cmd.exe", "/c ipconfig /flushdns", "PULIZIA RETE", ScriviConsole);
            
        CreaBottone("Pulizia Profonda\nResetta cache di Windows", 20, 270, Color.DarkRed).Click += async (s, e) => 
            await EsecutoreComandi.EseguiAsync("cmd.exe", "/c net stop wuauserv & net start wuauserv", "PULIZIA PROFONDA", ScriviConsole);

        CreaBottone("🔍 Gestore File Duplicati\nScegli cartella e scansiona", 20, 330, Color.Purple).Click += async (s, e) => {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Seleziona cartella da scansionare";
                if (fbd.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    ScriviConsole($"\n=========================================\n>>> SCANSIONE IN CORSO: {fbd.SelectedPath}");
                    var duplicati = await CacciatoreDuplicati.TrovaDuplicatiAsync(fbd.SelectedPath);
                    if (duplicati.Count == 0) { ScriviConsole(">>> [OK] Nessun clone trovato."); return; }
                    ApriFinestraGestioneDuplicati(duplicati);
                }
            }
        };


        // === COLONNA 2: MANUTENZIONE (X=290) ===
        this.Controls.Add(new Label { Text = "🛠️ MANUTENZIONE", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(290, 60), AutoSize = true });
        
        CreaBottone("Verifica Sistema (SFC)", 290, 90, Color.Black).Click += async (s, e) => 
            await EsecutoreComandi.EseguiAsync("cmd.exe", "/c sfc /scannow", "VERIFICA FILE", ScriviConsole);
            
        CreaBottone("Riparazione Immagine (DISM)", 290, 150, Color.MediumBlue).Click += async (s, e) => 
            await EsecutoreComandi.EseguiAsync("cmd.exe", "/c dism /online /cleanup-image /restorehealth", "RIPARAZIONE", ScriviConsole);
            
        CreaBottone("Ottimizzazione Dischi", 290, 210, Color.DarkSlateGray).Click += async (s, e) => 
            await EsecutoreComandi.EseguiAsync("defrag.exe", "/C /O /U /V", "OTTIMIZZAZIONE", ScriviConsole);
            
        CreaBottone("Aggiorna Software (Winget)", 290, 270, Color.Teal).Click += async (s, e) => 
            await EsecutoreComandi.EseguiAsync("cmd.exe", "/c winget upgrade --all --accept-source-agreements", "AGGIORNAMENTO", ScriviConsole);
            
        CreaBottone("📡 Backup Wi-Fi", 290, 330, Color.DarkMagenta).Click += async (s, e) => 
            await EsecutoreComandi.EseguiAsync("cmd.exe", $"/c netsh wlan export profile key=clear folder=\"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\"", "BACKUP WI-FI", ScriviConsole);

        CreaBottone("⏳ Gestore Avvio (Startup)\nDisabilita avvii automatici", 290, 390, Color.DarkSlateBlue).Click += (s, e) => {
             ApriFinestraStartup();
        };


        // === COLONNA 3: MODALITÀ ENERGIA E GIOCHI (X=560) ===
        this.Controls.Add(new Label { Text = "⚡ MODALITÀ & GIOCHI", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(560, 60), AutoSize = true });
        
        CreaBottone("❄️ Modalità ECO\nCPU 99% - Temperature Fresche", 560, 90, Color.DarkBlue, 220, 50).Click += async (s, e) => 
            await EsecutoreComandi.EseguiAsync("cmd.exe", "/c powercfg /setactive a1841308-3541-4fab-bc81-f71556f20b4a", "MODALITA ECO", ScriviConsole);
            
        CreaBottone("⚖️ Modalità NORMALE\nBilanciato (Default Windows)", 560, 150, Color.DarkGreen, 220, 50).Click += async (s, e) => 
            await EsecutoreComandi.EseguiAsync("cmd.exe", "/c powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e", "MODALITA NORMALE", ScriviConsole);

        CreaBottone("⚡ Modalità GAMING\nMassime Prestazioni", 560, 210, Color.DarkRed, 220, 50).Click += async (s, e) => {
            ScriviConsole("\n=========================================\n>>> ATTIVAZIONE HARDWARE ESTREMO...");
            await EsecutoreComandi.EseguiAsync("cmd.exe", "/c powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", "ENERGIA MASSIMA", ScriviConsole);
            await EsecutoreComandi.EseguiModalitaGamingAsync(ScriviConsole);
            ScriviConsole(">>> [OK] Sistema e Hardware pronti per il Gaming.");
        };


        // === COLONNA 4: DASHBOARD HARDWARE (X=830) ===
        this.Controls.Add(new Label { Text = "📊 DASHBOARD HARDWARE", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(830, 60), AutoSize = true });
        
        Panel cardTemps = CreaScheda(830, 90, 280, 200); 
        lblTemps = new Label { Font = new Font("Segoe UI", 9), Location = new Point(10, 10), AutoSize = true, ForeColor = Color.DarkRed };
        lblPing = new Label { Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(10, 165), AutoSize = true, ForeColor = Color.DarkCyan }; 
        cardTemps.Controls.Add(lblTemps);
        cardTemps.Controls.Add(lblPing);

        Panel cardRam = CreaScheda(830, 300, 280, 140); 
        lblRam = new Label { Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true, ForeColor = Color.DarkSlateBlue };
        lblTopRam = new Label { Font = new Font("Segoe UI", 9), Location = new Point(10, 35), AutoSize = true, ForeColor = Color.DimGray };
        cardRam.Controls.Add(lblRam);
        cardRam.Controls.Add(lblTopRam);

        Panel cardDischi = CreaScheda(830, 460, 280, 200); 
        lblDischi = new Label { Font = new Font("Segoe UI", 9), Location = new Point(10, 10), AutoSize = true, ForeColor = Color.DarkSlateGray };
        cardDischi.Controls.Add(lblDischi);


        // === CONSOLE MODERNA ===
        Panel contenitoreConsole = new Panel { 
            Location = new Point(20, 460), 
            Size = new Size(790, 350), 
            BackColor = Color.FromArgb(15, 15, 20) 
        };
        ArrotondaControllo(contenitoreConsole, 15);
        this.Controls.Add(contenitoreConsole);

        Panel headerConsole = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Color.FromArgb(25, 25, 30) };
        headerConsole.Controls.Add(new Label { Text = "🔴 🟡 🟢", Font = new Font("Segoe UI", 8), Location = new Point(10, 8), AutoSize = true, BackColor = Color.Transparent });
        headerConsole.Controls.Add(new Label { Text = ">_ LOG OPERAZIONI", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(80, 6), AutoSize = true });
        contenitoreConsole.Controls.Add(headerConsole);

        consoleOutput = new RichTextBox { 
            ReadOnly = true, 
            BackColor = Color.FromArgb(15, 15, 20), 
            ForeColor = Color.LightGray, 
            Font = new Font("Consolas", 10), 
            BorderStyle = BorderStyle.None, 
            Size = new Size(770, 310), 
            Location = new Point(10, 40) 
        };
        contenitoreConsole.Controls.Add(consoleOutput);

        timerDashboard = new System.Windows.Forms.Timer { Interval = 2500 }; 
        timerDashboard.Tick += (s, e) => {
            lblRam.Text = SensoriHardware.LeggiRAM();
            lblTopRam.Text = SensoriHardware.LeggiTopProcessi();

            // Filtra via qualsiasi riga riferita alla CPU
            string testoTemp = SensoriHardware.LeggiTemperature(monitorHardware);
            string[] righeFiltrate = Array.FindAll(
                testoTemp.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None),
                riga => !riga.Contains("CPU", StringComparison.OrdinalIgnoreCase)
            );
            lblTemps.Text = string.Join(Environment.NewLine, righeFiltrate).Trim();

            lblDischi.Text = SensoriHardware.LeggiDischi();
            
            Task.Run(() => {
                try {
                    var reply = new Ping().Send("1.1.1.1", 1000);
                    if (reply.Status == IPStatus.Success)
                        lblPing.Invoke(new Action(() => lblPing.Text = $"🌐 Latenza Rete: {reply.RoundtripTime} ms"));
                    else
                        lblPing.Invoke(new Action(() => lblPing.Text = "🌐 Latenza Rete: Instabile"));
                } catch { lblPing.Invoke(new Action(() => lblPing.Text = "🌐 Rete: Offline")); }
            });
        };
        timerDashboard.Start();
    }

    private async Task<double> CalcolaSpazioSprecatoAsync()
    {
        return await Task.Run(() =>
        {
            long totaleByte = 0;
            string[] cartelleDaPulire = {
                Path.GetTempPath(),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
                @"C:\$Recycle.Bin" 
            };

            foreach (string cartella in cartelleDaPulire)
            {
                if (Directory.Exists(cartella))
                {
                    try
                    {
                        DirectoryInfo di = new DirectoryInfo(cartella);
                        foreach (FileInfo file in di.EnumerateFiles("*", SearchOption.AllDirectories))
                        {
                            totaleByte += file.Length;
                        }
                    }
                    catch { }
                }
            }
            return totaleByte / 1048576.0; 
        });
    }

    private void ApriFinestraStartup()
    {
        Form formStartup = new Form { Text = "Gestore Avvio Sicuro", Size = new Size(600, 400), StartPosition = FormStartPosition.CenterParent, BackColor = Color.White };
        Label lblTitolo = new Label { Text = "🚀 Programmi in Avvio Automatico", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleCenter };
        Button btnElimina = new Button { Text = "🚫 Disabilita Programmi Selezionati", Dock = DockStyle.Bottom, Height = 50, BackColor = Color.DarkSlateBlue, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
        btnElimina.FlatAppearance.BorderSize = 0;

        ListView listStartup = new ListView { Dock = DockStyle.Fill, View = View.Details, CheckBoxes = true, FullRowSelect = true, GridLines = true, Font = new Font("Segoe UI", 10) };
        listStartup.Columns.Add("Nome Programma", 180);
        listStartup.Columns.Add("Percorso", 350);

        var regKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        if (regKey != null)
        {
            foreach (string v in regKey.GetValueNames())
            {
                ListViewItem item = new ListViewItem(v);
                item.SubItems.Add(regKey.GetValue(v).ToString());
                listStartup.Items.Add(item);
            }
        }

        btnElimina.Click += delegate {
            if (regKey != null) {
                foreach (ListViewItem item in listStartup.CheckedItems) {
                    try { regKey.DeleteValue(item.Text); item.Remove(); ScriviConsole($">>> [OK] {item.Text} rimosso dall'avvio automatico."); } catch {}
                }
            }
        };

        formStartup.Controls.Add(listStartup);
        formStartup.Controls.Add(lblTitolo);
        formStartup.Controls.Add(btnElimina);
        formStartup.ShowDialog();
    }

    private void ApriFinestraGestioneDuplicati(System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> duplicati)
    {
        Form formDuplicati = new Form { Text = "Gestore File Duplicati", Size = new Size(900, 650), StartPosition = FormStartPosition.CenterParent, BackColor = Color.White };
        Label lblTitolo = new Label { Text = "🔍 Cloni Esatti Trovati", Font = new Font("Segoe UI", 14, FontStyle.Bold), Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleCenter };
        Button btnElimina = new Button { Text = "🗑️ Elimina Definitivamente i Cloni", Dock = DockStyle.Bottom, Height = 60, BackColor = Color.Crimson, ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
        btnElimina.FlatAppearance.BorderSize = 0;
        ListView listDuplicati = new ListView { Dock = DockStyle.Fill, View = View.Details, CheckBoxes = true, FullRowSelect = true, GridLines = true, Font = new Font("Segoe UI", 9) };
        listDuplicati.Columns.Add("Percorso", formDuplicati.Width - 50);

        int gruppoNum = 1;
        foreach (var gruppo in duplicati.Values)
        {
            ListViewGroup listGroup = new ListViewGroup($"Gruppo {gruppoNum} (Trovate {gruppo.Count} copie)");
            listDuplicati.Groups.Add(listGroup);
            bool originale = true;
            foreach (string file in gruppo)
            {
                ListViewItem item = new ListViewItem(file) { Group = listGroup, Checked = !originale };
                if (originale) item.ForeColor = Color.DarkGreen; else item.ForeColor = Color.DimGray;
                listDuplicati.Items.Add(item);
                originale = false;
            }
            gruppoNum++;
        }

        btnElimina.Click += delegate {
            int eliminati = 0; long byteLiberati = 0;
            foreach (ListViewItem item in listDuplicati.CheckedItems)
            {
                try { byteLiberati += new FileInfo(item.Text).Length; File.Delete(item.Text); eliminati++; } catch { }
            }
            MessageBox.Show($"Eliminati {eliminati} file doppi!\nHai liberato {byteLiberati / 1048576.0:F2} MB.", "Pulizia Completata", MessageBoxButtons.OK, MessageBoxIcon.Information);
            formDuplicati.Close();
        };

        formDuplicati.Controls.Add(listDuplicati);
        formDuplicati.Controls.Add(lblTitolo);
        formDuplicati.Controls.Add(btnElimina);
        formDuplicati.ShowDialog();
    }

    private Button CreaBottone(string testo, int x, int y, Color coloreTesto, int larghezza = 250, int altezza = 50)
    {
        Button btn = new Button { 
            Text = testo, Size = new Size(larghezza, altezza), Location = new Point(x, y), 
            ForeColor = coloreTesto, FlatStyle = FlatStyle.Flat, BackColor = Color.White, 
            Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand 
        };
        btn.FlatAppearance.BorderSize = 0; 
        btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(240, 240, 248);
        btn.MouseLeave += (s, e) => btn.BackColor = Color.White;
        ArrotondaControllo(btn, 15);
        this.Controls.Add(btn);
        return btn;
    }

    private Panel CreaScheda(int x, int y, int larghezza, int altezza)
    {
        Panel ombra = new Panel { Location = new Point(x + 4, y + 4), Size = new Size(larghezza, altezza), BackColor = Color.FromArgb(30, 0, 0, 0) };
        ArrotondaControllo(ombra, 20);
        this.Controls.Add(ombra);

        Panel pnl = new Panel { Location = new Point(x, y), Size = new Size(larghezza, altezza), BackColor = Color.White, AutoScroll = true };
        ArrotondaControllo(pnl, 20);
        this.Controls.Add(pnl);
        pnl.BringToFront(); 
        
        return pnl;
    }

    private void ArrotondaControllo(Control ctrl, int raggio)
    {
        System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(0, 0, raggio, raggio, 180, 90);
        path.AddArc(ctrl.Width - raggio, 0, raggio, raggio, 270, 90);
        path.AddArc(ctrl.Width - raggio, ctrl.Height - raggio, raggio, raggio, 0, 90);
        path.AddArc(0, ctrl.Height - raggio, raggio, raggio, 90, 90);
        path.CloseFigure();
        ctrl.Region = new Region(path);
    }

    private void ScriviConsole(string testo)
    {
        if (consoleOutput.InvokeRequired) 
        {
            consoleOutput.Invoke(new Action(() => ScriviConsole(testo)));
            return;
        }

        consoleOutput.SelectionStart = consoleOutput.TextLength;
        consoleOutput.SelectionLength = 0;

        if (testo.Contains("[OK]") || testo.Contains("completata") || testo.Contains("successo") || testo.Contains("ANALISI COMPLETATA"))
            consoleOutput.SelectionColor = Color.MediumSpringGreen; 
        else if (testo.Contains("Errore") || testo.Contains("fallita") || testo.Contains("annullata"))
            consoleOutput.SelectionColor = Color.Tomato; 
        else if (testo.Contains("===") || testo.Contains(">>>") || testo.Contains("CORSO"))
            consoleOutput.SelectionColor = Color.DeepSkyBlue; 
        else
            consoleOutput.SelectionColor = Color.LightGray; 

        consoleOutput.AppendText(testo + Environment.NewLine);
        consoleOutput.ScrollToCaret(); 
    }
}