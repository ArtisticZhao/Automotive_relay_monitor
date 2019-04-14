using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
//using System.Windows.Forms;
using System.Threading;

namespace WebAPI
{
    public class CommandClass
    {
        //反馈状态
        public const string Code_200 = "200";//正常
        public const string Code_201 = "201";
        public const string Code_300 = "300";
        public const string Code_301 = "301";
        public const string Code_302 = "302";
        public const string Code_400 = "400";
        public const string Code_401 = "401";
        public const string Code_500 = "500";

        //发送到设备的命令头
        public const byte mastcmd_1 = 0xA1;  //DS0
        public const byte mastcmd_2 = 0xA2;  //DS1
        public const byte mastcmd_3 = 0xA3;  //DS0 DS1
        public const byte mastcmd_4 = 0xA4;  //获取状态   

        //设备返回的命令头
        public const byte mast_1 = 0x51;       //DS0
        public const byte mast_2 = 0x52;        //DS1
        public const byte mast_3 = 0x53;        //DS0 DS1
        public const byte mast_4 = 0x54;        //状态

        //原子云命令头    
        public const byte cmd_1 = 0x01;         //订阅设备消息   
        public const byte cmd_2 = 0x02;         //取消订阅
        public const byte cmd_3 = 0x03;         //发送消息到设备
        public const byte cmd_4 = 0x04;         //设备连接通知
        public const byte cmd_5 = 0x05;         //设备断开通知

        /// <summary>
        /// 原子云连接路径
        /// </summary>
        public const string GetOrgURL = "https://cloud.alientek.com/api/orgs";

        /// <summary>
        /// 获取所有的分组信息
        /// </summary>
        /// <param name="id">机构唯一标识</param>
        /// <returns>URL</returns>
        public static string GetGroupListUrl(string id)
        {
            try
            {
                return "https://cloud.alientek.com/api/orgs/" + id.ToString() + "/grouplist";
            }
            catch
            {
                return "";
            }

        }
        
        /// <summary>
        /// 获取选中分组下的设备信息URL
        /// </summary>
        /// <param name="id">机构唯一标识</param>
        /// <param name="GroupId">分组的编号</param>
        /// <returns></returns>
        public static string GetDevOfGroupUrl(string id, string GroupId)
        {
            try
            {
                return "https://cloud.alientek.com/api/orgs/" + id + "/groups/" + GroupId + "/devices";
            }
            catch
            {
                return "";
            }


        }
        
        /// <summary>
        /// 获取设备连接状态URL
        /// </summary>
        /// <param name="id">机构唯一标识</param>
        /// <param name="device_id">设备唯一标识</param>
        /// <returns>设备连接状态URL</returns>
        public static string GetConStateUrl(string id, string device_id)
        {
            return "https://cloud.alientek.com/api/orgs/" + id + "/devicestate/" + device_id + "";
        }
        
        
        /// <summary>
        /// 获取设备历史信息URL
        /// </summary>
        /// <param name="id">机构唯一标识</param>
        /// <param name="number">设备编号</param>
        /// <returns>返回获取设备历史URL</returns>
        public static string GetDevHistory(string id, string number)
        {
            return "https://cloud.alientek.com/api/orgs/" + id + "/devicepacket/" + number + "";
        }

        /// <summary>
        /// 获取webSocket通信URl
        /// </summary>
        /// <param name="api_token">原子云账号的Token</param>
        /// <param name="id"></param>
        /// getGUID()本地随机生成的唯一标识 GUID字符串
        /// <returns>返回 webSocket URl</returns>
        public static string GetWWSUrl(string api_token, string id)
        {
            return "wss://cloud.alientek.com/connection/" + api_token + "/org/" + id + "?token=" + getGUID();
        }


        public static string getStr = "";   //返回的值
        public static string url = "";      //请求的API路劲
        public static string token = "";    //原子云token的值
        
        /// <summary>
        /// 请求API获取返回值
        /// </summary>
        public static void Get()
        {
            try
            {
                Encoding encoding = Encoding.UTF8;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Accept = "*/*";
                request.ContentType = "application/json";
                WebHeaderCollection hea = new WebHeaderCollection();
                hea.Add("token", token);
                request.Headers.Add(hea);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    getStr = reader.ReadToEnd();
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// 调用webAPI
        /// </summary>
        /// <param name="url">地址</param>
        /// <param name="token">原子云上token的值</param>
        /// <returns></returns>
        public static string GetOrg(string gurl, string gtoken)
        {
            try
            {
                getStr = "";
                url = gurl;
                token = gtoken;
                Thread th = new Thread(new ThreadStart(Get));
                th.IsBackground = true;
                th.Start();
                int i = 0;
                while (true)
                {
                    if (getStr.Length > 0)
                    {
                        break;
                    }
                    else
                    {
                        i += 100;
                        Thread.Sleep(100);
                    }
                    if (i >= 3000)
                    {
                        th.Abort();
                        // MessageBox.Show("请求服务超时", "提示");
                        System.Console.WriteLine("请求服务超时");
                        return getStr;
                    }
                }
                return getStr;
            }
            catch 
            {
                return "";
            }
        }

        //生成GUID码
        public static string getGUID()
        {
            System.Guid guid = new Guid();
            guid = Guid.NewGuid();
            string str = guid.ToString();
            return str;
        }
    }


    //账号机构基本信息
    public class OrgInfo
    {
        //[{"id":2,"name":"123","acc_id":0,"device_limit":1000,"device_counter":4,"created_at":"2018-04-16T11:58:51+08:00"}]}
        public string id { get; set; }     //唯一标识
        public string name { get; set; }    //机构名称
        public string device_limit { get; set; }    //允许设备数量
        public string device_count { get; set; }    //已添加设备数量
    }

    //分组
    public class GroupList
    {
        // {"id":53,"acc_id":0,"acc_name":"","org_id":0,"org_name":"","name":"分组2","transfer_groups":"","count_device":0,"created_at":"","operation":""}
        //未写注释的参数可以忽略，API调用暂时未使用未注释的参数
        public string devID { get; set; }       //分组编号
        public string acc_id { get; set; }
        public string acc_name { get; set; }
        public string org_id { get; set; }
        public string org_name { get; set; }
        public string name { get; set; }          //组名
        public string transfer_groups { get; set; }
        public string count_device { get; set; }
        public string created_at { get; set; }
        public string operation { get; set; }
    }

    //设备
    public class DevInfo
    {
        //[{"id":469,"show_id":1106,"name":"节点1","number":"44722465364284678784"},{"id":509,"show_id":1108,"name":"dsfsdfsdfsdf","number":"75890705305358438083"}]}
        public string iD { get; set; }  //设备唯一标识
        public string showID { get; set; }//设备显示编号
        public string name { get; set; }    //设备名称
        public string number { get; set; }  //设备编号
    }

    //设备历史信息
    public class DevHistory
    {
        //time":"2018-09-03T10:24:35.089372+08:00","hex_packet":"41 4C 49 45 4E 54 45 4B 2D 48 52 54 44 54","length":14,"number":"44722465364284678784"
        public string timer { get; set; }       //时间
        public string hex_packet { get; set; }  //十六进制包
        public string Length { get; set; }      //数据长度
        public string number { get; set; }      //设备编号
    }
}
