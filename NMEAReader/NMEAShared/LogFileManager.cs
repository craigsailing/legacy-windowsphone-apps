using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.IsolatedStorage;

namespace NMEAShared
{
    struct FileInfo
    {
        string FileName;
        DateTimeOffset DateTime;

        public FileInfo(string fileName, DateTimeOffset dateTime)
        {
            FileName = fileName;
            DateTime = dateTime;
        }
    }

    class LogFileManager : INotifyPropertyChanged
    {
        public ObservableCollection<FileInfo> _files = null;
        private string _Directory;
        private string _wildCardPattern;

        public LogFileManager(string baseDirectory, string wildCardPattern)
        {
            _Directory = baseDirectory; 
            _wildCardPattern = wildCardPattern;
            _files = new ObservableCollection<FileInfo>();
            BuildFileData();
        }
        
        private void BuildFileData(string wildCardPattern = "*")
        {
            using (IsolatedStorageFile appIsoStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                _files.Clear();
                var files = appIsoStorage.GetFileNames(wildCardPattern);
                foreach (string file in files)
                {
                    _files.Add( new FileInfo (file, appIsoStorage.GetCreationTime(file)));
                }
            }
        }

        private void DeleteAllFiles()
        {
            //All files are removed.
            using (IsolatedStorageFile appIsoStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                _files.Clear();
                var files = appIsoStorage.GetFileNames(_wildCardPattern);
                foreach (string file in files)
                {
                    try
                    {
                        appIsoStorage.DeleteFile(file);
                    }
                    catch (IsolatedStorageException) { }
                }
            }
        }

        private void DeleteOlderFiles(int numberOfFilesToKeep)
        {
            //All files are removed.
            using (IsolatedStorageFile appIsoStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                _files.Clear();
                var files = appIsoStorage.GetFileNames(_wildCardPattern);
                foreach (string file in files)
                {
                    try
                    {
                        appIsoStorage.DeleteFile(file);
                    }
                    catch (IsolatedStorageException) { }
                }
            }
        }

        #region NotifyPropetyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
