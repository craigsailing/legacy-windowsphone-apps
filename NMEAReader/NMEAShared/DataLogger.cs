using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace NMEAShared
{
    public class DataLogger
    {
        private static DataLogger _instance = null;
        private SemaphoreSlim _semaphoreLock = new SemaphoreSlim(1);
        private List<string> _data;
        
        private const string LogDir = @"logger";
        private string logFile = @"log1.txt";
        
        private DataLogger()
        {
            BufferCount = 10;
            Init();
        }

        private DataLogger(int bufferCount)
        {
            BufferCount = bufferCount;
            Init();
        }

        private void Init()
        {
            Logging = false;
            _data = new List<string>();
            using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!isoStore.DirectoryExists(LogDir))
                {
                    isoStore.CreateDirectory(LogDir);
                }
            }
        }

        public static DataLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DataLogger();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Number of data items to buffer before writting to file
        /// </summary>
        public int BufferCount { get; private set; }

        public bool Logging { get; set; }

        public void StartLogging()
        {
            Logging = true;
        }

        public void StopLogging()
        {
            Logging = false;
            CloseLogAndFlush();
        }

        public void DeleteLog() 
        {
            //Stop the logger
            bool tempState = Logging;
            Logging = false;

            try
            {
                _semaphoreLock.Wait();

                string fileName = Path.Combine(LogDir, logFile);
                using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (isoStore.FileExists(fileName))
                    {
                        isoStore.DeleteFile(fileName);
                    }
                }

                //restore logging if we are logging
                Logging = tempState;
            }
            finally
            {
                _semaphoreLock.Release();
            }
        }

        public async void CloseLogAndFlush()
        {
            await WriteData();
        }
        
        public async void LogData(string logItem) 
        {

            if (Logging == false)
                return;

            try
            {
                await _semaphoreLock.WaitAsync();
                if (!string.IsNullOrWhiteSpace(logItem))
                {
                    _data.Add(logItem);
                }

                //Flush the buffer now if needed
                if (_data.Count >= BufferCount)
                {
                    await WriteData();
                }
            }
            finally
            {
                _semaphoreLock.Release();
            }
        }

        private async Task WriteData()
        {
            if (_data.Count > 0)
            {
                string fileName = Path.Combine(ApplicationData.Current.LocalFolder.Path, LogDir, logFile);
                using (StreamWriter dataWriter = new StreamWriter(fileName, true, Encoding.UTF8))
                {
                    //Loop over the data and write it out to the file now in append mode. 
                    foreach (string item in _data)
                    {
                        await dataWriter.WriteLineAsync(item);
                    }
                    _data.Clear();
                }
            }
        }
    }
}
