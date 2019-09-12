using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;

namespace myMineSweeper
{
    class mineSweeper
    {
        private int wNum, hNum, mNum;//地图长宽和雷的数量
        private byte[,] map;//生成地图时的原图
        private byte[,] kmap;//游戏时的显示图
        private bool[,] umap;//绘制时用来确认是否需要重绘
        public Size clientSize;//地图显示区域的大小
        private Bitmap tBmp;//临时存放绘制的结果
        private int pp=25;//绘图精度,25和picturebox长宽耦合
        private bool firstClick = true;
        private PictureBox pBox;
        private Color mainColor, secondColor;
        public int remainNum;
        
        enum mapStatus
        {
            empty=0,
            mine=99,
            marked=200,
            opened=201,
            unknown=255,
            //1~8刚好对应雷的数量
        }

        private bool start = false;
        public DateTime gameTime =DateTime.Parse("00:00:00");

        public mineSweeper(int wn,int hn,int mn,PictureBox pb)
        {
            wNum = wn;hNum = hn;mNum = mn;
            remainNum = mNum;
            pBox = pb;
            pBox.Location = new Point(0, 0);
            pBox.Width = wNum * 25;
            pBox.Height = hNum * 25;
            clientSize = pb.ClientSize;
            tBmp = new Bitmap(wNum * pp, hNum * pp);
            //tBmp绘制放大过的地图以保持精度
            mainColor = Color.Black;
            secondColor = Color.FromArgb(
                mainColor.A,
                255 - mainColor.R,
                255 - mainColor.G,
                255 - mainColor.B);
            initMap();
            drawField();
            draw();
            showPic();
        }
        private void initMap()
        {
            map = new byte[wNum, hNum];
            kmap = new byte[wNum, hNum];
            umap = new bool[wNum, hNum];
            for (int x = 0; x < wNum; ++x)
                for (int y = 0; y < hNum; ++y)
                {
                    map[x, y] = 0;
                    kmap[x, y] = (byte)mapStatus.unknown;
                    umap[x, y] = true;
                }
        }
        public void generateMap(Point firstClickBlock)
        {
            Point fcb = firstClickBlock;
            int gCount = 0;//已经生成的数量
            Random r=new Random();
            while (gCount < mNum)
            {
                int gx, gy;
                gx = r.Next(wNum);
                gy = r.Next(hNum);
                
                if ((gx >= fcb.X - 1) && (gx <= fcb.X + 1) &&
                    (gy >= fcb.Y - 1) && (gy <= fcb.Y + 1)) 
                    continue;//确认是否在初始位置一圈内

                if (map[gx, gy] == (byte)mapStatus.mine)
                    continue;//是否重复
                map[gx, gy] =(byte)mapStatus.mine;

                //给周围一圈计数
                for(int x = gx - 1; x <= gx + 1; ++x)
                    for(int y = gy - 1; y <= gy + 1; ++y)
                    {
                        if ((x < 0) || (x >= wNum) ||
                            (y < 0) || (y >= hNum))
                            continue;//不超出边界
                        //给周围的空白格子加上数字
                        if(map[x,y]<9)++map[x, y];
                    }
                ++gCount;
            }
        }
        private Point getBlock(Point pos)
        {
            //把实际的窗口坐标转换成地图格子坐标
            //数量x比例
            return new Point
            {
                X = wNum * pos.X / clientSize.Width,
                Y = hNum * pos.Y / clientSize.Height
            };
        }
        private void chainOpen(Point b)
        {
            //打开格子,空白处按照bfs连续打开
            if (map[b.X, b.Y] == (byte)mapStatus.mine)
            {
                fail(b);
                return;
            }

            Queue<Point> q = new Queue<Point>(wNum * hNum);
            int count = 0;//用来记录bfs每层的个数
            int lastCount = 0;
            bool[,] toOpen = new bool[wNum, hNum];
            toOpen.Initialize();//存放是不是马上要被打开

            q.Enqueue(b);lastCount = 1;
            while (lastCount!=0)
            {
                //把上一层的拿出来
                for(int i = 0; i < lastCount; ++i)
                {
                    Point p = q.Dequeue();
                    int x = p.X;
                    int y = p.Y;
                    umap[x, y] = true;
                    kmap[x, y] = (byte)mapStatus.opened;
                    //检查周围
                    for (int ex = x - 1; ex <= x + 1; ++ex) 
                        for(int ey = y - 1; ey <= y + 1; ++ey)
                        {
                            if ((ex < 0) || (ex >= wNum) ||
                                (ey < 0) || (ey >= hNum))
                                continue;
                            if (toOpen[ex, ey]) continue;
                            if ((ex == x) && (ey == y)) continue;
                            //把还没打开的打开
                            //检查有没有空格继续打开
                            if (kmap[ex, ey] == (byte)mapStatus.unknown)
                            {
                                switch (map[ex, ey])
                                {
                                    case (byte)mapStatus.empty:
                                        ++count;
                                        q.Enqueue(new Point(ex, ey));
                                        toOpen[ex, ey] = true;
                                        break;
                                    default:
                                        kmap[ex, ey] = (byte)mapStatus.opened;
                                        umap[ex, ey] = true;
                                        break;
                                }
                            }
                            
                        }
                }
                draw();
                showPic();
                lastCount = count;
                count = 0;
            }
        }
        public void leftClick(Point pos)
        {
            //左击用来打开单个格子
            //但是如果刚好点击到空白处
            //也需要连续打开
            Point pb = getBlock(pos);
            if (firstClick) {
                generateMap(pb);
                start = true;
                firstClick = false;
            }
            if(kmap[pb.X, pb.Y]== (byte)mapStatus.unknown)
            {
                switch (map[pb.X, pb.Y])
                {
                    case (byte)mapStatus.mine:
                        fail(pb);
                        break;
                    case (byte)mapStatus.empty:
                        chainOpen(pb);
                        break;
                    default:
                        umap[pb.X, pb.Y] = true;
                        kmap[pb.X, pb.Y] = (byte)mapStatus.opened;
                        draw();
                        showPic();
                        break;
                }
            }
        }
        public void rightClick(Point pos)
        {
            Point pb = getBlock(pos);
            switch(kmap[pb.X, pb.Y])
            {
                case (byte)mapStatus.unknown:
                    kmap[pb.X, pb.Y] = (byte)mapStatus.marked;
                    umap[pb.X, pb.Y] = true;
                    --remainNum;
                    if (remainNum == 0) winCheck();
                    break;
                case (byte)mapStatus.marked:
                    kmap[pb.X, pb.Y] = (byte)mapStatus.unknown;
                    umap[pb.X, pb.Y] = true;
                    ++remainNum;
                    break;
            }
            draw();
            showPic();
        }
        public void doubleClick(Point pos)
        {
            Point pb = getBlock(pos);
            Point[] q=new Point[9];
            int x = pb.X;
            int y = pb.Y;
            if (kmap[x, y] == (byte)mapStatus.opened)
            {
                //检查数字大小是否等于标记个数
                int mines = 0;
                int unknowns = 0;
                for (int ex = x - 1; ex <= x + 1; ++ex)
                    for (int ey = y - 1; ey <= y + 1; ++ey)
                    {
                        if ((ex < 0) || (ex >= wNum) ||
                            (ey < 0) || (ey >= hNum))
                            continue;
                        if ((ex == x) && (ey == y)) continue;
                        switch (kmap[ex, ey])
                        {
                            case (byte)mapStatus.marked:
                                q[mines] = new Point(ex, ey);
                                ++mines;
                                break;
                            case (byte)mapStatus.unknown:
                                ++unknowns;
                                break;
                        }
                    }
                //检查周围的标记是否正确
                if ((map[x, y] == mines) && (unknowns > 0)) 
                {
                    for(int c = 0; c < mines; ++c)
                    {
                        int ex = q[c].X;
                        int ey = q[c].Y;
                        if (map[ex, ey] != (byte)mapStatus.mine)
                        {
                            fail(pb);
                            return;
                        }
                    }
                    //无误就打开
                    chainOpen(pb);
                }
            }
        }
        private void fail(Point b)
        {
            Graphics gr = Graphics.FromImage(tBmp);
            Pen p = new Pen(Color.Red, pp / 20);
            //首先把所有格子检查一遍
            //有没有标记错,错的打x
            //打x的地方不要更新
            //否则会把x覆盖掉
            int ax, ay;
            for (int x = 0; x < wNum; ++x)
                for (int y = 0; y < hNum; ++y)
                {
                    if ((kmap[x, y] == (byte)mapStatus.marked)&&
                            (map[x,y]!= (byte)mapStatus.mine))
                    {
                        ax = x * pp;ay = y * pp;
                        gr.DrawLine(p, ax + 1, ay + 1, ax + pp - 1, ay + pp - 1);
                        gr.DrawLine(p, ax + pp - 1, ay + 1, ax + 1, ay + pp - 1);
                    }
                    else
                        umap[x, y] = true;
                    kmap[x, y] = (byte)mapStatus.opened;
                }
            draw();
            //然后把当前点击的地方也画×
            //umap[b.X, b.Y] = false;//attention
            ax = b.X * pp;ay = b.Y * pp;
            gr.DrawLine(p, ax + 1, ay + 1, ax + pp - 1, ay + pp - 1);
            gr.DrawLine(p, ax + pp - 1, ay + 1, ax + 1, ay + pp - 1);
            gr.Flush();
            showPic();
            start = false;
        }
        private void winCheck()
        {
            for (int x = 0; x < wNum; ++x)
                for (int y = 0; y < hNum; ++y)
                    //检查标记有没有错误
                    if (kmap[x, y] == (byte)mapStatus.marked &&
                        map[x, y] != (byte)mapStatus.mine) 
                    {
                        return;
                    }
            Graphics gr = Graphics.FromImage(tBmp);
            StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            Rectangle rect = new Rectangle(0, 0, tBmp.Width, tBmp.Height);
            gr.DrawString("Win!!!",
                new Font("Consolas",100, 0, GraphicsUnit.Pixel),
                new SolidBrush(mainColor), rect, sf);
            showPic();
            start = false;
        }
        private void drawField()
        {
            //画格子线
            Graphics gr = Graphics.FromImage(tBmp);
            for (int x = 0; x < wNum * pp; x += pp)
                gr.DrawLine(new Pen(mainColor, 1), x, 0, x, 0 + tBmp.Height);
            for (int y = 0; y < hNum * pp; y += pp)
                gr.DrawLine(new Pen(mainColor, 1), 0, y, 0 + tBmp.Width, y);
            gr.Flush();
        }
        private void draw()
        {
            //把kmap绘制成bitmap
            Graphics gr = Graphics.FromImage(tBmp);
            Brush b1 = new SolidBrush(mainColor);
            Brush b2 = new SolidBrush(secondColor);
            StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            Font ft = new Font("宋体", pp, 0, GraphicsUnit.Pixel);

            for (int x = 0; x < wNum; ++x) 
                for(int y = 0; y < hNum; ++y)
                    if (umap[x, y])
                    {
                        umap[x, y] = false;
                        int ax = x * pp;
                        int ay = y * pp;
                        Rectangle rect = new Rectangle(
                            ax + 1, ay + 1, pp - 1, pp - 1);
                        switch (kmap[x, y])
                        {
                            case (byte)mapStatus.unknown:
                                gr.FillRectangle(b1, rect);
                                break;
                            case (byte)mapStatus.marked:
                                gr.FillRectangle(b1, rect);
                                gr.DrawString("P", ft, b2, rect, sf);
                                break;
                            case (byte)mapStatus.opened:
                                switch (map[x, y])
                                {
                                    case (byte)mapStatus.empty:
                                        gr.FillRectangle(b2, rect);
                                        break;
                                    case (byte)mapStatus.mine:
                                        //仅在失败的时候才会绘制
                                        gr.FillRectangle(b2, rect);
                                        gr.FillEllipse(b1, rect);
                                        break;
                                    default:
                                        gr.FillRectangle(b2, rect);
                                        gr.DrawString(map[x, y].ToString(),
                                            ft, b1, rect, sf);
                                        break;
                                }
                                break;
                        }
                    }
            gr.Flush();
            showPic();
            pBox.Refresh();
        }
        private void showPic()
        {
            //把tbmp缩小,画到picturebox中
            Bitmap showbmp = new Bitmap(tBmp, clientSize.Width, clientSize.Height);
            pBox.Image = showbmp;
        }
        public DateTime tick()
        {
            if (start)
            {
                gameTime=gameTime.AddSeconds(1);
            }
            return gameTime;
        }
    }
}
