using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Text;
using ECCBase16;
using RSECC;
using RSService;
using static System.Net.Mime.MediaTypeNames;

namespace FormTest
{
    public partial class Form1 : Form
    {
        private static readonly ECCBase16.Curve _curve = new ECCBase16.Curve(ECCBase16.CurveName.test);
        private static Stopwatch _sw = new Stopwatch();

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _sw.Reset();
            _sw.Start();
            ECCBase16.AffinePoint point = ECCBase16.AffinePoint.FastX4(new ECCBase16.AffinePoint(_curve.G.X, _curve.G.Y, _curve));
            _sw.Stop();
            setText(point.X, point.Y);
            setTime();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _sw.Reset();
            _sw.Start();
            ECCBase16.AffinePoint point = ECCBase16.AffinePoint.FastX8(new ECCBase16.AffinePoint(_curve.G.X, _curve.G.Y, _curve));
            _sw.Stop();
            setText(point.X, point.Y);
            setTime();
        }

        private void setText(BigInteger x, BigInteger y, BigInteger? z = null, bool reset = true)
        {
            if (reset)
            {
                tbox.ResetText();
            }
            tbox.AppendText("x=" + x);
            tbox.AppendText(Environment.NewLine);
            tbox.AppendText("y=" + y);
            if (z != null)
            {
                tbox.AppendText(Environment.NewLine);
                tbox.AppendText("z=" + z);
            }
        }

        private void setTime()
        {
            lblTime.Text = String.Format("Thời gian: {0} ms", _sw.ElapsedMilliseconds);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _sw.Reset();
            _sw.Start();
            ECCBase16.AffinePoint point = ECCBase16.AffinePoint.FastX16(new ECCBase16.AffinePoint(_curve.G.X, _curve.G.Y, _curve));
            _sw.Stop();
            setText(point.X, point.Y);
            setTime();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            _sw.Reset();
            _sw.Start();
            ECCBase16.AffinePoint point = ECCBase16.AffinePoint.FastX3(new ECCBase16.AffinePoint(_curve.G.X, _curve.G.Y, _curve));
            _sw.Stop();
            setText(point.X, point.Y);
            setTime();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            _sw.Reset();
            _sw.Start();
            ECCBase16.AffinePoint point = ECCBase16.AffinePoint.DirectDoulbing(new ECCBase16.AffinePoint(_curve.G.X, _curve.G.Y, _curve));
            _sw.Stop();
            setText(point.X, point.Y);
            setTime();
        }
        string _test = "1";
        private void button6_Click(object sender, EventArgs e)
        {
            _sw.Reset();
            _sw.Start();
            ECCBase16.AffinePoint point = new ECCBase16.AffinePoint(5, 1, _curve);
            //ECCBase16.EiSiPoint eisiPoint = ECCBase16.EiSiPoint.Multiply(BigInteger.Parse(_test), ECCBase16.AffinePoint.ToEiSiPoint(point));
            //ECCBase16.AffinePoint point_convert_back = EiSiPoint.ToAffine(eisiPoint);

            ECCBase16.EiSiPoint eisiPoint1 = new EiSiPoint(8, 11, 14, _curve);
            ECCBase16.EiSiPoint eisiPoint2 = new EiSiPoint(6, 14, 1, _curve);
            ECCBase16.EiSiPoint eisiPoint3 = ECCBase16.EiSiPoint.Addition(eisiPoint2, eisiPoint1);
            // ECCBase16.EiSiPoint eisiPoint4 = ECCBase16.EiSiPoint.Multiply(2, eisiPoint3);

            ECCBase16.AffinePoint point_convert_back = EiSiPoint.ToAffine(eisiPoint3);
            _sw.Stop();
            setText(point_convert_back.X, point_convert_back.Y);
            setTime();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            _sw.Reset();
            _sw.Start();
            //ECCBase16.AffinePoint point = new ECCBase16.AffinePoint(5, 1, _curve);
            //ECCBase16.EiSiPoint eisiPoint4 = EiSiPoint.Base16Multiplicands(BigInteger.Parse(_test), ECCBase16.AffinePoint.ToEiSiPoint(point));
            //ECCBase16.AffinePoint point_convert_back = EiSiPoint.ToAffine(eisiPoint4);


            ECCBase16.AffinePoint point = _curve.G;
            ECCBase16.EiSiPoint eisiPoint4 = EiSiPoint.Base16Multiplicands(BigInteger.Parse(_test), ECCBase16.AffinePoint.ToEiSiPoint(point));
            ECCBase16.AffinePoint point_convert_back = EiSiPoint.ToAffine(eisiPoint4);
            _sw.Stop();
            setText(point_convert_back.X, point_convert_back.Y);
            setTime();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            _sw.Reset();
            _sw.Start();
            RSECC.Point point = new RSECC.Point(5, 1);
            RSECC.Point p = EcdsaMath.Multiply(point, BigInteger.Parse(_test), 19, 2, 17);
            _sw.Stop();
            setText(p.x, p.y);
            setTime();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            _sw.Reset();
            _sw.Start();
            try
            {
                ReSysUtils.Run();

            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.StackTrace);
            }
            _sw.Stop();
            setTime();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            _test = textBox1.Text;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            _sw.Reset();
            _sw.Start();
            //EiSiPoint point = AffinePoint.ToEiSiPoint(new AffinePoint())
            //EiSiPoint point2 = new EiSiPoint(13,7,14, _curve);
            //EiSiPoint point3 = EiSiPoint.Subtract(point, point2);
            //AffinePoint p = EiSiPoint.ToAffine(point3);

            //EiSiPoint test = EiSiPoint.Multiply(17, AffinePoint.ToEiSiPoint(_curve.G));
            ////AffinePoint p = EiSiPoint.ToAffine(test);
            ///
            //RSECC.Point p1 = new RSECC.Point(16,4);
            //RSECC.Point p2 = new RSECC.Point(7, 6);

            //RSECC.Point p = EcdsaMath.Sub(p1, p2, _curve.A, _curve.P);


            //EiSiPoint p1 = new EiSiPoint(13,7,14,_curve);
            //EiSiPoint p2 = new EiSiPoint(11,12,8,_curve);
            //var test = EiSiPoint.Subtract(p1, p2);
            //AffinePoint p = EiSiPoint.ToAffine(test);
            EiSiPoint p1 = new EiSiPoint(13, 7, 14, _curve);
            EiSiPoint p2 = new EiSiPoint(11, 12, 8, _curve);
            EiSiPoint p3 = p1 + p2;
            //EiSiPoint p4 = p2 + p1;
            //var test = p3 - p4;
            ////AffinePoint p = EiSiPoint.ToAffine(test);
            ////setText(p.X, p.Y);
            //_sw.Stop();
            //setText(p3.Nx, p3.Ny,p3.U);
            //setText(p4.Nx, p4.Ny, p4.U,false);
            setTime();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            _sw.Reset();
            _sw.Start();

            EiSiPoint p1 = new EiSiPoint(BigInteger.Parse("23695177185302695959474394037632632634647038205"), BigInteger.Parse("603240289522561656651601992413550636336740688900"), BigInteger.Parse("419473684555853174556433946121262519071822226632"), _curve);
            EiSiPoint p2 = new EiSiPoint(BigInteger.Parse("557943640025563640782133097065223563853896509928"), BigInteger.Parse("224104266571527473196064898897862771797759537015"), 1, _curve);
            //EiSiPoint p2 = new EiSiPoint(11, 12, 10, _curve);
            // EiSiPoint test =EiSiPoint.Addition(p1,p2);
            EiSiPoint test = EiSiPoint.Addition(p1, p2);
            EiSiPoint test2 = p2 + p1;
            AffinePoint p = EiSiPoint.ToAffine(test);
            setText(test.Nx, test.Ny, test.U);
            setText(test2.Nx, test2.Ny, test2.U, false);
            _sw.Stop();
            setTime();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            int n = 5;
            int m = 20;
            int max = 5;
            Random rd = new Random();
            ConcurrentBag<string> bag = new ConcurrentBag<string>();
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    int r = rd.Next(0, max + 1);
                    bag.Add(string.Format("{0},{1},{2}", i + 1, j + 1, r));
                }
            }
            WriteFile("Data.txt", string.Join(Environment.NewLine, bag), false);
        }

        private static string _data_folder = "D:\\Test\\Input";
        private static void WriteFile(string file_name, string content, bool append = true)
        {
            if (!string.IsNullOrWhiteSpace(file_name))
            {
                if (!Directory.Exists(_data_folder))
                {
                    Directory.CreateDirectory(_data_folder);
                }
                string full_path = Path.Combine(_data_folder, file_name);
                if (append)
                {
                    File.AppendAllText(full_path, content + "\n");
                }
                else
                {

                    File.WriteAllText(full_path, content);
                }
            }
        }
    }
}