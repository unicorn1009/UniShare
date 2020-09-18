using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using FastDFSdemo.View;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

namespace FastDFSdemo.ViewModel
{
    public class DialogViewModel:ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public DialogViewModel()
        {
            Ip = ConfigurationManager.AppSettings["fastdfs_Nodes"];
            DatabaseServer = ConfigurationManager.AppSettings["mysql_server"];
            DatabaseName = ConfigurationManager.AppSettings["mysql_database"];
            Username = ConfigurationManager.AppSettings["mysql_user"];
            Password = ConfigurationManager.AppSettings["mysql_pwd"];

        }

        #region 属性

        private string ip;

        public string Ip
        {
            get { return ip; }
            set { ip = value; RaisePropertyChanged(); }
        }

        private string databaseServer;

        public string DatabaseServer
        {
            get { return databaseServer; }
            set { databaseServer = value; RaisePropertyChanged(); }
        }


        private string databaseName;

        public string DatabaseName
        {
            get { return databaseName; }
            set { databaseName = value; RaisePropertyChanged(); }
        }

        private string username;

        public string Username
        {
            get { return username; }
            set { username = value; RaisePropertyChanged(); }
        }

        private string password;

        public string Password
        {
            get { return password; }
            set { password = value; RaisePropertyChanged(); }
        }

        #endregion



    }
}
