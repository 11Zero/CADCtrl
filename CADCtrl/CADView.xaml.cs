using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SharpGL;
using SharpGL.SceneGraph;
using System.Collections.ObjectModel;
using log4net;

namespace CADCtrl
{
    /// <summary>
    /// CADView.xaml 的交互逻辑
    /// </summary>
    public partial class CADView : UserControl
    {
        public static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Point m_center_offset;
        private double m_distance;
        public double m_scale;
        public double m_rotate_x_degree;
        public double m_rotate_y_degree;
        public double m_rotate_z_degree;

        private OpenGL m_openGLCtrl;
        private double m_pixaxis;//像素对应平移的倍数
        private double m_gridstep;//栅格间距，像素为单位
        private int LineNumber;
        private int RectNumber;
        private int PointNumber;
        private int CubeNumber;
        private CADRect m_border;
        private double m_wheel_multi;//滚轮滚动一次放大的倍数
        Dictionary<int, CADLine> AllLines = new Dictionary<int, CADLine>();
        Dictionary<int, List<int>> AllPointsInLines = new Dictionary<int, List<int>>();
        Dictionary<int, CADRect> AllRects = new Dictionary<int, CADRect>();
        Dictionary<int, List<int>> AllPointsInRects = new Dictionary<int, List<int>>();
        Dictionary<int, CADPoint> AllPoints = new Dictionary<int, CADPoint>();
        Dictionary<int, CADCube> AllCubes = new Dictionary<int, CADCube>();
        Dictionary<int, CADCube> SelCubes = new Dictionary<int, CADCube>();
        Dictionary<int, CADPoint> SelPoints = new Dictionary<int, CADPoint>();
        Dictionary<int, CADLine> SelLines = new Dictionary<int, CADLine>();
        Dictionary<int, CADRect> SelRects = new Dictionary<int, CADRect>();
        Dictionary<int, CADRGB> AllColors = new Dictionary<int, CADRGB>();
        Dictionary<int, int> AllLinesColor = new Dictionary<int, int>();
        Dictionary<int, int> AllRectsColor = new Dictionary<int, int>();
        Dictionary<int, int> AllCubesColor = new Dictionary<int, int>();
        Point MidMouseDownStart = new Point(0, 0);
        Point MidMouseDownEnd = new Point(0, 0);
        Point m_currentpos = new Point(0, 0);
        CADPoint m_cur_sel_point = new CADPoint(0, 0);
        public ObservableCollection<CADRect> m_sel_rect_list = new ObservableCollection<CADRect>();
        public ObservableCollection<CADPoint> m_sel_point_list = new ObservableCollection<CADPoint>();
        //public ObservableCollection<CADPoint> m_sel_rebar_list = new ObservableCollection<CADPoint>();
        public bool key_down_esc;
        public bool key_down_copy;
        public bool key_down_move;
        public bool key_down_del;
        public bool key_down_shift;

        CADPoint zero = null;
        CADPoint ax_p = null;
        CADPoint ay_p = null;
        CADPoint az_p = null;
        CADPoint ax_n = null;
        CADPoint ay_n = null;
        CADPoint az_n = null;



        private bool cross_mouse_view = true;
        private CADLine tempLine = new CADLine();
        private CADPoint tempPoint = new CADPoint();
        private CADPoint m_curaxispos = new CADPoint();
        public bool b_draw_line = false;
        private int clicked_count = 0;

        public int isRebar = 0;
        public CADView()
        {
            InitializeComponent();
            m_openGLCtrl = openGLCtrl.OpenGL;
            log4net.Config.XmlConfigurator.Configure();
            log.Info("dll start up");
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            m_openGLCtrl = openGLCtrl.OpenGL;
            m_center_offset = new Point(0, 0);
            m_distance = -10;
            m_pixaxis = 0.1208;//与移动速度成反比
            m_scale = 0.00002;
            if (isRebar == 1)
                m_scale = 0.005;
            m_border = new CADRect(0, 0, 0, 0);
            m_wheel_multi = 0.2;
            m_gridstep = 50;
            LineNumber = 0;
            RectNumber = 0;
            PointNumber = 1;
            CubeNumber = 0;
            AllPoints[PointNumber] = new CADPoint();
            AllPoints[PointNumber].m_id = PointNumber;
            m_pixaxis = m_pixaxis * this.Height;//(this.Width < this.Height ? this.Width : this.Height);
            AllColors.Add(1, new CADRGB(1, 1, 1));
            AllColors.Add(2, new CADRGB(1, 0, 0));
            AllColors.Add(3, new CADRGB(0, 1, 0));
            AllColors.Add(4, new CADRGB(0, 0, 1));

            key_down_esc = false;
            key_down_copy = false;
            key_down_move = false;
            key_down_del = false;
            key_down_shift = false;

            zero = new CADPoint(0, 0, 0);
            ax_p = new CADPoint(1, 0, 0);
            ay_p = new CADPoint(0, 1, 0);
            az_p = new CADPoint(0, 0, 1);
            ax_n = new CADPoint(-1, 0, 0);
            ay_n = new CADPoint(0, -1, 0);
            az_n = new CADPoint(0, 0, -1);

            m_rotate_x_degree = 0;
            m_rotate_y_degree = 0;
            m_rotate_z_degree = 0;
        }



        private void OpenGLControl_OpenGLDraw(object sender, OpenGLEventArgs args)
        {
            m_openGLCtrl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
            m_openGLCtrl.MatrixMode(OpenGL.GL_MODELVIEW);
            m_openGLCtrl.LoadIdentity();
            m_openGLCtrl.LookAt(0, 0, 0, 0, 0, -1, 0, 1, 0);
            this.RedrawAll();

        }

        private void OpenGLControl_OpenGLInitialized(object sender, OpenGLEventArgs args)
        {
            ///定义材质属性值
            //float[] no_mat = new float[] { 0.0f, 0.0f, 0.0f, 1.0f };        // 无材质颜色
            //float[] mat_ambient = new float[] { 0.7f, 0.7f, 0.7f, 1.0f };   // 环境颜色
            //float[] mat_ambient_color = new float[] { 0.8f, 0.6f, 0.2f, 1.0f };
            //float[] mat_diffuse = new float[] { 0.2f, 0.5f, 0.8f, 1.0f };   // 散射颜色
            //float[] mat_specular = new float[] { 1.0f, 1.0f, 1.0f, 1.0f };  // 镜面反射颜色
            //float[] no_shininess = new float[] { 0.0f };                    // 镜面反射指数为0
            //float[] low_shininess = new float[] { 5.0f };                   // 镜面反射指数为5.0
            //float[] high_shininess = new float[] { 100.0f };                // 镜面反射指数为100.0
            //float[] mat_emission = new float[] { 0.3f, 0.2f, 0.3f, 0.0f };  // 发射光颜色

            //m_openGLCtrl.PushMatrix();
            //m_openGLCtrl.Translate(1f, 1.5f, -7.0f);
            //m_openGLCtrl.Material(OpenGL.GL_FRONT, OpenGL.GL_AMBIENT, no_mat);
            //m_openGLCtrl.Material(OpenGL.GL_FRONT, OpenGL.GL_DIFFUSE, mat_diffuse);
            //m_openGLCtrl.Material(OpenGL.GL_FRONT, OpenGL.GL_SPECULAR, mat_specular);
            //m_openGLCtrl.Material(OpenGL.GL_FRONT, OpenGL.GL_SHININESS, low_shininess);
            //m_openGLCtrl.Material(OpenGL.GL_FRONT, OpenGL.GL_EMISSION, no_mat);
            //m_openGLCtrl.PopMatrix();
            //m_openGLCtrl.Enable(OpenGL.GL_DEPTH_TEST);
            
        }

        private void OpenGLControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed && key_down_shift == false)
            {
                if (e.ClickCount == 2)
                {
                    this.Cursor = Cursors.SizeAll;
                    if ((m_border.m_ye - m_border.m_ys) > (m_border.m_xe - m_border.m_xs))
                        m_scale = this.Height / 50 / (m_border.m_ye - m_border.m_ys);
                    else
                        m_scale = this.Width / 50 / (m_border.m_xe - m_border.m_xs);
                    if (m_scale > 1000000)
                        m_scale = 8 / (m_border.m_xe - m_border.m_xs);
                    if (m_scale > 1000000)
                        m_scale = 8 / (m_border.m_ye - m_border.m_ys);
                    if (m_scale > 1000000)
                    {
                        if (isRebar == 0)
                            m_scale = 0.00001;
                        else
                            m_scale = 0.005;
                    }
                    m_center_offset.X = -(m_border.m_xe + m_border.m_xs) / 2 * m_pixaxis * m_scale;
                    m_center_offset.Y = -(m_border.m_ye + m_border.m_ys) / 2 * m_pixaxis * m_scale;
                }
                else
                {
                    if (e.ClickCount == 1)
                    {
                        MidMouseDownStart = e.GetPosition(e.Source as FrameworkElement);
                        this.Cursor = Cursors.SizeAll;
                        this.cross_mouse_view = false;
                    }
                    else
                        this.cross_mouse_view = true;
                }
            }

            if (e.MiddleButton == MouseButtonState.Pressed && key_down_shift == true)
            {
                MidMouseDownStart = e.GetPosition(e.Source as FrameworkElement);
                this.Cursor = Cursors.ScrollNS;
            }
                if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (b_draw_line && clicked_count == 1)
                {
                    this.UserEndLine();
                    clicked_count = 0;
                }
                if (b_draw_line && clicked_count == 0)
                {
                    this.UserStartLine();
                    clicked_count++;
                }
                Point mousepos = new Point(m_currentpos.X / m_scale / m_pixaxis, m_currentpos.Y / m_scale / m_pixaxis);
                int id_sel_point = -1;
                double sel_dis_point = 1 / m_scale;
                double real_dis = 0.1 / m_scale;
                if (isRebar == 1)
                    real_dis = 4 * real_dis;
                if (AllPoints.Count > 0)
                {
                    foreach (int id in this.AllPoints.Keys)
                    {
                        double dis = this.GetDistance(mousepos, AllPoints[id]);
                        if (dis < real_dis && dis < sel_dis_point)
                        {
                            id_sel_point = id;
                            sel_dis_point = dis;
                        }
                    }
                }

                if (id_sel_point > 0)
                {
                    CADPoint temp_cur_point = null;

                    if (AllPoints.ContainsKey(id_sel_point))
                    {

                        if (key_down_move || key_down_copy)
                        {
                            Vector move = new Vector(AllPoints[id_sel_point].m_x - m_cur_sel_point.m_x, AllPoints[id_sel_point].m_y - m_cur_sel_point.m_y);
                            if (SelRects.Count > 0)
                            {
                                if (key_down_move)
                                {
                                    foreach (int value in SelRects.Keys)
                                    {
                                        if (AllPointsInRects[value].Contains(id_sel_point))
                                        {
                                            temp_cur_point = AllPoints[id_sel_point].Copy();
                                        }
                                        SelRects[value].m_xs = AllRects[value].m_xs + (float)move.X;
                                        SelRects[value].m_ys = AllRects[value].m_ys + (float)move.Y;
                                        SelRects[value].m_xe = AllRects[value].m_xe + (float)move.X;
                                        SelRects[value].m_ye = AllRects[value].m_ye + (float)move.Y;
                                        this.AddRect(SelRects[value]);
                                    }
                                    key_down_move = false;
                                }
                                if (key_down_copy)
                                {
                                    CADRect new_rect = new CADRect();
                                    int[] keys = SelRects.Keys.ToArray();
                                    SelRects.Clear();
                                    foreach (int value in keys)
                                    {
                                        new_rect = AllRects[value].Copy();
                                        new_rect.m_xs = AllRects[value].m_xs + (float)move.X;
                                        new_rect.m_ys = AllRects[value].m_ys + (float)move.Y;
                                        new_rect.m_xe = AllRects[value].m_xe + (float)move.X;
                                        new_rect.m_ye = AllRects[value].m_ye + (float)move.Y;
                                        new_rect.UpdataWH();
                                        RectNumber++;
                                        new_rect.m_id = RectNumber;
                                        SelRects.Add(RectNumber, new_rect);
                                        this.AddRect(new_rect);
                                    }
                                    key_down_copy = false;
                                }
                            }
                        }

                        if (temp_cur_point != null)
                        {
                            m_cur_sel_point = temp_cur_point;
                            this.AddPoint(temp_cur_point);
                        }
                        else
                            m_cur_sel_point = AllPoints[id_sel_point].Copy();
                        key_down_move = false;
                    }

                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        if (isRebar == 1)
                            this.SelPoints.Clear();
                        this.SelPoint(id_sel_point);
                        if (temp_cur_point != null)
                        {
                            this.AllPoints.Remove(id_sel_point);
                        }
                        return;
                    }
                    else
                    {
                        if (!SelPoints.ContainsKey(id_sel_point))
                        {
                            SelPoints.Clear();
                            m_sel_point_list.Clear();
                        }
                        this.SelPoint(id_sel_point);
                        if (temp_cur_point != null)
                        {
                            this.AllPoints.Remove(id_sel_point);
                        }
                        return;
                    }
                }
                else
                {

                    if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                    {
                        m_cur_sel_point = new CADPoint();
                        this.SelPoints.Clear();
                        m_sel_point_list.Clear();
                    }
                }


                int id_sel_line = -1;
                double sel_dis_line = 1 / m_scale;
                if (AllLines.Count > 0)
                {
                    foreach (int id in this.AllLines.Keys)
                    {
                        double dis = this.GetDistance(mousepos, AllLines[id]);
                        if (dis < 0)
                            continue;
                        if (dis < 0.06 / m_scale && dis < sel_dis_line)
                        {
                            id_sel_line = id;
                            sel_dis_line = dis;
                        }
                    }
                }



                if (id_sel_line > 0)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        this.SelLine(id_sel_line);
                    else
                    {
                        if (!SelLines.ContainsKey(id_sel_line))
                            SelLines.Clear();
                        this.SelLine(id_sel_line);
                    }
                }
                else
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                        this.SelLines.Clear();
                }

                int id_sel_rect = -1;
                double sel_dis_rect = 1 / m_scale;
                if (AllRects.Count > 0)
                {
                    foreach (int id in this.AllRects.Keys)
                    {
                        double dis = this.GetDistance(mousepos, AllRects[id]);
                        if (dis < 0)
                            continue;
                        if (dis < 0.06 / m_scale && dis < sel_dis_rect)
                        {
                            id_sel_rect = id;
                            sel_dis_rect = dis;
                        }
                    }
                }



                if (id_sel_rect > 0)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        this.SelRect(id_sel_rect);
                    else
                    {
                        if (!SelRects.ContainsKey(id_sel_rect))
                        {
                            SelRects.Clear();
                            m_sel_rect_list.Clear();
                        }
                        this.SelRect(id_sel_rect);
                    }
                }
                else
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                    {
                        this.SelRects.Clear();
                        m_sel_rect_list.Clear();
                    }
                }
            }
        }

        private void OpenGLControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released)
            {
                this.cross_mouse_view = true;
                //this.Cursor = Cursors.Arrow;
            }
        }

        private void OpenGLControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released)
                this.Cursor = Cursors.None;
            if (e.MiddleButton == MouseButtonState.Pressed && key_down_shift == false)
            {
                MidMouseDownEnd = e.GetPosition(e.Source as FrameworkElement);
                Vector vDistance = MidMouseDownEnd - MidMouseDownStart;
                m_center_offset.X = m_center_offset.X + vDistance.X;
                m_center_offset.Y = m_center_offset.Y - vDistance.Y;
                MidMouseDownStart = MidMouseDownEnd;
            }

            if (e.MiddleButton == MouseButtonState.Pressed && key_down_shift == true)
            {
                MidMouseDownEnd = e.GetPosition(e.Source as FrameworkElement);
                
                Vector vDistance = MidMouseDownEnd - MidMouseDownStart;
                m_rotate_x_degree = m_rotate_x_degree + vDistance.X;
                m_rotate_y_degree = m_rotate_y_degree + vDistance.Y;
                if (m_rotate_x_degree >= 360)
                    m_rotate_x_degree = m_rotate_x_degree - 360;
                if (m_rotate_y_degree >= 360)
                    m_rotate_y_degree = m_rotate_x_degree - 360;
                if (m_rotate_x_degree <= -360)
                    m_rotate_x_degree = m_rotate_x_degree + 360;
                if (m_rotate_y_degree <= -360)
                    m_rotate_y_degree = m_rotate_x_degree + 360;
                //log.Info(string.Format("vDistance = [{0},{1}]", vDistance.X, vDistance.Y));
                //this.DrawText(string.Format("shift down:%d,%d", vDistance.X, vDistance.Y), new Point(0, 15));
                //m_center_offset.X = m_center_offset.X + vDistance.X;
                //m_center_offset.Y = m_center_offset.Y - vDistance.Y;
                MidMouseDownStart = MidMouseDownEnd;
                
            }

            if (key_down_move || key_down_copy)
            {
                if (this.SelRects.Count > 0)
                {
                    Vector move = new Vector(m_currentpos.X / m_scale / m_pixaxis - m_cur_sel_point.m_x, m_currentpos.Y / m_scale / m_pixaxis - m_cur_sel_point.m_y);
                    if (SelRects.Count > 0)
                    {
                        foreach (int value in SelRects.Keys)
                        {
                            SelRects[value].m_xs = AllRects[value].m_xs + (float)move.X;
                            SelRects[value].m_ys = AllRects[value].m_ys + (float)move.Y;
                            SelRects[value].m_xe = AllRects[value].m_xe + (float)move.X;
                            SelRects[value].m_ye = AllRects[value].m_ye + (float)move.Y;
                        }
                    }
                }
            }
            m_currentpos.Y = this.Height / 2 - e.GetPosition(e.Source as FrameworkElement).Y - m_center_offset.Y;
            m_currentpos.X = -this.Width / 2 + e.GetPosition(e.Source as FrameworkElement).X - m_center_offset.X;

            m_curaxispos.m_x = (float)(m_currentpos.X / m_scale / m_pixaxis);
            m_curaxispos.m_y = (float)(m_currentpos.Y / m_scale / m_pixaxis);

            if (b_draw_line && clicked_count == 1)
            {
                tempLine.m_xe = m_curaxispos.m_x;
                tempLine.m_ye = m_curaxispos.m_y;
            }

        }

        private void OpenGLControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Vector vDistance = new Vector();
            if (e.Delta > 0)
            {
                vDistance.X = (m_center_offset.X - e.GetPosition(e.Source as FrameworkElement).X + this.Width / 2) * -m_wheel_multi;
                vDistance.Y = (m_center_offset.Y + e.GetPosition(e.Source as FrameworkElement).Y - this.Height / 2) * -m_wheel_multi;
                m_scale -= m_scale * m_wheel_multi;
                m_gridstep -= 1;
            }
            else
            {
                vDistance.X = (m_center_offset.X - e.GetPosition(e.Source as FrameworkElement).X + this.Width / 2) * m_wheel_multi;
                vDistance.Y = (m_center_offset.Y + e.GetPosition(e.Source as FrameworkElement).Y - this.Height / 2) * m_wheel_multi;
                m_scale += m_scale * m_wheel_multi;
                m_gridstep += 1;
            }

            if (m_gridstep > 70)
                m_gridstep = 50;
            if (m_gridstep < 30)
                m_gridstep = 50;
            if (m_scale <= 0.0000001)
                m_scale = 0.0000001;
            else
            {
                m_center_offset.X = m_center_offset.X + vDistance.X;
                m_center_offset.Y = m_center_offset.Y + vDistance.Y;
            }

            m_currentpos.Y = this.Height / 2 - e.GetPosition(e.Source as FrameworkElement).Y - m_center_offset.Y;
            m_currentpos.X = -this.Width / 2 + e.GetPosition(e.Source as FrameworkElement).X - m_center_offset.X;


            m_curaxispos.m_x = (float)(m_currentpos.X / m_scale / m_pixaxis);
            m_curaxispos.m_y = (float)(m_currentpos.Y / m_scale / m_pixaxis);
        }




        private void RedrawAll()
        {
            //m_openGLCtrl.Rotate(90, 0.0f, 1.0f, 0.0f);
            m_openGLCtrl.Rotate(m_rotate_x_degree, 0.0f, 1.0f, 0.0f);
            m_openGLCtrl.Rotate(m_rotate_y_degree, 1.0f, 0.0f, 0.0f);
            //m_openGLCtrl.LookAt(0, 0, 20000* m_scale+100, 0, 0, 0*m_scale, 0,1,0);
            //log.Info(string.Format("scale = {0}",m_scale));
            m_openGLCtrl.Translate(m_center_offset.X / m_pixaxis, m_center_offset.Y / m_pixaxis, m_distance);

            m_openGLCtrl.Scale(m_scale, m_scale, m_scale);



            //float[] light_position = new float[] { -900, 0, 0,0};
            //float[] fLightDiffuse = new float[4] { 0.8f, 0.8f, 0.8f, 0.8f};// 漫射光参数
            //float[] ambient = new float[] { 0.4f, 0.4f, 0.4f, 1.0f };
            //float[] fLightSpecular = new float[4] { 1f, 1f, 1f, 1f }; //镜面反射

            //m_openGLCtrl.Enable(OpenGL.GL_LIGHTING);//开启光照 
            //m_openGLCtrl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_DIFFUSE, fLightDiffuse);
            //m_openGLCtrl.Enable(OpenGL.GL_LIGHT1);

            //m_openGLCtrl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_AMBIENT, ambient);
            //m_openGLCtrl.Enable(OpenGL.GL_LIGHT0);

            //m_openGLCtrl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_POSITION, light_position);

            //m_openGLCtrl.Light(OpenGL.GL_LIGHT2, OpenGL.GL_SPECULAR, fLightSpecular);//漫射光源  
            //m_openGLCtrl.Enable(OpenGL.GL_LIGHT2);





            //float[] fLightPosition = new float[4] { 0.0f, 3.0f, 2.0f, 0.0f }; //5f, 8f, -8f, 1f };// 光源位置 
            //float[] fLightAmbient = new float[4] { 1f, 1f, 1f, 1f };// 环境光参数 
            //float[] fLightDiffuse = new float[4] { 1f, 1f, 1f, 1f };// 漫射光参数
            //float[] fLightSpecular = new float[4] { 1f, 1f, 1f, 1f }; //镜面反射

            //m_openGLCtrl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_AMBIENT, fLightAmbient);//环境光源 
            //m_openGLCtrl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_DIFFUSE, fLightDiffuse);//漫射光源 
            //m_openGLCtrl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_POSITION, fLightPosition);//光源位置 
            ////m_openGLCtrl.ClearColor(0.0f, 0.2f, 0.2f, 0.0f);
            ////m_openGLCtrl.ClearDepth(1f);
            //m_openGLCtrl.DepthFunc(OpenGL.GL_LEQUAL);
            //m_openGLCtrl.Enable(OpenGL.GL_DEPTH_TEST);
            //m_openGLCtrl.ShadeModel(OpenGL.GL_SMOOTH);             
            //m_openGLCtrl.Enable(OpenGL.GL_LIGHTING);//开启光照 
            //m_openGLCtrl.Enable(OpenGL.GL_LIGHT0);
            //m_openGLCtrl.Enable(OpenGL.GL_NORMALIZE);



            this.DrawGrids();

            if (cross_mouse_view)
                this.DrawMouseLine(m_scale);

            if (b_draw_line)
                this.DrawLine(tempLine);

            if (SelLines.Count > 0)
            {
                foreach (int value in this.SelLines.Keys)
                {
                    this.DrawSelLine(value);
                }
            }

            if (SelRects.Count > 0)
            {
                foreach (int value in this.SelRects.Keys)
                {
                    this.DrawSelRect(value);
                }
            }


            if (AllLines.Count > 0)
            {
                foreach (int key in this.AllLines.Keys)
                {
                    this.DrawLine(AllLines[key], AllColors[AllLinesColor[key]]);
                }
            }

            if (AllRects.Count > 0)
            {
                foreach (int key in this.AllRects.Keys)
                {
                    this.DrawRect(AllRects[key], AllColors[AllRectsColor[key]]);
                }
            }

            if (AllCubes.Count > 0)
            {
                foreach (int key in this.AllCubes.Keys)
                {
                    this.DrawCube(AllCubes[key], AllColors[AllCubesColor[key]]);
                }
            }

            if (AllPoints.Count > 0)
            {
                foreach (int key in this.AllPoints.Keys)
                {
                    this.DrawPoint(AllPoints[key]);
                }
            }

            if (SelPoints.Count > 0)
            {
                foreach (int value in this.SelPoints.Keys)
                {
                    this.DrawSelPoint(value);
                }
            }

            double real_scale = m_scale;
            if (isRebar == 1)
                real_scale = m_scale / 3;

            this.DrawCoord(m_scale);

            //this.DrawLine(new CADLine(0, 0, 0.5 / real_scale, 0));
            //this.DrawLine(new CADLine(0.5 / real_scale, 0, 0.4 / real_scale, 0.05 / real_scale));
            //this.DrawLine(new CADLine(0.5 / real_scale, 0, 0.4 / real_scale, -0.05 / real_scale));
            //this.DrawLine(new CADLine(0.4 / real_scale, 0.05 / real_scale, 0.4 / real_scale, -0.05 / real_scale));

            //this.DrawLine(new CADLine(0.15 / real_scale, -0.1 / real_scale, 0.35 / real_scale, -0.3 / real_scale));
            //this.DrawLine(new CADLine(0.15 / real_scale, -0.3 / real_scale, 0.35 / real_scale, -0.1 / real_scale));

            //this.DrawLine(new CADLine(0, 0, 0, 0.5 / real_scale));
            //this.DrawLine(new CADLine(0, 0.5 / real_scale, 0.05 / real_scale, 0.4 / real_scale));
            //this.DrawLine(new CADLine(0, 0.5 / real_scale, -0.05 / real_scale, 0.4 / real_scale));
            //this.DrawLine(new CADLine(0.05 / real_scale, 0.4 / real_scale, -0.05 / real_scale, 0.4 / real_scale));

            //this.DrawLine(new CADLine(-0.2 / real_scale, 0.25 / real_scale, -0.1 / real_scale, 0.35 / real_scale));
            //this.DrawLine(new CADLine(-0.2 / real_scale, 0.25 / real_scale, -0.3 / real_scale, 0.35 / real_scale));
            //this.DrawLine(new CADLine(-0.2 / real_scale, 0.25 / real_scale, -0.2 / real_scale, 0.1 / real_scale));

            string pos_str = string.Format("Point:[{2:0.00},{3:0.00}]   Position:[{0:0.00},{1:0.00}]", m_currentpos.X / m_scale / m_pixaxis, m_currentpos.Y / m_scale / m_pixaxis, m_cur_sel_point.m_x, m_cur_sel_point.m_y);
            if (isRebar == 1)
                pos_str = string.Format("[XY]:[{0:0.00},{1:0.00}]", m_cur_sel_point.m_x, m_cur_sel_point.m_y);
            this.DrawText(pos_str, new Point(0, 5));
        }

        private void AddLine(CADLine line, int color_id = 0)
        {
            if (line == null)
                return;
            CADLine this_line = line.Copy();
            if (this_line.m_id == 0)
            {
                LineNumber++;
                this_line.m_id = LineNumber;
            }
            int this_line_id = this_line.m_id;
            if (!AllColors.ContainsKey(color_id))
                color_id = 1;
            if (this.AllLines.ContainsKey(this_line_id))
            {
                AllLines[this_line_id] = this_line.Copy();
                AllLinesColor[this_line_id] = color_id;
                CADPoint point = new CADPoint(this_line.m_xs, this_line.m_ys);
                List<int> pointsItems = AllPointsInLines[this_line_id];
                for (int i = 0; i < pointsItems.Count; i++)
                {
                    if (AllPoints.ContainsKey(pointsItems[i]))
                        AllPoints.Remove(pointsItems[i]);
                }
                pointsItems.Clear();
                this.AddPoint(point);
                pointsItems.Add(PointNumber);
                point.m_x = this_line.m_xe;
                point.m_y = this_line.m_ye;
                this.AddPoint(point);
                pointsItems.Add(PointNumber);
                AllPointsInLines[this_line_id] = pointsItems;
            }
            else
            {
                AllLines.Add(this_line_id, this_line.Copy());
                AllLinesColor.Add(this_line_id, color_id);
                CADPoint point = new CADPoint(this_line.m_xs, this_line.m_ys);
                List<int> pointsItems = new List<int>();
                this.AddPoint(point);
                pointsItems.Add(PointNumber);
                point.m_x = this_line.m_xe;
                point.m_y = this_line.m_ye;
                this.AddPoint(point);
                pointsItems.Add(PointNumber);
                AllPointsInLines.Add(this_line_id, pointsItems);
                if (this_line.m_xs > this_line.m_xe)
                {
                    m_border.m_xs = m_border.m_xs < this_line.m_xe ? m_border.m_xs : this_line.m_xe;
                    m_border.m_xe = m_border.m_xe > this_line.m_xs ? m_border.m_xe : this_line.m_xs;
                }
                else
                {
                    m_border.m_xs = m_border.m_xs < this_line.m_xs ? m_border.m_xs : this_line.m_xs;
                    m_border.m_xe = m_border.m_xe > this_line.m_xe ? m_border.m_xe : this_line.m_xe;
                }
                if (this_line.m_ys > this_line.m_ye)
                {
                    m_border.m_ys = m_border.m_ys < this_line.m_ye ? m_border.m_ys : this_line.m_ye;
                    m_border.m_ye = m_border.m_ye > this_line.m_ys ? m_border.m_ye : this_line.m_ys;
                }
                else
                {
                    m_border.m_ys = m_border.m_ys < this_line.m_ys ? m_border.m_ys : this_line.m_ys;
                    m_border.m_ye = m_border.m_ye > this_line.m_ye ? m_border.m_ye : this_line.m_ye;
                }
            }
            if (AllLines.Count > 0)
            {
                foreach (int value in AllLines.Keys)
                {
                    CADPoint point = this.GetCrossPoint(this_line, AllLines[value]);
                    if (point != null)
                    {
                        point.m_style = 1;
                        this.AddPoint(point);
                        AllPointsInLines[this_line_id].Add(PointNumber);
                        AllPointsInLines[value].Add(PointNumber);
                    }
                }
            }
            if (AllRects.Count > 0)
            {
                foreach (int value in AllRects.Keys)
                {
                    CADLine temp_this_line = new CADLine();
                    temp_this_line.m_xs = AllRects[value].m_xs;
                    temp_this_line.m_ys = AllRects[value].m_ys;

                    temp_this_line.m_xe = AllRects[value].m_xs;
                    temp_this_line.m_ye = AllRects[value].m_ye;
                    CADPoint point = this.GetCrossPoint(this_line, temp_this_line);
                    if (point != null)
                    {
                        point.m_style = 1;
                        this.AddPoint(point);
                        AllPointsInLines[this_line_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }
                    temp_this_line.m_xs = AllRects[value].m_xe;
                    temp_this_line.m_ys = AllRects[value].m_ye;
                    point = this.GetCrossPoint(this_line, temp_this_line);
                    if (point != null)
                    {
                        point.m_style = 1;
                        this.AddPoint(point);
                        AllPointsInLines[this_line_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }
                    temp_this_line.m_xe = AllRects[value].m_xe;
                    temp_this_line.m_ye = AllRects[value].m_ys;
                    point = this.GetCrossPoint(this_line, temp_this_line);
                    if (point != null)
                    {
                        point.m_style = 1;
                        this.AddPoint(point);
                        AllPointsInLines[this_line_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }
                    temp_this_line.m_xs = AllRects[value].m_xs;
                    temp_this_line.m_ys = AllRects[value].m_ys;
                    point = this.GetCrossPoint(this_line, temp_this_line);
                    if (point != null)
                    {
                        point.m_style = 1;
                        this.AddPoint(point);
                        AllPointsInLines[this_line_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }
                }
            }
        }



        private void AddPoint(CADPoint point)
        {
            if (point == null)
                return;
            CADPoint this_point = point.Copy();
            if (this_point.m_id == 0)
            {
                PointNumber++;
                this_point.m_id = PointNumber;
            }
            int this_point_id = this_point.m_id;
            //if (!AllColors.ContainsKey(color_id))
            //    color_id = 1;
            this_point.m_count++;
            if (this.AllPoints.ContainsKey(this_point_id))
            {
                AllPoints[this_point_id] = this_point.Copy();
                //AllLinesColor[LineNumber] = color_id;
            }
            else
            {
                AllPoints.Add(this_point_id, this_point.Copy());
            }
        }

        private bool AddCube(CADCube cube, int color_id = 0)
        {
            if (cube == null)
                return false;
            CADCube this_cube = cube.Copy();
            if (this_cube.m_id == 0)
            {
                CubeNumber++;
                this_cube.m_id = CubeNumber;
            }
            int this_cube_id = this_cube.m_id;
            if (!AllColors.ContainsKey(color_id))
                color_id = 1;
            if (!AllCubesColor.ContainsKey(this_cube_id))
                AllCubesColor.Add(this_cube_id, color_id);
            if (this.AllCubes.ContainsKey(this_cube_id))
            {
                AllCubes[this_cube_id] = this_cube.Copy();
            }
            else
            {
                AllCubes.Add(this_cube_id, this_cube.Copy());
            }
            return true;
        }

        private bool AddRect(CADRect rect, int color_id = 0)
        {
            if (rect == null)
                return false;
            foreach (CADRect value in this.AllRects.Values)
            {
                if (((int)(value.m_xs + value.m_xe) - (int)(rect.m_xs + rect.m_xe)) == 0 &&
                    ((int)(value.m_ys + value.m_ye) - (int)(rect.m_ys + rect.m_ye)) == 0 &&
                    (int)(value.m_height - rect.m_height) == 0 &&
                    (int)(value.m_width - rect.m_width) == 0 &&
                    value.m_id != rect.m_id)
                {
                    return false;
                }
            }
            CADRect this_rect = rect.Copy();
            if (this_rect.m_id == 0)
            {
                RectNumber++;
                this_rect.m_id = RectNumber;
            }
            if (!AllColors.ContainsKey(color_id))
                color_id = 1;
            int this_rect_id = this_rect.m_id;
            if (this.AllRects.ContainsKey(this_rect_id))
            {
                AllRects[this_rect_id] = this_rect.Copy();
                AllRectsColor[this_rect_id] = color_id;
                CADPoint point = new CADPoint(this_rect.m_xs, this_rect.m_ys);
                List<int> pointsItems = AllPointsInRects[this_rect_id];
                for (int i = 0; i < pointsItems.Count; i++)
                {
                    if (AllPoints.ContainsKey(pointsItems[i]))
                        AllPoints.Remove(pointsItems[i]);
                }
                pointsItems.Clear();
                this.AddPoint(point);//角点
                pointsItems.Add(PointNumber);
                point.m_x = this_rect.m_xs;
                point.m_y = this_rect.m_ye;
                this.AddPoint(point);//角点
                pointsItems.Add(PointNumber);
                point.m_x = this_rect.m_xe;
                point.m_y = this_rect.m_ys;
                this.AddPoint(point);//角点
                pointsItems.Add(PointNumber);
                point.m_x = this_rect.m_xe;
                point.m_y = this_rect.m_ye;
                this.AddPoint(point);//角点
                pointsItems.Add(PointNumber);
                if (isRebar != 1)
                {
                    point.m_x = (this_rect.m_xs + this_rect.m_xe) / 2;
                    point.m_y = (this_rect.m_ys + this_rect.m_ye) / 2;
                    this.AddPoint(point);//中心
                    pointsItems.Add(PointNumber);
                }
                point.m_x = this_rect.m_xs;
                point.m_y = (this_rect.m_ys + this_rect.m_ye) / 2;
                this.AddPoint(point);//边中点
                pointsItems.Add(PointNumber);
                point.m_x = this_rect.m_xe;
                point.m_y = (this_rect.m_ys + this_rect.m_ye) / 2;
                this.AddPoint(point);//边中点
                pointsItems.Add(PointNumber);
                point.m_x = (this_rect.m_xs + this_rect.m_xe) / 2;
                point.m_y = this_rect.m_ys;
                this.AddPoint(point);//边中点
                pointsItems.Add(PointNumber);
                point.m_x = (this_rect.m_xs + this_rect.m_xe) / 2;
                point.m_y = this_rect.m_ye;
                this.AddPoint(point);//边中点
                pointsItems.Add(PointNumber);
                //int[] pointsItems = { PointNumber - 3, PointNumber - 2,PointNumber - 1, PointNumber };
                AllPointsInRects[this_rect_id] = pointsItems;
                for (int i = 0; i < m_sel_rect_list.Count; i++)
                {
                    if (m_sel_rect_list[i].m_id == this_rect_id)
                    {
                        m_sel_rect_list[i] = AllRects[this_rect_id];
                    }
                }
            }
            else
            {
                AllRects.Add(this_rect_id, this_rect.Copy());
                AllRectsColor.Add(this_rect_id, color_id);
                CADPoint point = new CADPoint(this_rect.m_xs, this_rect.m_ys);
                List<int> pointsItems = new List<int>();
                this.AddPoint(point);//角点
                pointsItems.Add(PointNumber);
                point = new CADPoint();
                point.m_x = this_rect.m_xs;
                point.m_y = this_rect.m_ye;
                this.AddPoint(point);//角点
                pointsItems.Add(PointNumber);
                point = new CADPoint();
                point.m_x = this_rect.m_xe;
                point.m_y = this_rect.m_ys;
                this.AddPoint(point);//角点
                pointsItems.Add(PointNumber);
                point = new CADPoint();
                point.m_x = this_rect.m_xe;
                point.m_y = this_rect.m_ye;
                this.AddPoint(point);//角点
                pointsItems.Add(PointNumber);
                if (isRebar != 1)
                {
                    point = new CADPoint();
                    point.m_x = (this_rect.m_xs + this_rect.m_xe) / 2;
                    point.m_y = (this_rect.m_ys + this_rect.m_ye) / 2;
                    this.AddPoint(point);//中心
                    pointsItems.Add(PointNumber);
                }
                point = new CADPoint();
                point.m_x = this_rect.m_xs;
                point.m_y = (this_rect.m_ys + this_rect.m_ye) / 2;
                this.AddPoint(point);//边中点
                pointsItems.Add(PointNumber);
                point = new CADPoint();
                point.m_x = this_rect.m_xe;
                point.m_y = (this_rect.m_ys + this_rect.m_ye) / 2;
                this.AddPoint(point);//边中点
                pointsItems.Add(PointNumber);
                point = new CADPoint();
                point.m_x = (this_rect.m_xs + this_rect.m_xe) / 2;
                point.m_y = this_rect.m_ys;
                this.AddPoint(point);//边中点
                pointsItems.Add(PointNumber);
                point = new CADPoint();
                point.m_x = (this_rect.m_xs + this_rect.m_xe) / 2;
                point.m_y = this_rect.m_ye;
                this.AddPoint(point);//边中点
                pointsItems.Add(PointNumber);
                //int[] pointsItems = { PointNumber - 3, PointNumber - 2, PointNumber - 1, PointNumber };
                AllPointsInRects.Add(this_rect_id, pointsItems);
                if (this_rect.m_xs > this_rect.m_xe)
                {
                    m_border.m_xs = m_border.m_xs < this_rect.m_xe ? m_border.m_xs : this_rect.m_xe;
                    m_border.m_xe = m_border.m_xe > this_rect.m_xs ? m_border.m_xe : this_rect.m_xs;
                }
                else
                {
                    m_border.m_xs = m_border.m_xs < this_rect.m_xs ? m_border.m_xs : this_rect.m_xs;
                    m_border.m_xe = m_border.m_xe > this_rect.m_xe ? m_border.m_xe : this_rect.m_xe;
                }
                if (this_rect.m_ys > this_rect.m_ye)
                {
                    m_border.m_ys = m_border.m_ys < this_rect.m_ye ? m_border.m_ys : this_rect.m_ye;
                    m_border.m_ye = m_border.m_ye > this_rect.m_ys ? m_border.m_ye : this_rect.m_ys;
                }
                else
                {
                    m_border.m_ys = m_border.m_ys < this_rect.m_ys ? m_border.m_ys : this_rect.m_ys;
                    m_border.m_ye = m_border.m_ye > this_rect.m_ye ? m_border.m_ye : this_rect.m_ye;
                }
            }
            if (AllLines.Count > 0)
            {
                foreach (int value in AllLines.Keys)
                {

                    CADLine cur_line = AllLines[value];
                    CADLine this_rect_line = new CADLine();
                    this_rect_line.m_xs = this_rect.m_xs;
                    this_rect_line.m_ys = this_rect.m_ys;

                    this_rect_line.m_xe = this_rect.m_xs;
                    this_rect_line.m_ye = this_rect.m_ye;
                    CADPoint point = this.GetCrossPoint(this_rect_line, cur_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInLines[value].Add(PointNumber);
                    }

                    this_rect_line.m_xs = this_rect.m_xe;
                    this_rect_line.m_ys = this_rect.m_ye;
                    point = this.GetCrossPoint(this_rect_line, cur_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInLines[value].Add(PointNumber);
                    }
                    this_rect_line.m_xe = this_rect.m_xe;
                    this_rect_line.m_ye = this_rect.m_ys;
                    point = this.GetCrossPoint(this_rect_line, cur_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInLines[value].Add(PointNumber);
                    }
                    this_rect_line.m_xs = this_rect.m_xs;
                    this_rect_line.m_ys = this_rect.m_ys;
                    point = this.GetCrossPoint(this_rect_line, cur_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInLines[value].Add(PointNumber);
                    }
                }
            }
            if (AllRects.Count > 0)
            {
                foreach (int value in AllRects.Keys)
                {
                    if (value == this_rect.m_id)
                        continue;
                    CADRect cur_this_rect = AllRects[value];
                    CADLine this_rect_line = new CADLine();
                    this_rect_line.m_xs = this_rect.m_xs;
                    this_rect_line.m_ys = this_rect.m_ys;

                    this_rect_line.m_xe = this_rect.m_xs;
                    this_rect_line.m_ye = this_rect.m_ye;

                    CADLine temp_line = new CADLine();
                    temp_line.m_xs = cur_this_rect.m_xs;
                    temp_line.m_ys = cur_this_rect.m_ys;

                    temp_line.m_xe = cur_this_rect.m_xs;
                    temp_line.m_ye = cur_this_rect.m_ye;
                    CADPoint point = this.GetCrossPoint(this_rect_line, temp_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }

                    temp_line.m_xs = cur_this_rect.m_xe;
                    temp_line.m_ys = cur_this_rect.m_ye;
                    point = this.GetCrossPoint(this_rect_line, temp_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }
                    temp_line.m_xe = cur_this_rect.m_xe;
                    temp_line.m_ye = cur_this_rect.m_ys;
                    point = this.GetCrossPoint(this_rect_line, temp_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }
                    temp_line.m_xs = cur_this_rect.m_xs;
                    temp_line.m_ys = cur_this_rect.m_ys;
                    point = this.GetCrossPoint(this_rect_line, temp_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }


                    this_rect_line.m_xs = cur_this_rect.m_xe;
                    this_rect_line.m_ys = cur_this_rect.m_ye;

                    temp_line.m_xs = cur_this_rect.m_xs;
                    temp_line.m_ys = cur_this_rect.m_ys;

                    temp_line.m_xe = cur_this_rect.m_xs;
                    temp_line.m_ye = cur_this_rect.m_ye;
                    point = this.GetCrossPoint(this_rect_line, temp_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }

                    temp_line.m_xs = cur_this_rect.m_xe;
                    temp_line.m_ys = cur_this_rect.m_ye;
                    point = this.GetCrossPoint(this_rect_line, temp_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }
                    temp_line.m_xe = cur_this_rect.m_xe;
                    temp_line.m_ye = cur_this_rect.m_ys;
                    point = this.GetCrossPoint(this_rect_line, temp_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }
                    temp_line.m_xs = cur_this_rect.m_xs;
                    temp_line.m_ys = cur_this_rect.m_ys;
                    point = this.GetCrossPoint(this_rect_line, temp_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }


                    this_rect_line.m_xe = cur_this_rect.m_xe;
                    this_rect_line.m_ye = cur_this_rect.m_ys;

                    temp_line.m_xs = cur_this_rect.m_xs;
                    temp_line.m_ys = cur_this_rect.m_ys;

                    temp_line.m_xe = cur_this_rect.m_xs;
                    temp_line.m_ye = cur_this_rect.m_ye;
                    point = this.GetCrossPoint(this_rect_line, temp_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }

                    temp_line.m_xs = cur_this_rect.m_xe;
                    temp_line.m_ys = cur_this_rect.m_ye;
                    point = this.GetCrossPoint(this_rect_line, temp_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }
                    temp_line.m_xe = cur_this_rect.m_xe;
                    temp_line.m_ye = cur_this_rect.m_ys;
                    point = this.GetCrossPoint(this_rect_line, temp_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }
                    temp_line.m_xs = cur_this_rect.m_xs;
                    temp_line.m_ys = cur_this_rect.m_ys;
                    point = this.GetCrossPoint(this_rect_line, temp_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }

                    this_rect_line.m_xs = cur_this_rect.m_xs;
                    this_rect_line.m_ys = cur_this_rect.m_ys;

                    temp_line.m_xs = cur_this_rect.m_xs;
                    temp_line.m_ys = cur_this_rect.m_ys;

                    temp_line.m_xe = cur_this_rect.m_xs;
                    temp_line.m_ye = cur_this_rect.m_ye;
                    point = this.GetCrossPoint(this_rect_line, temp_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }

                    temp_line.m_xs = cur_this_rect.m_xe;
                    temp_line.m_ys = cur_this_rect.m_ye;
                    point = this.GetCrossPoint(this_rect_line, temp_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }
                    temp_line.m_xe = cur_this_rect.m_xe;
                    temp_line.m_ye = cur_this_rect.m_ys;
                    point = this.GetCrossPoint(this_rect_line, temp_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }
                    temp_line.m_xs = cur_this_rect.m_xs;
                    temp_line.m_ys = cur_this_rect.m_ys;
                    point = this.GetCrossPoint(this_rect_line, temp_line);
                    if (point != null)
                    {
                        this.AddPoint(point);
                        AllPointsInRects[this_rect_id].Add(PointNumber);
                        AllPointsInRects[value].Add(PointNumber);
                    }

                }
            }
            return true;
        }


        private void SelLine(int line_id)
        {
            if (!this.SelLines.ContainsKey(line_id))
            {
                this.SelLines.Add(line_id, this.AllLines[line_id]);
            }
            else
                this.SelLines.Remove(line_id);
        }


        private void SelPoint(int point_id)
        {
            if (!this.SelPoints.ContainsKey(point_id))
            {
                this.SelPoints.Add(point_id, this.AllPoints[point_id]);
                //m_sel_point_list.Add(this.AllPoints[point_id]);
                bool flag = false;
                for (int i = 0; i < m_sel_point_list.Count; i++)
                {
                    if (m_sel_point_list[i].m_id == point_id)
                    {
                        m_sel_point_list[i] = this.AllPoints[point_id];
                        flag = true;
                        break;
                    }

                }
                if (!flag)
                    m_sel_point_list.Add(this.AllPoints[point_id]);
            }
            else
            {
                this.SelPoints.Remove(point_id);
                for (int i = 0; i < m_sel_point_list.Count; i++)
                {
                    if (m_sel_point_list[i].m_id == point_id)
                    {
                        m_sel_point_list.RemoveAt(i);
                        break;
                    }
                }
                m_cur_sel_point = new CADPoint();
            }
        }


        private void SelRect(int rect_id)
        {
            if (!this.SelRects.ContainsKey(rect_id))
            {
                this.SelRects.Add(rect_id, this.AllRects[rect_id].Copy());
                bool flag = false;
                for (int i = 0; i < m_sel_rect_list.Count; i++)
                {
                    if (m_sel_rect_list[i].m_id == rect_id)
                    {
                        m_sel_rect_list[i] = this.AllRects[rect_id];
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                    m_sel_rect_list.Add(this.AllRects[rect_id]);
            }
            else
            {
                this.SelRects.Remove(rect_id);
                for (int i = 0; i < m_sel_rect_list.Count; i++)
                {
                    if (m_sel_rect_list[i].m_id == rect_id)
                    {
                        m_sel_rect_list.RemoveAt(i);
                        break;
                    }
                }

            }
        }


        private void DrawLine(CADLine line, CADRGB color = null)
        {
            if (line.m_id == -1)
                return;
            m_openGLCtrl.LineWidth(1);
            m_openGLCtrl.Begin(SharpGL.Enumerations.BeginMode.Lines);
            if (color == null)
                m_openGLCtrl.Color(1.0f, 1.0f, 1.0f);
            else
                m_openGLCtrl.Color(color.m_r, color.m_g, color.m_b);
            m_openGLCtrl.Vertex(line.m_xs, line.m_ys, line.m_zs);
            m_openGLCtrl.Vertex(line.m_xe, line.m_ye, line.m_ze);
            m_openGLCtrl.End();
            m_openGLCtrl.Flush();
        }

        private void DrawCoord(double scale)
        {
            m_openGLCtrl.LineWidth(2);
            m_openGLCtrl.Begin(SharpGL.Enumerations.BeginMode.Lines);

            m_openGLCtrl.Color(1.0f, 0.0f, 0.0f);
            m_openGLCtrl.Vertex(ax_p.m_x / scale, ax_p.m_y / scale, ax_p.m_z / scale);
            m_openGLCtrl.Vertex( zero.m_x / scale, zero.m_y / scale, zero.m_z / scale);

            m_openGLCtrl.Color(0.0f, 1.0f, 0.0f);
            m_openGLCtrl.Vertex(ay_p.m_x / scale, ay_p.m_y / scale, ay_p.m_z / scale);
            m_openGLCtrl.Vertex(zero.m_x / scale, zero.m_y / scale, zero.m_z / scale);

            m_openGLCtrl.Color(0.0f, 0.0f, 1.0f);
            m_openGLCtrl.Vertex(az_p.m_x / scale, az_p.m_y / scale, az_p.m_z / scale);
            m_openGLCtrl.Vertex(zero.m_x / scale, zero.m_y / scale, zero.m_z / scale);

            m_openGLCtrl.End();
            m_openGLCtrl.Flush();

            m_openGLCtrl.LineWidth(1);
            m_openGLCtrl.Begin(SharpGL.Enumerations.BeginMode.Lines);

            m_openGLCtrl.Color(0.0f, 1.0f, 1.0f);
            m_openGLCtrl.Vertex(ax_p.m_x / 3 / scale, ax_p.m_y / 3 / scale, ax_p.m_z / 3 / scale);
            m_openGLCtrl.Vertex(ay_p.m_x / 3 / scale, ay_p.m_y / 3 / scale, ay_p.m_z / 3 / scale);

            m_openGLCtrl.Color(1.0f, 0.0f, 1.0f);
            m_openGLCtrl.Vertex(ay_p.m_x / 3 / scale, ay_p.m_y / 3 / scale, ay_p.m_z / 3 / scale);
            m_openGLCtrl.Vertex(az_p.m_x / 3 / scale, az_p.m_y / 3 / scale, az_p.m_z / 3 / scale);

            m_openGLCtrl.Color(1.0f, 1.0f, 0.0f);
            m_openGLCtrl.Vertex(az_p.m_x / 3 / scale, az_p.m_y / 3 / scale, az_p.m_z / 3 / scale);
            m_openGLCtrl.Vertex(ax_p.m_x / 3 / scale, ax_p.m_y / 3 / scale, ax_p.m_z / 3 / scale);

            m_openGLCtrl.End();
            m_openGLCtrl.Flush();
        }


        private void DrawPoint(CADPoint point, CADRGB color = null)
        {
            if (point.m_style == 0)
                m_openGLCtrl.PointSize(3.0f);
            if (point.m_style == 1)
                m_openGLCtrl.PointSize(2.5f);
            if (isRebar == 1)
                m_openGLCtrl.PointSize(5.0f);
            if (point.m_is_rebar == 1)
            {
                m_openGLCtrl.Begin(SharpGL.Enumerations.BeginMode.Polygon);
                if (color == null)
                    m_openGLCtrl.Color(1.0f, 1.0f, 0.0f);

                double r = 12;
                if (point.m_diameter > 0)
                    r = 10 * point.m_diameter;
                double pi = 3.1415926;
                int n = 20;
                for (int i = 0; i < n; i++)
                    m_openGLCtrl.Vertex(point.m_x + r * Math.Cos(2 * pi / n * i), point.m_y + r * Math.Sin(2 * pi / n * i));
                m_openGLCtrl.End();
                m_openGLCtrl.Flush();
                return;

            }
            else
            {
                m_openGLCtrl.Begin(SharpGL.Enumerations.BeginMode.Points);

                if (color == null)
                {
                    m_openGLCtrl.Color(1.0f, 1.0f, 1.0f);
                    if (point.m_style == 1)
                        m_openGLCtrl.Color(0.0f, 1.0f, 1.0f);
                }
                else
                    m_openGLCtrl.Color(color.m_r, color.m_g, color.m_b);
                m_openGLCtrl.Vertex(point.m_x, point.m_y, point.m_z);
                m_openGLCtrl.End();
                m_openGLCtrl.Flush();
            }
        }


        private void DrawGridLine(CADLine line)
        {
            m_openGLCtrl.LineWidth(1);
            m_openGLCtrl.Begin(SharpGL.Enumerations.BeginMode.Lines);
            m_openGLCtrl.Color(0.2f, 0.2f, 0.2f);
            m_openGLCtrl.Vertex(line.m_xs, line.m_ys, line.m_zs);
            m_openGLCtrl.Vertex(line.m_xe, line.m_ye, line.m_ze);
            m_openGLCtrl.End();
            m_openGLCtrl.Flush();
        }

        private void DrawRect(CADRect rect, CADRGB color = null)
        {
            m_openGLCtrl.LineWidth(1);
            m_openGLCtrl.Begin(SharpGL.Enumerations.BeginMode.Lines);
            if (color == null)
                m_openGLCtrl.Color(1.0f, 1.0f, 1.0f);
            else
                m_openGLCtrl.Color(color.m_r, color.m_g, color.m_b);
            m_openGLCtrl.Vertex(rect.m_xs, rect.m_ys);
            m_openGLCtrl.Vertex(rect.m_xe, rect.m_ys);

            m_openGLCtrl.Vertex(rect.m_xe, rect.m_ys);
            m_openGLCtrl.Vertex(rect.m_xe, rect.m_ye);

            m_openGLCtrl.Vertex(rect.m_xe, rect.m_ye);
            m_openGLCtrl.Vertex(rect.m_xs, rect.m_ye);

            m_openGLCtrl.Vertex(rect.m_xs, rect.m_ye);
            m_openGLCtrl.Vertex(rect.m_xs, rect.m_ys);

            m_openGLCtrl.End();
            m_openGLCtrl.Flush();
        }



        private void DrawRect3D(Rect3D rect3d, CADRGB color = null)
        {

        }


        private void DrawCube(CADCube rect, CADRGB color = null)
        {
            //m_openGLCtrl.LineWidth(1);
            m_openGLCtrl.Begin(SharpGL.Enumerations.BeginMode.Quads);
            if (color == null)
                m_openGLCtrl.Color(1.0f, 1.0f, 1.0f);
            else
                m_openGLCtrl.Color(color.m_r, color.m_g, color.m_b);
            //m_openGLCtrl.Color(1.0f, 1.0f, 0.0f);
            m_openGLCtrl.Vertex(rect.m_surfs[0].m_points[0].m_x, rect.m_surfs[0].m_points[0].m_y, rect.m_surfs[0].m_points[0].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[0].m_points[1].m_x, rect.m_surfs[0].m_points[1].m_y, rect.m_surfs[0].m_points[1].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[0].m_points[2].m_x, rect.m_surfs[0].m_points[2].m_y, rect.m_surfs[0].m_points[2].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[0].m_points[3].m_x, rect.m_surfs[0].m_points[3].m_y, rect.m_surfs[0].m_points[3].m_z);

            //m_openGLCtrl.Color(1.0f, 0.0f, 1.0f);
            m_openGLCtrl.Vertex(rect.m_surfs[1].m_points[0].m_x, rect.m_surfs[1].m_points[0].m_y, rect.m_surfs[1].m_points[0].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[1].m_points[1].m_x, rect.m_surfs[1].m_points[1].m_y, rect.m_surfs[1].m_points[1].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[1].m_points[2].m_x, rect.m_surfs[1].m_points[2].m_y, rect.m_surfs[1].m_points[2].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[1].m_points[3].m_x, rect.m_surfs[1].m_points[3].m_y, rect.m_surfs[1].m_points[3].m_z);

            //m_openGLCtrl.Color(0.0f, 1.0f, 1.0f);
            m_openGLCtrl.Vertex(rect.m_surfs[2].m_points[0].m_x, rect.m_surfs[2].m_points[0].m_y, rect.m_surfs[2].m_points[0].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[2].m_points[1].m_x, rect.m_surfs[2].m_points[1].m_y, rect.m_surfs[2].m_points[1].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[2].m_points[2].m_x, rect.m_surfs[2].m_points[2].m_y, rect.m_surfs[2].m_points[2].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[2].m_points[3].m_x, rect.m_surfs[2].m_points[3].m_y, rect.m_surfs[2].m_points[3].m_z);

            //m_openGLCtrl.Color(0.0f, 0.0f, 1.0f);
            m_openGLCtrl.Vertex(rect.m_surfs[3].m_points[0].m_x, rect.m_surfs[3].m_points[0].m_y, rect.m_surfs[3].m_points[0].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[3].m_points[1].m_x, rect.m_surfs[3].m_points[1].m_y, rect.m_surfs[3].m_points[1].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[3].m_points[2].m_x, rect.m_surfs[3].m_points[2].m_y, rect.m_surfs[3].m_points[2].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[3].m_points[3].m_x, rect.m_surfs[3].m_points[3].m_y, rect.m_surfs[3].m_points[3].m_z);

            //m_openGLCtrl.Color(0.0f, 1.0f, 0.0f);
            m_openGLCtrl.Vertex(rect.m_surfs[4].m_points[0].m_x, rect.m_surfs[4].m_points[0].m_y, rect.m_surfs[4].m_points[0].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[4].m_points[1].m_x, rect.m_surfs[4].m_points[1].m_y, rect.m_surfs[4].m_points[1].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[4].m_points[2].m_x, rect.m_surfs[4].m_points[2].m_y, rect.m_surfs[4].m_points[2].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[4].m_points[3].m_x, rect.m_surfs[4].m_points[3].m_y, rect.m_surfs[4].m_points[3].m_z);

            //m_openGLCtrl.Color(0.0f, 1.0f, 0.5f);
            m_openGLCtrl.Vertex(rect.m_surfs[5].m_points[0].m_x, rect.m_surfs[5].m_points[0].m_y, rect.m_surfs[5].m_points[0].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[5].m_points[1].m_x, rect.m_surfs[5].m_points[1].m_y, rect.m_surfs[5].m_points[1].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[5].m_points[2].m_x, rect.m_surfs[5].m_points[2].m_y, rect.m_surfs[5].m_points[2].m_z);
            m_openGLCtrl.Vertex(rect.m_surfs[5].m_points[3].m_x, rect.m_surfs[5].m_points[3].m_y, rect.m_surfs[5].m_points[3].m_z);

            log.Info(rect.m_surfs[5].m_points[3].m_x);

            m_openGLCtrl.End();
            m_openGLCtrl.Flush();
        }

        private void DrawSelLine(int line_id)
        {
            if (!AllLines.ContainsKey(line_id))
                return;
            CADLine line = AllLines[line_id];
            m_openGLCtrl.LineWidth(5);
            m_openGLCtrl.Begin(SharpGL.Enumerations.BeginMode.Lines);
            m_openGLCtrl.Color(0.7f, 0.2f, 0.2f);
            m_openGLCtrl.Vertex(line.m_xs, line.m_ys);
            m_openGLCtrl.Vertex(line.m_xe, line.m_ye);

            m_openGLCtrl.End();
            m_openGLCtrl.Flush();
        }

        private void DrawSelPoint(int point_id)
        {
            if (!AllPoints.ContainsKey(point_id))
                return;
            CADPoint point = AllPoints[point_id];
            if (point.m_is_rebar == 1)
            {
                m_openGLCtrl.Begin(SharpGL.Enumerations.BeginMode.Polygon);
                m_openGLCtrl.Color(0.7f, 0.2f, 0.7f);
                double r = 12;
                if (point.m_diameter > 0)
                    r = 10 * point.m_diameter;
                double pi = 3.1415926;
                int n = 20;
                for (int i = 0; i < n; i++)
                    m_openGLCtrl.Vertex(point.m_x + r * Math.Cos(2 * pi / n * i), point.m_y + r * Math.Sin(2 * pi / n * i));
                m_openGLCtrl.End();
                m_openGLCtrl.Flush();
                return;

            }
            else
            {
                m_openGLCtrl.PointSize(8.0f);
                m_openGLCtrl.Begin(SharpGL.Enumerations.BeginMode.Points);
                m_openGLCtrl.Color(0.7f, 0.2f, 0.7f);
                m_openGLCtrl.Vertex(point.m_x, point.m_y);

                m_openGLCtrl.End();
                m_openGLCtrl.Flush();
            }
        }


        private void DrawSelRect(int rect_id)
        {
            if (!AllRects.ContainsKey(rect_id))
                return;
            CADRect rect = SelRects[rect_id];//AllRects[rect_id];

            m_openGLCtrl.LineWidth(5);
            m_openGLCtrl.Begin(SharpGL.Enumerations.BeginMode.Lines);
            m_openGLCtrl.Color(0.2f, 0.7f, 0.2f);
            m_openGLCtrl.Vertex(rect.m_xs, rect.m_ys);
            m_openGLCtrl.Vertex(rect.m_xe, rect.m_ys);

            m_openGLCtrl.Vertex(rect.m_xe, rect.m_ys);
            m_openGLCtrl.Vertex(rect.m_xe, rect.m_ye);

            m_openGLCtrl.Vertex(rect.m_xe, rect.m_ye);
            m_openGLCtrl.Vertex(rect.m_xs, rect.m_ye);

            m_openGLCtrl.Vertex(rect.m_xs, rect.m_ye);
            m_openGLCtrl.Vertex(rect.m_xs, rect.m_ys);

            m_openGLCtrl.End();
            m_openGLCtrl.Flush();
        }


        private void DrawText(string text, Point pos)
        {
            float font_size = 12.0f;
            if (isRebar == 1)
                font_size = 8.0f;
            m_openGLCtrl.DrawText((int)(pos.X), (int)(pos.Y), 0.5f, 1.0f, 0.5f, "Lucida Console", font_size, text);
        }


        private void DrawMouseLine(double scale)
        {
            //UserDrawLine(new Point(0,0),new Point(1000,1000));
            m_openGLCtrl.LineWidth(1);
            m_openGLCtrl.Begin(SharpGL.Enumerations.BeginMode.Lines);
            m_openGLCtrl.Color(1.0f, 1.0f, 1.0f);
            //m_openGLCtrl.Vertex(0, 0);
            //m_openGLCtrl.Vertex(1000, -1000);
            int line_len = 50;
            int rect_len = 5;
            m_openGLCtrl.Vertex(m_currentpos.X / scale / m_pixaxis, (m_currentpos.Y - line_len) / scale / m_pixaxis,0);
            m_openGLCtrl.Vertex(m_currentpos.X / scale / m_pixaxis, (m_currentpos.Y + line_len) / scale / m_pixaxis,0);

            m_openGLCtrl.Vertex((m_currentpos.X - line_len) / scale / m_pixaxis, m_currentpos.Y / scale / m_pixaxis,0);
            m_openGLCtrl.Vertex((m_currentpos.X + line_len) / scale / m_pixaxis, m_currentpos.Y / scale / m_pixaxis,0);

            m_openGLCtrl.Vertex((m_currentpos.X - rect_len) / scale / m_pixaxis, (m_currentpos.Y - rect_len) / scale / m_pixaxis,0);
            m_openGLCtrl.Vertex((m_currentpos.X - rect_len) / scale / m_pixaxis, (m_currentpos.Y + rect_len) / scale / m_pixaxis,0);

            m_openGLCtrl.Vertex((m_currentpos.X - rect_len) / scale / m_pixaxis, (m_currentpos.Y + rect_len) / scale / m_pixaxis,0);
            m_openGLCtrl.Vertex((m_currentpos.X + rect_len) / scale / m_pixaxis, (m_currentpos.Y + rect_len) / scale / m_pixaxis,0);

            m_openGLCtrl.Vertex((m_currentpos.X + rect_len) / scale / m_pixaxis, (m_currentpos.Y + rect_len) / scale / m_pixaxis,0);
            m_openGLCtrl.Vertex((m_currentpos.X + rect_len) / scale / m_pixaxis, (m_currentpos.Y - rect_len) / scale / m_pixaxis,0);

            m_openGLCtrl.Vertex((m_currentpos.X + rect_len) / scale / m_pixaxis, (m_currentpos.Y - rect_len) / scale / m_pixaxis,0);
            m_openGLCtrl.Vertex((m_currentpos.X - rect_len) / scale / m_pixaxis, (m_currentpos.Y - rect_len) / scale / m_pixaxis,0);

            m_openGLCtrl.End();
            m_openGLCtrl.Flush();
        }


        private void DrawGrids()
        {

            CADLine line = new CADLine(0, 0, 0, 0);
            double real_gridstep = m_gridstep;
            if (isRebar == 1)
                real_gridstep = m_gridstep / 2;
            for (int i = 0; i <= (int)(this.Width / real_gridstep / 2) + 1; i++)
            {
                line.m_xs = (float)((i - (int)(m_center_offset.X / real_gridstep)) * (real_gridstep / m_scale / m_pixaxis));
                line.m_xe = (float)((i - (int)(m_center_offset.X / real_gridstep)) * (real_gridstep / m_scale / m_pixaxis));
                line.m_ys = (float)((-this.Height / 2 - m_center_offset.Y) / m_scale / m_pixaxis);
                line.m_ye = (float)((this.Height / 2 - m_center_offset.Y) / m_scale / m_pixaxis);
                this.DrawGridLine(line);
                line.m_xs = (float)((-i - (int)(m_center_offset.X / real_gridstep)) * (real_gridstep / m_scale / m_pixaxis));
                line.m_xe = (float)((-i - (int)(m_center_offset.X / real_gridstep)) * (real_gridstep / m_scale / m_pixaxis));
                line.m_ys = (float)((-this.Height / 2 - m_center_offset.Y) / m_scale / m_pixaxis);
                line.m_ye = (float)((this.Height / 2 - m_center_offset.Y) / m_scale / m_pixaxis);
                this.DrawGridLine(line);
            }

            for (int i = 0; i <= (int)(this.Height / real_gridstep / 2) + 1; i++)
            {
                line.m_ys = (float)((i - (int)(m_center_offset.Y / real_gridstep)) * (real_gridstep / m_scale / m_pixaxis));
                line.m_ye = (float)((i - (int)(m_center_offset.Y / real_gridstep)) * (real_gridstep / m_scale / m_pixaxis));
                line.m_xs = (float)((-this.Width / 2 - m_center_offset.X) / m_scale / m_pixaxis);
                line.m_xe = (float)((this.Width / 2 - m_center_offset.X) / m_scale / m_pixaxis);
                this.DrawGridLine(line);
                line.m_ys = (float)((-i - (int)(m_center_offset.Y / real_gridstep)) * (real_gridstep / m_scale / m_pixaxis));
                line.m_ye = (float)((-i - (int)(m_center_offset.Y / real_gridstep)) * (real_gridstep / m_scale / m_pixaxis));
                line.m_xs = (float)((-this.Width / 2 - m_center_offset.X) / m_scale / m_pixaxis);
                line.m_xe = (float)((this.Width / 2 - m_center_offset.X) / m_scale / m_pixaxis);
                this.DrawGridLine(line);
            }

        }



        private void DelLine(int line_id)
        {
            if (this.AllLines.ContainsKey(line_id))
            {
                if (AllPointsInLines[line_id].Count > 0)
                {
                    foreach (int value in AllPointsInLines[line_id])
                    {
                        if (AllPoints.ContainsKey(value))
                            AllPoints.Remove(value);
                        if (SelPoints.ContainsKey(value))
                        {
                            SelPoints.Remove(value);
                            for (int i = 0; i < m_sel_point_list.Count; i++)
                            {
                                if (m_sel_point_list[i].m_id == value)
                                {
                                    m_sel_point_list.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                    }
                }

                AllPointsInLines.Remove(line_id);
                AllLines.Remove(line_id);
                AllLinesColor.Remove(line_id);


            }
            if (AllPointsInLines.Count > 0)
            {
                foreach (int value in AllPointsInLines.Keys)
                {

                    for (int i = 0; i < AllPointsInLines[value].Count; i++)
                    {
                        if (!AllPoints.ContainsKey(AllPointsInLines[value][i]))
                            AllPointsInLines[value].Remove(i);
                    }

                }
            }
            if (AllPointsInRects.Count > 0)
            {
                foreach (int value in AllPointsInRects.Keys)
                {

                    for (int i = 0; i < AllPointsInRects[value].Count; i++)
                    {
                        if (!AllPoints.ContainsKey(AllPointsInRects[value][i]))
                            AllPointsInRects[value].RemoveAt(i);
                    }

                }
            }
            this.UpdateBorder();
        }

        private void DelAllLines()
        {
            if (AllLines.Count > 0)
            {
                foreach (int line_id in AllLines.Keys)
                {
                    if (AllPointsInLines[line_id].Count > 0)
                    {
                        foreach (int value in AllPointsInLines[line_id])
                        {
                            if (AllPoints.ContainsKey(value))
                                AllPoints.Remove(value);
                            if (SelPoints.ContainsKey(value))
                            {
                                SelPoints.Remove(value);
                                for (int i = 0; i < m_sel_point_list.Count; i++)
                                {
                                    if (m_sel_point_list[i].m_id == value)
                                    {
                                        m_sel_point_list.RemoveAt(i);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            AllPointsInLines.Clear();
            AllLines.Clear();
            AllLinesColor.Clear();
            SelLines.Clear();
            LineNumber = 0;
            if (AllPointsInRects.Count > 0)
            {
                foreach (int value in AllPointsInRects.Keys)
                {
                    for (int i = 0; i < AllPointsInRects[value].Count; i++)
                    {
                        if (!AllPoints.ContainsKey(AllPointsInRects[value][i]))
                            AllPointsInRects[value].RemoveAt(i);
                    }
                }
            }

            this.UpdateBorder();
        }


        private void DelCube(int cube_id)
        {
            if (this.AllCubes.ContainsKey(cube_id))
            {
                //    if (AllPointsInLines[cube_id].Count > 0)
                //    {
                //        foreach (int value in AllPointsInLines[line_id])
                //        {
                //            if (AllPoints.ContainsKey(value))
                //                AllPoints.Remove(value);
                //            if (SelPoints.ContainsKey(value))
                //            {
                //                SelPoints.Remove(value);
                //                for (int i = 0; i < m_sel_point_list.Count; i++)
                //                {
                //                    if (m_sel_point_list[i].m_id == value)
                //                    {
                //                        m_sel_point_list.RemoveAt(i);
                //                        break;
                //                    }
                //                }
                //            }
                //        }
                //    }

                //AllPointsInLines.Remove(cube_id);
                AllCubes.Remove(cube_id);
                AllCubesColor.Remove(cube_id);


            }
            //if (AllPointsInLines.Count > 0)
            //{
            //    foreach (int value in AllPointsInLines.Keys)
            //    {

            //        for (int i = 0; i < AllPointsInLines[value].Count; i++)
            //        {
            //            if (!AllPoints.ContainsKey(AllPointsInLines[value][i]))
            //                AllPointsInLines[value].Remove(i);
            //        }

            //    }
            //}
            //if (AllPointsInRects.Count > 0)
            //{
            //    foreach (int value in AllPointsInRects.Keys)
            //    {

            //        for (int i = 0; i < AllPointsInRects[value].Count; i++)
            //        {
            //            if (!AllPoints.ContainsKey(AllPointsInRects[value][i]))
            //                AllPointsInRects[value].RemoveAt(i);
            //        }

            //    }
            //}
            this.UpdateBorder();
        }


        public void Test(object obj = null)
        {
            //CADCube m_cube = (CADCube)obj;
            if (obj == null)
            {
                CADCube m_cube = new CADCube();
                m_cube.m_id = CubeNumber;
                CubeNumber++;
                CADPoint point1 = new CADPoint(100, 100, 100);
                CADPoint point2 = new CADPoint(900, 100, 100);
                CADPoint point3 = new CADPoint(900, 900, 100);
                CADPoint point4 = new CADPoint(100, 900, 100);

                CADPoint point5 = new CADPoint(100, 100, 900);
                CADPoint point6 = new CADPoint(900, 100, 900);
                CADPoint point7 = new CADPoint(900, 900, 900);
                CADPoint point8 = new CADPoint(100, 900, 900);

                //if (m_cube.m_surfs[0] == null)
                //{
                //    MessageBox.Show("surfs");
                //    return;
                //}
                m_cube.m_surfs[0].m_points[0] = point1.Copy();
                m_cube.m_surfs[0].m_points[1] = point2.Copy();
                m_cube.m_surfs[0].m_points[2] = point3.Copy();
                m_cube.m_surfs[0].m_points[3] = point4.Copy();

                m_cube.m_surfs[1].m_points[0] = point5.Copy();
                m_cube.m_surfs[1].m_points[1] = point6.Copy();
                m_cube.m_surfs[1].m_points[2] = point7.Copy();
                m_cube.m_surfs[1].m_points[3] = point8.Copy();

                m_cube.m_surfs[2].m_points[0] = point1.Copy();
                m_cube.m_surfs[2].m_points[1] = point4.Copy();
                m_cube.m_surfs[2].m_points[2] = point8.Copy();
                m_cube.m_surfs[2].m_points[3] = point5.Copy();

                m_cube.m_surfs[3].m_points[0] = point2.Copy();
                m_cube.m_surfs[3].m_points[1] = point3.Copy();
                m_cube.m_surfs[3].m_points[2] = point7.Copy();
                m_cube.m_surfs[3].m_points[3] = point6.Copy();

                m_cube.m_surfs[4].m_points[0] = point1.Copy();
                m_cube.m_surfs[4].m_points[1] = point2.Copy();
                m_cube.m_surfs[4].m_points[2] = point6.Copy();
                m_cube.m_surfs[4].m_points[3] = point5.Copy();

                m_cube.m_surfs[5].m_points[0] = point4.Copy();
                m_cube.m_surfs[5].m_points[1] = point3.Copy();
                m_cube.m_surfs[5].m_points[2] = point7.Copy();
                m_cube.m_surfs[5].m_points[3] = point8.Copy();

                this.UserDrawCube(m_cube);


            }
        }

        private void DelRect(int rect_id)
        {
            if (this.AllRects.ContainsKey(rect_id))
            {
                if (AllPointsInRects[rect_id].Count > 0)
                {
                    foreach (int value in AllPointsInRects[rect_id])
                    {
                        if (AllPoints.ContainsKey(value))
                            AllPoints.Remove(value);
                        if (SelPoints.ContainsKey(value))
                        {
                            SelPoints.Remove(value);
                            for (int i = 0; i < m_sel_point_list.Count; i++)
                            {
                                if (m_sel_point_list[i].m_id == value)
                                {
                                    m_sel_point_list.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                    }
                }
                AllPointsInRects.Remove(rect_id);
                AllRects.Remove(rect_id);
                AllRectsColor.Remove(rect_id);
                for (int i = 0; i < m_sel_rect_list.Count; i++)
                {
                    if (m_sel_rect_list[i].m_id == rect_id)
                    {
                        m_sel_rect_list.RemoveAt(i);
                        break;
                    }
                }

            }
            if (AllPointsInLines.Count > 0)
            {
                foreach (int value in AllPointsInLines.Keys)
                {
                    for (int i = 0; i < AllPointsInLines[value].Count; i++)
                    {
                        if (!AllPoints.ContainsKey(AllPointsInLines[value][i]))
                            AllPointsInLines[value].RemoveAt(i);
                    }
                }
            }
            if (AllPointsInRects.Count > 0)
            {
                foreach (int value in AllPointsInRects.Keys)
                {
                    for (int i = 0; i < AllPointsInRects[value].Count; i++)
                    {
                        if (!AllPoints.ContainsKey(AllPointsInRects[value][i]))
                            AllPointsInRects[value].RemoveAt(i);
                    }
                }
            }
            this.UpdateBorder();
        }

        private void DelAllRects()
        {
            if (AllRects.Count > 0)
            {
                foreach (int rect_id in AllRects.Keys)
                {
                    if (AllPointsInRects[rect_id].Count > 0)
                    {
                        foreach (int value in AllPointsInRects[rect_id])
                        {
                            if (AllPoints.ContainsKey(value))
                                AllPoints.Remove(value);
                            if (SelPoints.ContainsKey(value))
                            {
                                SelPoints.Remove(value);
                                for (int i = 0; i < m_sel_point_list.Count; i++)
                                {
                                    if (m_sel_point_list[i].m_id == value)
                                    {
                                        m_sel_point_list.RemoveAt(i);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            AllPointsInRects.Clear();
            AllRects.Clear();
            AllRectsColor.Clear();
            SelRects.Clear();
            m_sel_rect_list.Clear();
            RectNumber = 0;
            if (AllPointsInLines.Count > 0)
            {
                foreach (int value in AllPointsInLines.Keys)
                {
                    if (AllPointsInLines[value].Count > 0)
                    {
                        for (int i = 0; i < AllPointsInLines[value].Count; i++)
                        {
                            if (!AllPoints.ContainsKey(AllPointsInLines[value][i]))
                                AllPointsInLines[value].RemoveAt(i);
                        }
                    }
                }
            }
            this.UpdateBorder();
        }

        private void UpdateBorder()
        {
            m_border = new CADRect(0, 0, 0, 0);
            if (AllRects.Count > 0)
            {
                foreach (CADRect rect in this.AllRects.Values)
                {
                    if (rect.m_xs > rect.m_xe)
                    {
                        m_border.m_xs = m_border.m_xs < rect.m_xe ? m_border.m_xs : rect.m_xe;
                        m_border.m_xe = m_border.m_xe > rect.m_xs ? m_border.m_xe : rect.m_xs;
                    }
                    else
                    {
                        m_border.m_xs = m_border.m_xs < rect.m_xs ? m_border.m_xs : rect.m_xs;
                        m_border.m_xe = m_border.m_xe > rect.m_xe ? m_border.m_xe : rect.m_xe;
                    }
                    if (rect.m_ys > rect.m_ye)
                    {
                        m_border.m_ys = m_border.m_ys < rect.m_ye ? m_border.m_ys : rect.m_ye;
                        m_border.m_ye = m_border.m_ye > rect.m_ys ? m_border.m_ye : rect.m_ys;
                    }
                    else
                    {
                        m_border.m_ys = m_border.m_ys < rect.m_ys ? m_border.m_ys : rect.m_ys;
                        m_border.m_ye = m_border.m_ye > rect.m_ye ? m_border.m_ye : rect.m_ye;
                    }
                }
            }
            if (AllLines.Count > 0)
            {
                foreach (CADLine line in this.AllLines.Values)
                {
                    if (line.m_xs > line.m_xe)
                    {
                        m_border.m_xs = m_border.m_xs < line.m_xe ? m_border.m_xs : line.m_xe;
                        m_border.m_xe = m_border.m_xe > line.m_xs ? m_border.m_xe : line.m_xs;
                    }
                    else
                    {
                        m_border.m_xs = m_border.m_xs < line.m_xs ? m_border.m_xs : line.m_xs;
                        m_border.m_xe = m_border.m_xe > line.m_xe ? m_border.m_xe : line.m_xe;
                    }
                    if (line.m_ys > line.m_ye)
                    {
                        m_border.m_ys = m_border.m_ys < line.m_ye ? m_border.m_ys : line.m_ye;
                        m_border.m_ye = m_border.m_ye > line.m_ys ? m_border.m_ye : line.m_ys;
                    }
                    else
                    {
                        m_border.m_ys = m_border.m_ys < line.m_ys ? m_border.m_ys : line.m_ys;
                        m_border.m_ye = m_border.m_ye > line.m_ye ? m_border.m_ye : line.m_ye;
                    }
                }
            }
        }

        private double GetDistance(Point point, CADLine line)
        {
            //if ((point.X - line.m_xs) * (point.X - line.m_xe) > 0 || (point.Y - line.m_ys) * (point.Y - line.m_ye) > 0)
            //    return -1;
            double result = -1;
            double a = line.m_ys - line.m_ye;
            double b = line.m_xe - line.m_xs;
            double c = line.m_xs * line.m_ye - line.m_ys * line.m_xe;

            CADLine normal = new CADLine(point.X, point.Y, point.X - a, point.Y + b);
            // 如果分母为0 则平行或共线, 不相交  
            double denominator = (line.m_ye - line.m_ys) * (normal.m_xe - normal.m_xs) - (line.m_xs - line.m_xe) * (normal.m_ys - normal.m_ye);
            if (Math.Abs(denominator) < 0.00005)
            {
                return result;
            }

            // 线段所在直线的交点坐标 (x , y)      
            double x = ((line.m_xe - line.m_xs) * (normal.m_xe - normal.m_xs) * (normal.m_ys - line.m_ys) + (line.m_ye - line.m_ys) * (normal.m_xe - normal.m_xs) * line.m_xs - (normal.m_ye - normal.m_ys) * (line.m_xe - line.m_xs) * normal.m_xs) / denominator;
            double y = -((line.m_ye - line.m_ys) * (normal.m_ye - normal.m_ys) * (normal.m_xs - line.m_xs) + (line.m_xe - line.m_xs) * (normal.m_ye - normal.m_ys) * line.m_ys - (normal.m_xe - normal.m_xs) * (line.m_ye - line.m_ys) * normal.m_ys) / denominator;

            if ((x - line.m_xs) * (x - line.m_xe) <= 0 && (y - line.m_ys) * (y - line.m_ye) <= 0)
                result = Math.Abs((a * point.X + b * point.Y + c) / Math.Sqrt(a * a + b * b));
            //GetCrossPoint(line, normal);
            return result;

        }

        private double GetDistance(Point point, CADPoint cad_point)
        {
            double result = 0.0;
            result = (cad_point.m_x - point.X) * (cad_point.m_x - point.X) + (cad_point.m_y - point.Y) * (cad_point.m_y - point.Y);
            return Math.Sqrt(result);
        }
        private double GetDistance(Point point, CADRect rect)
        {
            //if ((point.X - rect.m_xs) * (point.X - rect.m_xe) > 0 || (point.Y - rect.m_ys) * (point.Y - rect.m_ye) > 0)
            //    return -1;
            double result = -1;
            double dis = 0.0;
            CADLine line = new CADLine(rect.m_xs, rect.m_ys, rect.m_xs, rect.m_ye);
            result = this.GetDistance(point, line);
            line.m_xs = rect.m_xe;
            line.m_ys = rect.m_ye;
            dis = this.GetDistance(point, line);
            if (dis >= 0)
            {
                if (result > 0)
                    result = result < dis ? result : dis;
                else
                    result = dis;
            }
            line.m_xe = rect.m_xe;
            line.m_ye = rect.m_ys;
            dis = this.GetDistance(point, line);
            if (dis >= 0)
            {
                if (result > 0)
                    result = result < dis ? result : dis;
                else
                    result = dis;
            }
            line.m_xs = rect.m_xs;
            line.m_ys = rect.m_ys;
            dis = this.GetDistance(point, line);
            if (dis >= 0)
            {
                if (result > 0)
                    result = result < dis ? result : dis;
                else
                    result = dis;
            }
            return result;
        }

        private CADPoint GetCrossPoint(CADLine line1, CADLine line2)
        {
            CADPoint point = null;
            // 如果分母为0 则平行或共线, 不相交  
            double denominator = (line1.m_ye - line1.m_ys) * (line2.m_xe - line2.m_xs) - (line1.m_xs - line1.m_xe) * (line2.m_ys - line2.m_ye);
            if (Math.Abs(denominator) < 0.00005)
            {
                return point;
            }

            // 线段所在直线的交点坐标 (x , y)      
            double x = ((line1.m_xe - line1.m_xs) * (line2.m_xe - line2.m_xs) * (line2.m_ys - line1.m_ys) + (line1.m_ye - line1.m_ys) * (line2.m_xe - line2.m_xs) * line1.m_xs - (line2.m_ye - line2.m_ys) * (line1.m_xe - line1.m_xs) * line2.m_xs) / denominator;
            double y = -((line1.m_ye - line1.m_ys) * (line2.m_ye - line2.m_ys) * (line2.m_xs - line1.m_xs) + (line1.m_xe - line1.m_xs) * (line2.m_ye - line2.m_ys) * line1.m_ys - (line2.m_xe - line2.m_xs) * (line1.m_ye - line1.m_ys) * line2.m_ys) / denominator;

            /** 2 判断交点是否在两条线段上 **/
            if (
                // 交点在线段1上  
                (x - line1.m_xs) * (x - line1.m_xe) <= 0 && (y - line1.m_ys) * (y - line1.m_ye) <= 0
                 // 且交点也在线段2上  
                 && (x - line2.m_xs) * (x - line2.m_xe) <= 0 && (y - line2.m_ys) * (y - line2.m_ye) <= 0)

                // 返回交点p  
                point = new CADPoint(x, y);
            return point;
        }

        public void UserDrawLine(Point p1, Point p2, int color_id = 0)
        {
            CADLine line = new CADLine(p1, p2);
            this.AddLine(line, color_id);
        }

        public void UserDrawLine(CADLine line, int color_id = 0)
        {
            this.AddLine(line, color_id);
        }

        public void UserDelAllLines()
        {
            this.DelAllLines();
        }

        public void UserDelLine(int line_id)
        {
            this.DelLine(line_id);
        }

        public void UserDelAllRects()
        {
            this.DelAllRects();
        }

        public void UserDelRect(int rect_id)
        {
            this.DelRect(rect_id);
        }

        public void UserDelPoint(int point_id)
        {
            if (this.SelPoints.ContainsKey(point_id) && SelPoints[point_id].m_id > 0)
                this.SelPoints.Remove(point_id);
            if (this.AllPoints.ContainsKey(point_id) && AllPoints[point_id].m_id > 0)
                this.AllPoints.Remove(point_id);
        }

        public void UserDelAllPoints()
        {
            this.SelPoints.Clear();
            this.AllPoints.Clear();
            PointNumber = 1;
            this.AllPoints.Add(PointNumber, new CADPoint());

        }

        public bool UserDrawRect(Point p1, Point p2, int color_id = 0)
        {

            CADRect rect = new CADRect(p1, p2);
            return this.AddRect(rect, color_id);

        }

        public bool UserDrawRect(CADRect rect, int color_id = 0)
        {
            return this.AddRect(rect, color_id);
        }

        public bool UserDrawCube(CADCube cube, int color_id = 0)
        {
            return this.AddCube(cube);
        }

        public void UserDrawPoint(CADPoint point, int color_id = 0)
        {
            this.AddPoint(point);
        }

        public void UserSelLine(int id)
        {
            this.SelLine(id);
        }

        public void UserSelRect(int id)
        {
            this.SelRect(id);
        }

        public void UserSelPoint(int id)
        {
            this.SelPoint(id);
        }

        public int[] UserGetSelLines()
        {
            int[] result = this.SelLines.Keys.ToArray();
            return result;
        }

        public int[] UserGetSelRects()
        {
            int[] result = this.SelRects.Keys.ToArray();
            return result;
        }

        public int[] UserGetSelPoints()
        {
            int[] result = this.SelPoints.Keys.ToArray();
            return result;
        }

        public Dictionary<int, CADLine> UserGetLines()
        {
            return AllLines;
        }

        public Dictionary<int, CADRect> UserGetRects()
        {
            return AllRects;
        }

        public Dictionary<int, CADPoint> UserGetPoints()
        {
            return AllPoints;
        }

        public void UserStartLine()
        {
            tempLine.m_id = 0;
            tempLine.m_xs = m_curaxispos.m_x;
            tempLine.m_ys = m_curaxispos.m_y;
            tempLine.m_xe = m_curaxispos.m_x;
            tempLine.m_ye = m_curaxispos.m_y;
        }

        public void UserEndLine()
        {
            if (tempLine.m_id == -1)
                return;
            tempLine.m_xe = m_curaxispos.m_x;
            tempLine.m_ye = m_curaxispos.m_y;
            if ((tempLine.m_xs - tempLine.m_xe) * (tempLine.m_xs - tempLine.m_xe) + (tempLine.m_ys - tempLine.m_ye) * (tempLine.m_ys - tempLine.m_ye) <= 1)
            {
                tempLine.m_id = -1;
                return;
            }
            else
            {
                this.UserDrawLine(tempLine.Copy());
                tempLine.m_id = -1;
                tempLine.m_xs = m_curaxispos.m_x;
                tempLine.m_ys = m_curaxispos.m_y;
            }
        }

        public void ZoomView()
        {
            //m_scale = 8 / ((m_border.m_ye - m_border.m_ys) > (m_border.m_xe - m_border.m_xs) ? (m_border.m_ye - m_border.m_ys) : (m_border.m_xe - m_border.m_xs));
            if ((m_border.m_ye - m_border.m_ys) > (m_border.m_xe - m_border.m_xs))
                m_scale = this.Height / 50 / (m_border.m_ye - m_border.m_ys);
            else
                m_scale = this.Width / 50 / (m_border.m_xe - m_border.m_xs);
            if (m_scale > 1000000)
                m_scale = 8 / (m_border.m_xe - m_border.m_xs);
            if (m_scale > 1000000)
                m_scale = 8 / (m_border.m_ye - m_border.m_ys);
            if (m_scale > 1000000)
                m_scale = 0.00001;
            m_center_offset.X = -(m_border.m_xe - m_border.m_xs) / 2 * m_pixaxis * m_scale;
            m_center_offset.Y = -(m_border.m_ye - m_border.m_ys) / 2 * m_pixaxis * m_scale;
        }

        public void ReactToL()
        {
            this.key_down_copy = false;
            this.key_down_move = false;
            this.key_down_del = false;
            this.b_draw_line = true;
            return;
        }
        public void ReactToDel()
        {
            this.key_down_copy = false;
            this.key_down_move = false;
            this.key_down_del = true;
            this.b_draw_line = false;
            int[] sel_keys = this.UserGetSelRects();
            foreach (int key in sel_keys)
            {
                this.UserDelRect(key);
            }
            sel_keys = this.UserGetSelPoints();
            foreach (int key in sel_keys)
            {
                this.UserDelPoint(key);
            }
            sel_keys = this.UserGetSelLines();
            foreach (int key in sel_keys)
            {
                this.UserDelLine(key);
            }
            this.key_down_del = false;
            return;
        }

        public void ShiftDwon()
        {
            this.key_down_shift = true;
        }

        public void ShiftUp()
        {
            this.key_down_shift = false;
        }

        public void ReactToESC()
        {
            if (!key_down_move && !key_down_copy)
            {
                SelRects.Clear();
                SelPoints.Clear();
                m_sel_point_list.Clear();
                SelLines.Clear();
            }

            if (key_down_copy)
            {
                if (SelRects.Count > 0)
                {
                    int[] keys = SelRects.Keys.ToArray();
                    foreach (int value in keys)
                    {
                        SelRects[value] = AllRects[value].Copy();
                    }
                }
                key_down_copy = false;
            }

            if (b_draw_line)
            {
                b_draw_line = false;
                tempLine.m_id = -1;
            }

            if (key_down_move)
            {
                if (SelRects.Count > 0)
                {
                    int[] keys = SelRects.Keys.ToArray();
                    foreach (int value in keys)
                    {
                        SelRects[value] = AllRects[value].Copy();
                    }
                }
                key_down_move = false;
            }



            key_down_del = false;
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                key_down_esc = true;
                return;
            }
            if (e.Key == Key.C)
            {
                key_down_copy = true;
                return;
            }
            if (e.Key == Key.M)
            {
                key_down_move = true;
                MessageBox.Show("M down");
                return;
            }
            if (e.Key == Key.Delete)
            {
                key_down_del = true;
                return;
            }
            if (e.Key == Key.LeftShift)
            {
                key_down_shift = true;
                MessageBox.Show("shift down");
                return;
            }
        }

        private void UserControl_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift)
            {
                key_down_shift = false;
                MessageBox.Show("shift up");
            }
        }


        public class CADLine
        {
            public int m_id { get; set; }
            public float m_xs { get; set; }
            public float m_ys { get; set; }
            public float m_xe { get; set; }
            public float m_ye { get; set; }
            public float m_zs { get; set; }
            public float m_ze { get; set; }

            public CADLine()
            {
                m_id = 0;
                m_xs = 0.0f;
                m_ys = 0.0f;
                m_zs = 0.0f;
                m_ze = 0.0f;
                m_xe = 0.0f;
                m_ye = 0.0f;
            }

            public CADLine(Point p1, Point p2)
            {
                m_id = 0;
                m_xs = (float)p1.X;
                m_ys = (float)p1.Y;
                m_xe = (float)p2.X;
                m_ye = (float)p2.Y;
                m_zs = 0;
                m_ze = 0;
            }

            public CADLine(double xs, double ys, double xe, double ye)
            {
                m_id = 0;
                m_xs = (float)xs;
                m_ys = (float)ys;
                m_zs =0;
                m_xe = (float)xe;
                m_ye = (float)ye;
                m_ze = 0;
            }

            public CADLine(double xs, double ys, double zs, double xe, double ye, double ze)
            {
                m_id = 0;
                m_xs = (float)xs;
                m_ys = (float)ys;
                m_zs = (float)zs;
                m_xe = (float)xe;
                m_ye = (float)ye;
                m_ze = (float)ze;
            }

            public CADLine(CADPoint p1,CADPoint p2)
            {
                m_id = 0;
                m_xs = p1.m_x;
                m_ys = p1.m_y;
                m_zs = p1.m_z;
                m_xe = p2.m_x;
                m_ye = p2.m_y;
                m_ze = p2.m_z;
            }
            public CADLine Copy()
            {
                CADLine result = new CADLine(m_xs, m_ys, m_zs, m_xe, m_ye, m_ze);
                result.m_id = m_id;
                return result;
            }

            public static implicit operator CADRect(CADLine value)//implicit隐式转换，explicit显式转换
            {
                checked
                {
                    if (value == null)
                        return null;
                    CADRect result = new CADRect(value.m_xs, value.m_ys, value.m_xe, value.m_ye);
                    result.m_id = value.m_id;
                    return result;
                }
            }
        }

        public class CADRect
        {
            public int m_id { get; set; }
            public float m_xs { get; set; }
            public float m_ys { get; set; }
            public float m_xe { get; set; }
            public float m_ye { get; set; }
            public float m_len { get; set; }
            public int m_flag { get; set; }//梁柱标志，0表示梁，1表示柱
            public float m_width { get; set; }
            public float m_height { get; set; }
            public string m_rebar { get; set; }//钢筋布置索引，编号意味着对应1好钢筋图
            public int m_concrete { get; set; }//混凝土等级索引，需要预定义好
            public CADRect()
            {

                m_id = 0;
                m_xs = 0.0f;
                m_ys = 0.0f;
                m_xe = 0.0f;
                m_ye = 0.0f;
                m_len = 0.0f;
                m_flag = 1;
                m_rebar = "";
                m_concrete = 0;
                m_width = Math.Abs(m_xs - m_xe);
                m_height = Math.Abs(m_ys - m_ye);
            }

            public CADRect(Point p1, Point p2, int flag = 1)
            {

                m_id = 0;
                m_xs = (float)p1.X;
                m_ys = (float)p1.Y;
                m_xe = (float)p2.X;
                m_ye = (float)p2.Y;
                m_len = 0.0f;
                m_flag = flag;
                m_rebar = "";
                m_concrete = 0;
                m_width = Math.Abs(m_xs - m_xe);
                m_height = Math.Abs(m_ys - m_ye);
            }

            public CADRect(double xs, double ys, double xe, double ye, int flag = 1)
            {

                m_id = 0;
                m_xs = (float)xs;
                m_ys = (float)ys;
                m_xe = (float)xe;
                m_ye = (float)ye;
                m_len = 0.0f;
                m_flag = flag;
                m_rebar = "";
                m_concrete = 0;
                m_width = Math.Abs(m_xs - m_xe);
                m_height = Math.Abs(m_ys - m_ye);
            }

            public void UpdataWH()
            {
                m_width = Math.Abs(m_xs - m_xe);
                m_height = Math.Abs(m_ys - m_ye);
            }

            public CADRect Copy()
            {
                CADRect result = new CADRect(m_xs, m_ys, m_xe, m_ye);
                result.m_len = m_len;
                result.m_id = m_id;
                result.m_flag = m_flag;
                result.m_rebar = m_rebar;
                result.m_concrete = m_concrete;
                return result;
            }

            public static implicit operator CADLine(CADRect value)//implicit隐式转换，explicit显式转换
            {
                checked
                {
                    if (value == null)
                        return null;
                    CADLine result = new CADLine(value.m_xs, value.m_ys, value.m_xe, value.m_ye);
                    result.m_id = value.m_id;

                    return result;
                }
            }

        }




        public class CADPoint
        {
            public int m_id { get; set; }
            public float m_x { get; set; }
            public float m_y { get; set; }
            public float m_z { get; set; }
            public int m_is_rebar { get; set; }
            public int m_diameter { get; set; }
            public int m_strength { get; set; }
            public int m_count { get; set; }
            public int m_style { get; set; }//0正常点，1辅助点
            public CADPoint()
            {
                m_id = 0;
                m_x = 0.0f;
                m_y = 0.0f;
                m_z = 0.0f;
                m_is_rebar = 0;
                m_diameter = -1;
                m_strength = -1;
                m_count = 0;
                m_style = 0;

            }


            public CADPoint(double x, double y, double z = 0)
            {
                m_id = 0;
                m_x = (float)x;
                m_y = (float)y;
                m_z = (float)z;
                m_is_rebar = 0;
                m_diameter = -1;
                m_strength = -1;
                m_count = 0;
                m_style = 0;

            }

            public CADPoint Copy()
            {
                CADPoint result = new CADPoint(m_x, m_y, m_z);
                result.m_id = m_id;
                result.m_is_rebar = m_is_rebar;
                result.m_diameter = m_diameter;
                result.m_strength = m_strength;
                result.m_count = m_count;
                result.m_style = m_style;
                return result;
            }

            public static CADPoint operator/(CADPoint src, double divide)
            {
                return new CADPoint(src.m_x/divide, src.m_y / divide, src.m_z / divide);
            }

            //public static CADLine operator -(CADPoint a, CADPoint b)
            //{
            //    return new CADLine(a,b);
            //}
        }


        public class Rect3D
        {
            public int m_id = 0;
            public CADPoint[] m_points = new CADPoint[4];
            public Rect3D()
            {

            }
            public Rect3D(CADPoint[] points)
            {
                int len = points.Length;
                if (len != 4)
                    return;
                for (int i = 0; i < len; i++)
                {
                    m_points[i] = points[i].Copy();
                }
            }

            public Rect3D(CADPoint point1, CADPoint point2, CADPoint point3, CADPoint point4)
            {
                m_points[0] = point1.Copy();
                m_points[1] = point2.Copy();
                m_points[2] = point3.Copy();
                m_points[3] = point4.Copy();
            }

            public Rect3D Copy()
            {
                Rect3D result = new Rect3D(m_points);
                result.m_id = m_id;
                return result;
            }
        }

        public class CADCube
        {
            public Rect3D[] m_surfs = null;
            public int m_id = 0;

            public CADCube()
            {
                m_surfs = new Rect3D[6];
                for (int i = 0; i < 6; i++)
                    m_surfs[i] = new Rect3D();
            }
            public CADCube(Rect3D[] surfs)
            {
                int len = surfs.Length;
                if (len != 6)
                    return;
                for (int i = 0; i < len; i++)
                    m_surfs[i] = surfs[i].Copy();
            }
            public CADCube(Rect3D surf1, Rect3D surf2, Rect3D surf3, Rect3D surf4, Rect3D surf5, Rect3D surf6)
            {
                m_surfs[0] = surf1.Copy();
                m_surfs[1] = surf2.Copy();
                m_surfs[2] = surf3.Copy();
                m_surfs[3] = surf4.Copy();
                m_surfs[4] = surf5.Copy();
                m_surfs[5] = surf6.Copy();

            }
            public CADCube Copy()
            {
                CADCube result = new CADCube();
                result.m_id = this.m_id;
                result.m_surfs = this.m_surfs;
                return result;
            }
        }



        public class CADRGB
        {
            public float m_r = 0.0f;
            public float m_g = 0.0f;
            public float m_b = 0.0f;
            public CADRGB()
            { }
            public CADRGB(double r, double g, double b)
            {
                m_r = (float)r;
                m_g = (float)g;
                m_b = (float)b;
            }

            public CADRGB Copy()
            {

                return new CADRGB(m_r, m_g, m_b);
            }
        }
    }
}

