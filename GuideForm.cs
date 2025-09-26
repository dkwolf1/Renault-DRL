using System;
using System.Drawing;
using System.Windows.Forms;

namespace Renault_DRL
{
    public class GuideForm : Form
    {
        private PictureBox _picture;
        private RichTextBox _stepsBox;
        private Button _closeBtn;

        // Keep a reference so multiple clicks don't spawn multiple overlays
        private Form _overlay;

        public GuideForm()
        {
            Text = "DDT4All Guide";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(900, 600);
            KeyPreview = true;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 62f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 38f));
            Controls.Add(layout);

            _picture = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Hand // indicate clickable
            };
            _picture.Click += Picture_Click;
            layout.Controls.Add(_picture, 0, 0);

            var bottom = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            layout.Controls.Add(bottom, 0, 1);

            _stepsBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                DetectUrls = false,
                BorderStyle = BorderStyle.None,
                BackColor = SystemColors.Window,
                Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 9.5f),
                Text = BuildStepsText()
            };
            bottom.Controls.Add(_stepsBox);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 42,
                Padding = new Padding(0, 8, 0, 0)
            };
            bottom.Controls.Add(buttons);

            _closeBtn = new Button { Text = "Close", Width = 100, Height = 30 };
            _closeBtn.Click += (s, e) => Close();
            buttons.Controls.Add(_closeBtn);

            // Load the screenshot from resources
            try
            {
                _picture.Image = Properties.Resources.ddt; // ensure resource name is 'ddt'
            }
            catch
            {
                _picture.Image = null;
            }
        }

        private void Picture_Click(object sender, EventArgs e)
        {
            if (_picture.Image == null)
                return;

            // Toggle: if overlay is open, close it; otherwise open
            if (_overlay != null && !_overlay.IsDisposed)
            {
                _overlay.Close();
                return;
            }

            _overlay = new ImageOverlayForm(_picture.Image);
            // When it closes, release reference
            _overlay.FormClosed += (s, _) => _overlay = null;
            // Show non-modal so Esc works immediately within the overlay
            _overlay.Show(this);
        }

        private static string BuildStepsText()
        {
            return
@"1. Start DDT4All and connect to the car via OBD.
2. Select the correct vehicle (e.g., Renault Mégane IV).
3. In the ECU list, choose:
   UPC-EMM | USM_CMF1_Sailing_B4_v2.1
4. Go to Screens → Lights → Lights - Configuration.
5. Locate the fields Welcome / Goodbye scenario (e.g., E1d_DRL_GoodbyeScenario).
5. Enter the desired values:
   0 = disabled
   Higher value = increased brightness / different timing.
6. Enable ""Einstein mode"" (bottom of DDT4All) to unlock parameter writing.
7. Click Send next to the parameter to apply it to the ECU.
8. Test by locking/unlocking the car to see the light sequence.

⚠️ Warning: Wrong values may cause error codes or unexpected behavior. Always write down the original settings before editing.";
        }

        // Fullscreen overlay that displays the image; close on click or Esc
        private sealed class ImageOverlayForm : Form
        {
            private readonly PictureBox _pic;

            public ImageOverlayForm(Image image)
            {
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                KeyPreview = true;
                BackColor = Color.Black;
                TopMost = true;
                ShowInTaskbar = false;

                // Use the screen where the cursor is, so it's intuitive on multi-monitor setups
                var screen = Screen.FromPoint(Cursor.Position);
                Bounds = screen.Bounds;

                _pic = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Black,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = image,
                    Cursor = Cursors.Hand
                };

                Controls.Add(_pic);

                // Close interactions
                _pic.Click += (s, e) => Close();
                MouseDown += (s, e) => Close();
                KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };

                // Optional: slight fade-in (comment out if not desired)
                try
                {
                    Opacity = 0.0;
                    var t = new Timer { Interval = 10 };
                    t.Tick += (s, e) =>
                    {
                        Opacity += 0.08;
                        if (Opacity >= 1.0)
                        {
                            Opacity = 1.0;
                            t.Stop();
                            t.Dispose();
                        }
                    };
                    t.Start();
                }
                catch
                {
                    // Opacity not critical; ignore if environment disallows it
                }
            }
        }
    }
}