using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SftpSync.Core;
using SftpSync.Services;

namespace SftpSync.UI
{
    public class ConnectionWindow : Window
    {
        private readonly ConfigService _configService;
        private readonly EncryptionService _encryptionService;

        private ListBox? _lstConnections;
        private TextBox? _txtPathName;
        private TextBox? _txtHost;
        private TextBox? _txtPort;
        private TextBox? _txtUsername;
        private PasswordBox? _txtPassword;

        public ConnectionWindow()
        {
            Title = "Gestion des Connexions SFTP";
            Height = 450;
            Width = 600;
            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _configService = new ConfigService();
            _encryptionService = new EncryptionService();

            BuildUserInterface();
            RefreshConnectionList();
        }

        private void BuildUserInterface()
        {
            Grid mainGrid = new Grid { Margin = new Thickness(15) };
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // --- COLONNE GAUCHE : Liste des serveurs ---
            Grid leftGrid = new Grid();
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock lblList = new TextBlock { Text = "Serveurs configurés :", FontWeight = FontWeights.Bold, Margin = new Thickness(0,0,0,5) };
            Grid.SetRow(lblList, 0);
            leftGrid.Children.Add(lblList);

            _lstConnections = new ListBox { Margin = new Thickness(0, 0, 10, 10) };
            _lstConnections.SelectionChanged += LstConnections_SelectionChanged;
            Grid.SetRow(_lstConnections, 1);
            leftGrid.Children.Add(_lstConnections);

            // Grille pour mettre les deux boutons côte à côte
            Grid actionButtonsGrid = new Grid { Margin = new Thickness(0, 0, 10, 0) };
            actionButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Button btnNew = new Button { Content = "➕ Nouveau", Height = 30, Margin = new Thickness(0, 0, 5, 0), Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)), Foreground = Brushes.White, FontWeight = FontWeights.Bold };
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

            // --- COLONNE DROITE : Formulaire de saisie ---
            StackPanel formPanel = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };

            formPanel.Children.Add(new TextBlock { Text = "Nom de la configuration :", Margin = new Thickness(0, 5, 0, 2) });
            _txtPathName = new TextBox { Height = 25 };
            formPanel.Children.Add(_txtPathName);

            formPanel.Children.Add(new TextBlock { Text = "Hôte (IP ou Domaine) :", Margin = new Thickness(0, 10, 0, 2) });
            _txtHost = new TextBox { Height = 25 };
            formPanel.Children.Add(_txtHost);

            formPanel.Children.Add(new TextBlock { Text = "Port SFTP (Défaut : 22) :", Margin = new Thickness(0, 10, 0, 2) });
            _txtPort = new TextBox { Height = 25, Text = "22" };
            formPanel.Children.Add(_txtPort);

            formPanel.Children.Add(new TextBlock { Text = "Nom d'utilisateur :", Margin = new Thickness(0, 10, 0, 2) });
            _txtUsername = new TextBox { Height = 25 };
            formPanel.Children.Add(_txtUsername);

            formPanel.Children.Add(new TextBlock { Text = "Mot de passe :", Margin = new Thickness(0, 10, 0, 2) });
            _txtPassword = new PasswordBox { Height = 25 };
            formPanel.Children.Add(_txtPassword);

            Button btnSave = new Button { Content = "💾 Enregistrer la configuration", Height = 35, Margin = new Thickness(0, 20, 0, 0), Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)), Foreground = Brushes.White, FontWeight = FontWeights.Bold };
            btnSave.Click += BtnSave_Click;
            formPanel.Children.Add(btnSave);

            Grid.SetColumn(formPanel, 1);
            mainGrid.Children.Add(formPanel);

            Content = mainGrid;
        }

        private void RefreshConnectionList()
        {
            if (_lstConnections == null) return;
            _lstConnections.Items.Clear();

            var connections = _configService.GetConnections();
            foreach (var conn in connections)
            {
                _lstConnections.Items.Add(conn.Name);
            }
        }

        private void LstConnections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_lstConnections == null || _lstConnections.SelectedIndex == -1) return;

            var selectedConn = _configService.GetConnections()[_lstConnections.SelectedIndex];
            
            if (_txtPathName != null) _txtPathName.Text = selectedConn.Name;
            if (_txtHost != null) _txtHost.Text = selectedConn.Host;
            if (_txtPort != null) _txtPort.Text = selectedConn.Port.ToString();
            if (_txtUsername != null) _txtUsername.Text = selectedConn.Username;
            if (_txtPassword != null) _txtPassword.Password = _encryptionService.Decrypt(selectedConn.EncryptedPassword);
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            if (_lstConnections != null) _lstConnections.SelectedIndex = -1;
            if (_txtPathName != null) _txtPathName.Text = "Nouveau Serveur";
            if (_txtHost != null) _txtHost.Clear();
            if (_txtPort != null) _txtPort.Text = "22";
            if (_txtUsername != null) _txtUsername.Clear();
            if (_txtPassword != null) _txtPassword.Clear();
            _txtPathName?.Focus();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtPathName?.Text) || string.IsNullOrWhiteSpace(_txtHost?.Text))
            {
                MessageBox.Show("Le nom et l'hôte sont obligatoires.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int.TryParse(_txtPort?.Text ?? "22", out int port);
            var connections = _configService.GetConnections();

            if (_lstConnections != null && _lstConnections.SelectedIndex != -1)
            {
                // Mode Modification
                var current = connections[_lstConnections.SelectedIndex];
                current.Name = _txtPathName.Text;
                current.Host = _txtHost.Text;
                current.Port = port;
                current.Username = _txtUsername?.Text ?? string.Empty;
                current.EncryptedPassword = _encryptionService.Encrypt(_txtPassword?.Password ?? string.Empty);
            }
            else
            {
                // Mode Création
                var newConn = new SftpConnection
                {
                    Name = _txtPathName.Text,
                    Host = _txtHost.Text,
                    Port = port,
                    Username = _txtUsername?.Text ?? string.Empty,
                    EncryptedPassword = _encryptionService.Encrypt(_txtPassword?.Password ?? string.Empty)
                };
                connections.Add(newConn);
            }

            _configService.Save();
            RefreshConnectionList();
            MessageBox.Show("Configuration enregistrée avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_lstConnections == null || _lstConnections.SelectedIndex == -1)
            {
                MessageBox.Show("Veuillez sélectionner un serveur à supprimer dans la liste.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var connections = _configService.GetConnections();
            var selectedConn = connections[_lstConnections.SelectedIndex];

            var result = MessageBox.Show($"Êtes-vous sûr de vouloir supprimer le serveur '{selectedConn.Name}' ?\n\nNote : Les règles associées à ce serveur risquent de ne plus fonctionner.", "Confirmation de suppression", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                connections.RemoveAt(_lstConnections.SelectedIndex);
                _configService.Save();
                RefreshConnectionList();
                BtnNew_Click(sender, e); // Réinitialise le formulaire à blanc
                MessageBox.Show("Serveur supprimé avec succès.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}