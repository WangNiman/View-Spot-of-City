﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using LiveCharts;
using LiveCharts.Wpf;
using static System.Configuration.ConfigurationManager;

using View_Spot_of_City.ClassModel;
using View_Spot_of_City.Language.Language;
using View_Spot_of_City.UIControls.Form;
using View_Spot_of_City.UIControls.Helper;

namespace View_Spot_of_City.UIControls.VisualizationControl
{
    /// <summary>
    /// HistogramOfType.xaml 的交互逻辑
    /// </summary>
    public partial class HistogramOfType : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        ObservableCollection<ViewSpot> _ViewSpotList = new ObservableCollection<ViewSpot>();

        List<string> ViewType = new List<string>();//统计的景点类型
        List<string> MostViewType = new List<string>();//统计的达到一定数量的景点

        List<int> TypeNumber = new List<int>();//统计的达到一定数量的景点的数量
        public SeriesCollection SeriesCollection { get; set; }
        public string[] ViewTypestr { get; set; }//统计的景点类型string[]
        public int[] number { get; set; }//各个景点的统计数量，对应于ViewType的顺序


        // int[] number;
        /// <summary>
        /// 景点列表
        /// </summary>
        public ObservableCollection<ViewSpot> ViewSpotList
        {
            get { return _ViewSpotList; }
            set
            {
                _ViewSpotList = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ViewSpotList"));
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public HistogramOfType()
        {
              InitializeComponent();
            GetViewSpotsDataAsync();
         
           
        }

        /// <summary>
        /// 获得数据之后的处理
        /// </summary>
        private void ProcessAfterGetData()
        {
            GetAllType();//得到所有景点类型
            Subtract();//删除重复类型
            SumNumber();//统计各个类型景点数目

            //显示柱状图
            SeriesCollection = new SeriesCollection
             {
                 new ColumnSeries
                 {
                     Title="景点类型统计",
                  Values=new ChartValues<int>(number)
                 }
             };

            DataContext = this;
        }

        /// <summary>
        /// 获取景点数据
        /// </summary>
        private async void GetViewSpotsDataAsync()
        {
            //API返回内容
            string jsonString = string.Empty;

            //景点数量
            int viewCount = -1;

            try
            {
                jsonString = (await WebServiceHelper.GetHttpResponseAsync(AppSettings["WEB_API_GET_VIEW_COUNT_BY_NAME"] + "?name=%", string.Empty, RestSharp.Method.GET)).Content;
                if (jsonString == "")
                    throw new Exception("");

                JObject jobject = (JObject)JsonConvert.DeserializeObject(jsonString);

                JToken jtoken = jobject["ViewCount"][0];

                viewCount = (int)jtoken["COUNT(*)"];

            }
            catch
            {
                MessageboxMaster.Show(LanguageDictionaryHelper.GetString("Server_Connect_Error"), LanguageDictionaryHelper.GetString("MessageBox_Error_Title"));
                return;
            }

            if (viewCount <= 0)
            {
                MessageboxMaster.Show(LanguageDictionaryHelper.GetString("SpotSearch_Null"), LanguageDictionaryHelper.GetString("MessageBox_Tip_Title"));
                return;
            }

            List<ViewSpot> viewSpotList = new List<ViewSpot>(viewCount);

            try
            {
                jsonString = (await WebServiceHelper.GetHttpResponseAsync(AppSettings["WEB_API_GET_VIEW_INFO_BY_NAME"] + "?name=%", string.Empty, RestSharp.Method.GET)).Content;
                if (jsonString == "")
                    throw new Exception("");

                JObject jobject = (JObject)JsonConvert.DeserializeObject(jsonString);

                string content_string = jobject["ViewInfo"].ToString();

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(content_string)))
                {
                    DataContractJsonSerializer deseralizer = new DataContractJsonSerializer(typeof(List<ViewSpot>));
                    viewSpotList = (List<ViewSpot>)deseralizer.ReadObject(ms);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageboxMaster.Show(LanguageDictionaryHelper.GetString("Server_Connect_Error"), LanguageDictionaryHelper.GetString("MessageBox_Error_Title"));
                return;
            }

            //检查数据
            for (int i = 0; i < viewSpotList.Count; i++)
            {
                viewSpotList[i].CheckData();
            }

            ViewSpotList = new ObservableCollection<ViewSpot>(viewSpotList);

            ProcessAfterGetData();
        }
        
        /// <summary>
        /// 获取所有的景点类型
        /// </summary>
        public void GetAllType()
        {
          
         
           for(int i=0;i<ViewSpotList.Count;i++)
            {    
                string[] str = ViewSpotList[i].type.Split(new char[] { ';', '|' }).ToArray();
             
                foreach(var name in str)
                {
                    if(!ViewType.Contains(name))
                    {
                        ViewType.Add(name);
                    }
                    
                }
               
            }

           
        }
        /// <summary>
        /// 减去不必要的重复类别
        /// </summary>
        public void Subtract()
        {
            for(int i=0;i<ViewType.Count;i++)
            {
                if(ViewType[i].Contains("公园")|| ViewType[i].Contains("风景名胜相关"))
                {
                    ViewType.Remove(ViewType[i]);
                    i--;
                }

            }
        }

       


        /// <summary>
        /// 统计各个景点数量
        /// </summary>
        public void SumNumber()
        {

            int list = ViewType.Count;
            int[] Count = new int[list];

           

            for (int i = 0; i < ViewSpotList.Count; i++)
            {

                string[] str = ViewSpotList[i].type.Split(new char[] { ';', '|' }).ToArray();

                foreach (var name in str)
                {
                     for (int j=0;j<ViewType.Count;j++)
                {
                    
                        if (ViewType[j].Contains(name))
                        {
                            Count[j] = Count[j] + 1;
                        }
                    

                }
               
                  
                }

            }
            //筛选数目>10的类型
            for (int i = 0; i < Count.Length; i++)
            {
                if (Count[i] > 9)
                {
                    MostViewType.Add(ViewType[i]);
                    TypeNumber.Add(Count[i]);

                }
            }
            TypeNumber.RemoveAt(0);
            MostViewType.RemoveAt(0);
          
            ViewTypestr = MostViewType.ToArray();
            number = TypeNumber.ToArray();
        }
       
        
       
    }
}
