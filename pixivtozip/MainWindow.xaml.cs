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
using System.IO.Compression;
using System.Windows.Forms;

namespace PixivToZip
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        PixivHelper pixivHelper;

        public MainWindow()
        {
            InitializeComponent();

            appRun();
        }

        private async void appRun()
        {
            viewLogOut();
            loadSetting();

            if (checkLogInInfo())
            {
                await logIn();
            }
        }

        private void viewLogOut()
        {
            tbUserId.IsEnabled = true;
            tbPassword.IsEnabled = true;
            btnLogIn.IsEnabled = true;

            tbId.IsEnabled = false;
            tbFolder.IsEnabled = false;
            btnFolder.IsEnabled = false;
            cbZip.IsEnabled = false;
            btnDownload.IsEnabled = false;
        }

        private void viewLogIn()
        {
            tbUserId.IsEnabled = false;
            tbPassword.IsEnabled = false;
            btnLogIn.IsEnabled = false;

            tbId.IsEnabled = true;
            tbFolder.IsEnabled = true;
            btnFolder.IsEnabled = true;
            cbZip.IsEnabled = true;
            btnDownload.IsEnabled = true;
        }

        private void disableAllContext()
        {
            tbUserId.IsEnabled = false;
            tbPassword.IsEnabled = false;
            btnLogIn.IsEnabled = false;

            tbId.IsEnabled = false;
            tbFolder.IsEnabled = false;
            btnFolder.IsEnabled = false;
            cbZip.IsEnabled = false;
            btnDownload.IsEnabled = false;
        }

        private void saveSetting()
        {
            PixivToZip.Properties.Settings.Default.UserId = tbUserId.Text;
            PixivToZip.Properties.Settings.Default.Password = tbPassword.Text;
            PixivToZip.Properties.Settings.Default.Folder = tbFolder.Text;
            PixivToZip.Properties.Settings.Default.IllustId = tbId.Text;
            PixivToZip.Properties.Settings.Default.IsZip = (cbZip.IsChecked == true);

            PixivToZip.Properties.Settings.Default.Save();
        }

        private void loadSetting()
        {
            tbUserId.Text = PixivToZip.Properties.Settings.Default.UserId;
            tbPassword.Text = PixivToZip.Properties.Settings.Default.Password;
            tbFolder.Text = PixivToZip.Properties.Settings.Default.Folder;
            tbId.Text = PixivToZip.Properties.Settings.Default.IllustId;
            cbZip.IsChecked = PixivToZip.Properties.Settings.Default.IsZip;
        }

        private bool checkLogInInfo()
        {
            return string.IsNullOrEmpty(tbUserId.Text) == false
                && string.IsNullOrEmpty(tbPassword.Text) == false;
        }

        private async void btnLogIn_Click(object sender, RoutedEventArgs e)
        {
            await logIn();
        }

        private async Task logIn()
        {
            writeProgress(tbUserId.Text + "としてログイン中...");

            pixivHelper = new PixivHelper();
            bool result = await pixivHelper.logIn(tbUserId.Text, tbPassword.Text);

            if (result)
            {
                writeProgress("ログイン成功");
                saveSetting();
                viewLogIn();
            }
            else
            { writeProgress("ログイン失敗"); }
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            disableAllContext();

            writeProgress(string.Format("{0}の情報を取得中...", tbId.Text));

            string pictureTitle = await pixivHelper.getPicturesTitle(tbId.Text);

            writeProgress(string.Format("{0}をダウンロード中...", pictureTitle));

            string folderPath = tbFolder.Text;

            string dirPath = await pixivHelper.DownloadPicturesAsync(tbId.Text, folderPath);

            string zipName = dirPath.Split('\\').Last() + ".zip";

            if (cbZip.IsChecked == true)
            {
                ZipFile.CreateFromDirectory(dirPath, folderPath + @"\" + zipName);
                Directory.Delete(dirPath, true);
            }

            saveSetting();

            writeProgress("ダウンロード完了");

            viewLogIn();
        }

        private void btnFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.Desktop;

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                tbFolder.Text = fbd.SelectedPath;
                saveSetting();
            }
        }

        private void writeProgress(string message)
        {
            tbProgress.Text += message + "\r\n";
        }
    }
}
