using System.Windows;

namespace Monitor
{
    /// <summary>
    /// Details.xaml 的交互逻辑
    /// </summary>
    public partial class DetailsWindow : Window
    {
        // logger，实现动态图表
        private readonly List<ScottPlot.Plottables.DataLogger> loggers = new();
        private long id;
        // 是否允许关闭此窗口
        public bool allow_close = false;

        public DetailsWindow()
        {
            InitializeComponent();

            loggers.Add(Plot_Temperature.Plot.Add.DataLogger());
            loggers.Add(Plot_Humidity.Plot.Add.DataLogger());
            //loggers.Add(Plot_KValue.Plot.Add.DataLogger());

            foreach (var i in loggers)
            {
                i.LineStyle.Width = 3;
                i.ViewSlide(100);
            }

            //Plot_Temperature.Plot.Axes.DateTimeTicksBottom();
            
            //loggers[0].Data.OffsetX = DateTime.Now.ToOADate();

            Plot_Temperature.Plot.Title("Temperature", 18);

            Plot_Humidity.Plot.Title("Humidity", 18);

            //Plot_KValue.Plot.Title("Water Flow Rate", 25);


            Plot_Temperature.Plot.Axes.AutoScale();
            Plot_Humidity.Plot.Axes.AutoScale();
            //Plot_KValue.Plot.Axes.AutoScale();

            // 当关闭事件发生时，如果不允许关闭，则隐藏窗口
            Closing += (s, e) => {
                if (!allow_close)
                {
                    // 拦截并取消关闭事件
                    e.Cancel = true; 
                    Hide(); 
                }
            };

        }

        public long getId()
        {
            return id;
        }

        public void setId(long id)
        {
            this.id = id;
            TextBlock_Id.Text = id.ToString();
        }

        public void addTemperature(long temperature)
        {
            double t = temperature / 10.0;
            TextBlock_Temperature.Text = t.ToString();
            loggers[0].Add(t);
            Plot_Temperature.Refresh();
            TextBlock_Time.Text = DateTime.Now.ToString("MM/dd HH:mm");
        }

        public void addHumidity(long humidity)
        {
            double t = humidity / 10.0;
            TextBlock_Humidity.Text = t.ToString();
            loggers[1].Add(t);
            Plot_Humidity.Refresh();
        }

        public void addWaterflow(long waterflow)
        {
            TextBlock_WaterFlow.Text = (waterflow / 100.0).ToString();
        }

        public void addWaterLevel(long water_level)
        {
            TextBlock_Water_Level.Text = (water_level / 10.0).ToString();
        }
    }
}
