# AppManutenzione

Applicazione desktop per Windows che raccoglie in un'unica finestra alcuni strumenti di pulizia, manutenzione e monitoraggio del PC. È un progetto personale per imparare C# e Windows Forms.

> \*\*Nota sullo sviluppo:\*\* il progetto è stato sviluppato con l'aiuto di strumenti di intelligenza artificiale.



## Funzioni

**Pulizia**

* Analisi dello spazio recuperabile (cartelle Temp e Cestino)
* Pulizia dei file temporanei e del Cestino, con i MB liberati mostrati a fine operazione
* Svuotamento della cache DNS (`ipconfig /flushdns`)
* "Pulizia profonda": riavvia il servizio Windows Update (`wuauserv`)
* Gestore file duplicati: scansiona una cartella, raggruppa i file per dimensione e poi confronta l'hash SHA-256 per trovare copie identiche. In ogni gruppo il primo file resta deselezionato come originale

**Manutenzione**

* Verifica dei file di sistema (`sfc /scannow`)
* Riparazione dell'immagine di Windows (`DISM`)
* Ottimizzazione dei dischi (`defrag`)
* Aggiornamento dei programmi installati (`winget upgrade --all`)
* Backup dei profili Wi-Fi in formato XML nella cartella Documenti
* Gestore avvio: elenca e rimuove i programmi in `HKCU\\...\\Run`

**Energia**

* Modalità ECO, NORMALE e GAMING, che attivano i piani energetici di Windows tramite `powercfg`

**Dashboard (si aggiorna ogni 2,5 secondi)**

* Temperature di GPU e scheda madre (tramite LibreHardwareMonitor)
* RAM usata e le 3 applicazioni non di sistema più pesanti
* Spazio libero sui dischi fissi
* Latenza di rete (ping verso 1.1.1.1)

## Avvertenze

* L'app richiede i **privilegi di amministratore** (il manifest li richiede all'avvio): molti comandi, come SFC, DISM e i servizi, non funzionano senza.
* **L'eliminazione dei file duplicati è definitiva**: i file non passano dal Cestino. Controlla la lista prima di confermare.
* **Il gestore avvio rimuove la voce dal registro**, non la disattiva in modo reversibile.
* **Il backup Wi-Fi salva le password in chiaro** (`key=clear`). Non condividere quei file.
* Le temperature della CPU non sono mostrate: la lettura è disattivata nel codice.

## Requisiti

* Windows 10 o 11 (64 bit)
* [.NET 10 SDK](https://dotnet.microsoft.com/download) per compilare il progetto

## Come avviarlo

```
git clone https://github.com/<tuo-username>/AppManutenzione.git
cd AppManutenzione
dotnet run
```

Oppure apri `AppManutenzione.csproj` con Visual Studio e premi Avvia. Se lanci il programma da un terminale, aprilo come amministratore.

## Struttura del codice

|File|Cosa fa|
|-|-|
|`Form1.cs`|Interfaccia grafica e collegamento dei pulsanti ai comandi|
|`EsecutoreComandi.cs`|Esegue comandi CMD/PowerShell in modo asincrono e scrive l'output nel log|
|`CacciatoreDuplicati.cs`|Ricerca dei file duplicati con raggruppamento per dimensione e hash SHA-256|
|`SensoriHardware.cs`|Lettura di RAM, dischi, processi e temperature|
|`GameBooster.cs`|Sospensione di processi con `NtSuspendProcess` (scritto ma non ancora collegato all'interfaccia)|
|`Program.cs`, `Form1.Designer.cs`|Avvio dell'applicazione e codice generato da Visual Studio|

## Tecnologie

C# · .NET 10 · Windows Forms · [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) (lettura sensori)

