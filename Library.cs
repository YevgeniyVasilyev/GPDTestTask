using System.Windows;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows.Input;



namespace Library.Module
{
    public static class OpenFileInOSShell
    {
        public static void OpenFile(string fileName)
        {
            try
            {
                if (HasRegisteredFileExstension(Path.GetExtension(fileName)))
                {
                    _ = Process.Start(new ProcessStartInfo { FileName = fileName, UseShellExecute = true });
                }
                else
                {
                    _ = MessageBox.Show("Программы для указанного расширения файла в ОС не зарегистрировано!",
                        "Ошибка открытия файла во внешней программе", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка открытия файла во внешней программе", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Проверяем есть ли в ОС программа, которая может открыть файл с указанным расширением.
        private static bool HasRegisteredFileExstension(string fileExstension)
        {
            RegistryKey rkRoot = Registry.ClassesRoot;
            RegistryKey? rkFileType = rkRoot.OpenSubKey(fileExstension);

            return rkFileType != null;
        }
    }

#nullable enable
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> execute;
        private readonly Func<object?, bool>? canExecute;

        private readonly EventHandler _requerySuggested;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;

            _requerySuggested = (o, e) => Invalidate();
            CommandManager.RequerySuggested += _requerySuggested;
        }

        public void Invalidate()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute == null || canExecute.Invoke(parameter);
        }

        public void Execute(object? parameter)
        {
            execute?.Invoke(parameter);
        }
    }

}

