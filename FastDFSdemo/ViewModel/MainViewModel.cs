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
            Logger.Info("点击刷新按钮");
            Init();
            Logger.Info("已刷新！");
        }

        #region 修改配置

        private async void OpenDialog()
        {
            var viewDialog = new SimpleDialog();
            viewDialog.DataContext = new DialogViewModel();

            //show the dialog
            var result = await DialogHost.Show(viewDialog, "Root", ClosingEventHandler);

            if ((bool) result)
            {
                // 获取输入的值并写入配置
                Console.WriteLine("点击确定");
                Logger.Info("点击确定");
                string ip = ((DialogViewModel) viewDialog.DataContext).Ip;
                string DatabaseServer = ((DialogViewModel) viewDialog.DataContext).DatabaseServer;
                string DatabaseName = ((DialogViewModel) viewDialog.DataContext).DatabaseName;
                string Username = ((DialogViewModel) viewDialog.DataContext).Username;
                string Password = ((DialogViewModel) viewDialog.DataContext).Password;
                string downloadUrl = "http://" + ip + ":80/group1/";

                Console.WriteLine("输入IP:" + ip);
                Logger.Info("输入IP = {0}", ip);
                Logger.Info("输入DatabaseServer = {0}", DatabaseServer);
                Logger.Info("输入DatabaseName = {0}", DatabaseName);
                Logger.Info("输入Username = {0}", Username);
                Logger.Info("输入Password = {0}", Password);

                Configuration cf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                cf.AppSettings.Settings["fastdfs_Nodes"].Value = ip;
                cf.AppSettings.Settings["fastdfs_DownloadUrl"].Value = downloadUrl;
                cf.AppSettings.Settings["mysql_server"].Value = DatabaseServer;
                cf.AppSettings.Settings["mysql_database"].Value = DatabaseName;
                cf.AppSettings.Settings["mysql_user"].Value = Username;
                cf.AppSettings.Settings["mysql_pwd"].Value = Password;
                // 保存并刷新
                cf.Save();
                ConfigurationManager.RefreshSection("appSettings");

                // 刷新界面数据
                // Init();
                MessageBox.Show("请手动刷新数据！");
            }
            else
            {
                // 获取配置 还原输入值
                Console.WriteLine("点击取消");
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
        /// 初始化界面数据
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
                    Console.WriteLine("出错了");
                    Files = new ObservableCollection<TCommonFile>();
                    MessageBox.Show("连接失败");
                }
            }
        }

        #region 上传

        private void UploadFile()
        {
            string path = SelectFile();
            if (path == null)
            {
                Logger.Debug("UploadFile:File Path is NULL");
                return;
            }

            // 上传到FDFS
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
                Logger.Error("文件（{0}）上传失败", path);
                MessageBox.Show("上传失败");
                return null;
            }

            // 文件信息
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(path);

            // 存数据库
            string filename = fileInfo.Name;
            double fileSize = System.Math.Ceiling(fileInfo.Length / 1024.0);
            string fileType = System.IO.Path.GetExtension(path).Split('.').Last();
            string ip = ConfigurationManager.AppSettings["fastdfs_Nodes"];
            Logger.Info("上传文件名：" + filename);
            Logger.Info("上传文件大小：{0} KB", fileSize);
            Logger.Info("上传文件类型：" + fileType);

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
            Logger.Info("文件（{0}）上传成功, 耗时：{1}s", filename, time / 1000);
            string info = "上传成功！耗时: ";
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

            MessageBox.Show(Application.Current.MainWindow, info, "上传状态", MessageBoxButton.OK,
                MessageBoxImage.Information);
            return "OK";
        }

        #endregion


        #region 下载

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
                Logger.Info("下载文件本地路径：{0}", filePath);
                var watch = new Stopwatch();
                watch.Start();
                string s = fastdfs_client_net.Facade.DownLoad(downPath, filePath, false);
                watch.Stop();
                var time = watch.ElapsedMilliseconds;
                string info = "下载成功！耗时: ";
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

                MessageBox.Show(info + "\n文件路径：" + s);
                Logger.Info("下载成功：{0}", s);
            }
            else
            {
                Logger.Info("文件未保存");
            }
        }

        #endregion


        #region 删除

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

                // 删除服务器文件
                // bool isSuccess = StartDelete(deletePath);

                // 假删除，仅改数据库
                file.IsDelete = true;
                db.SaveChanges();
                Logger.Info("id = {0} 的文件删除成功", id);
                MessageBox.Show("删除成功");
                // 更新界面文件列表
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
            //         // 删除文件
            //         bool isSuccess = StartDelete(deletePath);
            //
            //         if (isSuccess == true)  // 删除成功
            //         {
            //             // 删除数据库记录
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
            //             MessageBox.Show("删除失败！");
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
        /// 文件选择窗口
        /// </summary>
        /// <returns></returns>
        private string SelectFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                //Filter = "select documents (.txt)|*.txt|All files (*.*)|*.*"
            };
            openFileDialog.Title = "选择需要上传的文件";

            bool? isSelected = openFileDialog.ShowDialog();
            if (isSelected == true)
            {
                return openFileDialog.FileName;
            }
            else
            {
                Logger.Info("SelectFile：未选择任何文件");
                return null;
            }
        }


        //TODO: 异步下载
        //TODO: 列表分页
        //TODO: 文件搜索

        #region 命令

        public ICommand UploadFileCommand { get; set; }

        public ICommand DownloadCommand { get; set; }

        public ICommand DeleteCommand { get; set; }

        public ICommand OpenDialogCommand { get; set; }

        public ICommand RefreshCommand { get; set; }

        #endregion
    }
}