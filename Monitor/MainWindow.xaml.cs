using System.Net.Http;
using System.Text.Json;
using System.Windows;

namespace Monitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static HttpClient httpclient = new();
        private readonly Timer timer;

        private Dictionary<long, DetailsWindow> dwindows = new();

        public MainWindow()
        {
            InitializeComponent();
            // 退出时确保所有子窗口都关闭，否则会有“残留”的后台进程
            Closing += (s,e) =>
            {
                foreach (var i in dwindows)
                {
                    i.Value.allow_close = true;
                    i.Value.Close();
                }
                // 即使是空的，若不进行一次清除，
                // 仍然会导致后台进程残留
                dwindows.Clear();
            };

            // 初始化定时器
            timer = new(new TimerCallback(async (s) => {
                // 必须使用Dispatcher.Invoke
                // 否则会出现线程错误
                await Dispatcher.Invoke(async () =>
                {
                    bool result = await fetchMessages();
                });
            }));


        }
        private async void On_Button_Connect_Clicked(object sender, RoutedEventArgs e)
        {
            Button_Connect.IsEnabled = false;

            if (httpclient.BaseAddress != null)
            {
                httpclient.Dispose();
                httpclient = new();
            }
            httpclient.BaseAddress = new Uri(TextBox_Api.Text);
            bool result = await fetchMessages();

            if(result)
            {
                // 启动定时器,2s一更新
                timer.Change(0, 2000);
                ComboBox_Id.SelectedIndex = ComboBox_Id.Items.Count <= 0 ? -1 : 0;
                ComboBox_Id.IsEnabled = true;
                Button_Details.IsEnabled = true;
                TextBlock_ConnectStatus.Text = "已连接";
                TextBlock_NodeCount.Text = dwindows.Count().ToString();
            }
            Button_Connect.IsEnabled = true;
        }

        private void On_Button_Details_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                bool res = long.TryParse(ComboBox_Id.SelectedItem.ToString(), out long id);
                if (res && dwindows.ContainsKey(id - 1))
                {
                    dwindows[id - 1].Show();
                }
                //else
                //{
                // 应该抛出一个致命错误？
                //}
            }
            catch
            {
                showMessageBox("非法的节点!");
            }
        }
        private async Task<bool> fetchMessages()
        {
            var json_temperature = new NodesList();
            var json_humidity = new NodesList();
            var json_water_flow = new NodesList();
            var json_water_level = new NodesList();

            try
            {
                using HttpResponseMessage res_temperature = await httpclient.GetAsync("/t");
                using HttpResponseMessage res_humidity = await httpclient.GetAsync("/h");
                using HttpResponseMessage res_water_flow = await httpclient.GetAsync("/fl");
                using HttpResponseMessage res_water_level = await httpclient.GetAsync("/wl");

                json_temperature = JsonSerializer.Deserialize<NodesList>(
                    await res_temperature.Content.ReadAsStringAsync()
                    );

                json_humidity = JsonSerializer.Deserialize<NodesList>(
                    await res_humidity.Content.ReadAsStringAsync()
                    );

                json_water_flow = JsonSerializer.Deserialize<NodesList>(
                    await res_water_flow.Content.ReadAsStringAsync()
                    );

                json_water_level = JsonSerializer.Deserialize<NodesList>(
                    await res_water_level.Content.ReadAsStringAsync()
                    );
            }
            catch
            {
                showMessageBox("api返回内容非法，将断开连接");
                // 停止计时器
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                // 清空下拉框
                ComboBox_Id.Items.Clear();
                ComboBox_Id.IsEnabled = false;
                Button_Details.IsEnabled = false;
                TextBlock_ConnectStatus.Text = "未连接";
                // 清空所有子窗口
                foreach (var i in dwindows)
                {
                    i.Value.allow_close = true;
                    i.Value.Close();
                }
                dwindows.Clear();
                return false;
            }

            // 以温度信息是变化最快的，以它的数量为基准
            TextBlock_NodeCount.Text = json_temperature.datas.Count.ToString();

            foreach(var t in json_temperature.datas)
            {
                // 根据id新建一个详细信息窗口
                if (!dwindows.ContainsKey(t["id"] - 1))
                {
                    ComboBox_Id.Items.Add(t["id"]);
                    dwindows[t["id"] - 1] = new();
                    dwindows[t["id"] - 1].setId(t["id"]);
                }

                dwindows[t["id"] - 1].addTemperature(t["t"]);
            }

            foreach (var h in json_humidity.datas)
            {
                dwindows[h["id"] - 1].addHumidity(h["h"]);
            }

            foreach (var fl in json_water_flow.datas)
            {
                dwindows[fl["id"] - 1].addWaterflow(fl["fl"]);
            }

            foreach (var wl in json_water_level.datas)
            {
                dwindows[wl["id"] - 1].addWaterLevel(wl["wl"]);
            }
            return true;
        }

        private static void showMessageBox(string message,int level = 2)
        {
            switch(level)
            {
                case 0:
                    MessageBox.Show(message,"信息");
                    break;
                case 1:
                    MessageBox.Show(message, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                case 2:
                    MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                default:
                    MessageBox.Show(message);
                    break;
            }
        }
    }

    
    public struct NodesList
    {
        public List<Dictionary<string,long>> datas { get; set; }
    }

}