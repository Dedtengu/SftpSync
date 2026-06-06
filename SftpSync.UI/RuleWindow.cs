using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SftpSync.Core;
using SftpSync.Services;

namespace SftpSync.UI
{
    public class RuleWindow : Window
    {
        private readonly ConfigService _configService;
        
        private ListBox? _lstRules;
        private TextBox? _txtRuleName;
        private ComboBox? _cmbServers;
        private TextBox? _txtLocalFolder;
        private TextBox? _txtRemoteFolder;
        private TextBox? _txtFileFilter;
        private CheckBox? _chkArchive;
        private TextBox? _txtArchiveFolder;
        private CheckBox? _chkAutoStart;

        public RuleWindow()
        {
            Title = "Gestion des Règles de Synchronisation";
            Height = 500;
            Width = 650;
            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _configService = new ConfigService();

            BuildUserInterface();
            LoadServersIntoComboBox();
            RefreshRuleList();
        }

        private void BuildUserInterface()
        {
            Grid mainGrid = new Grid { Margin = new Thickness(15) };
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // --- GAUCHE : Liste des règles ---
            Grid leftGrid = new Grid();
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            leftGrid.Children.Add(new TextBlock { Text = "Règles actives :", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            
            _lstRules = new ListBox { Margin = new Thickness(0, 0, 10, 10) };
            _lstRules.SelectionChanged += LstRules_SelectionChanged;
            Grid.SetRow(_lstRules, 1);
            leftGrid.Children.Add(_lstRules);

            // Grille pour deux boutons
            Grid actionButtonsGrid = new Grid { Margin = new Thickness(0, 0, 10, 0) };
            actionButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Button btnNew = new Button { Content = "➕ Nouvelle", Height = 30, Margin = new Thickness(0, 0, 5, 0), Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)), Foreground = Brushes.White, FontWeight = FontWeights.Bold };
            btnNew.Click += BtnNew_Click;
            Grid.SetColumn(btnNew, 0);
            actionButtonsGrid.Children.Add(btnNew);

            Button btnDelete = new Button { Content = "❌ Supprimer", Height = 30, Margin = new Thickness(5, 0, 0, 0), Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)), Foreground = Brushes.White, FontWeight = FontWeights.Bold };
            btnDelete.Click += BtnDelete_Click;
            Grid.SetColumn(btnDelete, 1);
            actionButtonsGrid.Children.Add(btnDelete);

            Grid.SetRow(actionButtonsGrid, 2);
            leftGrid.Children.Add(actionButtonsGrid);

            Grid.SetColumn(leftGrid, 0);
            mainGrid.Children.Add(leftGrid);

            // --- DROITE : Formulaire ---
            StackPanel formPanel = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };

            formPanel.Children.Add(new TextBlock { Text = "Nom de la règle :", Margin = new Thickness(0, 5, 0, 2) });
            _txtRuleName = new TextBox { Height = 25 };
            formPanel.Children.Add(_txtRuleName);

            formPanel.Children.Add(new TextBlock { Text = "Serveur SFTP associé :", Margin = new Thickness(0, 10, 0, 2) });
            _cmbServers = new ComboBox { Height = 25 };
            formPanel.Children.Add(_cmbServers);

            formPanel.Children.Add(new TextBlock { Text = "Dossier Local à surveiller (ex: C:\\SftpSource) :", Margin = new Thickness(0, 10, 0, 2) });
            _txtLocalFolder = new TextBox { Height = 25 };
            formPanel.Children.Add(_txtLocalFolder);

            formPanel.Children.Add(new TextBlock { Text = "Dossier Distant (ex: /imports/factures) :", Margin = new Thickness(0, 10, 0, 2) });
            _txtRemoteFolder = new TextBox { Height = 25 };
            formPanel.Children.Add(_txtRemoteFolder);

            formPanel.Children.Add(new TextBlock { Text = "Filtre de fichiers (Défaut : *.* ou *.xml) :", Margin = new Thickness(0, 10, 0, 2) });
            _txtFileFilter = new TextBox { Height = 25, Text = "*.*" };
            formPanel.Children.Add(_txtFileFilter);

            _chkArchive = new CheckBox { Content = "Archiver localement après envoi", Margin = new Thickness(0, 15, 0, 5) };
            formPanel.Children.Add(_chkArchive);

            formPanel.Children.Add(new TextBlock { Text = "Dossier d'archive (ex: C:\\SftpArchive) :", Margin = new Thickness(0, 5, 0, 2) });
            _txtArchiveFolder = new TextBox { Height = 25 };
            formPanel.Children.Add(_txtArchiveFolder);

            _chkAutoStart = new CheckBox { Content = "🚀 Lancer la surveillance automatiquement au démarrage", Margin = new Thickness(0, 5, 0, 5) };
            formPanel.Children.Add(_chkAutoStart);

            Button btnSave = new Button { Content = "💾 Enregistrer la Règle", Height = 35, Margin = new Thickness(0, 20, 0, 0), Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)), Foreground = Brushes.White, FontWeight = FontWeights.Bold };
            btnSave.Click += BtnSave_Click;
            formPanel.Children.Add(btnSave);

            Grid.SetColumn(formPanel, 1);
            mainGrid.Children.Add(formPanel);

            Content = mainGrid;
        }

        private void LoadServersIntoComboBox()
        {
            if (_cmbServers == null) return;
            _cmbServers.Items.Clear();
            foreach (var conn in _configService.GetConnections())
            {
                _cmbServers.Items.Add(conn.Name);
            }
        }

        private void RefreshRuleList()
        {
            if (_lstRules == null) return;
            _lstRules.Items.Clear();
            foreach (var rule in _configService.GetRules())
            {
                _lstRules.Items.Add(rule.Name);
            }
        }

        private void LstRules_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_lstRules == null || _lstRules.SelectedIndex == -1) return;

            var rule = _configService.GetRules()[_lstRules.SelectedIndex];
            if (_txtRuleName != null) _txtRuleName.Text = rule.Name;
            if (_txtLocalFolder != null) _txtLocalFolder.Text = rule.LocalFolder;
            if (_txtRemoteFolder != null) _txtRemoteFolder.Text = rule.RemoteFolder;
            if (_txtFileFilter != null) _txtFileFilter.Text = rule.FileFilter;
            if (_chkArchive != null) _chkArchive.IsChecked = rule.ArchiveAfterSend;
            if (_txtArchiveFolder != null) _txtArchiveFolder.Text = rule.ArchiveFolder;

            if (_cmbServers != null)
            {
                var connections = _configService.GetConnections();
                int idx = connections.FindIndex(c => c.Id == rule.ConnectionId);
                _cmbServers.SelectedIndex = idx != -1 ? idx : 0;
            }

            if (_chkAutoStart != null) _chkAutoStart.IsChecked = rule.IsAutoStart;
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            if (_lstRules != null) _lstRules.SelectedIndex = -1;
            if (_txtRuleName != null) _txtRuleName.Text = "Nouvelle Règle";
            if (_txtLocalFolder != null) _txtLocalFolder.Clear();
            if (_txtRemoteFolder != null) _txtRemoteFolder.Clear();
            if (_txtFileFilter != null) _txtFileFilter.Text = "*.*";
            if (_chkArchive != null) _chkArchive.IsChecked = false;
            if (_txtArchiveFolder != null) _txtArchiveFolder.Clear();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtRuleName?.Text) || string.IsNullOrWhiteSpace(_txtLocalFolder?.Text) || string.IsNullOrWhiteSpace(_txtRemoteFolder?.Text))
            {
                MessageBox.Show("Le nom, le dossier local et le dossier distant sont obligatoires.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_cmbServers == null || _cmbServers.SelectedIndex == -1)
            {
                MessageBox.Show("Veuillez associer un serveur à cette règle.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedConn = _configService.GetConnections()[_cmbServers.SelectedIndex];
            var rules = _configService.GetRules();

            if (_lstRules != null && _lstRules.SelectedIndex != -1)
            {
                // Modification
                var current = rules[_lstRules.SelectedIndex];
                current.Name = _txtRuleName.Text;
                current.LocalFolder = _txtLocalFolder.Text;
                current.RemoteFolder = _txtRemoteFolder.Text;
                current.FileFilter = _txtFileFilter?.Text ?? "*.*";
                current.ConnectionId = selectedConn.Id;
                current.ArchiveAfterSend = _chkArchive?.IsChecked ?? false;
                current.ArchiveFolder = _txtArchiveFolder?.Text ?? string.Empty;
            }
            else
            {
                // Création
                var newRule = new SyncRule
                {
                    Name = _txtRuleName.Text,
                    LocalFolder = _txtLocalFolder.Text,
                    RemoteFolder = _txtRemoteFolder.Text,
                    FileFilter = _txtFileFilter?.Text ?? "*.*",
                    ConnectionId = selectedConn.Id,
                    IsEnabled = true,
                    ArchiveAfterSend = _chkArchive?.IsChecked ?? false,
                    ArchiveFolder = _txtArchiveFolder?.Text ?? string.Empty
                };
                rules.Add(newRule);
            }
            
           // --- LOGIQUE AUTO-START MULTI-RÈGLES ---
            var targetRule = (_lstRules != null && _lstRules.SelectedIndex != -1) ? rules[_lstRules.SelectedIndex] : rules.Last();
            targetRule.IsAutoStart = _chkAutoStart?.IsChecked ?? false;

            _configService.Save();
            RefreshRuleList();
            MessageBox.Show("Règle enregistrée avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_lstRules == null || _lstRules.SelectedIndex == -1)
            {
                MessageBox.Show("Veuillez sélectionner une règle à supprimer dans la liste.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rules = _configService.GetRules();
            var selectedRule = rules[_lstRules.SelectedIndex];

            var result = MessageBox.Show($"Êtes-vous sûr de vouloir supprimer la règle '{selectedRule.Name}' ?", "Confirmation de suppression", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                rules.RemoveAt(_lstRules.SelectedIndex);
                _configService.Save();
                RefreshRuleList();
                BtnNew_Click(sender, e); // Réinitialise le formulaire à blanc
                MessageBox.Show("Règle supprimée avec succès.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}