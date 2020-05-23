using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lab3Sos
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {

            chart1.Series.Add("");
            for (int intensity = 30; intensity > 0; intensity--)
            {
                int delay = intensity;
                var gen = GenFactory.getGenerator(3, 10, 0, 10);
                var rnd = new Random();

                var pq = new PriorityQueue(30);
                for (int i = 0; i < 10000; ++i)
                {
                    if (--delay == 0)
                    {
                        gen.MoveNext();
                        pq.Add(gen.Current);
                        delay = intensity;
                    }
                    pq.Tick();
                }
                int tavrg = pq.avt / pq.number;
                chart1.Series[0].Name = "Среднее время ожидания от интенсивности";
                chart1.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                chart1.Series[0].Points.AddXY((intensity), tavrg);

                chart1.Series[1].Name = "Среднее время простоя от интенсивности";
                chart1.Series[1].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                chart1.Series[1].Points.AddXY((intensity), pq.dt / 100.0);
            }
        }
    }
}