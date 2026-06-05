using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SftpSync.Core;
using SftpSync.Services;
using System.IO;

namespace SftpSync.UI
{
    public class MainWindow : Window
    {
        private readonly ConfigService _configService;
        private readonly SftpService _sftpService;
        private readonly SyncEngine _syncEngine;
        private readonly EncryptionService _encryptionService;
        
        private TextBox? _txtStatus;
        private Button? _btnTest;
        private Button? _btnStart;
        private ComboBox? _cmbServers; // Notre nouvelle liste déroulante

        public MainWindow()
        {
            Title = "SftpSync - Assistant de Transfert";
            Height = 380; // Légèrement agrandie pour faire de la place au sélecteur
            Width = 720;
            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            _configService = new ConfigService();
            _sftpService = new SftpService();
            _syncEngine = new SyncEngine();
            _encryptionService = new EncryptionService();

            BuildUserInterface();
            PreconfigureDemoIfNeeded();
            RefreshServerList(); // Charger les serveurs au démarrage
        }

        private void BuildUserInterface()
        {
            Grid mainGrid = new Grid { Margin = new Thickness(20) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Titre
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Sélecteur de serveur
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

            // 1B. NOUVEAU : Zone de sélection du serveur active
            StackPanel serverSelectionPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            serverSelectionPanel.Children.Add(new TextBlock { Text = "Serveur actif : ", VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.Bold });
            
            _cmbServers = new ComboBox { Width = 250, Height = 25, VerticalAlignment = VerticalAlignment.Center };
            serverSelectionPanel.Children.Add(_cmbServers);
            
            Grid.SetRow(serverSelectionPanel, 1);
            mainGrid.Children.Add(serverSelectionPanel);

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
            buttonPanel.Children.Add(btnConfig); 
            buttonPanel.Children.Add(btnRules);
            
            Grid.SetRow(buttonPanel, 2);
            mainGrid.Children.Add(buttonPanel);

            // 3. Zone de Logs
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
            Grid.SetRow(groupBox, 3);
            mainGrid.Children.Add(groupBox);

            Content = mainGrid;
        }

        // Remplit la liste déroulante avec les serveurs enregistrés
        private void RefreshServerList()
        {
            if (_cmbServers == null) return;
            
            _cmbServers.Items.Clear();
            var connections = _configService.GetConnections();
            
            foreach (var conn in connections)
            {
                _cmbServers.Items.Add(conn.Name);
            }

            // Sélectionner le premier serveur par défaut s'il y en a
            if (_cmbServers.Items.Count > 0)
            {
                _cmbServers.SelectedIndex = 0;
            }
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

            // On récupère le serveur sélectionné dans la liste déroulante !
            var selectedConn = _configService.GetConnections()[_cmbServers.SelectedIndex];
            _txtStatus?.AppendText($"[SFTP] Tentative de connexion à [{selectedConn.Name}] -> {selectedConn.Host}:{selectedConn.Port}...\r\n");

            var result = await _sftpService.TestConnectionAsync(selectedConn);

            if (result.success)
            {
                _txtStatus?.AppendText($"[SUCCÈS] Connexion réussie ! Empreinte : {result.fingerprint}\r\n");
            }
            else
            {
                _txtStatus?.AppendText($"[ÉCHEC] Connexion échouée : {result.errorMessage}\r\n");
            }
        }

       private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. On récupère la liste des règles sauvegardées en JSON
                var activeRules = _configService.GetRules();

                if (activeRules.Count == 0)
                {
                    _txtStatus?.AppendText("[ATTENTION] Aucune règle de synchronisation n'est configurée. Créez-en une via 'Config Règles'.\r\n");
                    return;
                }

                // 2. On arrête le moteur s'il tournait déjà pour le réinitialiser proprement
                _syncEngine.Stop();

                // 3. On injecte les règles et les connexions dans le moteur de suivi
                _syncEngine.Initialize(activeRules, _configService.GetConnections());

                // On s'abonne aux logs du moteur
                _syncEngine.OnLog += (message) => 
                {
                    string logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";

                    // 1. Affichage à l'écran (IHM)
                    Dispatcher.Invoke(() => _txtStatus?.AppendText($"{logLine}\r\n"));

                    // 2. Écriture dans un fichier texte journalier
                    try
                    {
                        string logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                        if (!Directory.Exists(logFolder)) Directory.CreateDirectory(logFolder);

                        string logFile = Path.Combine(logFolder, $"log_{DateTime.Now:yyyy-MM-dd}.txt");
                        File.AppendAllText(logFile, logLine + Environment.NewLine);
                    }
                    catch
                    {
                        // On ignore silencieusement les erreurs d'écriture de log pour ne pas bloquer le moteur
                    }
                };

                // 4. On lance la surveillance des dossiers locaux
                _syncEngine.Start();

                _txtStatus?.AppendText("[MOTEUR] Surveillance active ! Dossiers surveillés :\r\n");
                foreach (var rule in activeRules)
                {
                    if (rule.IsEnabled)
                    {
                        _txtStatus?.AppendText($" -> Règle '{rule.Name}' : {rule.LocalFolder} ({rule.FileFilter}) -> Distant: {rule.RemoteFolder}\r\n");
                    }
                }

                if (_btnStart != null) _btnStart.IsEnabled = false;
            }
            catch (Exception ex)
            {
                _txtStatus?.AppendText($"[ERREUR] Impossible de lancer le moteur : {ex.Message}\r\n");
            }
        }

        private void BtnConfig_Click(object sender, RoutedEventArgs e)
        {
            ConnectionWindow configWin = new ConnectionWindow();
            configWin.Owner = this;
            configWin.ShowDialog();
            
            // Une fois la fenêtre de config fermée, on rafraîchit la liste pour voir les nouveautés !
            RefreshServerList();
        }

        private void BtnRules_Click(object sender, RoutedEventArgs e)
        {
            RuleWindow ruleWin = new RuleWindow();
            ruleWin.Owner = this;
            ruleWin.ShowDialog();
            
            // Recharger si nécessaire
            RefreshServerList();
        }
    }
}