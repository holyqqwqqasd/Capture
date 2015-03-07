using System;
using System.Windows.Forms;

using OpenCvSharp;
using OpenCvSharp.Extensions;

using AForge.Video.DirectShow;
using System.Diagnostics;

namespace Capture
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        IplImage frame = null;
        IplImage src = null;
        CvCapture capture = null;

        FilterInfoCollection videoDevices; //Список подключенных видео-устройств
        int NumDev = 0; //Номер выбранного видео-устройства 
        bool Stop = false; //Флаг остановки "вечного" цикла

        CvColor[] colors = new CvColor[]{
                new CvColor(0,0,255),
                new CvColor(0,128,255),
                new CvColor(0,255,255),
                new CvColor(0,255,0),
                new CvColor(255,128,0),
                new CvColor(255,255,0),
                new CvColor(255,0,0),
                new CvColor(255,0,255),
            };

        const double ScaleFactor = 1.0850;
        const int MinNeighbors = 2;


        private void Form1_Load(object sender, EventArgs e)
        {

            try
            {
                //Создать список видео-устройств
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count == 0)
                {
                    throw new Exception();
                }

                for (int i = 0; i < videoDevices.Count; i++)
                {
                    string cameraName = i + " : " + videoDevices[i].Name;
                    camera1Combo.Items.Add(cameraName);
                }

                camera1Combo.SelectedIndex = 0;
                NumDev = camera1Combo.SelectedIndex;
            }
            catch
            {
                btConnect.Enabled = false;

                camera1Combo.Items.Add("No cameras found");

                camera1Combo.SelectedIndex = 0;

                camera1Combo.Enabled = false;
            }
        }

        private void btConnect_Click(object sender, EventArgs e)
        {
            try
            {
                capture = new CvCapture(NumDev);
            }
            //            catch (TypeInitializationException ex)     //Определение ошибок
            //           { Console.WriteLine(ex.InnerException); }  //инициализации библиотек (Dll)
            catch
            {
                MessageBox.Show("[!] Error: Can't open camera.");
            }

            if (capture == null) //Не удалось соединиться с камерой !
                return;

            Stop = false;
            btStop.Enabled = true;

            double width = capture.GetCaptureProperty(CaptureProperty.FrameWidth);
            double height = capture.GetCaptureProperty(CaptureProperty.FrameHeight);

            tbOut.AppendText("[i] Подключение к камере:\r\n     " + camera1Combo.Items[NumDev] + "\r\n");
            tbOut.AppendText("[i] width: " + width + "\r\n");
            tbOut.AppendText("[i] height: " + height + "\r\n");

            while (true)
            {
                capture.GrabFrame(); //Захватываем кадр
                frame = capture.RetrieveFrame();
                using (IplImage gray = new IplImage(frame.Size, BitDepth.U8, 1))
                {
                    Cv.CvtColor(frame, gray, ColorConversion.BgrToGray); //Преобразование img в тоны серого gray
                    //                    Cv.Resize(gray, smallImg, Interpolation.Linear);   //Уменьшение размеров
                    Cv.EqualizeHist(gray, gray);  //Повышение контрастности методом гистограмм

                    using (var cascade = CvHaarClassifierCascade.FromFile("Data/haarcascade_frontalface_default.xml"))
                    using (var storage = new CvMemStorage())
                    {
                        storage.Clear();
                        
                        //Метод Виолы-Джонса для картинки в тонах серого
                        CvSeq<CvAvgComp> faces = Cv.HaarDetectObjects(gray, cascade, storage, ScaleFactor, 2, 0, new CvSize(30, 30));

                        // Обвод окружностями областей с обнаруженными лицами 
                        for (int i = 0; i < faces.Total; i++)
                        {
                            CvRect r = faces[i].Value.Rect;
                            /*CvPoint center = new CvPoint
                            {
                                X = Cv.Round((r.X + r.Width * 0.5)),
                                Y = Cv.Round((r.Y + r.Height * 0.5))
                            };
                            int radius = Cv.Round((r.Width + r.Height) * 0.25 );
                            frame.Circle(center, radius, colors[i % 8], 3, LineType.AntiAlias, 0);*/
                            frame.Rectangle(r, colors[0]);
                        }
                    }
                    pictureBox1.Image = BitmapConverter.ToBitmap(frame);   //pictureBox1.Image = BitmapConverter.ToBitmap(capture.RetrieveFrame());
                    pictureBox1.Refresh();

                    Application.DoEvents();
                    if (Stop) break;
                }
            }
        }

        private void camera1Combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            NumDev = camera1Combo.SelectedIndex;
        }

        private void btStop_Click(object sender, EventArgs e)
        {
            Stop = true;
            btStop.Enabled = false;
        }

    }
}
