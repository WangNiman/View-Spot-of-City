﻿using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using static System.Configuration.ConfigurationManager;

using View_Spot_of_City.UIControls.Command;
using View_Spot_of_City.UIControls.Helper;
using static View_Spot_of_City.Language.Language.LanguageDictionaryHelper;
using static View_Spot_of_City.UIControls.Helper.EmailHelper;
using System.Windows.Threading;
using View_Spot_of_City.UIControls.Form;
using static View_Spot_of_City.UIControls.Helper.LoginDlgMaster;

namespace View_Spot_of_City.UIControls.UIcontrol
{
    /// <summary>
    /// Register.xaml 的交互逻辑
    /// </summary>
    public partial class Register : UserControl
    {
        /// <summary>
        /// 验证码
        /// </summary>
        char[] validateCode = new char[6];

        /// <summary>
        /// 开始验证时的邮箱
        /// </summary>
        string user_mail_before_invalidate = string.Empty;
        
        /// <summary>
        /// 用于倒计时的计时器
        /// </summary>
        DispatcherTimer sendMailBtnTimer = new DispatcherTimer();

        /// <summary>
        /// 倒计时
        /// </summary>
        int countdown = 60;

        public Register()
        {
            InitializeComponent();
            btnGetValidateCode.Content = GetString("RegisterGetValidateCode");

            //初始化定时器
            sendMailBtnTimer.Tick += new EventHandler(BeginChangeTextTimer_Tick);
            sendMailBtnTimer.Interval = new TimeSpan(0, 0, 1);
        }

        private async void btnRegister_ClickAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                //用户注册信息
                string user_mail = mailTextBox.Text;
                string user_password = passwordTextBox.Password;
                string user_password1 = password1TextBox.Password;
                string user_validateCode = validateCodeTextBox.Text;

                //验证输入
                if (user_mail == string.Empty || user_password == string.Empty || user_password1 == string.Empty || user_validateCode == string.Empty)
                {
                    MessageboxMaster.Show(GetString("Input_Empty"), AppSettings["MessageBox_Error_Title"]);
                    return;
                }
                if (!IsEmail(user_mail))
                {
                    MessageboxMaster.Show(GetString("RegisterMailFormatError"), GetString("MessageBox_Error_Title"));
                    return;
                }
                if (user_password != user_password1)
                {
                    MessageboxMaster.Show(GetString("RegisterPasswordsMismatching"), GetString("MessageBox_Error_Title"));
                    return;
                }
                if (user_password.Length > 16)
                {
                    MessageboxMaster.Show(GetString("RegisterPasswordTooLong"), GetString("MessageBox_Warning_Title"));
                    return;
                }
                if (user_password.Length < 4)
                {
                    MessageboxMaster.Show(GetString("RegisterPasswordTooShort"), GetString("MessageBox_Warning_Title"));
                    return;
                }
                string validateCodeStr = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(validateCode));
                if (user_validateCode.ToLower() != validateCodeStr.ToLower() || user_mail != user_mail_before_invalidate)
                {
                    MessageboxMaster.Show(GetString("RegisterValidateCodeError"), GetString("MessageBox_Error_Title"));
                    return;
                }

                //密码加密
                string password_encoded = MD5_Encryption.MD5Encode(user_password);

                //数据库信息
                string mysql_host = AppSettings["MYSQL_HOST"];
                string mysql_port = AppSettings["MYSQL_PORT"];
                string mysql_user = AppSettings["MYSQL_USER"];
                string mysql_password = AppSettings["MYSQK_PASSWORD"];
                string mysql_database = AppSettings["MYSQK_DATABASE"];
                string sql_string = "INSERT INTO " + AppSettings["MYSQK_TABLE_USER"] + "(Mail, Password) VALUES('" + user_mail + "','" + password_encoded + "');";

                //执行SQL查询
                string qury_result = await MySqlHelper.ExcuteNonQueryAsync(mysql_host, mysql_port, mysql_user, mysql_password, mysql_database, sql_string);

                //判断返回
                if (qury_result != "true" && qury_result != "false")
                {
                    //连接服务器错误
                    MessageboxMaster.Show(GetString("Server_Connect_Error"), GetString("MessageBox_Exception_Title"));
                    return;
                }
                else if(qury_result != "true")
                {
                    //用户已注册
                    MessageboxMaster.Show(GetString("RegisterMailEcho"), GetString("MessageBox_Tip_Title"));
                    return;
                }
                else
                {
                    MessageboxMaster.Show(GetString("RegisterOK"), GetString("MessageBox_Tip_Title"));
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageboxMaster.Show(GetString("Config_File_Error"), GetString("MessageBox_Error_Title"));
                return;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            LoginDlgCommands.CancelAndCloseFormCommand.Execute(null, this);
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            LoginDlgCommands.ChangePageCommand.Execute(LoginControls.Login, this);
        }

        private void btnGetValidateCode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //用户注册信息
                string user_mail = mailTextBox.Text;
                string user_password = passwordTextBox.Password;
                string user_password1 = password1TextBox.Password;
                string user_validateCode = validateCodeTextBox.Text;

                //验证输入
                if (user_mail == string.Empty || user_password == string.Empty || user_password1 == string.Empty)
                {
                    MessageboxMaster.Show(GetString("Input_Empty"), AppSettings["MessageBox_Error_Title"]);
                    return;
                }
                if (!IsEmail(user_mail))
                {
                    MessageboxMaster.Show(GetString("RegisterMailFormatError"), GetString("MessageBox_Error_Title"));
                    return;
                }
                if (user_password != user_password1)
                {
                    MessageboxMaster.Show(GetString("RegisterPasswordsMismatching"), GetString("MessageBox_Error_Title"));
                    return;
                }
                if (user_password.Length > 16)
                {
                    MessageboxMaster.Show(GetString("RegisterPasswordTooLong"), GetString("MessageBox_Warning_Title"));
                    return;
                }
                if (user_password.Length < 4)
                {
                    MessageboxMaster.Show(GetString("RegisterPasswordTooShort"), GetString("MessageBox_Warning_Title"));
                    return;
                }
                user_mail_before_invalidate = mailTextBox.Text;
                Random rand = new Random();
                for(int i=0;i< validateCode.Length;i++)
                {
                    validateCode[i] = rand.Next(0, 9).ToString()[0];
                }
                if(SendEmail(user_mail_before_invalidate, GetString("RegisterMailTitle"), GetString("RegisterMailContentBodyPre") + Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(validateCode)) + GetString("RegisterMailContentBodyPost")))
                {
                    BeginChangeTextTimer();
                }
                else
                {
                    MessageboxMaster.Show(GetString("RegisterMailSendError"), GetString("MessageBox_Error_Title"));
                    return;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageboxMaster.Show(GetString("Config_File_Error"), GetString("MessageBox_Error_Title"));
                return;
            }
        }

        /// <summary>
        /// 启动定时器
        /// </summary>
        private void BeginChangeTextTimer()
        {
            btnGetValidateCode.IsEnabled = false;
            btnGetValidateCode.Content = GetString("RegisterMailSendAgain") + "(" + countdown-- + "s)";
            sendMailBtnTimer.Start();
        }

        /// <summary>
        /// 定时器响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BeginChangeTextTimer_Tick(object sender, EventArgs e)
        {
            btnGetValidateCode.Content = GetString("RegisterMailSendAgain") + "(" + countdown-- + "s)";
            if (countdown <= 0)
            {
                sendMailBtnTimer.Stop();
                btnGetValidateCode.Content = GetString("RegisterGetValidateCode");
                countdown = 60;
                btnGetValidateCode.IsEnabled = true;
            }
        }
    }
}
