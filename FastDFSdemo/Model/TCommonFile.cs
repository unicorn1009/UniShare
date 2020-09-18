using System;
using System.Collections.Generic;

namespace FastDFSdemo.Model
{
    public partial class TCommonFile
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public double FileSize { get; set; }
        public string FileType { get; set; }
        public string Ip { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadTime { get; set; }
        public string Uploader { get; set; }
        public bool IsDelete { get; set; }
        public string Description { get; set; }
    }
}
