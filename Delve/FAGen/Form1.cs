using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FAGen
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            StringBuilder sbHeader = new StringBuilder();
            StringBuilder sbSource = new StringBuilder();
            string[] lines = textBox1.Text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in lines)
            {
                if (str.StartsWith("#define"))
                {
                    string[] subs = str.Split(' ');
                    sbHeader.AppendLine(string.Format("static property System::String^ {0} {{ System::String^ get(); }}", subs[1].Replace("ICON_FA_",""), subs[2]));
                    //sbSource.AppendLine(string.Format("static System::String^ _{0} = gcnew System::String({1}); ", subs[1].Replace("ICON_FA_", ""), subs[2]));
                    sbSource.AppendLine(string.Format("System::String^ ICON_FA::{0}::get() {{ return gcnew System::String({1}); }}", subs[1].Replace("ICON_FA_", ""), subs[2]));
                }
            }
            textBox2.Text = sbHeader.ToString();
            textBox3.Text = sbSource.ToString();
        }
    }
}
