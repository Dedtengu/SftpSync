using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SftpSync.Core;
using SftpSync.Services;

namespace SftpSync.UI
{
    public class MainWindow : Window
    {
        private ConfigService _configService;
        private readonly SftpService _sftpService;
        private readonly SyncEngine _syncEngine;
        private readonly EncryptionService _encryptionService;
        
        private TextBox? _txtStatus;
        private Button? _btnTest;
        private Button? _btnStart;
        private Button? _btnStop;
        private ComboBox? _cmbServers;
        private TextBox? _txtLogPath; // NOUVEAU : Champ pour le chemin du log

        public MainWindow()
        {
            Title = "SftpSync - Assistant de Transfert";
            Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/app_icon.ico"));
            Height = 420; // Légèrement agrandi pour le champ de log
            Width = 820;
            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            _configService = new ConfigService();
            _sftpService = new SftpService();
            _syncEngine = new SyncEngine();
            _encryptionService = new EncryptionService();

            // --- GESTION DU DÉMARRAGE AUTOMATIQUE MULTI-RÈGLES ---
            Loaded += (s, e) =>
            {
                try
                {
                    // Au démarrage initial, on charge le chemin de log sauvegardé s'il existe
                    // (Adapte cette ligne selon comment ton ConfigService stocke les strings globales)
                    // _txtLogPath.Text = _configService.GetLogFolder(); 

                    BtnStart_Click(null!, new RoutedEventArgs());
                }
                catch (Exception ex)
                {
                    LogAction($"[ERREUR AUTO-START] {ex.Message}");
                }
            };

            BuildUserInterface();
            PreconfigureDemoIfNeeded();
            RefreshServerList();
        }

        private void BuildUserInterface()
        {
            Grid mainGrid = new Grid { Margin = new Thickness(20) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Titre
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Sélecteur de serveur
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // NOUVEAU : Option Log
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Boutons
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Logs

            // 1. Titre
            TextBlock title = new TextBlock
            {
                Text = "Contrôle SftpSync",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(title, 0);
            mainGrid.Children.Add(title);

            // 1B. Zone de sélection du serveur active
            StackPanel serverSelectionPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            serverSelectionPanel.Children.Add(new TextBlock { Text = "Serveur actif : ", VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.Bold });
            _cmbServers = new ComboBox { Width = 250, Height = 25, VerticalAlignment = VerticalAlignment.Center };
            serverSelectionPanel.Children.Add(_cmbServers);
            Grid.SetRow(serverSelectionPanel, 1);
            mainGrid.Children.Add(serverSelectionPanel);

            // 1C. NOUVEAU : Zone de configuration du chemin de Log
            StackPanel logConfigPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            logConfigPanel.Children.Add(new TextBlock { Text = "Dossier des Logs : ", VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.Bold });
            _txtLogPath = new TextBox { Width = 400, Height = 25, VerticalAlignment = VerticalAlignment.Center, ToolTip = "Laisser vide pour désactiver l'écriture des fichiers logs texte." };
            // Optionnel : attribuer une valeur par défaut au premier lancement
            _txtLogPath.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"); 
            logConfigPanel.Children.Add(_txtLogPath);
            Grid.SetRow(logConfigPanel, 2);
            mainGrid.Children.Add(logConfigPanel);

            // 2. Boutons d'action
            StackPanel buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            
            _btnTest = new Button
            {
                Content = "1. Tester Connexion",
                Width = 140,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };
            _btnTest.Click += BtnTest_Click;

            _btnStart = new Button
            {
                Content = "2. Lancer Surveillance",
                Width = 150,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };
            _btnStart.Click += BtnStart_Click;

            _btnStop = new Button 
            { 
                Content = "⏸ Arrêter la Surveillance", 
                Width = 160,
                Height = 35, 
                Margin = new Thickness(10, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)), 
                Foreground = Brushes.White, 
                FontWeight = FontWeights.Bold, 
                IsEnabled = false 
            };
            _btnStop.Click += BtnStop_Click;

            Button btnConfig = new Button
            {
                Content = "⚙️ Config Serveurs",
                Width = 140,
                Height = 35,
                Margin = new Thickness(10, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };
            btnConfig.Click += BtnConfig_Click;

            Button btnRules = new Button
            {
                Content = "📁 Config Règles",
                Width = 130,
                Height = 35,
                Margin = new Thickness(10, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };
            btnRules.Click += BtnRules_Click;

            buttonPanel.Children.Add(_btnTest);
            buttonPanel.Children.Add(_btnStart);
            buttonPanel.Children.Add(_btnStop);
            buttonPanel.Children.Add(btnConfig); 
            buttonPanel.Children.Add(btnRules);
            
            Grid.SetRow(buttonPanel, 3);
            mainGrid.Children.Add(buttonPanel);

            // 3. Zone de Logs IHM
            GroupBox groupBox = new GroupBox { Header = "Journal d'activité en direct", Padding = new Thickness(10) };
            _txtStatus = new TextBox
            {
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.Wrap,
                Background = Brushes.White,
                FontFamily = new FontFamily("Consolas")
            };
            groupBox.Content = _txtStatus;
            Grid.SetRow(groupBox, 4);
            mainGrid.Children.Add(groupBox);

            Content = mainGrid;
        }

        private void RefreshServerList()
        {
            if (_cmbServers == null) return;
            
            _cmbServers.Items.Clear();
            var connections = _configService.GetConnections();
            
            foreach (var conn in connections)
            {
                _cmbServers.Items.Add(conn.Name);
            }

            if (_cmbServers.Items.Count > 0) _cmbServers.SelectedIndex = 0;
        }

        private void PreconfigureDemoIfNeeded()
        {
            var connections = _configService.GetConnections();
            var rules = _configService.GetRules();

            if (connections.Count == 0)
            {
                var demoConn = new SftpConnection
                {
                    Name = "Serveur FTP Exemple",
                    Host = "127.0.0.1",
                    Port = 22,
                    Username = "andre",
                    EncryptedPassword = _encryptionService.Encrypt("MonMotDePasseSecret123")
                };
                connections.Add(demoConn);

                var demoRule = new SyncRule
                {
                    Name = "Envoi Factures XML",
                    LocalFolder = @"C:\SftpSource",
                    RemoteFolder = "/imports/factures",
                    ConnectionId = demoConn.Id,
                    FileFilter = "*.xml",
                    IsEnabled = true,
                    ArchiveAfterSend = true,
                    ArchiveFolder = @"C:\SftpArchive"
                };
                rules.Add(demoRule);

                _configService.Save();
            }
        }

        private async void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            if (_cmbServers == null || _cmbServers.SelectedIndex == -1)
            {
                MessageBox.Show("Veuillez sélectionner un serveur dans la liste avant de tester.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedConn = _configService.GetConnections()[_cmbServers.SelectedIndex];
            LogAction($"[SFTP] Tentative de connexion à [{selectedConn.Name}] -> {selectedConn.Host}:{selectedConn.Port}...");

            var result = await _sftpService.TestConnectionAsync(selectedConn);

            if (result.success)
            {
                LogAction($"[SUCCÈS] Connexion réussie ! Empreinte : {result.fingerprint}");
            }
            else
            {
                LogAction($"[ÉCHEC] Connexion échouée : {result.errorMessage}");
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
               // On recrée l'instance pour forcer la relecture fraîche du fichier JSON
                _configService = new ConfigService();
                
                var activeRules = _configService.GetRules();

                if (sender == null) // Auto-start au chargement
                {
                    activeRules = activeRules.Where(r => r.IsEnabled && r.IsAutoStart).ToList();
                    if (activeRules.Count == 0) return;
                }
                else
                {
                    activeRules = activeRules.Where(r => r.IsEnabled).ToList();
                }

                if (activeRules.Count == 0)
                {
                    LogAction("[ATTENTION] Aucune règle active à surveiller.");
                    return;
                }

                _syncEngine.Initialize(activeRules, _configService.GetConnections());
                // On écoute les notifications du moteur pour les envoyer dans nos logs
                _syncEngine.OnLogMessage += (msg) => {
                    // WPF impose de repasser par le Dispatcher pour mettre à jour l'IHM 
                    // depuis un thread d'arrière-plan (le FileSystemWatcher)
                    Dispatcher.Invoke(() => LogAction(msg));
                };
                _syncEngine.Start();

                LogAction("[MOTEUR] Surveillance démarrée pour les règles suivantes :");
                foreach (var rule in activeRules)
                {
                    LogAction($" -> [{rule.Name}] : {rule.LocalFolder} -> {rule.RemoteFolder}");
                }

                if (_btnStart != null) _btnStart.IsEnabled = false;
                if (_btnStop != null) _btnStop.IsEnabled = true;
                if (_txtLogPath != null) _txtLogPath.IsEnabled = false; // Bloquer le champ pendant la surveillance
            }
            catch (Exception ex)
            {
                LogAction($"[ERREUR MOTEUR] Impossible de démarrer : {ex.Message}");
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _syncEngine.OnLogMessage -= (msg) => { }; // Nettoyage de l'écouteur
                _syncEngine.Stop();
                LogAction("[MOTEUR] Surveillance mise en pause. Tous les dossiers locaux sont libérés.");
                
                if (_btnStart != null) _btnStart.IsEnabled = true;
                if (_btnStop != null) _btnStop.IsEnabled = false;
                if (_txtLogPath != null) _txtLogPath.IsEnabled = true; // Libérer le champ
            }
            catch (Exception ex)
            {
                LogAction($"[ERREUR MOTEUR] Impossible d'arrêter proprement : {ex.Message}");
            }
        }

        private void BtnConfig_Click(object sender, RoutedEventArgs e)
        {
            ConnectionWindow configWin = new ConnectionWindow();
            configWin.Owner = this;
            configWin.ShowDialog();
            RefreshServerList();
        }

        private void BtnRules_Click(object sender, RoutedEventArgs e)
        {
            RuleWindow ruleWin = new RuleWindow();
            ruleWin.Owner = this;
            ruleWin.ShowDialog();
            
            // CRUCIAL : Recharger le service de config local pour prendre en compte immédiatement les changements graphiques
            _configService = new ConfigService(); 
            RefreshServerList();
        }

        // centralisation des logs IHM + Écriture conditionnelle dans le fichier physique
        private void LogAction(string message)
        {
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            
            // 1. Affichage dans la boîte de dialogue de l'IHM
            _txtStatus?.AppendText(logLine + Environment.NewLine);
            _txtStatus?.ScrollToEnd();

            // 2. Écriture physique optionnelle si le chemin est renseigné
            if (_txtLogPath != null && !string.IsNullOrWhiteSpace(_txtLogPath.Text))
            {
                try
                {
                    string logFolder = _txtLogPath.Text.Trim();
                    if (!Directory.Exists(logFolder)) 
                    {
                        Directory.CreateDirectory(logFolder);
                    }

                    string logFile = Path.Combine(logFolder, $"log_{DateTime.Now:yyyy-MM-dd}.txt");
                    File.AppendAllText(logFile, logLine + Environment.NewLine);
                }
                catch (Exception)
                {
                    // On évite de faire planter l'application globale si les droits d'écriture sur le dossier ciblé ont sauté
                }
            }
        }
    }
}