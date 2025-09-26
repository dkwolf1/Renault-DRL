using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Renault_DRL
{
    public partial class Form1 : Form
    {
        private int currentStep = 0;
        private List<int> brightnessSteps = new List<int>();

        public Form1()
        {
            InitializeComponent();
            // Minder flikkeren en vloeiender lijnen
            this.DoubleBuffered = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Achtergrond instellen (megane.jpg)
            Image bg = Properties.Resources.megane;
            Bitmap bmp = new Bitmap(bg.Width, bg.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix33 = 0.9f; // 0 = volledig transparant, 1 = volledig zichtbaar (dus lichter)
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                g.DrawImage(bg, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bg.Width, bg.Height, GraphicsUnit.Pixel, attributes);
            }
            this.BackgroundImage = bmp;
            this.BackgroundImageLayout = ImageLayout.Stretch;

            // DataGridView instellen
            dataGridView1.ColumnCount = 1;
            dataGridView1.Columns[0].Name = "Brightness (0-100)";
            dataGridView1.Columns[0].Width = 80;
            dataGridView1.RowTemplate.Height = 18;
            dataGridView1.RowHeadersVisible = true;
            dataGridView1.RowCount = 63;
            for (int i = 0; i < 63; i++)
            {
                dataGridView1.Rows[i].HeaderCell.Value = (i + 1).ToString();
                dataGridView1.Rows[i].Cells[0].Value = "0";
            }

            // DataGridView visueel mooier maken
            dataGridView1.BackgroundColor = this.BackColor;
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = this.BackColor;
            dataGridView1.RowHeadersDefaultCellStyle.BackColor = this.BackColor;
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.None;
            dataGridView1.GridColor = this.BackColor;
            dataGridView1.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            // Valideer invoer vóór starten
            for (int i = 0; i < dataGridView1.RowCount; i++)
            {
                object raw = dataGridView1.Rows[i].Cells[0].Value;
                int parsed;
                if (!int.TryParse(raw == null ? string.Empty : raw.ToString(), out parsed) || parsed < 0 || parsed > 100)
                {
                    // Stop en wijs de foutieve cel aan
                    timer1.Stop();
                    currentStep = 0;

                    dataGridView1.ClearSelection();
                    dataGridView1.CurrentCell = dataGridView1.Rows[i].Cells[0];
                    dataGridView1.Rows[i].Cells[0].Selected = true;

                    MessageBox.Show(
                        $"Foutieve waarde op rij {i + 1}. Voer een getal tussen 0 en 100 in.",
                        "Ongeldige invoer",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }
            }

            // Invoer is geldig: waarden overnemen en starten
            brightnessSteps.Clear();
            for (int i = 0; i < dataGridView1.RowCount; i++)
            {
                int value = Convert.ToInt32(dataGridView1.Rows[i].Cells[0].Value);
                brightnessSteps.Add(value);
            }
            currentStep = 0;
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Hier kun je eventueel animatiecode toevoegen voor een effect over de achtergrond
            currentStep++;
            if (currentStep >= brightnessSteps.Count)
            {
                timer1.Stop();
            }
            this.Invalidate(); // Forceer hertekenen zodat de lamp-sterkte wordt aangepast
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            using (var f = new GuideForm())
            {
                f.ShowDialog(this);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Originele afmetingen van megane.jpg
            int origWidth = 1920;
            int origHeight = 995;

            // Schalen naar huidig venster
            float scaleX = (float)this.ClientSize.Width / origWidth;
            float scaleY = (float)this.ClientSize.Height / origHeight;

            // Huidige brightness (0-100)
            int brightness = 0;
            if (brightnessSteps != null && brightnessSteps.Count > 0 && currentStep < brightnessSteps.Count)
            {
                brightness = brightnessSteps[currentStep];
            }

            // Perceptueel prettigere mapping dan lineair
            int alpha = BrightnessToAlpha(brightness);

            // Teken LED-strip met core + zachte gloed
            DrawLedStrip(e.Graphics, scaleX, scaleY, alpha, origWidth);
        }

        private static int BrightnessToAlpha(int brightness)
        {
            // Brighter low-end mapping:
            // - Keep 0 fully off.
            // - From 1..100, apply a mild gamma (<1) and a small floor alpha so level 10 is clearly visible.
            if (brightness <= 0) return 0;
            if (brightness > 100) brightness = 100;

            double t = brightness / 100.0;          // 0..1
            const double gamma = 0.8;               // <1 boosts low end
            const int minAlphaWhenOn = 28;          // small floor alpha (~11% of 255)

            double aNorm = Math.Pow(t, gamma);      // brighten low end
            int a = (int)Math.Round(minAlphaWhenOn + (255 - minAlphaWhenOn) * aNorm);

            if (a < 0) a = 0;
            if (a > 255) a = 255;
            return a;
        }

        private void DrawLedStrip(Graphics g, float scaleX, float scaleY, int alpha, int origWidth)
        {
            GraphicsState state = g.Save();
            try
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.CompositingMode = CompositingMode.SourceOver;

                g.ScaleTransform(scaleX, scaleY);

                // 1.5% left shift (was 1.0%)
                const float offsetPercentX = 0.015f;
                int dx = (int)Math.Round(origWidth * offsetPercentX);

                var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddLine(new Point(860 - dx, 450), new Point(1035 - dx, 470));
                path.AddLine(new Point(860 - dx, 450), new Point(890 - dx, 540));
                path.AddLine(new Point(890 - dx, 540), new Point(1010 - dx, 540));

                int baseWidth = 10;

                // Stronger glow layers to improve low-brightness visibility
                for (int i = 3; i >= 1; i--)
                {
                    int w = baseWidth + i * 6;
                    int a = (int)(alpha * (i == 3 ? 0.15 : i == 2 ? 0.28 : 0.45));
                    if (a > 0)
                    {
                        using (var glowPen = new Pen(Color.FromArgb(Math.Min(255, a), 255, 255, 255), w))
                        {
                            glowPen.StartCap = LineCap.Round;
                            glowPen.EndCap = LineCap.Round;
                            g.DrawPath(glowPen, path);
                        }
                    }
                }

                using (var corePen = new Pen(Color.FromArgb(alpha, 255, 255, 255), baseWidth))
                {
                    corePen.StartCap = LineCap.Round;
                    corePen.EndCap = LineCap.Round;
                    g.DrawPath(corePen, path);
                }
            }
            finally
            {
                g.Restore(state);
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            // Zet alle waarden in de DataGridView terug op 0
            for (int i = 0; i < dataGridView1.RowCount; i++)
            {
                dataGridView1.Rows[i].Cells[0].Value = "0";
            }
            // Stop de timer en reset de huidige stap
            timer1.Stop();
            currentStep = 0;
        }
    }
}
