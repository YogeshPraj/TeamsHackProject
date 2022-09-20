using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TeamsHack
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void ListWindows()
        {
            var windows = DesktopWindow.GetWindows();
            foreach (var win in windows)
            {
                checkedListBox1.Items.Add(win.title + " [" + win.executablePath + "]");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ListWindows();
        }
    }
}
