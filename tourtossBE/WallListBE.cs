namespace Tourtoss.BE
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;

    public enum HtmAlign 
    { 
        none, left, right, center, line, content 
    }

    public class TD
    {
        public TD() 
        {
            this.Align = HtmAlign.none;
        }

        public HtmAlign Align { get; set; }
        
        public string Text { get; set; }
    }

    public class TR : List<TD>
    {
        public TR()
        {
            this.Cols = new List<TD>();
        }

        public List<TD> Cols { get; set; }

        public void AddCol()
        {
            this.Cols.Add(new TD());
        }

        public void AddCol(string text)
        {
            this.AddCol(text, HtmAlign.none);
        }

        public void AddCol(string text, HtmAlign align)
        {
            var col = new TD();
            col.Text = text;
            col.Align = align;
            this.Cols.Add(col);
        }
    }

    public class HtmTable
    {
        public HtmTable()
        {
            this.Rows = new List<TR>();
            this.ColChar = ' ';
            this.SpaceChar = ' ';
            this.FillChar = ' ';
            this.LineChar = '-';
            this.ContentChar = '.';
        }

        public List<TR> Rows { get; set; }

        public char ColChar { get; set; }
        
        public char SpaceChar { get; set; }
        
        public char FillChar { get; set; }
        
        public char LineChar { get; set; }
        
        public char ContentChar { get; set; }

        public string GetText()
        {
            var bldr = new StringBuilder();
            bldr.Append("<table>");
            foreach (var row in this.Rows)
            {
                bldr.Append("<tr>");
                foreach (var col in row.Cols)
                {
                    bldr.Append("<td");
                    switch (col.Align)
                    {
                        case HtmAlign.right:
                            bldr.Append(" align='right'");
                            break;
                        case HtmAlign.center:
                            bldr.Append(" align='center'");
                            break;
                     }

                    bldr.Append(">");
                    bldr.Append(col.Text);
                    bldr.Append("</td>");
                }

                bldr.Append("</tr>");
            }

            bldr.Append("</table>");
            return bldr.ToString();
        }

        public string GetPlainHtmlText()
        {
            var bldr = new StringBuilder();
            bldr.Append("<pre>");
            foreach (var row in this.Rows)
            {
                for (int i = 0; i < row.Cols.Count; i++)
                {
                    var cell = row.Cols[i];
                    var width = this.GetColWidth(i);
                    this.FitCellTextToWidth(bldr, cell, width);
                }

                bldr.Append("<br />");
            }

            bldr.Append("</pre>");
            return bldr.ToString();
        }

        public string GetPlainText()
        {
            var bldr = new StringBuilder();
            foreach (var row in this.Rows)
            {
                for (int i = 0; i < row.Cols.Count; i++)
                {
                    var cell = row.Cols[i];
                    var width = this.GetColWidth(i);
                    this.FitCellTextToWidth(bldr, cell, width);
                }

                bldr.AppendLine();
            }

            return bldr.ToString();
        }

        public TR AddRow()
        {
            var row = new TR();
            this.Rows.Add(row);
            return row;
        }
        
        private int GetHtmlTextWidth(string text)
        {
            int len = 0;
            bool fl = false;
            bool fl2 = false;
            if (!string.IsNullOrEmpty(text))
            {
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '&')
                    {
                        fl = true;
                    }

                    if (fl && text[i] == ';')
                    {
                        fl = false;
                    }

                    if (text[i] == '<')
                    {
                        fl2 = true;
                    }

                    if (fl2 && text[i] == '>')
                    {
                        fl2 = false;
                    }

                    if (!fl && !fl2)
                    {
                        len++;
                    }
                }
            }

            return len;
        }
        
        private int GetColWidth(int colNum)
        { 
            int result = 0;
            foreach (var row in this.Rows)
            {
                if (colNum >= 0 && colNum < row.Cols.Count)
                {
                    var col = row.Cols[colNum];
                    int len = this.GetHtmlTextWidth(col.Text);
                    if (result < len)
                    {
                        result = len;
                    }
                }
            }

            return result;
        }

        private void FitCellTextToWidth(StringBuilder sB, TD cell, int width)
        {
            int len = this.GetHtmlTextWidth(cell.Text);
            switch (cell.Align)
            {
                case HtmAlign.right:
                    for (int i = 0; i < width - len; i++)
                    {
                        sB.Append(this.SpaceChar);
                    }

                    sB.Append(cell.Text);
                    break;
                case HtmAlign.content:
                     sB.Append(cell.Text);
                     bool firstChar = true;
                     for (int i = 0; i < width - len; i++)
                     {
                         if (firstChar)
                         {
                             sB.Append(this.SpaceChar);
                             firstChar = false;
                         }
                         else
                         {
                             sB.Append(this.ContentChar);
                         }
                     }

                     break;
                case HtmAlign.line:
                    for (int i = 0; i < width; i++)
                    {
                        sB.Append(this.LineChar);
                    }

                    break;
                case HtmAlign.center:
                    int j = (int)Math.Ceiling((width - len) / 2.0f);
                    for (int i = 0; i < j; i++)
                    {
                        sB.Append(this.SpaceChar);
                    }

                    sB.Append(cell.Text);
                    for (int i = j; i < width - len; i++)
                    {
                        sB.Append(this.SpaceChar);
                    }

                    break;
                case HtmAlign.left:
                    sB.Append(cell.Text);
                    for (int i = 0; i < width - len; i++)
                    {
                        sB.Append(this.FillChar);
                    }
                    
                    break;
                default:
                    sB.Append(cell.Text);
                    for (int i = 0; i < width - len; i++)
                    {
                        sB.Append(this.SpaceChar);
                    }
                    
                    break;
            }

            sB.Append(this.ColChar);
        }
    }

    public class DataSetView
    {
        private DataTable datatable = new DataTable();

        public DataTable Table 
        { 
            get { return this.datatable; } 
        }

        public DataRow AddRow()
        {
            var row = this.datatable.NewRow();
            this.datatable.Rows.Add(row);
            return row;
        }

        public DataColumn AddCol(string name, string caption, Type dataType)
        {
            DataColumn col = this.datatable.Columns.Add(name, dataType);
            col.Caption = caption;
            return col;
        }

        public DataColumn AddCol(string name, Type dataType)
        {
            return this.AddCol(name, name, dataType);
        }

        public DataColumn AddCol(string name, string caption)
        {
            return this.AddCol(name, caption, typeof(string));
        }

        public DataColumn AddCol(string name)
        {
            return this.AddCol(name, typeof(string));
        }

        public string GetPlainText()
        {
            return "GetPlainText";
        }
    }
}
