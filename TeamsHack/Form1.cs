using OpenScreen.Core.Screenshot;
using OpenScreen.Core.Server;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using static OpenScreen.Core.Screenshot.Resolution;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TeamsHack
{
    public partial class Form1 : Form
    {
        private StreamingServer _streamingServer;
        List<Window> _windows;

        public Form1()
        {
            InitializeComponent();

            ListWindows();

        }

        private void ListWindows()
        {
            _windows = DesktopWindow.GetWindows();

            ((ListBox)this.checkedListBox1).DataSource = _windows;
            ((ListBox)this.checkedListBox1).DisplayMember = "DisplayText";
            ((ListBox)this.checkedListBox1).ValueMember = "IsChecked";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var selectedWindows = _windows;
            // DesktopWindow.PopulateArea(selectedWindows);

            Areas.areas.Clear();
            foreach (var item in selectedWindows)
            {
                Area areaToAdd = item.areas.FirstOrDefault();
                areaToAdd.Hwnd = item.hWnd;
                areaToAdd.IsShared = item.IsChecked;
                Areas.areas.Add(areaToAdd);
            }

            var resolution = Resolution.Resolutions.OneThousandAndEightyP;
            bool isDisplayCursor = true;

            _streamingServer = StreamingServer.GetInstance(resolution, Fps.OneHundredAndTwenty, isDisplayCursor);
            _streamingServer.Start(IPAddress.Parse("127.0.0.1"), 3030);

            DesktopWindow.StartPositionTracker();
        }

        private void Form1_LocationChanged(object sender, EventArgs e)
        {
            Areas.SetArea(this.DesktopBounds);
        }

        private IList<Window> SelectedWindows()
        {
             return _windows.Where(w => w.IsChecked).ToList();
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            var list = ((ListBox)sender).DataSource as IList<Window>;
             
            var windowObj = list.ElementAt(e.Index);
            windowObj.IsChecked = e.NewValue == CheckState.Checked;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DesktopWindow.StopPositionTracker();
            _streamingServer.Stop();
            Areas.areas.Clear();
        }
    }
}
