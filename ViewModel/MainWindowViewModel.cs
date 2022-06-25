using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.IO;
using FileSearch.Model;
using GalaSoft.MvvmLight.Command;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Threading;


namespace FileSearch.ViewModel
{
    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<FileModel> Files
        {
            get
            {
                return Filter(FilterTextBox);
            }
            private set { }
        }

        private List<string> FoundDuplicates { get; set; } = new List<string>();

        private string isScanning = "False";

        private bool isPremiumActivated;

        public bool IsPremiumActivated
        {
            get { return isPremiumActivated; }
            set { isPremiumActivated = value; OnPropertyChanged("IsPremiumActivated"); }
        }

        public string IsScanning
        {
            get { return isScanning; }
            set { isScanning = value; OnPropertyChanged("IsScanning"); }
        }

        private string fileSearchTextBox = @"C:\";

        public string FileSearchTextBox
        {
            get { return fileSearchTextBox; }
            set { fileSearchTextBox = value; OnPropertyChanged("FileSearchTextBox"); OnPropertyChanged("Files"); }
        }

        private string filterTextBox = String.Empty;

        public string FilterTextBox
        {
            get { return filterTextBox; }
            set { filterTextBox = value; OnPropertyChanged("FilterTextBox"); OnPropertyChanged("Files"); }
        }

        private string progressBarVisibility = "Hidden";

        public string ProgressBarVisibility
        {
            get { return progressBarVisibility; }
            set { progressBarVisibility = value; OnPropertyChanged("ProgressBarVisibility"); }
        }

        private int progressBarMaximum;

        public int ProgressBarMaximum
        {
            get { return progressBarMaximum; }
            set { progressBarMaximum = value; OnPropertyChanged("ProgressBarMaximum"); }
        }

        private int progressBarValue = 0;

        public int ProgressBarValue
        {
            get { return progressBarValue; }
            set { progressBarValue = value; OnPropertyChanged("ProgressBarValue"); }
        }

        private CancellationTokenSource tokenSource;
        private CancellationToken token;


        public ObservableCollection<FileModel> Filter(string filter)
        {
            var files = new ObservableCollection<FileModel>();
            if (filter == String.Empty)
            {
                foreach (var file in Directory.GetDirectories(FileSearchTextBox).Concat(Directory.GetFiles(FileSearchTextBox)))
                {
                    if (!new FileInfo(file).Attributes.HasFlag(FileAttributes.Hidden))
                        files.Add(new FileModel(file));

                }
                return files;
            }
            foreach (var file in Directory.GetFiles(FileSearchTextBox, $"*.{FilterTextBox.ToLower()}"))
            {
                if (!new FileInfo(file).Attributes.HasFlag(FileAttributes.Hidden))
                    files.Add(new FileModel(file));

            }
            return files;
        }

        private RelayCommand fileDialogCommand;

        public RelayCommand FileDialogCommand
        {
            get
            {
                return fileDialogCommand ?? (fileDialogCommand = new RelayCommand(() =>
                {
                    System.Windows.Forms.FolderBrowserDialog openFolderDialog = new System.Windows.Forms.FolderBrowserDialog();
                    System.Windows.Forms.DialogResult result = openFolderDialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        FileSearchTextBox = openFolderDialog.SelectedPath;
                    }
                }));
            }
        }

        private RelayCommand scanCommand;

        public RelayCommand ScanCommand
        {
            get
            {
                return scanCommand ?? (scanCommand = new RelayCommand(async () =>
                {
                    if (this.IsScanning == "True")
                        return;
                    this.IsScanning = "True";
                    this.tokenSource = new CancellationTokenSource();

                    this.token = this.tokenSource.Token;
                    Task findDupl = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            this.FoundDuplicates = FindDuplicates(FileSearchTextBox);
                            this.ProgressBarVisibility = "Hidden";
                            this.ProgressBarValue = 0;
                            if (FoundDuplicates != null)
                                if (FoundDuplicates.Count > 20)
                                    System.Windows.MessageBox.Show($"найдено {FoundDuplicates.Count} дубликатов");
                                else
                                    System.Windows.MessageBox.Show(String.Join("\n", FoundDuplicates));

                        }
                        catch { }
                        this.IsScanning = "False";
                    }, token);
                }));
            }
        }

        private RelayCommand stopCommand;

        public RelayCommand StopCommand
        {
            get
            {
                return stopCommand ?? (stopCommand = new RelayCommand(() =>
                {
                    this.tokenSource.Cancel();
                }));
            }
        }

        private RelayCommand buyCommand;

        public RelayCommand BuyCommand
        {
            get
            {
                return buyCommand ?? (buyCommand = new RelayCommand(() =>
                {
                    if (this.IsPremiumActivated) return;
                    string result = Microsoft.VisualBasic.Interaction.InputBox("Введите ключ:", "АКТИВАЦИЯ", "");
                    if (result == "SECRETKEY") this.IsPremiumActivated = true;
                }));
            }
        }

        private RelayCommand deleteCommand;

        public RelayCommand DeleteCommand
        {
            get
            {
                return deleteCommand ?? (deleteCommand = new RelayCommand(() =>
                {
                    if (this.FoundDuplicates.Count > 0)
                    {
                        System.Windows.MessageBox.Show($"\"\"\"\"Удалено\"\"\"\" {this.FoundDuplicates.Count} дубликатов");
                        this.FoundDuplicates.Clear();
                    }
                    else System.Windows.MessageBox.Show("Сначала сканируйте директорию");
                }));
            }
        }



        private List<string> FindDuplicates(string path)
        {
            var filesToCompare = new List<FileInfo>();
            var pathsToSearch = new Queue<string>();

            pathsToSearch.Enqueue(path);

            while (pathsToSearch.Count > 0)
            {
                var dir = pathsToSearch.Dequeue();

                try
                {
                    foreach (var file in Directory.GetFiles(dir))
                    {
                        filesToCompare.Add(new FileInfo(file));
                    }
                    foreach (var subdir in Directory.GetDirectories(dir))
                    {
                        pathsToSearch.Enqueue(subdir);
                    }
                }
                catch { }
                if (this.tokenSource.IsCancellationRequested)
                    return null;
            }
            System.Windows.MessageBox.Show($"{filesToCompare.Count} файлов сканируются...");
            this.ProgressBarMaximum = filesToCompare.Count;
            this.ProgressBarVisibility = "Visible";
            List<FileDetails> finalDetails = new List<FileDetails>();
            List<string> duplicates = new List<string>();
            finalDetails.Clear();
            //loop through all the files by file hash code
            foreach (var item in filesToCompare.Select(f => f.FullName))
            {
                try
                {
                    using (var fs = new FileStream(item, FileMode.Open, FileAccess.Read))
                    {
                        finalDetails.Add(new FileDetails()
                        {
                            FileName = item,
                            FileHash = BitConverter.ToString(SHA1.Create().ComputeHash(fs)),
                        });
                    }
                }
                catch { }
                finally { this.ProgressBarValue++; }
                if (this.tokenSource.IsCancellationRequested) return null;
            }
            //group by file hash code
            var similarList = finalDetails.GroupBy(f => f.FileHash)
                .Select(g => new { FileHash = g.Key, Files = g.Select(z => z.FileName).ToList() });


            duplicates.AddRange(similarList.SelectMany(f => f.Files.Skip(1)).ToList());

            return duplicates;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
