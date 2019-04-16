using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SciChart.Charting.Model.DataSeries;
using WebSocketSharp;
using WebAPI;
using System.Data.SQLite;
using System.Data;
using SciChart.Charting.ChartModifiers;
using SciChart.Data.Model;
using SciChart.Charting.Visuals.Annotations;

namespace car_monitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static WebSocket ws;
        List<byte[]> recv_all = new List<byte[]>();  //WebSocket接收数据
        SQLiteConnection cn = new SQLiteConnection("data source=" + @"C:\Users\ArtisticZhao\Desktop\car_relay_db.db");
        
        bool is_first_time = true;

        int rising_p = 0;
        int stable_p = 0;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Load_UI_Item();
            start_up_ws();
            // start up db
            cn.Open();
            
        }

        private void add_log(string dev_id, byte[] data)
        {
            SQLiteCommand cmd = new SQLiteCommand();
            cmd.Connection = cn;
            cmd.CommandText = "INSERT INTO log(log_id, dev_id, relay_id, recv_time, recv_data) VALUES(@log_id,@dev_id,@relay_id,@recv_time,@recv_data)";
            cmd.Parameters.Add("log_id", DbType.Int32).Value = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; ;
            cmd.Parameters.Add("dev_id", DbType.String).Value = dev_id;
            cmd.Parameters.Add("relay_id", DbType.String).Value = "lab_relay_24v";
            cmd.Parameters.Add("recv_time", DbType.String).Value = DateTime.Now.ToLocalTime().ToString(); ;
            cmd.Parameters.Add("recv_data", DbType.String).Value = Convert.ToBase64String(data); // to base64  byte[] outputb = Convert.FromBase64String("ztKwrsTj");
            cmd.ExecuteNonQuery();
        }

        private void Load_UI_Item()
        {
            this.Zoom_btn.IsEnabled = false;
            // set zoom mode on xdriection by mouse wheel
            MouseWheelZoomModifier mwz = new MouseWheelZoomModifier();
            mwz.XyDirection = SciChart.Charting.XyDirection.XDirection;
            this.sciChartSurface.ChartModifier = new ModifierGroup(mwz);

            this.dev_select.Items.Add("测试1号");
            this.dev_select.Items.Add("测试2号");
            this.dev_select.Items.Add("测试3号");
            this.dev_select.SelectedIndex = 0;  // select test no.1
            this.relay_id.Items.Add("华科16");  
        }

        private void start_up_ws()
        {
            string token = "9b6eb08f8827feb17b07fe8a2ded7751";
            string org_id = "566";
            string dev_id = "07800175317441215151";
            ws = new WebSocket(CommandClass.GetWWSUrl(token, org_id));//选择设备时 连接websocket
            mes();
            ws.Connect();
            byte[] senddata = Encoding.ASCII.GetBytes("\nla la test\n"); ;
            this.status_text.Text = "Ready";
            // send test
            //ws.Send(GetMasterCmd(CommandClass.cmd_3, dev_id, senddata));
            // set recv mode
            ws.Send(getcmd(CommandClass.cmd_1, dev_id));
        }

        public static byte[] GetMasterCmd(byte cmd, string devNumber, byte[] b)
        {
            byte[] by = null;
            byte[] num = System.Text.Encoding.UTF8.GetBytes(devNumber);
            by = new byte[num.Length + b.Length + 1];
            by[0] = cmd;//帧首为命令
            for (int i = 0; i < num.Length; i++)
            {
                by[i + 1] = num[i];
            }
            for (int i = 0; i < b.Length; i++)
            {
                by[21 + i] = b[i];
            }
            return by;
        }

        //获取命令
        private static byte[] getcmd(byte cmd, string devNumber)
        {
            byte[] by = new byte[21];
            by[0] = cmd;
            byte[] num = System.Text.Encoding.UTF8.GetBytes(devNumber);
            for (int i = 0; i < num.Length; i++)
            {
                by[i + 1] = num[i];
            }
            return by;
        }
        
        public void mes()
        {
            
            try
            {
                ws.OnMessage += (sender, e) =>
                {
                    byte[] b = e.RawData;
                    if (b.Length > 0)
                    {
                        if (b[0] == 0x04)//连接消息
                        {
                            System.Console.WriteLine("设备已连接");
                        }
                        else if (b[0] == 0x05)////断开消息
                        {
                           System.Console.WriteLine("设备断开连接");
                        }
                        else if (b[0] == 0x06)//信息记录
                        {
                            // dev_id
                            byte[] header = b.Take(21).ToArray();
                            string s_header = BitConverter.ToString(header).Replace("-", string.Empty);

                            // debug log
                            Console.WriteLine("recv a message!\n");
                            string hex = BitConverter.ToString(b).Replace("-", string.Empty);
                            Console.WriteLine(hex);

                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                this.status_text.Text = "recv message...";
                            }));
                            if (b[21+0]== 0xc0 && b[21+3]== 0xc0)
                            {
                                Console.WriteLine("find new header");
                                recv_all.Clear();
                            }
                            recv_all.Add(b.Skip(21).ToArray());
                            if (b[b.Length-1]== 0x0A)
                            {
                                // end of package
                                List<byte> data = new List<byte>();
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    this.status_text.Text = "all message recved ploting...";

                                }));
                                foreach (byte[] i in recv_all)
                                {
                                    data.AddRange(i);  // add [] to list
                                }
                                recv_all.Clear();
                                // save to db
                                add_log(s_header, data.ToArray());
                                // starting plot
                                double[] data_d = convert_to_int(data.ToArray());
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    this.status_text.Text = "recv all data, processing";
                                    var lineData = new XyDataSeries<double, double>();
                                    
                                    for (int i = 0; i < data_d.Length; i++)
                                    {
                                        lineData.Append(i, data_d[i]);
                                    }
                                    // Assign dataseries to RenderSeries
                                    LineSeries.DataSeries = lineData;
                                    
                                    this.status_text.Text = "ready";

                                }));
                            }
                        }
                    }
                };
            }
            catch
            {
            }
        }

        private void plot(double[] data_d)
        {

            this.status_text.Text = "recv all data, processing";
            var lineData = new XyDataSeries<double, double>();
            double max_y = 0;
            for (int i = 0; i < data_d.Length; i++)
            {
                if (data_d[i] > max_y)
                {
                    max_y = data_d[i];
                }
                lineData.Append(i, data_d[i]);
            }
            // Assign dataseries to RenderSeries
            LineSeries.DataSeries = lineData;
            this.sciChartSurface.YAxis.VisibleRange = new DoubleRange(-0.5, max_y + 0.5); // zoom out on Y // 
            this.status_text.Text = "ready";
            // auto zoom in
            this.sciChartSurface.XAxis.VisibleRange = new DoubleRange(0, data_d.Length-1);
            this.calc_Q(data_d);
            this.Zoom_btn.IsEnabled = true;
        }

        private double[] convert_to_int(byte[] data)
        {
            // debug log
//             string hex = BitConverter.ToString(data).Replace("-", " ");
//             Console.WriteLine(hex);

            // frame header c0 AA AA c0  //AAAA 
            byte[] header = data.Take(3).ToArray();
            int len = header[1] * 256 + header[2];
            List<double> datanew = new List<double>();
            if(len != (data.Length - 4) / 2){
                // not match
                // TODO
                Console.WriteLine(String.Format("header told len is {0}, array len is {1}", len, (data.Length - 4) / 2));
            }
            //byte[] data_p = data.Skip(3).ToArray();
            for (int index = 0; index<len; index++)
            {
                // 不要越界!
                if ((4 + index * 2 + 1) < data.Length && (4 + index * 2) <data.Length)
                {
                    datanew.Add(
                        (data[4 + index * 2] * 256 + data[4 + index * 2 + 1]) * 3.3 / 4096
                    );
                }
            }
            
            return datanew.ToArray();
        }

        // **************算法开始************//
        private void calc_Q(double[] data)
        {
            // get low level  250 point
            double level_low = 0;
            double sum_low = 0;
            int low_avg_count = 0;
            for(int i=1; i<250; i++)
            {
                if(System.Math.Abs(data[i-1] - data[i] )> 0.1)
                {

                }
                else
                {
                    sum_low += data[i];
                    low_avg_count++;
                }
            }
            level_low = sum_low / low_avg_count;

            // get high level  point 250
            double level_high = 0;
            double sum_high = 0;
            int high_avg_count = 0;
            for (int i=0; i<250; i++)
            {
                if(System.Math.Abs(data[data.Length-1 - i] - data[data.Length-1 - i - 1])> 0.1)
                {

                }
                else
                {
                    sum_high += data[data.Length - 1 - i];
                    high_avg_count++;
                }
            }
            level_high = sum_high / high_avg_count;
            Console.WriteLine("high level = {0} low level = {1}", level_high, level_low);

            // find rising point
            
            for (int i=0; i<data.Length; i++)
            {
                // 向后均值滤波
                double avg_flt_sum = 0;
                int avg_flt_count = 0;
                for(int j=0; j<5; j++)
                {
                    avg_flt_sum += data[i + j];
                    avg_flt_count++;
                }
                if((avg_flt_sum/ avg_flt_count) > level_low + 0.2)
                {
                    // find rise point
                    rising_p = i;
                    break;
                }
            }

            // find stable point 
            
            for (int p = data.Length - 15; p > rising_p; p--)
            {
                double avg_flt_sum = 0;
                for (int j = 0; j < 4; ++j)//每4个点求均值滤波
                {
                    avg_flt_sum += data[p + j]; ;
                }
                if ((avg_flt_sum / 4) < (level_high - 0.2))//认为寻找到上电燃弧终点
                {
                    stable_p = p+1;
                    break;
                }
            }

            Console.WriteLine("start at {0}, end at {1}", rising_p, stable_p);

            // draw range
            this.sciChartSurface.Annotations.Clear();
            this.sciChartSurface.Annotations.Add(new BoxAnnotation()
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#55279B27")),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#55279B27")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                X1 = rising_p,
                X2 = stable_p,
                Y1 = level_low,
                Y2 = level_high,
                IsEditable = false,
            });

            
        }

        // **************算法结束************//

        private void Dev_select_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(this.dev_select.SelectedItem.ToString() != "测试1号")
            {
                // 禁止选择
                this.relay_id.IsEnabled = false;
                this.data_id.IsEnabled = false;
            }
            else
            {
                // 允许选择
                this.relay_id.IsEnabled = true;
                this.data_id.IsEnabled = true;
                this.relay_id.SelectedIndex = 0;
            }
        }

        private void Data_id_DropDownOpened(object sender, EventArgs e)
        {
            if (is_first_time)
            {
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = cn;
                // check all data : SELECT recv_time FROM log;
                cmd.CommandText = "SELECT recv_time FROM log ";
                SQLiteDataReader sr = cmd.ExecuteReader();
                while (sr.Read())
                {
                    this.data_id.Items.Add(sr.GetString(0));
                }
                sr.Close();
                is_first_time = false;
            }
            
        }

        private void Data_id_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SQLiteCommand cmd = new SQLiteCommand();
            cmd.Connection = cn;
            // find data SELECT recv_data FROM log WHERE recv_time = "2019/4/14 16:10:26";
            string select_data = this.data_id.SelectedItem.ToString();
            cmd.CommandText = "SELECT recv_data FROM log WHERE recv_time = \"" + select_data + "\"";
            SQLiteDataReader sr = cmd.ExecuteReader();
            sr.Read();
            byte[] outputb = Convert.FromBase64String(sr.GetString(0));  // decode base64
            plot(convert_to_int(outputb));  // redraw data
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // auto zoom in
            this.sciChartSurface.XAxis.VisibleRange = new DoubleRange(rising_p, stable_p);
        }
    }
}
