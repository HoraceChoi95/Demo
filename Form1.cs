using System;
using System.Data;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Windows.Forms;





namespace BackStageSur
{
    using HoraceOriginal;//添加引用WCFError错误类
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]//返回详细错误信息开启
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
        }

        private ServiceHost host;

        private void button1_Click(object sender, EventArgs e)
        {

            Uri baseAddress = new Uri("http://202.115.74.254:9999/");
            this.host = new ServiceHost(typeof(cl), baseAddress);
            host.AddServiceEndpoint(typeof(Icl), new WSHttpBinding(SecurityMode.None), "cl");
            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            smb.HttpGetEnabled = true;
            smb.HttpGetUrl = new Uri("http://202.115.74.254:9999/cl/metadata");
            host.Description.Behaviors.Add(smb);
            host.Opened += delegate { MessageBox.Show("服务已经启动！"); };
            host.Open();
            this.bt_Ini.Enabled = false;
            this.bt_Stop.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {

            host.Closed += delegate { MessageBox.Show("服务已经停止！"); };
            this.host.Close();
            this.bt_Stop.Enabled = false;
            this.bt_Ini.Enabled = true;

        }


    }



    [ServiceBehavior(IncludeExceptionDetailInFaults = true, ConcurrencyMode = ConcurrencyMode.Multiple)]//返回详细错误信息开启,开启多线程

    
    public class cl : Icl

    {

        public const string connstr = "Server=124.161.78.133;Port=9620;Database=BackStageSur;Uid=postgres;Pwd=swjtu;";
        public int Login(string p, string pswd)//登录方法
        {
            try
            {
                string sqlstrlgn = "select passwd from ser.tb_login where cleintid='" + p + "'";//选择clientid相对应的MD5
                Npgsql.NpgsqlConnection myconnlgn = new Npgsql.NpgsqlConnection(connstr);
                Npgsql.NpgsqlCommand mycommlgn = new Npgsql.NpgsqlCommand(sqlstrlgn, myconnlgn);

                //Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(sqlstr, myconn);
                myconnlgn.Open();
                //DataTable dt = new DataTable();
                //DataSet ds = new DataSet();
                //myda.Fill(dt);
                //ds.Tables.Add(dt);
                string comp = mycommlgn.ExecuteScalar().ToString();  // TODO:test  //MD5赋值给临时变量
                myconnlgn.Close();
                myconnlgn.Dispose();
                if (comp == "" || comp == null)//判断是否有对应MD5
                {
                    return 2;
                }
                else if (comp == pswd)//判断相等
                {
                    return 0;
                }
                else return 1;
            }
            catch (Npgsql.NpgsqlException ne)//如果数据库连接过程中报错
            {
                var nerror = new WCFError("Select", ne.Message.ToString());//实例化WCFError，将错误信息传入WCFError
                throw new FaultException<WCFError>(nerror, nerror.Message);//抛出错误

            }
            catch (TimeoutException te)//如果数据库未在侦听
            {
                var terror = new WCFError("Select", te.Message.ToString());//实例化WCFError，将错误信息传入WCFError
                throw new FaultException<WCFError>(terror, terror.Message);//抛出错误
            }

        }


        public DataSet Intialize(string p)//服务器列表方法
        {
            try
            {
                string s = p;
                string sqlstrGtSrvr = "select * from sur.tb_server where tb_server.clientid='" + s + "'";
                string sqlstrGtNetbd = "select netboardid,tb_netboard.serverid,url from tb_netboard inner join tb_server on tb_netboard.serverid=tb_server.serverid where tb_server.clientid='" + s + "'";
                string sqlstrGtSrvis = "select serviceid,tb_service.serverid,servicetype,servicename,netboardid,port,connstring from tb_service inner join tb_server on tb_service.serverid=tb_server.serverid where tb_server.clientid='" + s + "'";
                Npgsql.NpgsqlConnection myconnInit = new Npgsql.NpgsqlConnection(connstr);
                Npgsql.NpgsqlCommand mycommGtSer = new Npgsql.NpgsqlCommand(sqlstrGtSrvr, myconnInit);
                Npgsql.NpgsqlDataAdapter myda = new Npgsql.NpgsqlDataAdapter(sqlstrGtSrvr, myconnInit);
                myconnInit.Open();
                DataTable dtGtSer = new DataTable("Server");
                DataSet dsInit = new DataSet("Intialize");
                myda.Fill(dtGtSer);
                mycommGtSer.CommandText = sqlstrGtNetbd;
                myda.SelectCommand.CommandText = sqlstrGtNetbd;
                DataTable dtGtNetbd = new DataTable("Netboard");
                myda.Fill(dtGtNetbd);
                DataTable dtGtNetbd2 = new DataTable("Netboard");
                dtGtNetbd2 = ChangeColumnType(dtGtNetbd);
                mycommGtSer.CommandText = sqlstrGtSrvis;
                myda.SelectCommand.CommandText = sqlstrGtSrvis;
                DataTable dtGtSrvis = new DataTable("Service");
                
                myda.Fill(dtGtSrvis);
                
                
                dsInit.Tables.Add(dtGtSer);
                dsInit.Tables.Add(dtGtNetbd2);
                dsInit.Tables.Add(dtGtSrvis);
                return dsInit;
            }

            catch (Npgsql.NpgsqlException ne)//如果数据库连接过程中报错
            {
                var nerror = new WCFError("Select", ne.Message.ToString());//实例化WCFError，将错误信息传入WCFError
                throw new FaultException<WCFError>(nerror, nerror.Message);//抛出错误

            }
            catch (TimeoutException te)//如果数据库未在侦听
            {
                var terror = new WCFError("Select", te.Message.ToString());//实例化WCFError，将错误信息传入WCFError
                throw new FaultException<WCFError>(terror, terror.Message);//抛出错误
            }


        }
        public DataTable ChangeColumnType(DataTable dt)
        {

            DataTable tempdt = new DataTable();
            for (int i = 0; i < 3; i++)
            {
                if (i != 2)
                {
                    DataColumn tempdc = new DataColumn();
                    tempdc.ColumnName = dt.Columns[i].ColumnName;
                    tempdc.DataType = dt.Columns[i].DataType;
                    tempdt.Columns.Add(tempdc);
                }
                else
                {
                    DataColumn tempdc = new DataColumn();
                    tempdc.ColumnName = dt.Columns[i].ColumnName;
                    tempdc.DataType = typeof(String);
                    tempdt.Columns.Add(tempdc);
                }
                
            }
            
            DataRow newrow;
            foreach (DataRow dr in dt.Rows)
            {
                newrow = tempdt.NewRow();
                newrow.ItemArray = dr.ItemArray;
                tempdt.Rows.Add(newrow);
            }
            return tempdt;

        }


        public int PingService(int serviceid, ref long RtT)//同步Ping方法
        {
            Random rm = new Random();
            int i = rm.Next(0, 100);
            if(i>=0&&i<4)
            {
                RtT = 12000;
                return 1;
            }
            else if (i >= 5 && i < 19)
            {
                RtT = rm.Next(230, 350);
                return 2;
            }
            else
            {
                RtT = rm.Next(1, 80);
                return 0;
            }

        }
        public int PingNtbd(int netboardid,ref long RtT)
        {
            Random rm = new Random();
            int i = rm.Next(0, 100);
            if (i >= 0 && i < 4)
            {
                RtT = 12000;
                return 1;
            }
            else if (i >= 5 && i < 19)
            {
                RtT = rm.Next(230, 350);
                return 2;
            }
            else
            {
                RtT = rm.Next(1, 80);
                return 0;
            }
        }


        
    }
    

        [ServiceContract(Namespace = "Horace")]

        public interface Icl
        {

            [OperationContract]

            int Login(string p, string pswd);
            [OperationContract]
            [FaultContract(typeof(WCFError))]//制定返回的错误为WCFError型
            DataSet Intialize(string p);
        [OperationContract]
        [FaultContract(typeof(WCFError))]//制定返回的错误为WCFError型
        int PingService(int serviceid, ref long RtT);
        [OperationContract]
        [FaultContract(typeof(WCFError))]//制定返回的错误为WCFError型
        int PingNtbd(int netboardid, ref long RtT);
        }
    

    
}
namespace HoraceOriginal
{
    [DataContractAttribute(Namespace = "Horace")]
    public class WCFError
    {
        public WCFError(string operation, string message)
        {
            if (string.IsNullOrEmpty(operation))
            {
                throw new ArgumentNullException("operation");
            }
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException("message");
            }

            Operation = operation;
            this.Message = message;
        }
        [DataMember]
        public string Operation
        { get; set; }
        [DataMember]
        public string Message
        { get; set; }
    }

}
