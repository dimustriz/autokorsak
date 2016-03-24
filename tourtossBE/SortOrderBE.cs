namespace Tourtoss.BE
{
    using System.Collections.Generic;
    using System.Text;

    //// http://rain.ifmo.ru/cat/view.php/vis/combinations/permutations-2000

    public class Figure
    {
        public int Value { get; set; }
    }

    public class Sequence
    {
        private List<Figure> figures = new List<Figure>();

        public Sequence(int figureLow, int figureHigh)
        {
            this.Min = figureLow;
            this.Max = figureHigh;
            for (int i = figureLow; i <= figureHigh; i++)
            {
                this.figures.Add(new Figure() { Value = i });
            }
        }

        public List<Figure> Figures
        {
            get { return this.figures; }
        }

        public string Text
        {
            get
            {
                StringBuilder bldr = new StringBuilder();
                if (this.figures != null)
                {
                    foreach (var item in this.figures)
                    {
                        if (bldr.Length > 0)
                        {
                            bldr.Append('-');
                        }

                        bldr.Append(item.Value);
                    }
                }

                return bldr.ToString();
            }
        }

        private int Min { get; set; }
        
        private int Max { get; set; }

        public bool Next()
        {
            if (this.figures.Count > 0)
            {
                int m = -1;

                for (int i = this.figures.Count - 2; i >= 0; i--)
                {
                    if (this.figures[i].Value < this.figures[i + 1].Value)
                    {
                        m = i;
                        break;
                    }
                }

                if (m == -1)
                {
                    return false;
                }

                int k = m;
                int minK = -1;

                for (int i = m + 1; i < this.figures.Count; i++)
                {
                    if (this.figures[i].Value > this.figures[m].Value)
                    {
                        if (minK == -1 || this.figures[i].Value < minK)
                        {
                            k = i;
                            minK = this.figures[i].Value;
                        }
                    }
                }

                int swap = this.figures[m].Value;
                this.figures[m].Value = this.figures[k].Value;
                this.figures[k].Value = swap;

                int j = 0;
                k = this.figures.Count - 1;
                int z = k + 1 - ((k + 1 - m) / 2);
                for (int i = m + 1; i < z; i++)
                {
                    swap = this.figures[i + j].Value;
                    this.figures[i + j].Value = this.figures[k - j].Value;
                    this.figures[k - j].Value = swap;
                    j++;
                }

                return true;
            }
            return false;
        }
    }
}
