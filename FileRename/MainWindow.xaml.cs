using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DevExpress.Xpf.Core;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace FileRename
{
    /// <summary>
    /// Interaction logic for DXWindow1.xaml
    /// </summary>
    public partial class DxWindow1 : ThemedWindow
    {
        public DxWindow1()
        {
            InitializeComponent();
        }

        private void MenuItemColse_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutBox1 about = new AboutBox1();
            about.ShowDialog();
        }

        private void SimpleButtonPath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FolderBrowserDialog folder = new FolderBrowserDialog
                {
                    Description = @"请选择数据相机图片文件目录",
                    ShowNewFolderButton = false
                };

                if (LabelPath.Content != null)
                {
                    folder.SelectedPath = LabelPath.Content.ToString();
                }

                if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Label1.Visibility = Visibility.Visible;
                    LabelPath.Content = folder.SelectedPath;
                    LabelImagecount.Content = "";
                }
                LoadImageList();
                LoadImage(_imageListIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发生错误，原因：{ex.Message}。", "提示", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private readonly List<ImagePath> _list1 = new List<ImagePath>();//图片列表
        private readonly Vehicle _veh = new Vehicle();
        private string _imagepath = string.Empty; //图片路径
        private int _maxvalue = 0;
        private int _imageListIndex = 0;
        /// <summary>
        /// 加载列表
        /// </summary>
        private void LoadImageList()
        {
            LabelImageInfo1.Content = null;
            LabelImageInfo2.Content = null;
            Image1.Source = null;
            Image2.Source = null;
            SetVehInfoIsDefault();
            if (LabelPath.Content == null)
            {
                MessageBox.Show("未选择图片目录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            else
            {
                _list1.Clear();
                _imagepath = LabelPath.Content.ToString();
                DirectoryInfo folder = new DirectoryInfo(_imagepath);
                FileInfo[] sortlsit = folder.GetFiles("*.jpg");
                Array.Sort(sortlsit, new MyDateSorter()); //按文件名排序
                foreach (var t in sortlsit)
                {
                    _list1.Add(new ImagePath(t.Name, t.FullName));
                }

                LabelImagecount.Content = $"。 共有图片：{_list1.Count}张。";
                if (_list1.Count % 2 != 0)
                {
                    MessageBox.Show("选择的图片必须为偶数，即两张一个违法。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                ProgresBar1.Maximum = _list1.Count;
                _maxvalue = _list1.Count;
            }
        }
        /// <summary>
        /// 加载图片
        /// </summary>
        /// <param name="index"></param>
        private void LoadImage(int index)
        {
            try
            {
                if (_list1.Count <= 0) return;
                Image1.Source = File.Exists(_list1[index].PhotoPath) ? GetBitmapImage(_list1[index].PhotoPath) : null;
                LabelImage1.Content = _list1[index].PhotoName;
                var s = GetCsData(_list1[index].PhotoPath, out string sblx);
                Image2.Source = File.Exists(_list1[index+1].PhotoPath) ? GetBitmapImage(_list1[index+1].PhotoPath) : null;
                LabelImage2.Content = _list1[index+1].PhotoName;
                var s2 = GetCsData(_list1[index+1].PhotoPath, out _);
                switch (sblx)
                {
                    case "6F":
                        LabelImageInfo1.Content = $"违法时间：{s[0]},速度：{s[1]},限速：{s[2]}";
                        LabelImageInfo2.Content = $"违法时间：{s2[0]},速度：{s2[1]},限速：{s2[2]}";
                        break;

                    case "9F":
                        LabelImageInfo1.Content =
                            $"违法时间：{s[0]} {s[1].Substring(0, 8)},速度：{s[4]},小车限速：{s[2]}，大车限速：{s[3]}";
                        LabelImageInfo2.Content =
                            $"违法时间：{s2[0]} {s2[1].Substring(0, 8)},速度：{s2[4]},小车限速：{s2[2]}，大车限速：{s2[3]}";
                        break;
                    case "DH9F":
                        LabelImageInfo1.Content =
                            $"违法时间：{s[0]} {s[1].Substring(0, 8)},速度：{s[3]},限速：{s[2]}，";
                        LabelImageInfo2.Content =
                            $"违法时间：{s2[0]} {s2[1].Substring(0, 8)},速度：{s2[3]},限速：{s2[2]}";
                        break;
                }

                SetVehicleWfxx(s, sblx);
                LabelCheck.Visibility = Visibility.Visible;
                
            }
            catch (Exception exf)
            {
                MessageBox.Show($"无法加载图片，原因：{exf.Message}。","提示",MessageBoxButton.OK,MessageBoxImage.Information);
            }
        }

        /// <summary>获取测速数据</summary>
        /// <param name="fileName"></param>
        /// <param name="sblx"></param>
        /// <returns></returns>
        private string[] GetCsData(string fileName, out string sblx)
        {
            var ss = new string[10];
            sblx = "";
            var s = Encoding.Default.GetString(BitmapImageToByte(fileName)).Substring(0,2300);
            if (s.IndexOf("CAM_SYSN=Robot", StringComparison.Ordinal) != -1) //6F
            {
                sblx = "6F";
                var date = s.Substring(s.IndexOf("INC_DATE", StringComparison.Ordinal) + 9, 6); //+9是除去字符本身长度和等号
                var time = s.Substring(s.IndexOf("INC_TIME", StringComparison.Ordinal) + 9, 6);
                ss[0] =
                    $"20{date.Substring(4, 2)}-{date.Substring(2, 2)}-{date.Substring(0, 2)} {time.Substring(0, 2)}:{time.Substring(2, 2)}:{time.Substring(4, 2)}"; //违法时间
                ss[1] = s.Substring(s.IndexOf("INC_SPEE", StringComparison.Ordinal) + 9, 3); //速度
                ss[2] = s.Substring(s.IndexOf("INC_LIMI", StringComparison.Ordinal) + 9, 3); //限速
            }

            if (s.IndexOf("JXKJCODE", StringComparison.Ordinal) != -1) //9F
            {
                if (s.IndexOf("DH_ITC", StringComparison.Ordinal) != -1 ||
                    s.IndexOf("INC_DATE", StringComparison.Ordinal) != -1)
                    sblx = "DH9F";
                else
                    sblx = "9F";
                var start = s.IndexOf("<JXKJCODE>", StringComparison.Ordinal) + 10;
                var end = s.IndexOf("</JXKJCODE>", StringComparison.Ordinal);
                var code = s.Substring(start, end - start);
                ss = code.Split('@');
            }

            return ss;
        }

        /// <summary>
        /// 设置车辆的违法信息
        /// </summary>
        /// <param name="s"></param>
        /// <param name="sblx"></param>
        private void SetVehicleWfxx(string[] s, string sblx)
        {
            switch (sblx)
            {
                case "6F":
                    _veh.Wfsj = Convert.ToDateTime(s[0]);
                    _veh.Clsd = Convert.ToInt32(s[1]);
                    _veh.Clxs = Convert.ToInt32(s[2]);
                    break;

                case "9F":
                    _veh.Wfsj = Convert.ToDateTime(s[0] + " " + s[1].Substring(0, 8));
                    _veh.Clsd = Convert.ToInt32(s[4].Substring(0, s[4].Length - 4)); //9F后面带了“KM/H”
                    _veh.Clxs = Convert.ToInt32(s[2].Substring(0, s[2].Length - 4));
                    _veh.DcClxs = Convert.ToInt32(s[3].Substring(0, s[3].Length - 4));
                    break;
                case "DH9F":
                    _veh.Wfsj = Convert.ToDateTime(s[0] + " " + s[1].Substring(0, 8));
                    _veh.Clsd = Convert.ToInt32(s[3].Substring(0, s[3].Length - 4)); //9F后面带了“KM/H”
                    _veh.Clxs = Convert.ToInt32(s[2].Substring(0, s[2].Length - 4));
                    break;
            }
        }

        /// <summary>
        /// 重命名文件
        /// </summary>
        /// <param name="wfdd">违法地点</param>
        /// <param name="backup">是否备份</param>
        /// <param name="mess"></param>
        /// <returns></returns>
        private bool RenameFile(string wfdd, bool backup, out string mess)
        {
            try
            {
                mess = "";
                if (_list1.Count == 0)
                {
                    MessageBox.Show("没有可重命名的图片。");
                    return false;
                }

                var newfilename = string.IsNullOrEmpty(wfdd)
                    ? $"{_veh.Wfsj:yyyyMMddHHmmss}_{_veh.Clsd}_{_veh.Clxs}"
                    : $"{_veh.Wfsj:yyyyMMddHHmmss}_{_veh.Clsd}_{_veh.Clxs}_{wfdd}";
                var renamepath = _imagepath + "\\RenameFiles\\" + newfilename;
                var errornamepath = _imagepath + "\\ErrorBackupFile\\";
                var backuppath = _imagepath + "\\BackupFile\\";
                if (backup)
                {
                    if (!Directory.Exists(backuppath))
                    {
                        Directory.CreateDirectory(backuppath);
                    }
                }

                FileInfo fi = new FileInfo(_list1[0].PhotoPath);
                FileInfo fi2 = new FileInfo(_list1[1].PhotoPath);
                var newfile1 = renamepath + "_1.jpg";
                var newfile2 = renamepath + "_2.jpg";
                if (!File.Exists(newfile1))
                {
                    if (backup)
                    {
                        fi.CopyTo(backuppath + _list1[0].PhotoName,true);
                    }

                    fi.MoveTo(newfile1);
                }
                else
                {
                    if (!Directory.Exists(errornamepath))
                    {
                        Directory.CreateDirectory(errornamepath);
                    }

                    fi.MoveTo(errornamepath + _list1[0].PhotoName);
                }

                if (!File.Exists(newfile2))
                {
                    if (backup)
                    {
                        fi2.CopyTo(backuppath + _list1[1].PhotoName,true);
                    }

                    fi2.MoveTo(newfile2);
                }
                else
                {
                    fi2.MoveTo(errornamepath + _list1[1].PhotoName);
                }

                return true;
            }
            catch (Exception exf)
            {
                mess = exf.Message;
                return false;
            }
        }

        private byte[] BitmapImageToByte(string fileName)
        {
            var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            try
            {
                byte[] by = new byte[fs.Length];
                fs.Read(by, 0, (int) fs.Length);
                return by;
            }
            catch
            {
                return null;
            }
            finally
            {
                fs.Dispose();
            }
        }

        private void SetVehInfoIsDefault()
        {
            _veh.Wfsj = Convert.ToDateTime("1900-01-01");
            _veh.Clsd = 0;
            _veh.Clxs = 0;
            _veh.DcClxs = 0;
            _veh.Hphm = "浙K";
            _veh.Hpzl = "02";
            _veh.Wfxw = "13521";
        }

        private BitmapImage GetBitmapImage(string filename)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(filename, UriKind.Absolute);
            bitmap.EndInit();
            return bitmap;
        }

        /// <summary>排序用</summary>
        private class MyDateSorter : IComparer
        {
            public int Compare(object x, object y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                FileInfo xInfo = (FileInfo) x;
                FileInfo yInfo = (FileInfo) y;
                return String.Compare(xInfo.FullName, yInfo.FullName, StringComparison.Ordinal); //递增
                //return String.Compare(yInfo.FullName, xInfo.FullName, StringComparison.Ordinal); //递减
                //return xInfo.LastWriteTime.CompareTo(yInfo.LastWriteTime);
            }
        }

        private void SimpleButtonRenamefile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_list1.Count == 0)
                {
                    MessageBox.Show("当前目录没有测速图片，请选择有数据的目录。","提示",MessageBoxButton.OK,MessageBoxImage.Information);
                    return;
                }

                ShowLoading();
                string dir = LabelPath.Content + "\\RenameFiles\\";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                ProgresBar1.Visibility = Visibility.Visible;
                string wfdd = CheckBox1.IsChecked == false ? "" : TextEditWfdd.Text;
                bool backup = CheckBoxBackup.IsChecked == true;
                ThreadPool.QueueUserWorkItem(o =>
                {
                    int i = 0;
                    while (i < _list1.Count)
                    {
                        if (!RenameFile(wfdd, backup, out string mess))
                        {
                            MessageBox.Show($"重命名或图片发生错误，原因：{mess}。", "提示", MessageBoxButton.OK,
                                MessageBoxImage.Information);
                            HiddLoading();
                            return;
                        }

                        ReLoadImage();
                    }

                    if (Application.Current.Dispatcher != null)
                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.SystemIdle,
                            new Action(() =>
                                {
                                    if (_list1.Count != 0) return;
                                    LabelImage1.Content = "图1";
                                    LabelImage2.Content = "图2";
                                    LabelImageInfo1.Content = null;
                                    LabelImageInfo2.Content = null;
                                    Image1.Source = null;
                                    Image2.Source = null;
                                    LabelImagecount.Content = "。 共有图片：0 张。";
                                    LabelCheck.Visibility = Visibility.Hidden;
                                    MessageBox.Show(
                                        $"重命名完成,图片已经移动到“RenameFiles”目录。", "提示", MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                                    HiddLoading();
                                    System.Diagnostics.Process.Start("Explorer.exe", dir);
                                    ProgresBar1.Visibility = Visibility.Hidden;
                                    ProgresBar1.Value = 0;
                                    LabelImageListIndex.Content = 1;
                                    LabelImageListIndex2.Content = 2;
                                }
                            ));
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重命名文件发生错误，原因：{ex.Message}。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                HiddLoading();
            }
        }

        private void ReLoadImage()
        {
            try
            {
                _list1.Clear();
                DirectoryInfo folder = new DirectoryInfo(_imagepath);
                FileInfo[] sortlsit = folder.GetFiles("*.jpg");
                Array.Sort(sortlsit, new MyDateSorter());
                foreach (var t in sortlsit)
                {
                    _list1.Add(new ImagePath(t.Name, t.FullName));
                }

                UpdateProgressInvoke(_maxvalue - _list1.Count);
                if (_list1.Count <= 0) return;
                var s = GetCsData(_list1[0].PhotoPath, out string sblx);
                SetVehicleWfxx(s, sblx);
            }
            catch (Exception exf)
            {
                MessageBox.Show($"无法重新加载数据，原因：{exf.Message}。","提示",MessageBoxButton.OK,MessageBoxImage.Information);
            }
        }

        private delegate void UpdateProgressDelegate(int currentlength);

        private void UpdateProgressInvoke(int currentlength)
        {
            UpdateProgressDelegate s = UpdateProgress;
            Dispatcher.BeginInvoke(s, currentlength);
        }

        private void UpdateProgress(int currentlength)
        {
            ProgresBar1.Value = currentlength;
        }

        /// <summary>显示数据下载进度</summary>
        private void ShowLoading()
        {
            LoadingWait1.Visibility = Visibility.Visible;
        }

        private void HiddLoading()
        {
            if (Application.Current.Dispatcher != null)
                Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.SystemIdle,
                    new Action(() => { LoadingWait1.Visibility = Visibility.Collapsed; }
                    ));
        }
        /// <summary>
        /// 上一组
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SimpleButtonNext_Click(object sender, RoutedEventArgs e)
        {
            _imageListIndex += 2;
            if (_imageListIndex > _list1.Count - 1)
            {
                _imageListIndex -= 2;
            }

            LabelImageListIndex.Content = _imageListIndex+1;
            LabelImageListIndex2.Content = _imageListIndex + 2;
            LoadImage(_imageListIndex);
        }
        /// <summary>
        /// 下一组
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SimpleButtonBack_Click(object sender, RoutedEventArgs e)
        {
            _imageListIndex -= 2;
            if (_imageListIndex < 0)
            {
                _imageListIndex = 0;
            }
            LabelImageListIndex.Content = _imageListIndex+1;
            LabelImageListIndex2.Content = _imageListIndex + 2;
            LoadImage(_imageListIndex);
        }
    }

    public class Vehicle
    {
        public string Hphm { get; set; }
        public string Hpzl { get; set; }
        public DateTime Wfsj { get; set; }
        public string Wfxw { get; set; }
        public int Clsd { get; set; }
        public int Clxs { get; set; }
        public int DcClxs { get; set; }
    }

    public class ImagePath
    {
        public string PhotoName { get; }
        public string PhotoPath { get; }

        public ImagePath(string photoName, string photoPath)
        {
            PhotoName = photoName;
            PhotoPath = photoPath;
        }
    }
}