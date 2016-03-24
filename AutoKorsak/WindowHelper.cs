using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.ComponentModel;
using Tourtoss.BC;
using Tourtoss.BE;

namespace AutoKorsak
{
    public enum ReturnResult
    { 
        Ok,
        No,
        Yes,
        Apply,
        Cancel,
        Delete,
        Prev,
        Next
    }

    public static class WindowHelper
    {
        private static RegBC _regBc = new RegBC();

        public delegate bool ResultHandler(ReturnResult result, object value); 
        
        public static void SaveSettings(this Window target)
        {
            var key = _regBc.CreateKey(target.Name);
            Rect rect = target.RestoreBounds;
            key.SetValue("Bounds",
                ((int)rect.Left).ToString() + "," +
                ((int)rect.Top).ToString() + "," +
                ((int)rect.Width).ToString() + "," +
                ((int)rect.Height).ToString());

            key.SetValue("WindowState", target.WindowState.ToString());
        }

        public static void SaveLanguage(string language)
        {
            var key = _regBc.CreateKey(string.Empty);
            key.SetValue("Language", language);
        }

        public static void SaveTheme(Theme theme)
        {
            var key = _regBc.CreateKey(string.Empty);
            key.SetValue("Theme", theme.ToString());
        }

        public static void SavePlayerDbKind(RtKind kind)
        {
            var key = _regBc.CreateKey(string.Empty);
            key.SetValue("RatingKind", kind.ToString());
        }

        public static void SavePlayerDbUsage(bool value)
        {
            var key = _regBc.CreateKey(string.Empty);
            key.SetValue("UseLocalPlayerDb", value.ToString());
        }

        public static void SaveUseTransliteration(bool value)
        {
            var key = _regBc.CreateKey(string.Empty);
            key.SetValue("UseTransliteration", value.ToString());
        }

        public static void SaveWallListSorting(string columnName, bool asc)
        {
            var key = _regBc.CreateKey(string.Empty);
            key.SetValue("WallListSortingColumn", columnName);
            key.SetValue("WallListSortingAsc", asc.ToString());
        }

        public static void LoadSettings(this Window target, bool sizeOnly = false)
        {
            var key = _regBc.OpenKey(target.Name);
            if (key != null)
            {
                var state = System.Windows.WindowState.Normal;
                string s = key.GetValue("WindowState").ToString();
                if (s == System.Windows.WindowState.Minimized.ToString())
                    state = System.Windows.WindowState.Minimized;
                else
                    if (s == System.Windows.WindowState.Maximized.ToString())
                        state = System.Windows.WindowState.Maximized;

                if (state != System.Windows.WindowState.Minimized)
                    target.WindowState = state;

                var rect = key.GetValue("Bounds");

                if (rect != null)
                {
                    s = rect.ToString();

                    try
                    {
                        string[] arr = s.Split(',');
                        int l = int.Parse(arr[0]);
                        int t = int.Parse(arr[1]);
                        int w = int.Parse(arr[2]);
                        int h = int.Parse(arr[3]);
                        double scrW = System.Windows.SystemParameters.PrimaryScreenWidth;
                        double scrH = System.Windows.SystemParameters.PrimaryScreenHeight;

                        if (!sizeOnly)
                        {
                            if (t > scrH - h / 2 || t < -h / 2)
                                t = ((int)scrH - h) / 2;
                            target.Top = t;

                            if (l > scrW - w / 2 || l < -w / 2)
                                l = ((int)scrW - w) / 2;
                            target.Left = l;
                        }
                        if (target.SizeToContent == SizeToContent.Manual)
                        {
                            if (w > 200)
                                target.Width = w;
                            if (h > 200)
                                target.Height = h;
                        }
                    } catch
                    {}
                }
            }
        }

        public static Theme LoadTheme()
        {
            var result = Theme.Default;

            var key = _regBc.OpenKey(string.Empty);
            if (key != null)
            {
                object val = key.GetValue("Theme");
                if (val != null)
                {
                    string s = val.ToString();
                    switch (s)
                    {
                        case "Aero": result = Theme.Aero; break;
                        case "Classic": result = Theme.Classic; break;
                        case "Default": result = Theme.Default; break;
                        case "Luna": result = Theme.Luna; break;
                        case "Royale": result = Theme.Royale; break;
                    }
                }
            }

            return result;
        }

        public static RtKind LoadPlayerDbKind()
        {
            var result = RtKind.eu;

            var key = _regBc.OpenKey(string.Empty);
            if (key != null)
            {
                object val = key.GetValue("RatingKind");
                if (val != null)
                {
                    string s = val.ToString();
                    switch (s)
                    {
                        case "ru": result = RtKind.ru; break;
                        case "ua": result = RtKind.ua; break;
                        case "eu": result = RtKind.eu; break;
                    }
                }
            }

            return result;
        }

        public static bool LoadPlayerDbUsage()
        {
            var result = false;

            var key = _regBc.OpenKey(string.Empty);
            if (key != null)
            {
                object val = key.GetValue("UseLocalPlayerDb");
                if (val != null)
                {
                    string s = val.ToString();
                    result = s.ToLower() == "true";
                }
            }

            return result;
        }

        public static bool LoadUseTransliteration()
        {
            var result = true;

            var key = _regBc.OpenKey(string.Empty);
            if (key != null)
            {
                object val = key.GetValue("UseTransliteration");
                if (val != null)
                {
                    string s = val.ToString();
                    result = string.Compare(s, "true", true) == 0;
                }
            }

            return result;
        }

        public static string LoadLanguage()
        {
            var result = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;

            var key = _regBc.OpenKey(string.Empty);
            if (key != null)
            {
                object val = key.GetValue("Language");
                if (val != null)
                    result = val.ToString();
            }
            return result;
        }

        public static string LoadWallListSorting(out bool asc)
        {
            var result = "";
            asc = false;

            var key = _regBc.OpenKey(string.Empty);
            if (key != null)
            {
                object val = key.GetValue("WallListSortingColumn");
                if (val != null)
                {
                    result = val.ToString();
                }

                val = key.GetValue("WallListSortingAsc");
                if (val != null)
                {
                    string s = val.ToString();
                    asc = string.Compare(s, "true", true) == 0;
                }
            }
            return result;
        }


        /// <summary>
        /// Get the required height and width of the specified text. Uses FortammedText
        /// </summary>
        public static Size MeasureTextSize(string text, FontFamily fontFamily, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, double fontSize)
        {
            FormattedText ft = new FormattedText(text,
                                                 CultureInfo.CurrentCulture,
                                                 FlowDirection.LeftToRight,
                                                 new Typeface(fontFamily, fontStyle, fontWeight, fontStretch),
                                                 fontSize,
                                                 Brushes.Black);
            return new Size(ft.Width, ft.Height);

        }

        /// <summary>
        /// Get the required height and width of the specified text. Uses Glyph's
        /// </summary>
        public static Size MeasureText(string text, FontFamily fontFamily, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, double fontSize)
        {
            Typeface typeface = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
            GlyphTypeface glyphTypeface;

            if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
            {
                return MeasureTextSize(text, fontFamily, fontStyle, fontWeight, fontStretch, fontSize);
            }

            double totalWidth = 0;
            double height = 0;

            for (int n = 0; n < text.Length; n++)
            {
                ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[text[n]];

                double width = glyphTypeface.AdvanceWidths[glyphIndex] * fontSize;

                double glyphHeight = glyphTypeface.AdvanceHeights[glyphIndex] * fontSize;

                if (glyphHeight > height)
                {
                    height = glyphHeight;
                }

                totalWidth += width;
            }

            return new Size(totalWidth, height);
        }

        public static void ShowWindow(this Window target)
        {
            try
            {
                target.Show();
                if (target.WindowState == WindowState.Minimized)
                    target.WindowState = WindowState.Normal;
                target.Focus();
            }
            catch(InvalidOperationException)
            {
            }
        }
    }

    public enum Theme
    {
        Classic,
        Aero,
        Luna,
        Royale,
        Default
    }

    public partial class App : Application
    {
        public ResourceDictionary ThemeDictionary
        {
            // You could probably get it via its name with some query logic as well.
            get { return Resources.MergedDictionaries[0]; }
        }

        public static Uri ThemeToUri(Theme theme)
        {
            switch (theme)
            {
                case Theme.Classic: return new Uri("/PresentationFramework.Classic;V4.0.0.0;31bf3856ad364e35;component/themes/classic.xaml", UriKind.Relative);
                case Theme.Aero: return new Uri("/PresentationFramework.Aero;V4.0.0.0;31bf3856ad364e35;component/themes/aero.normalcolor.xaml", UriKind.Relative);
                case Theme.Luna: return new Uri("/PresentationFramework.Luna;V4.0.0.0;31bf3856ad364e35;component/themes/luna.normalcolor.xaml", UriKind.Relative);
                case Theme.Royale: return new Uri("/PresentationFramework.Royale;V4.0.0.0;31bf3856ad364e35;component/themes/royale.normalcolor.xaml", UriKind.Relative);
                case Theme.Default: return null;
                default: return null;
            }
        }

        public void ChangeTheme(Theme theme)
        {
            Resources.MergedDictionaries.Clear();
            if (theme != Theme.Default)
                Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = ThemeToUri(theme) });
        }

        public static object GetOpenedWindow(Type T)
        {
            return Application.Current.Windows.OfType<Window>().Where(x => x.GetType().Name == T.Name).FirstOrDefault();
        }

        [DllImport("user32")]
        private static extern int ShowWindow(IntPtr hwnd, int nCmdShow);
        [DllImport("user32", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32")]
        static extern IntPtr GetLastActivePopup(IntPtr hWnd);
        [DllImport("user32")]
        static extern bool IsWindowEnabled(IntPtr hWnd);

        //Win32 API calls necesary to raise an unowned processs main window

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        [DllImport("user32.dll")]
        static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, Int32 nMaxCount);
        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, ref Int32 lpdwProcessId);
        [DllImport("User32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_NORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWMAXIMIZED = 3;
        private const int SW_MAXIMIZE = 3;
        private const int SW_SHOWNOACTIVATE = 4;
        private const int SW_SHOW = 5;
        private const int SW_MINIMIZE = 6;
        private const int SW_SHOWMINNOACTIVE = 7;
        private const int SW_SHOWNA = 8;
        private const int SW_RESTORE = 9;
        private const int SW_SHOWDEFAULT = 10;
        private const int SW_MAX = 10;

        private const uint SPI_GETFOREGROUNDLOCKTIMEOUT = 0x2000;
        private const uint SPI_SETFOREGROUNDLOCKTIMEOUT = 0x2001;
        private const int SPIF_SENDCHANGE = 0x2;


        public static bool SetFocusToWindow(IntPtr hWnd)
        {
            bool result = false;

            if (hWnd != IntPtr.Zero)
            {
                result = true;
                IntPtr hPopupWnd = GetLastActivePopup(hWnd);

                if (hPopupWnd != null && IsWindowEnabled(hPopupWnd))
                    hWnd = hPopupWnd;

                IntPtr foregroundWindow = GetForegroundWindow();

                if (IsIconic(hWnd))
                    ShowWindowAsync(hWnd, SW_RESTORE);

                ShowWindowAsync(hWnd, SW_SHOW);

                SetForegroundWindow(hWnd);

                IntPtr Dummy = IntPtr.Zero;

                uint foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, Dummy);
                uint thisThreadId = GetWindowThreadProcessId(hWnd, Dummy);
                /*
                if (AttachThreadInput(thisThreadId, foregroundThreadId, true))
                {
                    BringWindowToTop(hWnd); // IE 5.5 related hack
                    SetForegroundWindow(hWnd);
                    AttachThreadInput(thisThreadId, foregroundThreadId, false);
                }
                */
                if (GetForegroundWindow() != hWnd)
                {
                    IntPtr Timeout = IntPtr.Zero;
                    SystemParametersInfo(SPI_GETFOREGROUNDLOCKTIMEOUT, 0, Timeout, 0);
                    SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, Dummy, SPIF_SENDCHANGE);
                    BringWindowToTop(hWnd); // IE 5.5 related hack
                    SetForegroundWindow(hWnd);
                    SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, Timeout, SPIF_SENDCHANGE);
                }

            }
            return result;
        }

        public static bool SetFocusToPreviousInstance(string windowCaption)
        {
            return SetFocusToWindow(FindWindow(null, windowCaption));
        }

        public static T TryFindParent<T>(DependencyObject current) where T : class
        {
            DependencyObject parent = VisualTreeHelper.GetParent(current);

            if (parent == null) return null;

            if (parent is T) return parent as T;
            else return TryFindParent<T>(parent);
        }

        public static bool GridCheckDblClick(MouseButtonEventArgs e)
        {
            if (e == null)
                return true;

            var obj = VisualTreeHelper.GetParent(e.OriginalSource as DependencyObject);
            
            if (obj is DataGridCell)
                return true;
            /*
            if (obj is DataGridRow))
                return true;
            */
            if (obj is ScrollContentPresenter)
                return true;

            if (obj is ContentPresenter)
            {
                if ((obj as ContentPresenter).TemplatedParent == null)
                    return true;
                if ((obj as ContentPresenter).TemplatedParent is DataGridCell)
                    return true;

                if (!((obj as ContentPresenter).TemplatedParent is DataGridColumnHeader))
                    return true;
            }

            return false;
        }
    }
    
    public class ValueFromStyleExtension : MarkupExtension
    {
        public ValueFromStyleExtension()
        {
        }

        public object StyleKey { get; set; }
        public DependencyProperty Property { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (StyleKey == null || Property == null)
                return null;
            object value = GetValueFromStyle(StyleKey, Property);
            if (value is MarkupExtension)
            {
                return ((MarkupExtension)value).ProvideValue(serviceProvider);
            }
            return value;
        }

        public static object GetValueFromStyle(object styleKey, DependencyProperty property)
        {
            Style style = Application.Current.TryFindResource(styleKey) as Style;
            while (style != null)
            {
                var setter =
                    style.Setters
                        .OfType<Setter>()
                        .FirstOrDefault(s => s.Property == property);

                if (setter != null)
                {
                    return setter.Value;
                }

                style = style.BasedOn;
            }
            return null;
        }

        public static Brush BodyBrush = ValueFromStyleExtension.GetValueFromStyle(typeof(TabControl), TabControl.BackgroundProperty) as Brush;
        public static Brush BorderBrush = ValueFromStyleExtension.GetValueFromStyle(typeof(TabControl), TabControl.BorderBrushProperty) as Brush;
    }

    public delegate void MethodInvoker();

    public class SuperBarIconConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) 
                return null;

            int result;
            if (int.TryParse(value.ToString(), out result))
                return result;

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class RecentFile : INotifyPropertyChanged
    {
        public string FileName { get; set; }
        public string DisplayName { get { return System.IO.Path.GetFileName(FileName); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            Tournament.Changed = true;
            if (null != this.PropertyChanged)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class RecentFiles
    {
        private ObservableList<RecentFile> _items;
        private int _maxFiles;
        private RegBC _regBc = new RegBC();

        public void Load()
        {
            _items.Clear();
            var key = _regBc.OpenKey("/Recent");
            if (key != null)
            {
                for (int i = 0; i < _maxFiles; i++)
                {
                    object val = key.GetValue("File" + i.ToString());
                    if (val != null)
                        _items.Add(new RecentFile() { FileName = val.ToString() });
                }
            }
        }

        public void Save()
        {
            var key = _regBc.CreateKey("/Recent");
            if (key != null)
            {
                for (int i = 0; i < _items.Count; i++)
                    key.SetValue("File" + i.ToString(), _items[i].FileName);
            }
        }

        public RecentFiles(int capacity = 10)
        {
            _maxFiles = capacity;
            _items = new ObservableList<RecentFile>();
        }

        public void Add(string fileName)
        {
            int i = _items.FindIndex(item => item.FileName == fileName);
            if (i > -1)
                _items.RemoveAt(i);
            _items.Insert(0, new RecentFile() { FileName = fileName });
            if (_items.Count > _maxFiles)
                _items.RemoveAt(_items.Count - 1);
            
        }

        public ObservableList<RecentFile> List
        {
            get { return _items; }
        }
    }
    
    public class GameResultConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((value != null) && (value.ToString().IndexOf("?") > -1))
            {
                return true;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class NewRatingConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((value != null) && (value.ToString().IndexOf("!") > -1))
            {
                return true;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class PairingMemberConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((value != null) && ((bool)value == true))
            {
                return true;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
    
    public class DgTemplateColumn : DataGridTemplateColumn
    {
        public string ColumnName
        {
            get;
            set;
        }

        protected override System.Windows.FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            // The DataGridTemplateColumn uses ContentPresenter with your DataTemplate.
            ContentPresenter cp = (ContentPresenter)base.GenerateElement(cell, dataItem);
            // Reset the Binding to the specific column. The default binding is to the DataRowView.
            BindingOperations.SetBinding(cp, ContentPresenter.ContentProperty, new Binding(this.ColumnName));
            return cp;
        }
    }

}
