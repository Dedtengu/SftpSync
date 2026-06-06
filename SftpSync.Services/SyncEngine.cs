using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using SftpSync.Core;

namespace SftpSync.Services
{
    [SupportedOSPlatform("windows")]
    public class SyncEngine
    {
        public event Action<string>? OnLog;
        private readonly SftpService _sftpService;
        private List<SyncRule> _rules = new List<SyncRule>();
        private List<SftpConnection> _connections = new List<SftpConnection>();
        private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        public event Action<string>? OnLogMessage;

        public SyncEngine()
        {
            _sftpService = new SftpService();
        }

        /// <summary>
        /// Reçoit les règles et connexions à surveiller depuis l'IHM.
        /// </summary>
        public void Initialize(List<SyncRule> rules, List<SftpConnection> connections)
        {
            // On s'assure d'arrêter proprement la surveillance actuelle avant de modifier les règles
            Stop();
            
            _rules = rules ?? new List<SyncRule>();
            _connections = connections ?? new List<SftpConnection>();

            _watchers.Clear();

            // On prépare un FileSystemWatcher pour chaque règle active
            foreach (var rule in _rules)
            {
                if (!rule.IsEnabled || string.IsNullOrWhiteSpace(rule.LocalFolder))
                    continue;

                if (!Directory.Exists(rule.LocalFolder))
                    continue;

                var filter = string.IsNullOrWhiteSpace(rule.FileFilter) ? "*.*" : rule.FileFilter;
                
                var watcher = new FileSystemWatcher(rule.LocalFolder, filter)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                    EnableRaisingEvents = false // Sera activé au moment du Start()
                };

                // On s'abonne à l'événement en lui passant la règle correspondante
                watcher.Created += (sender, e) => OnFileDetected(e.FullPath, rule);
                
                _watchers.Add(watcher);
            }
        }

        /// <summary>
        /// Démarre la surveillance de tous les dossiers configurés.
        /// </summary>
        public void Start()
        {
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = true;
            }
        }

        /// <summary>
        /// Arrête tous les surveillants de dossiers.
        /// </summary>
        public void Stop()
        {
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            _watchers.Clear();
        }

        private void OnFileDetected(string localFilePath, SyncRule rule)
        {
            string fileName = Path.GetFileName(localFilePath);
            OnLog?.Invoke($"[MOTEUR] Fichier détecté : {fileName}");

            Task.Run(async () =>
            {
                // 1. Attente de la libération du fichier
                if (!WaitForFileReady(localFilePath))
                {
                    OnLog?.Invoke($"[ERREUR] Le fichier {fileName} est verrouillé. Abandon.");
                    return;
                }

                // 2. Récupération de la connexion
                var connection = _connections.FirstOrDefault(c => c.Id == rule.ConnectionId);
                if (connection == null) return;

                // --- GESTION DES DOUBLONS DISTANTS (Via renommage local préventif si besoin) ---
                // Note : Pour éviter d'écraser sur le SFTP, on peut vérifier si le fichier doit être renommé.
                // Idéalement, SftpService.UploadFileAtomicAsync devrait gérer le conflit, 
                // mais on va sécuriser l'archivage local ici pour commencer.

                try
                {
                    OnLog?.Invoke($"[SFTP] Connexion à {connection.Host} et envoi de {fileName}...");
                    bool transferSuccess = await _sftpService.UploadFileAtomicAsync(connection, localFilePath, rule.RemoteFolder);

                    if (transferSuccess)
                    {
                        OnLog?.Invoke($"[SUCCÈS] Transfert réussi pour {fileName}");
                        OnLogMessage?.Invoke($"[TRANSFERT] Fichier '{fileName}' envoyé avec succès vers le serveur.");
                        
                        // 4. Gestion Post-Transfert avec évitement des doublons d'archive
                        HandlePostTransferSuccessWithUniqueName(localFilePath, rule);
                    }
                    else
                    {
                        OnLog?.Invoke($"[ÉCHEC] Le transfert de {fileName} a échoué.");
                    }
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"[ERREUR] {fileName} : {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Déplace le fichier vers l'archive en générant un nom unique s'il existe déjà
        /// </summary>
        private void HandlePostTransferSuccessWithUniqueName(string filePath, SyncRule rule)
        {
            try
            {
                if (rule.ArchiveAfterSend && !string.IsNullOrEmpty(rule.ArchiveFolder))
                {
                    if (!Directory.Exists(rule.ArchiveFolder))
                    {
                        Directory.CreateDirectory(rule.ArchiveFolder);
                    }

                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string extension = Path.GetExtension(filePath);
                    string destinationPath = Path.Combine(rule.ArchiveFolder, Path.GetFileName(filePath));
                    
                    int counter = 1;
                    // Tant que le fichier existe déjà dans l'archive, on incrémente le nom
                    while (File.Exists(destinationPath))
                    {
                        string newFileName = $"{fileName}_{counter}{extension}";
                        destinationPath = Path.Combine(rule.ArchiveFolder, newFileName);
                        counter++;
                    }

                    File.Move(filePath, destinationPath);
                    OnLog?.Invoke($"[ARCHIVE] Fichier archivé sous : {Path.GetFileName(destinationPath)}");
                    OnLogMessage?.Invoke($"[ARCHIVE] Fichier '{Path.GetFileName(destinationPath)}' déplacé dans le dossier d'archive.");
                }
                else
                {
                    File.Delete(filePath);
                    OnLog?.Invoke($"[MOTEUR] Fichier source supprimé (pas d'archive requise).");
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[ERREUR ARCHIVE] Impossible de traiter le fichier local : {ex.Message}");
            }
        }

        private void HandlePostTransferSuccess(string filePath, SyncRule rule)
        {
            try
            {
                if (rule.ArchiveAfterSend && !string.IsNullOrEmpty(rule.ArchiveFolder))
                {
                    if (!Directory.Exists(rule.ArchiveFolder))
                    {
                        Directory.CreateDirectory(rule.ArchiveFolder);
                    }

                    string destinationPath = Path.Combine(rule.ArchiveFolder, Path.GetFileName(filePath));
                    
                    if (File.Exists(destinationPath)) File.Delete(destinationPath);

                    File.Move(filePath, destinationPath);
                }
                else
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Gestion silencieuse
            }
        }

        /// <summary>
        /// Boucle de vérification : Attend que le fichier ne soit plus en cours d'écriture.
        /// </summary>
        private bool WaitForFileReady(string filePath)
        {
            int maxAttempts = 10;
            int delayMilliseconds = 1000;

            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    if (!File.Exists(filePath)) return false;

                    using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        return true; 
                    }
                }
                catch (IOException)
                {
                    Thread.Sleep(delayMilliseconds);
                }
            }
            return false;
        }
    }

    
}