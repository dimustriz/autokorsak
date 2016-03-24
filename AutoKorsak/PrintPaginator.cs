using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;

using System.Data;
using System.Windows.Markup;

namespace AutoKorsak
{

    public class PrintPaginator : DocumentPaginator
    {
        private int _RowsPerPage;
        private Size _PageSize;
        private int _Rows;
        public string[] TextArr { get; set; }

        public PrintPaginator(string[] textArr, Size pageSize)
        {
            TextArr = textArr;
            _Rows = TextArr.Length;
            PageSize = pageSize;
        }

        public override DocumentPage GetPage(int pageNumber)
        {
            int currentRow = _RowsPerPage * pageNumber;

            var page = new PageElement(currentRow, Math.Min(_RowsPerPage, _Rows - currentRow), this)
            {
                Width = PageSize.Width,
                Height = PageSize.Height,
            };

            page.Measure(PageSize);
            page.Arrange(new Rect(new Point(0, 0), PageSize));

            return new DocumentPage(page);
        }

        public override bool IsPageCountValid
        { get { return true; } }

        public override int PageCount
        { get { return (int)Math.Ceiling(_Rows / (double)_RowsPerPage); } }

        public override Size PageSize
        {
            get { return _PageSize; }
            set
            {
                _PageSize = value;

                _RowsPerPage = PageElement.RowsPerPage(PageSize.Height);
            }
        }

        public override IDocumentPaginatorSource Source
        { get { return null; } }
    }
    
    public class PageElement : UserControl
    {
        private const int PageMargin = 75;
        private const int HeaderHeight = 25;
        private const int LineHeight = 20;
 
        private int _CurrentRow;
        private int _Rows;
        private PrintPaginator _paginator;

        public PageElement(int currentRow, int rows, PrintPaginator parent  )
        {
            Margin = new Thickness(PageMargin);
            _CurrentRow = currentRow;
            _Rows = rows;
            _paginator = parent;
            Width = parent.PageSize.Width;
        }

        public static int RowsPerPage(double height)
        {
            return (int)Math.Floor((height - (2 * PageMargin)
              - HeaderHeight) / LineHeight);
        }

        private static FormattedText MakeText(string text)
        {
            return new FormattedText(text, CultureInfo.CurrentCulture,
              FlowDirection.LeftToRight, new Typeface("Courier New"), 12, Brushes.Black);
        }

        protected override void OnRender(DrawingContext dc)
        {
            Point curPoint = new Point(0, 0);

            dc.DrawRectangle(Brushes.White, null,
              new Rect(curPoint, new Size(Width, Height))); 
            
            string[] arr = _paginator.TextArr;
            curPoint.X = 0;
            curPoint.Y += HeaderHeight;

            for (int i = _CurrentRow; i < _CurrentRow + _Rows && i < arr.Length; i++)
            {
                dc.DrawText(MakeText(arr[i]), curPoint);

                curPoint.Y += LineHeight;
                curPoint.X = 0;
            }
        }
    }
}
