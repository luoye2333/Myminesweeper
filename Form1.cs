using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace myMineSweeper
{
    public partial class Form1 : Form
    {
        mineSweeper m;
        public Form1()
        {
            InitializeComponent();
            timer1.Interval = 1000;
            timer1.Enabled = true;
            m = new mineSweeper(30, 16, 99, pictureBox1);
        }

        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) lmb = true;
            if (e.Button == MouseButtons.Right) rmb = true;
        }
        bool lmb = false;
        bool rmb = false;
        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (lmb && rmb) m.doubleClick(e.Location);
            else if (lmb) m.leftClick(e.Location);
            else if (rmb) m.rightClick(e.Location);
            lmb = false; rmb = false;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            try
            {
                int w, h, n;
                w = int.Parse(textBox1.Text);
                h = int.Parse(textBox2.Text);
                n = int.Parse(textBox3.Text);
                m = new mineSweeper(w, h, n, pictureBox1);
                
            }
            catch(Exception){
                textBox1.Text = 30.ToString();
                textBox2.Text = 16.ToString();
                textBox3.Text = 99.ToString();
            }
            
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            DateTime d = m.tick();
            label4.Text = d.ToString("HH:mm:ss");
            label5.Text = m.remainNum.ToString();
        }
    }
}
