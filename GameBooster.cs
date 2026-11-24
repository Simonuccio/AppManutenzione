using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;

namespace AppManutenzione;

public static class GameBooster
{
    // API native nascoste di Windows per il controllo profondo dei processi
    [DllImport("ntdll.dll", PreserveSig = false)]
    private static extern void NtSuspendProcess(IntPtr processHandle);

    [DllImport("ntdll.dll", PreserveSig = false)]
    private static extern void NtResumeProcess(IntPtr processHandle);

    // Elenco di processi che non vanno MAI congelati: componenti vitali di
    // Windows, sicurezza/antivirus, driver e strumenti di sviluppo/gestione.
    // Congelare uno di questi potrebbe rendere il sistema instabile o
    // esporlo a rischi mentre l'antivirus è sospeso.
    private static readonly string[] Ignorati = {
        "explorer", "Code", "devenv", "AppManutenzione", "Taskmgr",
        "svchost", "System", "Idle", "csrss", "smss", "wininit",
        "services", "lsass", "winlogon", "fontdrvhost", "dwm", "spoolsv",
        "MsMpEng", "SecurityHealthService", "NisSrv", "WUDFHost",
        "RuntimeBroker", "SearchHost", "StartMenuExperienceHost",
        "sihost", "conhost", "dllhost", "TiWorker", "WmiPrvSE",
        "audiodg", "ctfmon"
    };

    // Salviamo i PID congelati anche su disco: se l'app viene chiusa
    // (o crasha) prima di "Scongela", al prossimo avvio possiamo comunque
    // ripristinare i processi rimasti sospesi, invece di lasciarli bloccati
    // per sempre.
    private static readonly string PercorsoStato = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AppManutenzione", "processi_congelati.json");

    private static List<int> processiSospesiPid = new List<int>();

    public static string CongelaApplicazioni()
    {
        int congelati = 0;
        int giaProtettiSaltati = 0;
        int idProprio = Environment.ProcessId;

        foreach (Process p in Process.GetProcesses())
        {
            // Seleziona solo le app con un'interfaccia attiva (es. Chrome, Spotify)
            // ignorando i servizi di sistema e i processi critici/di sicurezza
            if (p.Id == idProprio) continue;

            if (p.MainWindowHandle != IntPtr.Zero &&
                !Ignorati.Any(x => p.ProcessName.Equals(x, StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    NtSuspendProcess(p.Handle);
                    processiSospesiPid.Add(p.Id);
                    congelati++;
                }
                catch { giaProtettiSaltati++; /* Processo protetto o terminato nel frattempo */ }
            }
        }

        SalvaStatoSuDisco();

        string extra = giaProtettiSaltati > 0 ? $" ({giaProtettiSaltati} protetti/non congelabili)" : "";
        return $"[GAME BOOSTER] {congelati} applicazioni congelate con successo{extra}.";
    }

    public static string ScongelaApplicazioni()
    {
        // Unisce la lista in memoria con quella salvata su disco (nel caso
        // l'app sia stata chiusa e riaperta con processi ancora sospesi),
        // eliminando eventuali duplicati.
        var pidDaRipristinare = processiSospesiPid
            .Concat(CaricaStatoDaDisco())
            .Distinct()
            .ToList();

        int scongelati = 0;
        int nonTrovati = 0;

        foreach (int pid in pidDaRipristinare)
        {
            try
            {
                using Process p = Process.GetProcessById(pid);
                NtResumeProcess(p.Handle);
                scongelati++;
            }
            catch { nonTrovati++; /* Processo già chiuso o non più accessibile */ }
        }

        processiSospesiPid.Clear();
        CancellaStatoSuDisco();

        string extra = nonTrovati > 0 ? $" ({nonTrovati} già chiusi in precedenza)" : "";
        return $"[GAME BOOSTER] {scongelati} applicazioni riattivate{extra}.";
    }

    private static void SalvaStatoSuDisco()
    {
        try
        {
            string cartella = Path.GetDirectoryName(PercorsoStato)!;
            Directory.CreateDirectory(cartella);
            File.WriteAllText(PercorsoStato, JsonSerializer.Serialize(processiSospesiPid));
        }
        catch { /* Se il salvataggio fallisce, la sospensione rimane comunque tracciata in RAM */ }
    }

    private static List<int> CaricaStatoDaDisco()
    {
        try
        {
            if (!File.Exists(PercorsoStato)) return new List<int>();
            string json = File.ReadAllText(PercorsoStato);
            return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }
        catch { return new List<int>(); }
    }

    private static void CancellaStatoSuDisco()
    {
        try { if (File.Exists(PercorsoStato)) File.Delete(PercorsoStato); }
        catch { /* Non critico: verrà sovrascritto al prossimo congelamento */ }
    }
}
