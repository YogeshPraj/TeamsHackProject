using OpenScreen.Core.Screenshot;
using OpenScreen.Core.Server;
using System;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace TeamsHack
{
    public partial class Form1 : Form
    {
        private StreamingServer _streamingServer;

        public Form1()
        {
            InitializeComponent();
            ListWindows();
        }

        private void ListWindows()
        {
            var windows = DesktopWindow.GetWindows();
            foreach (var win in windows)
            {
                checkedListBox1.Items.Add(win);
                Areas.areas.Add(win.area);
            }

            //Areas.areas = windows.Select(x => x.area).ToList();
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var resolution = Resolution.Resolutions.OneThousandAndEightyP;
            bool isDisplayCursor = true;

            _streamingServer = StreamingServer.GetInstance(resolution, Fps.OneHundredAndTwenty, isDisplayCursor);
            _streamingServer.Start(IPAddress.Parse("127.0.0.1"), 3030);

        }

        private void Form1_LocationChanged(object sender, EventArgs e)
        {
            //Areas.SetArea(this.DesktopBounds);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            Window window = checkedListBox1.Items[e.Index] as Window;
            window.area.IsShared = e.NewValue == CheckState.Checked;
        }
    }
}
