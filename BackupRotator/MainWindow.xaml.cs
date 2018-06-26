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
        public MainWindow()
        {
            InitializeComponent();
            _backupArrayFile = new List<FileInfo>();
            _backupArrayDirectory = new List<DirectoryInfo>();
            _backupTimer = new Timer();
            _backupTimer.Elapsed += onElapsed;
        }

        private void btSelectFile_Click(object sender, RoutedEventArgs e)
        {
            if (_folderMode)
            {
                Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog cfd = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();
                cfd.IsFolderPicker = true;
                cfd.ShowDialog();

                if(!string.IsNullOrEmpty(cfd.FileName))
                {
                    _backupTargetDirectory = new DirectoryInfo(cfd.FileName);
                }
            }
            else
            {
                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
                ofd.ShowDialog();

                if (!string.IsNullOrEmpty(ofd.FileName))
                {
                    _backupTargetFile = new FileInfo(ofd.FileName);
                }
            }
        }

        private void btStartBackups_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(tbBackupNum.Text, out int backupNum) && int.TryParse(tbBackupInterval.Text, out int backupInterval))
            {
                cbBackupSelection.Items.Clear();
                for(int i = 1; i <= backupNum; i++)
                {
                    cbBackupSelection.Items.Add(string.Format("{0} minutes back", i * backupInterval));
                }
                _backupNum = backupNum;
                _backupInterval = backupInterval;

                if (_folderMode)
                {
                    clearExtraDirectories();
                }
                else
                {
                    clearExtraFiles();
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
            _backupTargetDirectory.MoveTo(backupTempName);
            DirectoryInfo tempDir = new DirectoryInfo(backupTempName);
            _backupTargetDirectory = new DirectoryInfo(backupTargetName);
            _backupTargetDirectory.Create();

            foreach(FileInfo f in _backupArrayDirectory[backupIndex].EnumerateFiles())
            {
                string dest = string.Format("{0}\\{1}", backupTargetName, f.Name);
                f.CopyTo(dest);
            }

            tempDir.Delete(true);
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
            FileInfo newCopy = _backupTargetFile.CopyTo(string.Format("{0}_{1}-{2}", _backupTargetFile.Name, time.Hour, time.Minute));
            _backupArrayFile.Add(newCopy);

            if (_backupArrayFile.Count > _backupNum)
            {
                FileInfo oldCopy = _backupArrayFile[0];
                oldCopy.Delete();
                _backupArrayFile.RemoveAt(0);
            }
        }

        private void makeBackupDirectory()
        {
            DateTime time = DateTime.Now;
            DirectoryInfo newDirectory = new DirectoryInfo(string.Format("{0}_{1}-{2}", _backupTargetDirectory.FullName, time.Hour, time.Minute));
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
        }

        private void chkFolderMode_Checked(object sender, RoutedEventArgs e)
        {
            _folderMode = (bool)chkFolderMode.IsChecked;
        }
    }
}
