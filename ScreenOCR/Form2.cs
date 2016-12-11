using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenOCR
{
    public partial class Form2 : Form
    {
        Boolean hasMouse;
        Point originalPoint = new Point();
        Point lastPoint = new Point();

        Bitmap screenBitmap;
        Bitmap backgroundBitmap;

        public Form2()
        {
            InitializeComponent();

            // 画面全体をコピーする
            screenBitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height);
            Graphics g = Graphics.FromImage(screenBitmap);
            g.CopyFromScreen(new Point(0, 0), new Point(0, 0), screenBitmap.Size);
            g.Dispose();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            hasMouse = false;

            backgroundBitmap = MakeGrayscaleBitmap(screenBitmap);
            this.BackgroundImage = backgroundBitmap;
            this.TopMost = true;
        }

        private void Form2_FormClosed(object sender, FormClosedEventArgs e)
        {
            screenBitmap.Dispose();
        }

        private void Form2_MouseDown(Object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Make a note that we "have the mouse".
                hasMouse = true;
                // Store the "starting point" for this rubber-band rectangle.
                originalPoint.X = e.X;
                originalPoint.Y = e.Y;
                // Special value lets us know that no previous
                // rectangle needs to be erased.
                lastPoint.X = -1;
                lastPoint.Y = -1;
            }
        }

        private void Form2_MouseUp(Object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Set internal flag to know we no longer "have the mouse".
                hasMouse = false;
                // If we have drawn previously, draw again in that spot
                // to remove the lines.
                if (lastPoint.X != -1)
                {
                    Point ptCurrent = new Point(e.X, e.Y);
                    DrawTargetRect(originalPoint, lastPoint);
                }

                // なぜか1回のマウスアップでMyMouseUp()が2回呼ばれるので
                // ptOriginal.X を使って二度呼ばれないようにする
                if (originalPoint.X != -1)
                {
                    int x = (originalPoint.X < e.X) ? originalPoint.X : e.X;
                    int y = (originalPoint.Y < e.Y) ? originalPoint.Y : e.Y;
                    int w = Math.Abs(e.X - originalPoint.X) + 1;
                    int h = Math.Abs(e.Y - originalPoint.Y) + 1;

                    // Set flags to know that there is no "previous" line to reverse.
                    lastPoint.X = -1;
                    lastPoint.Y = -1;
                    originalPoint.X = -1;
                    originalPoint.Y = -1;

                    Rectangle rect = new Rectangle(x, y, w, h);
                    Bitmap trimmedBitmap = screenBitmap.Clone(rect, screenBitmap.PixelFormat);

                    Form1 form1 = (Form1)this.Owner;
                    form1.CaptureBitmap(trimmedBitmap);

                    trimmedBitmap.Dispose();
                    trimmedBitmap = null;

                    form1.ParseImage();

                    this.Dispose();
                }
            }
        }

        private void Form2_MouseMove(Object sender, MouseEventArgs e)
        {
            Point currentPoint = new Point(e.X, e.Y);
            // If we "have the mouse", then we draw our lines.
            if (hasMouse)
            {
                // If we have drawn previously, draw again in
                // that spot to remove the lines.
                if (lastPoint.X != -1)
                {
                    DrawTargetRect(originalPoint, lastPoint);
                }
                // Update last point.
                lastPoint = currentPoint;
                // Draw new lines.
                DrawTargetRect(originalPoint, currentPoint);
            }
        }

        private void Form2_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.Dispose();
            }
        }

        private Bitmap MakeGrayscaleBitmap(Bitmap original)
        {
            // create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            // get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            // create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(new float[][] {
                new float[] {.3f, .3f, .3f, 0, 0},
                new float[] {.59f, .59f, .59f, 0, 0},
                new float[] {.11f, .11f, .11f, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0, 0, 0, 0, 1}
            });

            // create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            // set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            // draw the original image on the new image
            // using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            // dispose the Graphics object
            g.Dispose();

            return newBitmap;
        }

        // Convert and normalize the points and draw the reversible frame.
        private void DrawTargetRect(Point p1, Point p2)
        {
            Rectangle rc = new Rectangle();

            // Convert the points to screen coordinates.
            p1 = PointToScreen(p1);
            p2 = PointToScreen(p2);

            // Normalize the rectangle.
            if (p1.X < p2.X)
            {
                rc.X = p1.X;
                rc.Width = p2.X - p1.X;
            }
            else
            {
                rc.X = p2.X;
                rc.Width = p1.X - p2.X;
            }
            if (p1.Y < p2.Y)
            {
                rc.Y = p1.Y;
                rc.Height = p2.Y - p1.Y;
            }
            else
            {
                rc.Y = p2.Y;
                rc.Height = p1.Y - p2.Y;
            }

            DrawTargetRect(rc);
        }

        private void DrawTargetRect(Rectangle rc)
        {
            BufferedGraphicsContext bgc;
            bgc = BufferedGraphicsManager.Current;
            BufferedGraphics bgr;
            bgr = bgc.Allocate(this.CreateGraphics(), this.DisplayRectangle);
            Graphics g = bgr.Graphics;

            g.DrawImage(backgroundBitmap, 0, 0);

            //Brushオブジェクトの作成
            SolidBrush b = new SolidBrush(Color.FromArgb(128, 255, 0, 0));
            //作成したブラシを使って、(10,20)の位置に100x80サイズの長方形を描画する
            g.FillRectangle(b, rc);
            //リソースを解放する
            b.Dispose();

            // バッファからバッファに関連付けられた描画サーフェイスに転送 
            bgr.Render();

            bgr.Dispose();

            /*
            // 以下のコードだとちらつくのでバッファー度グラフィックスを使う
            // http://www.geocities.co.jp/NatureLand/2023/reference/Graphics/Graphics03n.html
              
            //ImageオブジェクトのGraphicsオブジェクトを作成する
            Graphics g = this.CreateGraphics();

            g.DrawImage(BackgroundBitmap, 0, 0);

            //Brushオブジェクトの作成
            SolidBrush b = new SolidBrush(Color.FromArgb(128, 255, 0, 0));
            //作成したブラシを使って、(10,20)の位置に100x80サイズの長方形を描画する
            g.FillRectangle(b, rc);
            //リソースを解放する
            b.Dispose();
            g.Dispose();
            */
        }
    }
}
