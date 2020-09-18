using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FastDFSdemo.Model;
using FastDFSdemo.View;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace FastDFSdemo.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        // public ObservableCollection<TCommonFile> Files { get; set; }

        private ObservableCollection<TCommonFile> files;

        public ObservableCollection<TCommonFile> Files
        {
            get { return files; }
            set
            {
                files = value;
                RaisePropertyChanged();
            }
        }


        public MainViewModel()
        {
            Logger.Info("MainViewModel Created - Info");
            Logger.Debug("MainViewModel Created - Debug");

            UploadFileCommand = new RelayCommand(UploadFile);
            DownloadCommand = new RelayCommand<int>(DownloadFile);
            DeleteCommand = new RelayCommand<int>(DeleteFile);
            OpenDialogCommand = new RelayCommand(OpenDialog);
            RefreshCommand = new RelayCommand(Refresh);


            String currentIp = ConfigurationManager.AppSettings["fastdfs_Nodes"];
            String str2 = ConfigurationManager.AppSettings["fastdfs_GroupName"];
            String str3 = ConfigurationManager.AppSettings["fastdfs_DownloadUrl"];

            Logger.Info("fastdfs_Nodes:{0}, fastdfs_GroupName:{1}, fastdfs_DownloadUrl:{2}", currentIp, str2, str3);

            Init();
            // OpenDialog();
        }

        private void Refresh()
        {
            Logger.Info("���ˢ�°�ť");
            Init();
            Logger.Info("��ˢ�£�");
        }

        #region �޸�����

        private async void OpenDialog()
        {
            var viewDialog = new SimpleDialog();
            viewDialog.DataContext = new DialogViewModel();

            //show the dialog
            var result = await DialogHost.Show(viewDialog, "Root", ClosingEventHandler);

            if ((bool) result)
            {
                // ��ȡ�����ֵ��д������
                Console.WriteLine("���ȷ��");
                Logger.Info("���ȷ��");
                string ip = ((DialogViewModel) viewDialog.DataContext).Ip;
                string DatabaseServer = ((DialogViewModel) viewDialog.DataContext).DatabaseServer;
                string DatabaseName = ((DialogViewModel) viewDialog.DataContext).DatabaseName;
                string Username = ((DialogViewModel) viewDialog.DataContext).Username;
                string Password = ((DialogViewModel) viewDialog.DataContext).Password;
                string downloadUrl = "http://" + ip + ":80/group1/";

                Console.WriteLine("����IP:" + ip);
                Logger.Info("����IP = {0}", ip);
                Logger.Info("����DatabaseServer = {0}", DatabaseServer);
                Logger.Info("����DatabaseName = {0}", DatabaseName);
                Logger.Info("����Username = {0}", Username);
                Logger.Info("����Password = {0}", Password);

                Configuration cf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                cf.AppSettings.Settings["fastdfs_Nodes"].Value = ip;
                cf.AppSettings.Settings["fastdfs_DownloadUrl"].Value = downloadUrl;
                cf.AppSettings.Settings["mysql_server"].Value = DatabaseServer;
                cf.AppSettings.Settings["mysql_database"].Value = DatabaseName;
                cf.AppSettings.Settings["mysql_user"].Value = Username;
                cf.AppSettings.Settings["mysql_pwd"].Value = Password;
                // ���沢ˢ��
                cf.Save();
                ConfigurationManager.RefreshSection("appSettings");

                // ˢ�½�������
                // Init();
                MessageBox.Show("���ֶ�ˢ�����ݣ�");
            }
            else
            {
                // ��ȡ���� ��ԭ����ֵ
                Console.WriteLine("���ȡ��");
                var ip = ConfigurationManager.AppSettings["fastdfs_Nodes"];
                ((DialogViewModel) viewDialog.DataContext).Ip = ip;
            }
        }

        private void ClosingEventHandler(object sender, DialogOpenedEventArgs eventargs)
        {
            Console.WriteLine("You can intercept the closing event, and cancel here.");
        }

        #endregion


        /// <summary>
        /// ��ʼ����������
        /// </summary>
        private void Init()
        {
            using (var db = new db_coreContext())
            {
                try
                {
                    String currentIp = ConfigurationManager.AppSettings["fastdfs_Nodes"];

                    List<TCommonFile> fileList = db.TCommonFile.Where(f => f.IsDelete == false && f.Ip == currentIp)
                        .ToList();
                    if (fileList.Count > 0)
                    {
                        Files = new ObservableCollection<TCommonFile>(fileList);
                        //
                        // View.fList.ItemsSource = Files;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine("������");
                    Files = new ObservableCollection<TCommonFile>();
                    MessageBox.Show("����ʧ��");
                }
            }
        }

        #region �ϴ�

        private void UploadFile()
        {
            string path = SelectFile();
            if (path == null)
            {
                Logger.Debug("UploadFile:File Path is NULL");
                return;
            }

            // �ϴ���FDFS
            Logger.Info("StartUploadAsync");
            Task<string> filePath = StartUploadAsync(path);
            Logger.Info("EndUpload");
        }

        private async Task<string> StartUploadAsync(string path)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            string serverFileName = await Task.Run(() => fastdfs_client_net.Facade.Upload(path));
            if (string.IsNullOrEmpty(serverFileName))
            {
                Logger.Error("�ļ���{0}���ϴ�ʧ��", path);
                MessageBox.Show("�ϴ�ʧ��");
                return null;
            }

            // �ļ���Ϣ
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(path);

            // �����ݿ�
            string filename = fileInfo.Name;
            double fileSize = System.Math.Ceiling(fileInfo.Length / 1024.0);
            string fileType = System.IO.Path.GetExtension(path).Split('.').Last();
            string ip = ConfigurationManager.AppSettings["fastdfs_Nodes"];
            Logger.Info("�ϴ��ļ�����" + filename);
            Logger.Info("�ϴ��ļ���С��{0} KB", fileSize);
            Logger.Info("�ϴ��ļ����ͣ�" + fileType);

            using (var db = new db_coreContext())
            {
                var file = new TCommonFile();
                file.FileName = filename;
                file.FileSize = fileSize;
                file.FileType = fileType;
                file.Ip = ip;
                file.UploadTime = DateTime.Now;
                file.Uploader = "system";
                file.FilePath = serverFileName;
                db.TCommonFile.Add(file);
                db.SaveChanges();
                Files.Add(file);
                // Files.Add(file);
            }

            stopwatch.Stop();
            var time = stopwatch.ElapsedMilliseconds;
            Logger.Info("�ļ���{0}���ϴ��ɹ�, ��ʱ��{1}s", filename, time / 1000);
            string info = "�ϴ��ɹ�����ʱ: ";
            if (time > 60000)
            {
                info += time / 60000.0;
                info += "Min";
            }
            else if (time > 1000)
            {
                info += time / 1000.0;
                info += "S";
            }
            else
            {
                info += time;
                info += "ms";
            }

            MessageBox.Show(Application.Current.MainWindow, info, "�ϴ�״̬", MessageBoxButton.OK,
                MessageBoxImage.Information);
            return "OK";
        }

        #endregion


        #region ����

        private void DownloadFile(int id)
        {
            // string downPath = btn_download.Tag as string;
            // string filename = btn_download.CommandParameter as string;
            Logger.Info("Download File Id == {0}", id);

            string downPath = null;
            string filename = null;

            foreach (TCommonFile file in Files)
            {
                if (file.Id == id)
                {
                    downPath = file.FilePath;
                    filename = file.FileName;
                    break;
                }
            }


            if (!string.IsNullOrEmpty(downPath))
            {
                Logger.Info("Download File Server Path == {0}", downPath);
                StartDownload(downPath, filename);
            }
        }

        private void StartDownload(string downPath, string filename)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "All Files|*.*";
            saveFileDialog.FileName = filename;
            saveFileDialog.AddExtension = true;
            if (saveFileDialog.ShowDialog().GetValueOrDefault())
            {
                var filePath = saveFileDialog.FileName;
                Logger.Info("�����ļ�����·����{0}", filePath);
                var watch = new Stopwatch();
                watch.Start();
                string s = fastdfs_client_net.Facade.DownLoad(downPath, filePath, false);
                watch.Stop();
                var time = watch.ElapsedMilliseconds;
                string info = "���سɹ�����ʱ: ";
                if (time > 60000)
                {
                    info += time / 60000.0;
                    info += "Min";
                }
                else if (time > 1000)
                {
                    info += time / 1000.0;
                    info += "S";
                }
                else
                {
                    info += time;
                    info += "ms";
                }

                MessageBox.Show(info + "\n�ļ�·����" + s);
                Logger.Info("���سɹ���{0}", s);
            }
            else
            {
                Logger.Info("�ļ�δ����");
            }
        }

        #endregion


        #region ɾ��

        private void DeleteFile(int id)
        {
            Logger.Info("Delete File Id == {0}", id);
            string deletePath = null;

            // var f = new TCommonFile();

            using (var db = new db_coreContext())
            {
                TCommonFile file = db.TCommonFile.First(f => f.Id == id);
                deletePath = file.FilePath;
                Logger.Info("Delete File Server Path == {0}", deletePath);

                // ɾ���������ļ�
                // bool isSuccess = StartDelete(deletePath);

                // ��ɾ�����������ݿ�
                file.IsDelete = true;
                db.SaveChanges();
                Logger.Info("id = {0} ���ļ�ɾ���ɹ�", id);
                MessageBox.Show("ɾ���ɹ�");
                // ���½����ļ��б�
                foreach (var f in Files)
                {
                    if (f.Id == id)
                    {
                        Files.Remove(f);
                        break;
                    }
                }
            }

            // foreach (TCommonFile file in Files)
            // {
            //     if (file.Id == id)
            //     {
            //         deletePath = file.FilePath;
            //         Logger.Info("Delete File Server Path == {0}", deletePath);
            //
            //         // ɾ���ļ�
            //         bool isSuccess = StartDelete(deletePath);
            //
            //         if (isSuccess == true)  // ɾ���ɹ�
            //         {
            //             // ɾ�����ݿ��¼
            //             using (var db = new db_coreContext())
            //             {
            //                 // db.TCommonFile.Remove(f);
            //                 // f.IsDelete = true;
            //                 file.IsDelete = true;
            //                 
            //             }
            //         }
            //         else
            //         {
            //             MessageBox.Show("ɾ��ʧ�ܣ�");
            //         }
            //         // f = file;
            //         break;
            //     }
            // }
        }

        private bool StartDelete(string deletePath)
        {
            return fastdfs_client_net.Facade.Remove(deletePath);
        }

        #endregion


        /// <summary>
        /// �ļ�ѡ�񴰿�
        /// </summary>
        /// <returns></returns>
        private string SelectFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                //Filter = "select documents (.txt)|*.txt|All files (*.*)|*.*"
            };
            openFileDialog.Title = "ѡ����Ҫ�ϴ����ļ�";

            bool? isSelected = openFileDialog.ShowDialog();
            if (isSelected == true)
            {
                return openFileDialog.FileName;
            }
            else
            {
                Logger.Info("SelectFile��δѡ���κ��ļ�");
                return null;
            }
        }


        //TODO: �첽����
        //TODO: �б��ҳ
        //TODO: �ļ�����

        #region ����

        public ICommand UploadFileCommand { get; set; }

        public ICommand DownloadCommand { get; set; }

        public ICommand DeleteCommand { get; set; }

        public ICommand OpenDialogCommand { get; set; }

        public ICommand RefreshCommand { get; set; }

        #endregion
    }
}