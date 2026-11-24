#nullable disable
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AppManutenzione;

public static class EsecutoreComandi
{
    public static async Task EseguiAsync(string comando, string argomenti, string titolo, Action<string> scriviLog)
    {
        scriviLog(Environment.NewLine + "=========================================");
        scriviLog($">>> {titolo}...");
        
        ProcessStartInfo psi = new ProcessStartInfo { FileName = comando, Arguments = argomenti, UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
        try 
        {
            using (Process p = Process.Start(psi))
            {
                if (p != null)
                {
                    string linea;
                    // Corretto l'avviso CA2024 leggendo le linee in modo asincrono nativo
                    while ((linea = await p.StandardOutput.ReadLineAsync()) != null)
                    {
                        scriviLog(linea);
                    }
                }
            }
            scriviLog(">>> [OK] OPERAZIONE COMPLETATA.");
        }
        // Il messaggio d'errore ora riporta anche la causa tecnica: senza privilegi
        // di amministratore molti comandi (SFC, DISM, servizi) falliscono, ma
        // possono fallire anche per altri motivi (comando non trovato, percorso
        // non valido, ecc.) che prima venivano nascosti da un catch generico.
        catch (Exception ex) { scriviLog($">>> [ERRORE] {ex.Message}. Verifica di aver avviato l'app come Amministratore."); }
    }

    public static async Task EseguiModalitaGamingAsync(Action<string> scriviLog)
    {
        scriviLog(Environment.NewLine + "=========================================");
        scriviLog(">>> ATTIVAZIONE MASSIME PRESTAZIONI...");
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "powercfg", Arguments = "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61", CreateNoWindow = true, UseShellExecute = false })?.WaitForExit();
            ProcessStartInfo psi = new ProcessStartInfo { FileName = "powercfg", Arguments = "/list", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            
            using (Process p = Process.Start(psi))
            {
                if (p != null)
                {
                    string output = await p.StandardOutput.ReadToEndAsync();
                    bool trovato = false;
                    if (output != null)
                    {
                        foreach (string line in output.Split('\n'))
                        {
                            if (line.ToLower().Contains("eccellenti") || line.ToLower().Contains("ultimate"))
                            {
                                Match m = Regex.Match(line, @"[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}");
                                if (m.Success)
                                {
                                    Process.Start(new ProcessStartInfo { FileName = "powercfg", Arguments = $"/setactive {m.Value}", CreateNoWindow = true, UseShellExecute = false })?.WaitForExit();
                                    scriviLog("Piano energetico applicato con successo.");
                                    trovato = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (!trovato) scriviLog("Impossibile trovare il piano.");
                }
            }
            scriviLog(">>> [OK] OPERAZIONE COMPLETATA.");
        }
        catch (Exception ex) { scriviLog($">>> [ERRORE] Impossibile applicare il piano energetico: {ex.Message}"); }
    }
}
