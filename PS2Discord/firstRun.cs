using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PS2Discord
{
    public partial class firstRun : Form
    {
        public firstRun()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.ps2IP = ipMaskedTextBox1.FirstBox.Text + "." + ipMaskedTextBox1.SecondBox.Text + "." + ipMaskedTextBox1.ThirdBox.Text + "." + ipMaskedTextBox1.FourthBox.Text;
            Properties.Settings.Default.Save();
            this.DialogResult = DialogResult.OK;
        }

        private void firstRun_Load(object sender, EventArgs e)
        {

        }

        private void closing(object sender, FormClosingEventArgs e)
        {
        }
    }
}
