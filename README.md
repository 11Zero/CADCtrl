# CADCtrl
基于WPF的动态链接库形式的CAD控件

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