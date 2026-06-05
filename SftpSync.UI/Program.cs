using System;
using System.Windows;

namespace SftpSync.UI
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                var app = new System.Windows.Application();
                var mainWindow = new MainWindow();
                app.Run(mainWindow);
            }
            catch (Exception ex)
            {
                // Si l'application plante au démarrage, cette boîte va TOUT nous dire
                MessageBox.Show(
                    $"L'application a planté au démarrage.\n\n" +
                    $"Message : {ex.Message}\n\n" +
                    $"Détails : {ex.InnerException?.Message}\n\n" +
                    $"Détails techniques :\n{ex.StackTrace}", 
                    "Crash Majeur Détecté", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error
                );
            }
        }
    }
}