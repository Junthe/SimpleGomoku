using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace SimpleGomoku
{
    public partial class Form1 : Form
    {
        int[,] board = new int[30,30];
        int boardSize = 15; //棋盘大小
        int gridSize = 30; //格子大小
        Point startPoint = new Point(30, 30);  //棋盘起始点

        Process process = new Process();
        StreamWriter input; //输入数据流

        bool isBlack;  // 是否选择黑棋
        bool isHumanTurn;  //是否是玩家的回合
        int gameState = 0; //0：未开始，1：进行中， 2：结束

        string receivedText="";


        public Form1()
        {
            InitializeComponent();
            process.StartInfo.FileName = @"pbrain.exe";
            process.StartInfo.UseShellExecute = false;  //自定义shell
            process.StartInfo.CreateNoWindow = true;  //避免显示命令行窗口
            process.StartInfo.RedirectStandardInput = true;  //重定向标准输入
            process.StartInfo.RedirectStandardOutput = true;  //重定向标准输出

            //数据接收
            process.OutputDataReceived += new DataReceivedEventHandler(process_OutputDataReceived);

            process.Start();
            input = process.StandardInput;  //重定向输入
            process.BeginOutputReadLine();  //开始监控输出（异步读取）
        }

        void process_OutputDataReceived(object sender, DataReceivedEventArgs e)  //读取命令行响应
        {
            update(e.Data + Environment.NewLine);
        }

        delegate void updateDelegate(string msg);  //新建委托实现两个进程的数据交换
        void update(string msg)
        {
            if (this.InvokeRequired)
                Invoke(new updateDelegate(update), new object[] { msg });
            else
            {
                receivedText = msg;
                
                Regex regex = new Regex(@"\d+,\d+");
                if (isHumanTurn == false && regex.IsMatch(receivedText))  //判断接受的信息是否为计算机出棋
                {
                    Regex r1 = new Regex(@"\d+");
                    int[] num = new int[2];
                    int i = 0;

                    MatchCollection mc = r1.Matches(receivedText);
                    foreach (Match m in mc)
                    {
                        num[i] = int.Parse(m.Groups[0].Value);
                        i++;
                    }

                    board[num[0], num[1]] = 2;
                    paintChessPieces(startPoint.X + num[0] * gridSize, startPoint.Y + num[1] * gridSize, isBlack, isHumanTurn);
                    label2.Text = String.Format("计算机的上一步是：{0},{1}", num[0], num[1]);
                    label1.Text = "你的回合";
                    isHumanTurn = true;

                    if(isWin(num[0], num[1], 2))
                    {
                        gameState = 3;
                        label1.Text = "你输了!!!";
                    }
                }
                
            }
        }


        private void start_Click(object sender, EventArgs e)
        {
            if(gameState == 0)
            {
                gameState = 1;
                input.WriteLine("start {0}", boardSize);

                if (rBlack.Checked)
                {
                    isBlack = true;
                    isHumanTurn = true;
                    label1.Text = "你的回合";
                }
                else
                {
                    isBlack = false;
                    isHumanTurn = false;
                    input.WriteLine("begin");
                    label1.Text = "计算机的回合";
                }   
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g1 = this.CreateGraphics();
            Pen p1 = new Pen(Color.Black, 1);

            float length = boardSize * gridSize;  //绘制棋盘
            for(int i = 0; i<=boardSize; i++)
            {
                g1.DrawLine(p1, startPoint.X + gridSize * i, startPoint.Y, startPoint.X + gridSize * i, startPoint.Y + length);
                g1.DrawLine(p1, startPoint.X , startPoint.Y + gridSize * i, startPoint.X + length, startPoint.Y + gridSize * i);
            }

            p1 = new Pen(Color.Red, 3);  //绘制边框
            g1.DrawRectangle(p1, startPoint.X, startPoint.Y, length, length);      
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            input.WriteLine("restart");
            board = new int[30,30];
            gameState = 0;
            label1.Text = "";
            label2.Text = "计算机的上一步是：";
            label3.Text = "你的上一步是：";
            Invalidate();
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && gameState == 1 && isHumanTurn == true)
            {
                Point p = e.Location;
                float length = boardSize * gridSize;
                if (p.X >= startPoint.X && p.X <= startPoint.Y + length && p.Y >= startPoint.Y && p.Y < startPoint.Y + length)
                {
                    //绘制玩家棋子
                    int tempX = (int)((p.X - startPoint.X) / gridSize);
                    int tempY = (int)((p.Y - startPoint.Y) / gridSize);
                    if(board[tempX, tempY] == 0)
                    {
                        board[tempX, tempY] = 1;
                        paintChessPieces(startPoint.X + tempX * gridSize, startPoint.Y + tempY * gridSize, isBlack, isHumanTurn);
                        label3.Text = String.Format("你的上一步是：{0},{1}", tempX, tempY);
                        isHumanTurn = false;
                        label1.Text = "计算机的回合";
                        input.WriteLine("turn {0},{1}", tempX, tempY);

                        if(isWin(tempX, tempY, 1))
                        {
                            gameState = 3;
                            label1.Text = "你赢了!!!";
                        }
                    }
                    
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void paintChessPieces(float x, float y, bool isblack, bool ishumanturn)
        {
            Graphics g1 = this.CreateGraphics();
            if (isblack ^ ishumanturn)
            {
                Pen p1 = new Pen(Color.Blue, 2);
                g1.DrawEllipse(p1, x, y, gridSize, gridSize);
            }
            else
            {
                Brush b1 = new SolidBrush(Color.Black);
                g1.FillEllipse(b1, x, y, gridSize, gridSize);
            }

        }


        private void paintWinResult(float x, float y)
        {
            Graphics g1 = this.CreateGraphics();
            Brush b1 = new SolidBrush(Color.Red);
            g1.FillEllipse(b1, startPoint.X+ x*gridSize + gridSize/2 - 3, startPoint.Y + y*gridSize + gridSize/2 -3, 6, 6);
        }

        private bool isWin(int x, int y, int color)
        {
            int count;
            int posX = 0;
            int posY = 0;
            int[] stackX = new int[20];
            int[] stackY = new int[20];
            /*
             *判断水平方向上的胜负
             *将水平方向以传入的点x上的y轴作为分隔线分为两部分
             */
            count = 1;
            stackX[count] = x;
            stackY[count] = y;
            //向左遍历
            for (posX = x - 1; posX >= 0; posX--)
            {
                if (board[posX, y] == color)
                {
                    count++;
                    stackX[count] = posX;
                    stackY[count] = y;
                    if (count >= 5)
                    {
                        for (int i = 1; i <= count; i++)
                            paintWinResult(stackX[i], stackY[i]);
                        return true;
                    }
                }
                else
                {
                    break;
                }
            }
            //向右边遍历
            for (posX = x + 1; posX < gridSize; posX++)
            {
                if (board[posX,y] == color)
                {
                    count++;
                    stackX[count] = posX;
                    stackY[count] = y;
                    if (count >= 5)
                    {
                        for(int i = 1; i<=count; i++)
                            paintWinResult(stackX[i], stackY[i]);

                        return true;
                    }
                }
                else
                {
                    break;
                }
            }


            /*
             *判断垂直方向上的胜负
             *将垂直方向以传入的点y上的x轴作为分隔线分为两部分
             */
            count = 1;
            //向上遍历
            for (posY = y - 1; posY >= 0; posY--)
            {
                if (board[x,posY] == color)
                {
                    count++;
                    stackX[count] = x;
                    stackY[count] = posY;
                    if (count >= 5)
                    {
                        for (int i = 1; i <= count; i++)
                            paintWinResult(stackX[i], stackY[i]);
                        return true;
                    }
                }
                else
                {
                    break;
                }
            }
            //向下遍历
            for (posY = y + 1; posY < gridSize; posY++)
            {
                if (board[x,posY] == color)
                {
                    count++;
                    stackX[count] = x;
                    stackY[count] = posY;
                    if (count >= 5)
                    {
                        for (int i = 1; i <= count; i++)
                            paintWinResult(stackX[i], stackY[i]);
                        return true;
                    }
                }
                else
                {
                    break;
                }
            }

            /*
             *判断左上右下方向上的胜负
             *以坐标点为分割线，将棋盘分为左右两个等腰三角形
             */
            count = 1;
            //判断左边的
            for (posX = x - 1, posY = y - 1; posX >= 0 && posY >= 0; posX--, posY--)
            {
                if (board[posX,posY] == color)
                {
                    count++;
                    stackX[count] = posX;
                    stackY[count] = posY;
                    if (count >= 5)
                    {
                        for (int i = 1; i <= count; i++)
                            paintWinResult(stackX[i], stackY[i]);
                        return true;
                    }
                }
                else
                {
                    break;
                }
            }
            //判断右边的
            for (posX = x + 1, posY = y + 1; posX < boardSize && posY < boardSize; posX++, posY++)
            {
                if (board[posX,posY] == color)
                {
                    count++;
                    stackX[count] = posX;
                    stackY[count] = posY;
                    if (count >= 5)
                    {
                        for (int i = 1; i <= count; i++)
                            paintWinResult(stackX[i], stackY[i]);
                        return true;
                    }
                }
                else
                {
                    break;
                }
            }
            /*
             *判断右下左下方向上的胜负
             *以坐标点为分割线，将棋盘分为左右两个等腰三角形 
             */
            count = 1;
            //先判断左边的
            for (posX = x + 1, posY = y - 1; posX < boardSize && posY >= 0; posX++, posY--)
            {
                if (board[posX,posY] == color)
                {
                    count++;
                    stackX[count] = posX;
                    stackY[count] = posY;
                    if (count >= 5)
                    {
                        for (int i = 1; i <= count; i++)
                            paintWinResult(stackX[i], stackY[i]);
                        return true;
                    }
                }
                else
                {
                    break;
                }
            }
            //判断右下的
            for (posX = x - 1, posY = y + 1; posX >= 0 && posY < boardSize; posX--, posY++)
            {
                if (board[posX,posY] == color)
                {
                    count++;
                    stackX[count] = posX;
                    stackY[count] = posY;
                    if (count >= 5)
                    {
                        for (int i = 1; i <= count; i++)
                            paintWinResult(stackX[i], stackY[i]);
                        return true;
                    }
                }
                else
                {
                    break;
                }
            }
            return false;
        }

    }
}
