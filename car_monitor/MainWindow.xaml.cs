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

namespace car_monitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static WebSocket ws;
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
            
        }
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Load_UI_Item();
            start_up_ws();
        }
        private void Load_UI_Item()
        {
            this.dev_select.Items.Add("测试1号");
            this.dev_select.Items.Add("测试2号");
            this.dev_select.Items.Add("测试3号");

            this.relay_id.Items.Add("华科16");

            this.data_id.Items.Add("hk16-20190413002801");
            this.data_id.Items.Add("hk16-20190413002901");
            this.data_id.Items.Add("hk16-20190413003001");
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
            //this.sys_log.Text = "haha";
            //res = CommandClass.GetOrg(CommandClass.GetOrgURL, token);
            //System.Console.WriteLine(res);
            // send test
            ws.Send(GetMasterCmd(CommandClass.cmd_3, dev_id, senddata));
            // recv
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
        //WebSocket接收数据
        
        List<byte[]> recv_all = new List<byte[]>();
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
                            // MessageBox.Show("设备已连接");
                            System.Console.WriteLine("设备已连接");
                            //this.sys_log.Text = "设备已连接";
                        }
                        else if (b[0] == 0x05)////断开消息
                        {
                            //MessageBox.Show("设备断开连接");
                           // System.Console.WriteLine("设备断开连接");
                           // this.sys_log.Text = "设备断开连接";
                        }
                        else if (b[0] == 0x06)//信息记录
                        {

                            //System.Console.WriteLine(Encoding.ASCII.GetString(b));
                            //this.Invoke(updatedata, b);
                            //byte[] id = b.Take(21).ToArray();
                            //byte[] header = b.Skip(21).Take(3).ToArray();

                            //string hexb = BitConverter.ToString(b).Replace("-", string.Empty);
                            Console.WriteLine("recv a message!\n");
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                this.status_text.Text = "recv message...";
                                string hex = BitConverter.ToString(b).Replace("-", string.Empty);
                                Console.WriteLine(hex);
                            }));
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
                                //string hex = BitConverter.ToString(data.ToArray()).Replace("-", " ");
                                //Console.WriteLine(hex);
                                double[] data_d = convert_to_int(data.ToArray());
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    this.status_text.Text = "recv all data, processing";
                                    var lineData = new XyDataSeries<double, double>();
                                    //for (int i = 0; i < data_d.Length; i++)
                                    //{
                                    //    Console.WriteLine(data_d[i]);
                                    //    lineData.Append(i, data_d[i]);
                                    //    //scatterData.Append(i, Math.Cos(i * 0.1));
                                    //}
                                    for (int i = 0; i < data_d.Length; i++)
                                    {
                                        lineData.Append(i, data_d[i]);
                                    }
                                    // Assign dataseries to RenderSeries
                                    LineSeries.DataSeries = lineData;
                                    // save to db
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

        private double[] convert_to_int(byte[] data)
        {
            string hex = BitConverter.ToString(data).Replace("-", " ");
            Console.WriteLine(hex);
            byte[] header = data.Take(3).ToArray();
            int len = header[0] * 256 + header[1];
            List<double> datanew = new List<double>();
            if(len != (data.Length - 4) / 2){
                // not match
                // TODO
                Console.WriteLine(String.Format("header told len is {0}, array len is {1}", len, (data.Length - 4) / 2));
            }
            for (int index = 0; index<len; index++)
            {
                // 不要越界!
                if ((3 + index * 2 + 1) < data.Length && (3 + index * 2) <data.Length)
                {
                    datanew.Add(
                        (data[3 + index * 2] * 256 + data[3 + index * 2 + 1]) * 3.3 / 4096
                    );
                }
            }
            
            return datanew.ToArray();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
