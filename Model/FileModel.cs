using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;

namespace FileSearch.Model
{
    public class FileModel : INotifyPropertyChanged
    {
        public bool IsDirectory { get; set; }
        public string Name { get; set; }
        public string FullPath { get; set; }
        public Bitmap Icon { get; set; }
        public ImageSource IconSource
        {
            get
            {
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                MemoryStream ms = new MemoryStream();
                Icon.Save(ms, ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);
                bi.StreamSource = ms;
                bi.EndInit();
                return bi;
            }
        }

        public DateTime CreationDate { get; set; }
        public DateTime LastModifiedDate { get; set; }

        public FileModel(string filepath)
        {
            FileAttributes attr = File.GetAttributes(filepath);
            FileInfo info = new FileInfo(filepath);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                Icon = (Bitmap)Bitmap.FromFile(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName + "\\Resources\\folder.png");
                IsDirectory = true;
            }
            else
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(filepath).ToBitmap();
                IsDirectory = false;
            }
            Name = info.Name;
            CreationDate = info.CreationTime;
            LastModifiedDate = info.LastWriteTime;
            FullPath = filepath;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
