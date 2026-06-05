using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SftpSync.Core;

namespace SftpSync.Services
{
    public class AppConfig
    {
        public List<SftpConnection> Connections { get; set; } = new List<SftpConnection>();
        public List<SyncRule> Rules { get; set; } = new List<SyncRule>();
    }

    public class ConfigService
    {
        private readonly string _configFilePath;
        private AppConfig _currentConfig;

        public ConfigService()
        {
            // On détermine un chemin système propre sous Windows (ex: C:\Users\Nom\AppData\Roaming\SftpSync)
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderPath = Path.Combine(appDataPath, "SftpSync");
            
            // Si le dossier n'existe pas, on le crée
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            _configFilePath = Path.Combine(folderPath, "config.json");
            _currentConfig = LoadOrCreateConfig();
        }

        // Récupère la liste des connexions
        public List<SftpConnection> GetConnections() => _currentConfig.Connections;

        // Récupère la liste des règles
        public List<SyncRule> GetRules() => _currentConfig.Rules;

        /// <summary>
        /// Sauvegarde la configuration actuelle dans le fichier JSON.
        /// </summary>
        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(_currentConfig, options);
                File.WriteAllText(_configFilePath, jsonString);
            }
            catch (Exception ex)
            {
                // Si la sauvegarde échoue (droits d'accès par exemple), on lève une erreur explicite
                throw new InvalidOperationException("Impossible de sauvegarder la configuration.", ex);
            }
        }

        /// <summary>
        /// Charge le fichier JSON ou en crée un tout neuf s'il n'existe pas.
        /// </summary>
        private AppConfig LoadOrCreateConfig()
        {
            if (!File.Exists(_configFilePath))
            {
                var newConfig = new AppConfig();
                return newConfig;
            }

            try
            {
                string jsonString = File.ReadAllText(_configFilePath);
                var deserialized = JsonSerializer.Deserialize<AppConfig>(jsonString);
                return deserialized ?? new AppConfig();
            }
            catch
            {
                // En cas de fichier corrompu, on repart sur une configuration vide pour éviter le blocage
                return new AppConfig();
            }
        }
    }
}