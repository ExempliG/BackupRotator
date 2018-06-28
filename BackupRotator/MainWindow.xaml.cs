using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using System.Timers;

namespace BackupRotator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileInfo _backupTargetFile;
        private DirectoryInfo _backupTargetDirectory;
        private List<FileInfo> _backupArrayFile;
        private List<DirectoryInfo> _backupArrayDirectory;
        private int _backupNum;
        private int _backupInterval;
        private Timer _backupTimer;
        private bool _folderMode = false;

        private delegate void UpdateBackupSelectorDelegate(int numberOfBackups);

        public MainWindow()
        {
            InitializeComponent();
            _backupArrayFile = new List<FileInfo>();
            _backupArrayDirectory = new List<DirectoryInfo>();
            _backupTimer = new Timer();
            _backupTimer.Elapsed += onElapsed;

            if(Config.Load())
            {
                tbBackupInterval.Text = Config.Instance.BackupInterval.ToString();
                tbBackupNum.Text = Config.Instance.NumberOfBackups.ToString();
            }
        }

        private void btSelectFile_Click(object sender, RoutedEventArgs e)
        {
            if (_folderMode)
            {
                var cfd = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();
                cfd.IsFolderPicker = true;
                var result = cfd.ShowDialog();
                var desiredResult = Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok;

                if(result == desiredResult && !string.IsNullOrEmpty(cfd.FileName))
                {
                    _backupTargetDirectory = new DirectoryInfo(cfd.FileName);
                    btStartBackups.IsEnabled = true;
                }
            }
            else
            {
                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
                bool result = (bool)ofd.ShowDialog();

                if (result && !string.IsNullOrEmpty(ofd.FileName))
                {
                    _backupTargetFile = new FileInfo(ofd.FileName);
                    btStartBackups.IsEnabled = true;
                }
            }
        }

        private void btStartBackups_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(tbBackupNum.Text, out int backupNum) && int.TryParse(tbBackupInterval.Text, out int backupInterval))
            {
                cbBackupSelection.Items.Clear();
                _backupNum = backupNum;
                _backupInterval = backupInterval;
                Config.Instance.NumberOfBackups = _backupNum;
                Config.Instance.BackupInterval = _backupInterval;

                if (_folderMode)
                {
                    clearExtraDirectories();
                    makeBackupDirectory();
                }
                else
                {
                    clearExtraFiles();
                    makeBackupFile();
                }

                _backupTimer.Interval = _backupInterval * 60 * 1000;
                _backupTimer.Start();

                btFreezeBackups.IsEnabled = true;
                btSelectFile.IsEnabled = false;
                chkFolderMode.IsEnabled = false;
                btStartBackups.IsEnabled = false;
            }
        }

        private void clearExtraDirectories()
        {
            if (_backupArrayDirectory.Count > _backupNum)
            {
                int difference = _backupArrayDirectory.Count - _backupNum;
                for (int i = 0; i < difference; i++)
                {
                    _backupArrayDirectory[i].Delete(true);
                }
                _backupArrayDirectory.RemoveRange(0, difference);
            }
        }

        private void clearExtraFiles()
        {
            if (_backupArrayFile.Count > _backupNum)
            {
                int difference = _backupArrayFile.Count - _backupNum;
                for (int i = 0; i < difference; i++)
                {
                    _backupArrayFile[i].Delete();
                }
                _backupArrayFile.RemoveRange(0, difference);
            }
        }

        private void btFreezeBackups_Click(object sender, RoutedEventArgs e)
        {
            _backupTimer.Stop();
            btFreezeBackups.IsEnabled = false;
            btSelectFile.IsEnabled = true;
            chkFolderMode.IsEnabled = true;
            btStartBackups.IsEnabled = true;
        }

        private void btRestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            if(_folderMode)
            {
                restoreBackupDirectory();
            }
            else
            {
                restoreBackupFile();
            }
        }

        private void restoreBackupFile()
        {
            int lastIndex = _backupArrayFile.Count - 1;
            int backupIndex = lastIndex - cbBackupSelection.SelectedIndex;
            _backupArrayFile[backupIndex].CopyTo(_backupTargetFile.FullName, true);
        }

        private void restoreBackupDirectory()
        {
            int lastIndex = _backupArrayDirectory.Count - 1;
            int backupIndex = lastIndex - cbBackupSelection.SelectedIndex;
            string backupTargetName = _backupTargetDirectory.FullName;
            string backupTempName = string.Format("{0}{1}", _backupTargetDirectory.FullName, "_tmp");

            try
            {
                _backupTargetDirectory.MoveTo(backupTempName);
                DirectoryInfo tempDir = new DirectoryInfo(backupTempName);
                _backupTargetDirectory = new DirectoryInfo(backupTargetName);
                _backupTargetDirectory.Create();

                foreach (FileInfo f in _backupArrayDirectory[backupIndex].EnumerateFiles())
                {
                    string dest = string.Format("{0}\\{1}", backupTargetName, f.Name);
                    f.CopyTo(dest);
                }

                tempDir.Delete(true);
            }
            catch(Exception e)
            {
                MessageBox.Show(string.Format("Failed to restore backup! Your original directory may be found at {0}.", backupTempName));
            }
        }

        private void onElapsed(object sender, EventArgs e)
        {
            if (_folderMode)
            {
                makeBackupDirectory();
            }
            else
            {
                makeBackupFile();
            }
        }

        private void makeBackupFile()
        {
            DateTime time = DateTime.Now;
            FileInfo newCopy = _backupTargetFile.CopyTo(string.Format("{0}_{1}-{2}-{3}", _backupTargetFile.Name, time.Hour, time.Minute, time.Second));
            _backupArrayFile.Add(newCopy);

            if (_backupArrayFile.Count > _backupNum)
            {
                FileInfo oldCopy = _backupArrayFile[0];
                oldCopy.Delete();
                _backupArrayFile.RemoveAt(0);
            }
            else
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new UpdateBackupSelectorDelegate(updateBackupSelection), _backupArrayFile.Count);
            }
        }

        private void makeBackupDirectory()
        {
            DateTime time = DateTime.Now;
            DirectoryInfo newDirectory = new DirectoryInfo(string.Format("{0}_{1}-{2}-{3}", _backupTargetDirectory.FullName, time.Hour, time.Minute, time.Second));
            newDirectory.Create();
            _backupArrayDirectory.Add(newDirectory);

            foreach(FileInfo f in _backupTargetDirectory.EnumerateFiles())
            {
                f.CopyTo(string.Format("{0}\\{1}", newDirectory.FullName, f.Name));
            }

            if(_backupArrayDirectory.Count > _backupNum)
            {
                DirectoryInfo oldDirectory = _backupArrayDirectory[0];
                oldDirectory.Delete(true);
                _backupArrayDirectory.RemoveAt(0);
            }
            else
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new UpdateBackupSelectorDelegate(updateBackupSelection), _backupArrayDirectory.Count);                
            }
        }

        private void updateBackupSelection(int numberOfBackups)
        {
            cbBackupSelection.Items.Add(string.Format("{0} minutes back", numberOfBackups * _backupInterval));
        }

        private void chkFolderMode_Checked(object sender, RoutedEventArgs e)
        {
            _folderMode = (bool)chkFolderMode.IsChecked;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Config.Save();

            if (_backupArrayDirectory.Count > 0 || _backupArrayFile.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show("Would you like to keep the backups generated during this session?", "Before you go...", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.No)
                {
                    foreach (FileInfo f in _backupArrayFile)
                    {
                        f.Delete();
                    }

                    foreach (DirectoryInfo d in _backupArrayDirectory)
                    {
                        d.Delete(true);
                    }
                }
            }
        }
    }
}
