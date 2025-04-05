using Library.Module;
using Microsoft.Win32;
using NExcel;
using OfficeOpenXml;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;


namespace TestPGD.Models
{
    internal class ObjectInfo : INotifyPropertyChanged
    {
        private string _name = "";
        public string Name
        { 
            get { return _name; } 
            set
            {
                _name = value;
                SetInfoList();
                NotifyPropertyChanged(nameof(Name));
            }
        }
        private float _distance = 0.00F;
        public float Distance 
        {
            get { return _distance; } 
            set
            {
                _distance = value;
                SetInfoList();
                NotifyPropertyChanged(nameof(Distance));
            }
        }
        private float _angle = 0.00F;
        public float Angle 
        { 
            get { return _angle; }
            set
            {
                _angle = value;
                SetInfoList();
                NotifyPropertyChanged(nameof(Angle));
            }
        }
        private float _width = 0.00F;
        public float Width 
        {
            get { return _width; }
            set
            {
                _width = value;
                SetInfoList();
                NotifyPropertyChanged(nameof(Width));
            }
        }
        private float _heigth = 0.00F;
        public float Heigth 
        {
            get { return _heigth; }
            set
            {
                _heigth = value;
                SetInfoList();
                NotifyPropertyChanged(nameof(Heigth));
            }
        }
        private bool _isDefect = false;
        public bool IsDefect 
        {
            get { return _isDefect; }
            set
            {
                _isDefect = value;
                SetInfoList();
                NotifyPropertyChanged(nameof(IsDefect));
            }
        }

        public List<string> InfoList { get; set; } = [];

        private void SetInfoList()
        {
            InfoList.Clear();
            InfoList.Add($"Name:\t{Name}");
            InfoList.Add($"Distance:\t{string.Format("{0:N2}", Distance).Trim()}");
            InfoList.Add($"Angle:\t{string.Format("{0:N2}", Angle).Trim()}");
            InfoList.Add($"Width:\t{string.Format("{0:N2}", Width).Trim()}");
            InfoList.Add($"Heigth:\t{string.Format("{0:N2}", Heigth).Trim()}");
            InfoList.Add($"IsDefect:\t{ToYesNo()}");

            NotifyPropertyChanged(nameof(InfoList));
            string ToYesNo() { return IsDefect ? "Да" : "Нет"; }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    internal class ObjectInfoMVVM : INotifyPropertyChanged
    {
        public ObservableCollection<ObjectInfo> ListObjectInfo { get; set; } = [];
        public bool IsIndeterminate { get; set; } = false;
        private RelayCommand? openFileToExport;
        public int ProgressBarValue { get; set; } = 0;
        public PlotModel? GraphModel { get; private set; }

        public ObjectInfoMVVM() 
        {
            GraphModel = new()
            {
                Title = "Графическое представление",
                PlotType = PlotType.Cartesian
            };

            GraphModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0F, Maximum = 21F });    //X
            GraphModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 13 });        //Y
        }

        private void ObjectInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e) => DrawGraph();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public RelayCommand OpenFileToExport => openFileToExport ??= new RelayCommand(async (o) =>
        {
            if (WorkWithExcel.GetLoadinFileName(out string FileName))
            {
                ClearObjectInfoPropertyChanged();                
                var progress = new Progress<int>(value => UpdateProgressBarValue(value));
                if (Path.GetExtension(FileName).Equals(".xls", StringComparison.CurrentCultureIgnoreCase))
                {
                    ListObjectInfo = new ObservableCollection<ObjectInfo>(await WorkWithExcel.ExportFromXLSFile(FileName, progress));
                }
                else if (Path.GetExtension(FileName).Equals(".xlsx", StringComparison.CurrentCultureIgnoreCase))
                {
                    ListObjectInfo = new ObservableCollection<ObjectInfo>(await WorkWithExcel.ExportFromXLSXFile(FileName, progress));
                }
                else if (Path.GetExtension(FileName).Equals(".csv", StringComparison.CurrentCultureIgnoreCase))
                {
                    IsIndeterminate = true;
                    NotifyPropertyChanged(nameof(IsIndeterminate));
                    ListObjectInfo = new ObservableCollection<ObjectInfo>(await WorkWithExcel.ExportFromCSVFile(FileName));
                    IsIndeterminate = false;
                    NotifyPropertyChanged(nameof(IsIndeterminate));
                }
                ProgressBarValue = 0;
                NotifyPropertyChanged(nameof(ProgressBarValue));
                NotifyPropertyChanged(nameof(ListObjectInfo));
                progress = null;
                DrawGraph();
                SetObjectInfoPropertyChanged();
            }
        }, null);

        internal void SetObjectInfoPropertyChanged()
        {
            foreach (ObjectInfo objectInfo in ListObjectInfo)
            {
                objectInfo.PropertyChanged += ObjectInfo_PropertyChanged;
            }
        }

        internal void ClearObjectInfoPropertyChanged()
        {
            foreach (ObjectInfo objectInfo in ListObjectInfo)
            {
                objectInfo.PropertyChanged -= ObjectInfo_PropertyChanged;
            }
        }

        internal void UpdateProgressBarValue(int value)
        {
            ProgressBarValue = value;
            NotifyPropertyChanged(nameof(ProgressBarValue));
        }

        internal static class WorkWithExcel
        {
            internal static bool GetLoadinFileName(out string FileName) //получить имя загружаемого файла
            {
                FileName = "";
                try
                {
                    OpenFileDialog openFileDialog = new()
                    {
                        Filter = "Excel |*.xlsx|Excel 97-2003|*.xls|CSV file |*.csv",
                        InitialDirectory = Directory.GetCurrentDirectory()
                    };
                    if (openFileDialog.ShowDialog() == true) // Открываем окно диалога с пользователем
                    {
                        FileName = openFileDialog.FileName;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка выбора файла", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return false;
            }
            
            async internal static Task<List<ObjectInfo>> ExportFromXLSXFile(string FileName, IProgress<int> progress)
            {
                List<ObjectInfo> objectInfoList = [];
                try
                {
                    await Task.Run(() =>
                    {
                        using var package = new ExcelPackage(FileName);
                        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                        //int columnCount = worksheet.Dimension.End.Column;     //get Column Count ??? or 6
                        int rowCount = worksheet.Dimension.End.Row;             //get row count
                        ObjectInfo objectInfo;
                        for (int row = 2; row <= rowCount; row++)               //row number 1 is Header
                        {
                            _ = float.TryParse(worksheet.Cells[row, 2].Value.ToString(), out float distance);
                            _ = float.TryParse(worksheet.Cells[row, 3].Value.ToString(), out float angle);
                            _ = float.TryParse(worksheet.Cells[row, 4].Value.ToString(), out float width);
                            _ = float.TryParse(worksheet.Cells[row, 5].Value.ToString(), out float height);
                            bool isdefect = worksheet.Cells[row, 6].Value.ToString()!.Contains("yes", StringComparison.CurrentCultureIgnoreCase);
                            objectInfo = new ObjectInfo
                            {
                                Name = worksheet.Cells[row, 1].Value.ToString()!.Trim(),
                                Distance = distance,
                                Angle = angle,
                                Width = width,
                                Heigth = height,
                                IsDefect = isdefect
                            };
                            objectInfoList.Add(objectInfo);
                            if (row % 1000 == 0)
                            {
                                progress.Report(row/rowCount * 100);
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка экспорта файла", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return objectInfoList;
            }

            async internal static Task<List<ObjectInfo>> ExportFromXLSFile(string FileName, IProgress<int> progress)
            {
                List<ObjectInfo> objectInfoList = [];
                Workbook? workbook = null;
                try
                {
                    await Task.Run(() =>
                    {
                        // open the Excel workbook
                        workbook = Workbook.getWorkbook(FileName);
                        ObjectInfo objectInfo;
                        // for each sheet in workbook, write cell values to console
                        foreach (Sheet sheet in workbook.Sheets)
                        {
                            // for each row
                            for (int row = 1; row < sheet.Rows; row++) //row number 0 is Header
                            {
                                _ = float.TryParse(sheet.getCell(1, row).Value.ToString(), out float distance);
                                _ = float.TryParse(sheet.getCell(2, row).Value.ToString(), out float angle);
                                _ = float.TryParse(sheet.getCell(3, row).Value.ToString(), out float width);
                                _ = float.TryParse(sheet.getCell(4, row).Value.ToString(), out float height);
                                bool isdefect = sheet.getCell(5, row).Value.ToString()!.Contains("yes", StringComparison.CurrentCultureIgnoreCase);
                                objectInfo = new ObjectInfo
                                {
                                    Name = sheet.getCell(0, row).Value.ToString()!.Trim(),
                                    Distance = distance,
                                    Angle = angle,
                                    Width = width,
                                    Heigth = height,
                                    IsDefect = isdefect
                                };
                                objectInfoList.Add(objectInfo);
                                if (row % 1000 == 0)
                                {
                                    progress.Report(row / sheet.Rows * 100);
                                }
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка экспорта файла", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    workbook?.close();
                    workbook = null;
                }
                return objectInfoList;
            }

            async internal static Task<List<ObjectInfo>> ExportFromCSVFile(string FileName)
            {
                List<ObjectInfo> objectInfoList = [];
                try
                {
                    await Task.Run(() =>
                    {
                        objectInfoList = fastCSV.ReadFile<ObjectInfo>(
                            FileName,           // filename
                            true,               // has header
                            ';',                // delimiter
                            (o, c) =>           // to object function o : car object, c : columns array read
                            {
                                _ = float.TryParse(c[1].ToString(), out float distance);
                                _ = float.TryParse(c[2].ToString(), out float angle);
                                _ = float.TryParse(c[3].ToString(), out float width);
                                _ = float.TryParse(c[4].ToString(), out float height);
                                bool isdefect = c[5].ToString()!.Contains("yes", StringComparison.CurrentCultureIgnoreCase);
                                o.Name = c[0];
                                o.Distance = distance;
                                o.Angle = angle;
                                o.Width = width;
                                o.Heigth = height;
                                o.IsDefect = isdefect;
                                // add to list
                                return true;
                            });
                    });
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message ?? "", "Ошибка экспорта файла", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return objectInfoList;
            }
        }

        public void DrawGraph()
        {
            if (ListObjectInfo?.Count == 0)
            {
                return;
            }
            GraphModel?.Series.Clear();
            GraphModel?.Axes.Clear();
            GraphModel?.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0F, Maximum = 21F });    //X
            GraphModel?.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 13 });        //Y

            const int N = 4;
            //var customMarkerOutline = new ScreenPoint[N];
            //for (int i = 0; i < N; i++) //marker is five corner star if N=6
            //{
            //    double th = Math.PI * (4.0 * i / (N - 1) - 0.5);
            //    const double R = 1;
            //    customMarkerOutline[i] = new ScreenPoint(Math.Cos(th) * R, Math.Sin(th) * R);
            //}

            foreach (ObjectInfo objectInfo in ListObjectInfo) 
            {
                var customMarkerOutline = new ScreenPoint[N];
                //top left corner (X, Y).   Default (0,0). Start of coordinate axes (X to right, Y to down)
                //bottom left corner.       Default (0,1)
                //bottom right corner.      Default (1,1)
                //top right corner.         Default (1,0)
                customMarkerOutline[0] = new ScreenPoint(-0.5, -0.5);
                customMarkerOutline[1] = new ScreenPoint(-0.5, 0.5);
                customMarkerOutline[2] = new ScreenPoint(0.5, 0.5);
                customMarkerOutline[3] = new ScreenPoint(0.5, -0.5);
                var series = new LineSeries
                {
                    Title = objectInfo.Name,
                    LineStyle = LineStyle.None,
                    Color = OxyColors.Red,
                    //StrokeThickness = 2,
                    MarkerType = MarkerType.Custom,
                    MarkerOutline = customMarkerOutline,
                    MarkerFill = OxyColors.Transparent,
                    MarkerStroke = OxyColors.Black,
                    MarkerStrokeThickness = 1,
                    MarkerSize = (objectInfo.Width * objectInfo.Heigth) == 0 ? 10 : (objectInfo.Width * objectInfo.Heigth) * 10
                };
                series.Points.Add(new DataPoint(objectInfo.Distance, objectInfo.Angle));
                GraphModel?.Series.Add(series);
            }
            GraphModel?.InvalidatePlot(true);
        }
    }
}