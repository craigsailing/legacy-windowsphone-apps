using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Live;
using System.IO.IsolatedStorage;
using System.IO;

namespace NMEAReader
{
    public partial class Logs : PhoneApplicationPage
    {
        
        private LiveConnectClient liveClient = null;
        public bool LoggedOn { get; private set; }

        public Logs()
        {
            InitializeComponent();
            DataContext = this;

            this.Loaded += LogsPage_Loaded;
        }

        void LogsPage_Loaded(object sender, RoutedEventArgs e)
        {
        }
        
        private void OneDriveSignIn_SessionChanged(object sender, Microsoft.Live.Controls.LiveConnectSessionChangedEventArgs e)
        {
            if (e != null && e.Status == LiveConnectSessionStatus.Connected)
            {
                this.liveClient = new LiveConnectClient(e.Session);
                LoggedOn = true;
            }
            else
            {
                this.liveClient = null;
                LoggedOn = false;
                //this.tbError.Text = e.Error != null ? e.Error.ToString() : string.Empty;
            }
        }

        //private async void GetMe()
        //{
        //    try
        //    {
        //        LiveOperationResult operationResult = await this.liveClient.GetAsync("me");
        //        var jsonResult = operationResult.Result as dynamic;
        //        string firstName = jsonResult.first_name ?? string.Empty;
        //        string lastName = jsonResult.last_name ?? string.Empty;
        //        //this.tbGreeting.Text = "Welcome " + firstName + " " + lastName;
        //    }
        //    catch (Exception e)
        //    {
        //        //this.tbError.Text = e.ToString();
        //    }
        //}

        private async void UploadFile()
        {
            if (liveClient != null)
            {
                try
                {
                    string fileName = "sample.txt";
                    using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (myIsolatedStorage.FileExists(fileName))
                        {
                            IsolatedStorageFileStream isfs = myIsolatedStorage.OpenFile(fileName, FileMode.Open, FileAccess.Read);
                            var res = await liveClient.UploadAsync("me/skydrive", fileName, isfs, OverwriteOption.Overwrite);
                            //Enable the busy UI inidication TODO
                        }
                        else
                        {
                            //Error case the file should be present
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Please sign in with your Microsoft Account to upload file to OneDrive.");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Upload the file to cloud store now.
            UploadFile();
        } 
    }
}