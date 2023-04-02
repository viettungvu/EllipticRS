using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using ECCBase16;
using RSECC;
using RSService;

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

        private void setText(BigInteger x, BigInteger y, bool reset = true)
        {
            if (reset)
            {
                tbox.ResetText();
            }
            tbox.AppendText("x=" + x);
            tbox.AppendText(Environment.NewLine);
            tbox.AppendText("y=" + y);
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
            EiSiPoint point = AffinePoint.ToEiSiPoint(new AffinePoint())
            EiSiPoint point2 = new EiSiPoint(13,7,14, _curve);
            EiSiPoint point3 = EiSiPoint.Subtract(point, point2);
            AffinePoint p = EiSiPoint.ToAffine(point3);

            EiSiPoint test = EiSiPoint.Multiply(17, AffinePoint.ToEiSiPoint(_curve.G));
            //AffinePoint p = EiSiPoint.ToAffine(test);
            _sw.Stop();
            setText(p.X, p.Y);
            setTime();
        }
    }
}