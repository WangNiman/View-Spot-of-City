﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using static System.Configuration.ConfigurationManager;

using View_Spot_of_City.UIControls.Helper;
using Esri.ArcGISRuntime.Data;
using View_Spot_of_City.UIControls.Form;
using View_Spot_of_City.UIControls.UIcontrol;
using View_Spot_of_City.ClassModel;

namespace View_Spot_of_City.UIControls.ArcGISControl
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class MapView : UserControl
    {
        /// <summary>
        /// 地球半径
        /// </summary>
        private const double EARTH_RADIUS = 6378137;

        private GraphicsOverlay _PointOverlay = new GraphicsOverlay();
        /// <summary>
        /// 点图层
        /// </summary>
        public GraphicsOverlay PointOverlay
        {
            get { return _PointOverlay; }
            set
            {
                _PointOverlay = value;
            }
        }

        private GraphicsOverlay _LineOverlay = new GraphicsOverlay();
        /// <summary>
        /// 线图层
        /// </summary>
        public GraphicsOverlay LineOverlay
        {
            get { return _LineOverlay; }
            set
            {
                _LineOverlay = value;
            }
        }

        private GraphicsOverlay _PolygonOverlay = new GraphicsOverlay();
        /// <summary>
        /// 面图层
        /// </summary>
        public GraphicsOverlay PolygonOverlay
        {
            get { return _PolygonOverlay; }
            set
            {
                _PolygonOverlay = value;
            }
        }

        /// <summary>
        /// 几何要素图层
        /// </summary>
        public GraphicsOverlayCollection GraphicsOverlays
        {
            get { return mapView.GraphicsOverlays; }
        }

        /// <summary>
        /// 路径站点
        /// </summary>
        private List<MapPoint> routeStops = new List<MapPoint>();

        /// <summary>
        /// 多边形顶点
        /// </summary>
        private List<MapPoint> polygonVertexes = new List<MapPoint>();

        /// <summary>
        /// 图形对应属性
        /// </summary>
        private Dictionary<Graphic, object> _GraphicsAttributes = new Dictionary<Graphic, object>();

        /// <summary>
        /// 图形要素关联的属性
        /// </summary>
        public Dictionary<Graphic, object> GraphicsAttributes
        {
            get { return _GraphicsAttributes; }
            set { _GraphicsAttributes = value; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public MapView()
        {
            InitializeComponent();
            InitializeMapView();
        }

        /// <summary>
        /// 初始化地图
        /// </summary>
        private void InitializeMapView()
        {
            mapView.Map = new Map(new Uri(AppSettings["ARCGIS_BASEMAP"]));
            mapView.SetViewpointCenterAsync(new MapPoint(Convert.ToDouble(AppSettings["MAP_CENTER_LNG"]), Convert.ToDouble(AppSettings["MAP_CENTER_LAT"]), SpatialReferences.Wgs84), Convert.ToDouble(AppSettings["ARCGIS_MAP_ZOOM"]));
            mapView.GraphicsOverlays.Add(PolygonOverlay);
            mapView.GraphicsOverlays.Add(LineOverlay);
            mapView.GraphicsOverlays.Add(PointOverlay);
        }

        /// <summary>
        /// 重置地图状态
        /// </summary>
        public void ResetMapViewStatus()
        {
            //清除站点
            routeStops.Clear();
            polygonVertexes.Clear();

            //清除回调框
            mapView.DismissCallout();

            //清除图层
            PointOverlay.Graphics.Clear();
            LineOverlay.Graphics.Clear();
            PolygonOverlay.Graphics.Clear();
        }

        /// <summary>
        /// 地图点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void mapView_GeoViewTappedAsync(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            MapPoint mapLocation = e.Location;
            Esri.ArcGISRuntime.Geometry.Geometry myGeometry = GeometryEngine.Project(mapLocation, SpatialReferences.Wgs84);
            MapPoint projectedLocation = myGeometry as MapPoint;

            routeStops.Add(projectedLocation);

            double tolerance = 0;
            int maximumResults = 1;
            bool onlyReturnPopups = false;
            
            IdentifyGraphicsOverlayResult identifyResults = await mapView.IdentifyGraphicsOverlayAsync(
                PointOverlay,
                e.Position,
                tolerance,
                onlyReturnPopups,
                maximumResults);
            
            if (identifyResults.Graphics.Count > 0)
            {
                Graphic graphic = identifyResults.Graphics[0];
                MapPoint mapPoint = graphic.Geometry as MapPoint;
                PictureMarkerSymbol iconSymbol = graphic.Symbol as PictureMarkerSymbol;
                double iconHeight = iconSymbol.Height;
                ViewSpot viewInfo = GraphicsAttributes.ContainsKey(graphic) ? (GraphicsAttributes[graphic] as ViewSpot) : null;
                if(viewInfo != null)
                    mapView.ShowCalloutAt(mapPoint, new ViewSpotCallout(viewInfo), new Point(0, iconHeight));
            }
            else
            {
                mapView.DismissCallout();
            }
        }

        /// <summary>
        /// 地图双击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mapView_GeoViewDoubleTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if (routeStops == null || routeStops.Count <= 1)
                return;
            AddNavigateRouteToGraphicsOverlay(LineOverlay, routeStops, SimpleLineSymbolStyle.Dash, Colors.Blue, 3);
            //AddPolygonToGraphicsOverlay(PolygonOverlay, polygonVertexes, SimpleFillSymbolStyle.DiagonalCross, Colors.LawnGreen, new SimpleLineSymbol(SimpleLineSymbolStyle.Dash,Colors.DarkBlue, 2));
            e.Handled = true;
        }

        /// <summary>
        /// 放大按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 缩小按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 在指定图层上添加图标
        /// </summary>
        /// <param name="overlay">图层实例</param>
        /// <param name="point">位置</param>
        /// <param name="iconUri">图片URI</param>
        /// <param name="width">图片显示宽度</param>
        /// <param name="height">图片显示高度</param>
        /// <param name="offsetX">图片相对point的横向偏移</param>
        /// <param name="offsetY">图片相对point的纵向偏移</param>
        public void AddIconToGraphicsOverlay(GraphicsOverlay overlay, MapPoint point, Uri iconUri, double width, double height, double offsetX, double offsetY, object attributeData = null)
        {
            PictureMarkerSymbol pictureMarkerSymbol = new PictureMarkerSymbol(iconUri)
            {
                Width = width,
                Height = height,
                OffsetX = offsetX,
                OffsetY = offsetY
            };
            Graphic graphic = new Graphic(point, pictureMarkerSymbol);
            GraphicsAttributes.Add(graphic, attributeData);
            overlay.Graphics.Add(graphic);
        }

        /// <summary>
        /// 在指定图层上添加点要素
        /// </summary>
        /// <param name="overlay">图层实例</param>
        /// <param name="point">位置</param>
        /// <param name="pointSymbolStyle">点的呈现样式</param>
        /// <param name="pointColor">颜色</param>
        /// <param name="pointSize">大小</param>
        public void AddPointToGraphicsOverlay(GraphicsOverlay overlay, MapPoint point, SimpleMarkerSymbolStyle pointSymbolStyle, Color pointColor, double pointSize)
        {
            Graphic graphic = new Graphic(point, new SimpleMarkerSymbol(pointSymbolStyle, pointColor, pointSize));
            overlay.Graphics.Add(graphic);
        }

        /// <summary>
        /// 在指定图层上添加线要素
        /// </summary>
        /// <param name="overlay">图层实例</param>
        /// <param name="stops">站点</param>
        /// <param name="lineStyle">线的呈现样式</param>
        /// <param name="lineColor">颜色</param>
        /// <param name="lineWidth">线宽</param>
        private void AddRouteToGraphicsOverlay(GraphicsOverlay overlay, List<MapPoint> stops, SimpleLineSymbolStyle lineStyle, Color lineColor, double lineWidth)
        {
            Polyline routePolyline = new Polyline(stops);
            SimpleLineSymbol routeSymbol = new SimpleLineSymbol(lineStyle, lineColor, lineWidth);
            Graphic routeGraphic = new Graphic(routePolyline, routeSymbol);
            overlay.Graphics.Add(routeGraphic);
        }

        /// <summary>
        /// 在指定图层上添加面要素
        /// </summary>
        /// <param name="overlay">图层实例</param>
        /// <param name="vertexes">顶点</param>
        /// <param name="polygonStyle">面的呈现样式</param>
        /// <param name="fillColor">填充颜色</param>
        /// <param name="outline">边缘样式</param>
        private void AddPolygonToGraphicsOverlay(GraphicsOverlay overlay, List<MapPoint> vertexes, SimpleFillSymbolStyle polygonStyle, Color fillColor, SimpleLineSymbol outline)
        {
            Polygon polygon = new Polygon(vertexes);
            SimpleFillSymbol polygonSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.DiagonalCross, fillColor, outline);
            Graphic polygonGraphic = new Graphic(polygon, polygonSymbol);
            overlay.Graphics.Add(polygonGraphic);
        }

        /// <summary>
        /// 添加导航路线并返回路线长度
        /// </summary>
        /// <param name="overlay">图层实例</param>
        /// <param name="stops">站点</param>
        /// <param name="lineStyle">线的呈现样式</param>
        /// <param name="lineColor">颜色</param>
        /// <param name="lineWidth">线宽</param>
        /// <returns>路径长度，站点数小于2时返回-1</returns>
        public double AddNavigateRouteToGraphicsOverlay(GraphicsOverlay overlay, List<MapPoint> stops, SimpleLineSymbolStyle lineStyle, Color lineColor, double lineWidth)
        {
            if (stops.Count <= 1)
                return -1;
            AddRouteToGraphicsOverlay(overlay, stops, lineStyle, lineColor, lineWidth);
            AddIconToGraphicsOverlay(overlay, stops[0], IconDictionaryHelper.IconDictionary[IconDictionaryHelper.Icons.start], 16, 24, 0.0, 9.5);
            AddIconToGraphicsOverlay(overlay, stops[stops.Count - 1], IconDictionaryHelper.IconDictionary[IconDictionaryHelper.Icons.end], 16, 24, 0.0, 9.5);
            double distance = 0;
            for (int i=1;i<stops.Count - 1;i++)
            {
                distance += GetDistance(stops[i - 1].Y, stops[i - 1].X, stops[i].Y, stops[i].X);
            }
            return distance;
        }
        
        /// <summary>
        /// 右键按下响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mapView_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ResetMapViewStatus();
        }

        /// <summary>
        /// 地图状态改变响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDrawStatusChanged(object sender, DrawStatusChangedEventArgs e)
        {
            Dispatcher.Invoke(delegate ()
            {
                if (e.Status == DrawStatus.InProgress)
                {

                }
                else
                {

                }
            });
        }

        /// <summary>
        /// 定位按钮响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LocaltionButton_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 显示回调框
        /// </summary>
        public void ShowCallout(MapPoint mapPoint, UIElement uiElement, Point offset)
        {
            mapView.ShowCalloutAt(mapPoint, uiElement, offset);
        }

        /// <summary>
        /// 设置地图视图
        /// </summary>
        /// <param name="point"></param>
        /// <param name="scale"></param>
        public void SetScaleAndLoction(MapPoint point, double scale)
        {
            mapView.SetViewpointCenterAsync(point ,scale);
        }

        /// <summary>
        /// 清除指定图层的要素
        /// </summary>
        /// <param name="overlay"></param>
        public void ClearFeatureOnGraphicsOverlay(GraphicsOverlay overlay)
        {
            foreach(Graphic g in overlay.Graphics)
            {
                if (GraphicsAttributes.ContainsKey(g))
                    GraphicsAttributes.Remove(g);
            }
            overlay.Graphics.Clear();
        }

        #region Calculate
        /// <summary>
        /// 经纬度转化成弧度
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        private double Rad(double d)
        {
            return d * Math.PI / 180d;
        }

        /// <summary>
        /// 计算两点位置的距离，返回两点的距离，单位 米
        /// 该公式为GOOGLE提供，误差小于0.2米
        /// </summary>
        /// <param name="lat1">第一点纬度</param>
        /// <param name="lng1">第一点经度</param>
        /// <param name="lat2">第二点纬度</param>
        /// <param name="lng2">第二点经度</param>
        /// <returns></returns>
        private double GetDistance(double lat1, double lng1, double lat2, double lng2)
        {
            double radLat1 = Rad(lat1);
            double radLng1 = Rad(lng1);
            double radLat2 = Rad(lat2);
            double radLng2 = Rad(lng2);
            double a = radLat1 - radLat2;
            double b = radLng1 - radLng2;
            double result = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) + Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Pow(Math.Sin(b / 2), 2))) * EARTH_RADIUS;
            return result;
        }

        /// <summary>
        /// Calculate area of a polygon by it's latitute and longitude
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public double CalculatePolygonArea(List<MapPoint> coordinates)
        {
            List<MapPoint> xyList = new List<MapPoint>();
            for (int i = 0; i < coordinates.Count; i++)
            {
                xyList.Add(new MapPoint(coordinates[i].X, coordinates[i].Y));
            }

            double area = 0;
            if (xyList.Count > 2)
            {
                for (var i = 0; i < xyList.Count - 1; i++)
                {
                    MapPoint p1, p2;
                    p1 = xyList[i];
                    p2 = xyList[i + 1];
                    area += Rad(p2.X - p1.X) * (2 + Math.Sin(Rad(p1.Y)) + Math.Sin(Rad(p2.Y)));
                }
                area = area * Rad(EARTH_RADIUS) * Rad(EARTH_RADIUS) / 2.0;
            }

            return Math.Abs(area);
        }

        /// <summary>
        /// Get the center point of points
        /// </summary>
        /// <param name="points">points</param>
        /// <returns></returns>
        private MapPoint GetCenterLatLng(List<MapPoint> points)
        {
            double sumLat = 0;
            double sumLng = 0;
            foreach (MapPoint point in points)
            {
                sumLat += point.Y;
                sumLng += point.X;
            }
            return new MapPoint(sumLng / points.Count, sumLat / points.Count);
        }
        #endregion
    }
}
