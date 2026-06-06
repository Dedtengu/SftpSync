using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using System.Collections.Generic; // AJOUTÉ : Nécessaire pour Dictionary
using System.Linq; // AJOUTÉ : Nécessaire pour l'utilisation de .Where() et .ToList()
using SftpSync.Core;
using SftpSync.Services;

namespace SftpSync.UI
{
    public class MainWindow : Window
    {
        private readonly ConfigService _configService;
        private readonly SftpService _sftpService;
        private readonly SyncEngine _syncEngine;
        private readonly EncryptionService _encryptionService;
        
        // Associe l'ID ou le nom d'une règle à son instance de surveillance active
        private readonly Dictionary<string, FileSystemWatcher> _activeWatchers = new Dictionary<string, FileSystemWatcher>();
        private Button? _btnStop; // Déclaration du bouton Stop
        
        private TextBox? _txtStatus;
        private Button? _btnTest;
        private Button? _btnStart;
        private ComboBox? _cmbServers; // Notre nouvelle liste déroulante

        public MainWindow()
        {
            Title = "SftpSync - Assistant de Transfert";
            Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/app_icon.ico"));
            Height = 380; // Légèrement agrandie pour faire de la place au sélecteur
            Width = 850;
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
                    // On passe un sender "null" pour indiquer au code que c'est le démarrage automatique
                    BtnStart_Click(null!, new RoutedEventArgs());
                }
                catch (Exception ex)
                {
                    _txtStatus?.AppendText($"[ERREUR AUTO-START] {ex.Message}\r\n");
                }
            };

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

            _btnStop = new Button 
            { 
                Content = "⏸ Arrêter la Surveillance", 
                Width = 160,
                Height = 35, 
                Margin = new Thickness(10, 0, 0, 0), // Aligné horizontalement
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

            // Ajout ordonné de tous les boutons dans le même bandeau horizontal
            buttonPanel.Children.Add(_btnTest);
            buttonPanel.Children.Add(_btnStart);
            buttonPanel.Children.Add(_btnStop); // CORRIGÉ : Ajouté au bon panel et au bon endroit
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
                var activeRules = _configService.GetRules();

                // Si le clic vient du démarrage automatique (sender est null), 
                // on filtre uniquement les règles cochées "IsAutoStart"
                if (sender == null)
                {
                    activeRules = activeRules.Where(r => r.IsEnabled && r.IsAutoStart).ToList();
                    if (activeRules.Count == 0) return; // Rien à lancer en auto, on sort discrètement
                }
                else
                {
                    // Sinon (clic manuel), on prend toutes les règles activées
                    activeRules = activeRules.Where(r => r.IsEnabled).ToList();
                }

                if (activeRules.Count == 0)
                {
                    _txtStatus?.AppendText("[ATTENTION] Aucune règle active à surveiller.\r\n");
                    return;
                }

                // On injecte les règles filtrées dans notre moteur de services
                _syncEngine.Initialize(activeRules, _configService.GetConnections());
                _syncEngine.Start();

                _txtStatus?.AppendText("[MOTEUR] Surveillance démarrée pour les règles suivantes :\r\n");
                foreach (var rule in activeRules)
                {
                    _txtStatus?.AppendText($" -> [{rule.Name}] : {rule.LocalFolder} -> {rule.RemoteFolder}\r\n");
                }

                if (_btnStart != null) _btnStart.IsEnabled = false;
                if (_btnStop != null) _btnStop.IsEnabled = true;
            }
            catch (Exception ex)
            {
                _txtStatus?.AppendText($"[ERREUR MOTEUR] Impossible de démarrer : {ex.Message}\r\n");
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _syncEngine.Stop();
                _txtStatus?.AppendText("[MOTEUR] Surveillance mise en pause. Tous les dossiers locaux sont libérés.\r\n");
                
                if (_btnStart != null) _btnStart.IsEnabled = true;
                if (_btnStop != null) _btnStop.IsEnabled = false;
            }
            catch (Exception ex)
            {
                _txtStatus?.AppendText($"[ERREUR MOTEUR] Impossible d'arrêter proprement : {ex.Message}\r\n");
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
            
            RefreshServerList();
        }
    }
}