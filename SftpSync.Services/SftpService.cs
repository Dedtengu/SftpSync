using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Renci.SshNet;
using SftpSync.Core;

namespace SftpSync.Services
{
    [SupportedOSPlatform("windows")]
    public class SftpService
    {
        private readonly EncryptionService _encryptionService;

        public SftpService()
        {
            // On récupère notre outil de déchiffrement pour les mots de passe
            _encryptionService = new EncryptionService();
        }

        /// <summary>
        /// Permet de tester une connexion SFTP et de récupérer l'empreinte (fingerprint) du serveur.
        /// </summary>
        public async Task<(bool success, string fingerprint, string errorMessage)> TestConnectionAsync(SftpConnection connection)
        {
            return await Task.Run(() =>
            {
                string decryptedPassword = _encryptionService.Decrypt(connection.EncryptedPassword);
                
                try
                {
                    using (var client = new SftpClient(connection.Host, connection.Port, connection.Username, decryptedPassword))
                    {
                        string detectedFingerprint = string.Empty;

                        // On s'abonne à l'événement de vérification de la clé du serveur (Anti-MITM)
                        client.HostKeyReceived += (sender, e) =>
                        {
                            detectedFingerprint = BitConverter.ToString(e.FingerPrint).Replace("-", ":");
                            // Si une empreinte est déjà enregistrée, on valide qu'elle correspond
                            if (!string.IsNullOrEmpty(connection.ServerFingerprint) && connection.ServerFingerprint != detectedFingerprint)
                            {
                                e.CanTrust = false; // Bloque la connexion si l'empreinte a changé de manière suspecte
                            }
                            else
                            {
                                e.CanTrust = true;
                            }
                        };

                        client.Connect();
                        client.Disconnect();

                        return (true, detectedFingerprint, string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    return (false, string.Empty, ex.Message);
                }
            });
        }

        /// <summary>
        /// Transfère un fichier local de manière atomique (.tmp puis renommage) vers le serveur SFTP.
        /// </summary>
        public async Task<bool> UploadFileAtomicAsync(SftpConnection connection, string localFilePath, string remoteDirectory)
        {
            return await Task.Run(() =>
            {
                string decryptedPassword = _encryptionService.Decrypt(connection.EncryptedPassword);
                string fileName = Path.GetFileName(localFilePath);
                
                // Chemins distants : Fichier final et Fichier temporaire (.tmp)
                string remoteTempPath = Path.Combine(remoteDirectory, fileName + ".tmp").Replace("\\", "/");
                string remoteFinalPath = Path.Combine(remoteDirectory, fileName).Replace("\\", "/");

                try
                {
                    using (var client = new SftpClient(connection.Host, connection.Port, connection.Username, decryptedPassword))
                    {
                        // Vérification de sécurité de l'empreinte avant envoi
                        client.HostKeyReceived += (sender, e) =>
                        {
                            string currentFingerprint = BitConverter.ToString(e.FingerPrint).Replace("-", ":");
                            e.CanTrust = string.IsNullOrEmpty(connection.ServerFingerprint) || connection.ServerFingerprint == currentFingerprint;
                        };

                        client.Connect();

                        // 1. Envoi du fichier local sous sa forme temporaire (.tmp)
                        using (var fileStream = File.OpenRead(localFilePath))
                        {
                            client.UploadFile(fileStream, remoteTempPath);
                        }

                        // 2. Si un fichier du même nom existe déjà à l'arrivée, on applique la stratégie (ici : écraser)
                        if (client.Exists(remoteFinalPath))
                        {
                            client.DeleteFile(remoteFinalPath);
                        }

                        // 3. Renommage atomique une fois le transfert 100% complété
                        client.RenameFile(remoteTempPath, remoteFinalPath);

                        client.Disconnect();
                        return true;
                    }
                }
                catch (Exception)
                {
                    // En cas de plantage, la méthode renvoie false (la gestion de logs et retry interviendra plus haut)
                    return false;
                }
            });
        }
    }
}