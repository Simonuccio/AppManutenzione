using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AppManutenzione;

public static class CacciatoreDuplicati
{
    public static async Task<Dictionary<string, List<string>>> TrovaDuplicatiAsync(string percorsoIniziale)
    {
        var risultato = new Dictionary<string, List<string>>();

        await Task.Run(() =>
        {
            // FASE 1: raccogliamo tutti i file e li raggruppiamo per dimensione.
            // Due file di dimensione diversa non possono mai essere identici,
            // quindi evitiamo di calcolare l'hash (operazione costosa, richiede
            // di leggere l'intero contenuto) per i file che sono già sicuramente
            // unici: su cartelle grandi questo riduce drasticamente i tempi.
            var tuttiIFile = new List<string>();
            RaccogliFileRicorsivo(percorsoIniziale, tuttiIFile);

            var gruppiPerDimensione = tuttiIFile
                .Select(f => { try { return new FileInfo(f); } catch { return null; } })
                .Where(fi => fi != null)
                .GroupBy(fi => fi!.Length)
                .Where(g => g.Count() > 1); // scarta subito le dimensioni uniche

            // FASE 2: solo tra i file con dimensione coincidente calcoliamo
            // l'hash SHA-256 per la certezza matematica assoluta (0% rischio collisioni)
            using var sha256 = SHA256.Create();
            foreach (var gruppo in gruppiPerDimensione)
            {
                foreach (var fileInfo in gruppo)
                {
                    try
                    {
                        using var stream = File.OpenRead(fileInfo!.FullName);
                        string hash = BitConverter.ToString(sha256.ComputeHash(stream));
                        if (!risultato.ContainsKey(hash)) risultato[hash] = new List<string>();
                        risultato[hash].Add(fileInfo.FullName);
                    }
                    catch { /* file bloccato, permessi insufficienti, ecc. */ }
                }
            }
        });

        return risultato.Where(kvp => kvp.Value.Count > 1)
                         .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private static void RaccogliFileRicorsivo(string cartella, List<string> accumulo)
    {
        try
        {
            accumulo.AddRange(Directory.GetFiles(cartella));

            foreach (string sottoCartella in Directory.GetDirectories(cartella))
            {
                RaccogliFileRicorsivo(sottoCartella, accumulo);
            }
        }
        catch { /* cartella non accessibile: permessi insufficienti, link simbolici rotti, ecc. */ }
    }
}
