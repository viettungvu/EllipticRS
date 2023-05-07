using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ServiceBuildRS
{
    public partial class Form1 : Form
    {
        private System.Timers.Timer _timer = new System.Timers.Timer();
        public Form1()
        {
            InitializeComponent();
            _init();
        }

        private void _init()
        {
            _timer.AutoReset = true;
            _timer.Interval = 10000;
            _timer.Elapsed += _timer_Elapsed;
        }

        private async void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //await RecommendUtils.XayDungHeGoiY();
            await RSUtilsMayChuGoiY.XayDungHeGoiY();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _timer.Start();
        }
    }
}
