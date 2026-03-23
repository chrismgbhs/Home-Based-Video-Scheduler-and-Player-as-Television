// Global using aliases — resolves ambiguity between WPF and WinForms
// when both UseWPF and UseWindowsForms are true in the .csproj
global using Application        = System.Windows.Application;
global using MessageBox         = System.Windows.MessageBox;
global using MessageBoxButton   = System.Windows.MessageBoxButton;
global using MessageBoxImage    = System.Windows.MessageBoxImage;
global using MessageBoxResult   = System.Windows.MessageBoxResult;
global using Visibility         = System.Windows.Visibility;
