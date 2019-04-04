using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using System.Net.WebSockets;
using WebSocketSharp;
using WebAPI;

namespace test_wss_client
{
    class Program
    {
        public static WebSocket ws;
        static void Main(string[] args)
        {
            //string res = "";
            string token = "9b6eb08f8827feb17b07fe8a2ded7751";
            string org_id = "566";
            string dev_id = "07800175317441215151";
            ws = new WebSocket(CommandClass.GetWWSUrl(token, org_id));//选择设备时 连接websocket
            mes();
            ws.Connect();
            byte[] senddata = Encoding.ASCII.GetBytes("\nla la test\n");;
            //res = CommandClass.GetOrg(CommandClass.GetOrgURL, token);
            //System.Console.WriteLine(res);
            // send test
            ws.Send(GetMasterCmd(CommandClass.cmd_3, dev_id, senddata));
            // recv
            ws.Send(getcmd(CommandClass.cmd_1, dev_id));


            while (true) ;

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
        public static void mes()
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
                        }
                        else if (b[0] == 0x05)////断开消息
                        {
                            //MessageBox.Show("设备断开连接");
                            System.Console.WriteLine("设备断开连接");
                        }
                        else if (b[0] == 0x06)//信息记录
                        {
                            try
                            {
                                System.Console.WriteLine(Encoding.ASCII.GetString(b));
                                //this.Invoke(updatedata, b);
                            }
                            catch
                            {
                            }

                        }
                    }
                };
            }
            catch
            {
            }
        }
    }
}
