using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Course_work
{
    public partial class Form1 : Form
    {
        private readonly GF DATA_FIELD = new GF(0x011D, 256, 0);

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string CodeHamStr = "";
            HC coderHam = new HC();
            var firtsCode = (coderHam.Code(textBox1.Text));
            for (int i = 0; i < firtsCode.Length; i++)
            {
                CodeHamStr += Convert.ToByte(firtsCode[i]);
            }
            textBox2.Text = CodeHamStr;
            RSC coder = new RSC(DATA_FIELD, (int)numericUpDown1.Value, (int)numericUpDown2.Value);
            byte[] encodedText = coder.encode(Encoding.UTF8.GetBytes(CodeHamStr));
            textBox2.Text = BitConverter.ToString(encodedText).Replace("-", "");
            textBox2.MaxLength = textBox2.Text.Length;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            string endDecodeStr = "";
            var coderHam = new HC();
            RSC coder = new RSC(DATA_FIELD, (int)numericUpDown1.Value, (int)numericUpDown2.Value);
            byte[] encodedData = this.hexStringToByteArray(textBox2.Text);
            byte[] decodedText = coder.decode(encodedData);
            textBox1.Text = (Convert.ToString(Encoding.UTF8.GetString(decodedText)));
            var endDecode = coderHam.Decode(textBox1.Text);
            for (int i = 0; i < endDecode.Count; i++)
            {
                endDecodeStr += Convert.ToByte(endDecode[i]);
            }
            if (endDecodeStr == "000000000000000")
            {
                MessageBox.Show("ERROR");
            }
            textBox1.Text = endDecodeStr;
            if (textBox1.Text == "000000000000000")
            {
                textBox1.Text = "";
            }
            //var stringArray = Enumerable.Range(0, endDecodeStr.Length / 8).Select(i => Convert.ToByte(endDecodeStr.Substring(i * 8, 8), 2)).ToArray();
            //var str = Encoding.UTF8.GetString(stringArray);
            //textBox1.Text = str;
            //textBox1.Text = Convert.ToString(coderHam.Decode(textBox1.Text));

        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = File.ReadAllText(open.FileName);
            }
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            textBox2.Clear();
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(ishexdigit(e.KeyChar)))
            {
                if (e.KeyChar != (char)Keys.Back)
                {
                    e.Handled = true;
                }
            }
        }

        private byte[] hexStringToByteArray(String hexString)
        {
            try
            {
                int charNumber = hexString.Length;
                byte[] result = new byte[charNumber / 2];
                for (int i = 0; i < charNumber; i += 2)
                {
                    result[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
                }
                return result;
            }
            catch
            {
                MessageBox.Show("Ошибка");
                textBox2.Clear();
                return null;
            }
        }

        public static bool ishexdigit(char c)
        {
            if ('0' <= c && '1' >= c)             
            {
                return true;
            }
            return false;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //if (!(ishexdigit(e.keyChar)))
            //{
            //    if (e.keyChar != (char)Keys.Back)
            //    {
            //        e.Handled = true;
            //    }
            //}
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            char number = e.KeyChar;
            if (e.KeyChar == 1 || e.KeyChar == 0)
            {
                e.Handled = true;
            }
            else e.Handled = false;
        }
    }
}
