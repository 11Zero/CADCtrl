# CADCtrl
基于WPF的动态链接库形式的CAD控件
## 使用说明 ##
1. 解决方案添加CADCtrl.dll引用
2. 在所需添加控件的窗体中using CADCtrl
3. 在所需添加控件的窗体xaml布局文件头部Window属性中添加xmlns:CAD="clr-namespace:CADCtrl;assembly=CADCtrl"
4. 在窗体中的Grid或其他容器内添加<CAD:CADView HorizontalAlignment="Left" x:Name="CADctrl_frame" VerticalAlignment="Top" Height="466" Width="663" Margin="315,0,0,0" />，上述布局属性可根据控件父类UserControl属性来修改。
## 接口列表 ##
- public void UserDrawLine(Point p1, Point p2, int color_id = 0)
- public void UserDrawLine(CADLine line, int color_id = 0)
- public void UserDelAllLines()
- public void UserDelLine(int line_id)
- public void UserDelAllRects()
- public void UserDelRect(int rect_id)
- public void UserDelPoint(int point_id)
- public void UserDelAllPoints()
- public bool UserDrawRect(Point p1, Point p2, int color_id = 0)
- public bool UserDrawRect(CADRect rect, int color_id = 0)
- public void UserDrawPoint(CADPoint point, int color_id = 0)
- public void UserSelLine(int id)
- public void UserSelRect(int id)
- public void UserSelPoint(int id)
- public int[] UserGetSelLines()
- public int[] UserGetSelRects()
- public int[] UserGetSelPoints()
- public Dictionary<int, CADLine> UserGetLines()
- public Dictionary<int, CADRect> UserGetRects()
- public Dictionary<int, CADPoint> UserGetPoints()