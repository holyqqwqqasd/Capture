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

        private CvSeq<CvAvgComp> GetFaces(IplImage img)
        {
            using (IplImage gray = new IplImage(img.Size, BitDepth.U8, 1))
            {
                Cv.CvtColor(img, gray, ColorConversion.BgrToGray); //Преобразование img в тоны серого gray
                //                    Cv.Resize(gray, smallImg, Interpolation.Linear);   //Уменьшение размеров
                Cv.EqualizeHist(gray, gray);  //Повышение контрастности методом гистограмм

                using (var cascade = CvHaarClassifierCascade.FromFile("Data/haarcascade_frontalface_default.xml"))
                using (var storage = new CvMemStorage())
                {
                    storage.Clear();

                    //Метод Виолы-Джонса для картинки в тонах серого
                    CvSeq<CvAvgComp> faces = Cv.HaarDetectObjects(gray, cascade, storage, ScaleFactor, 2, HaarDetectionType.FindBiggestObject, new CvSize(30, 30));

                    return faces;
                }
            }
        }

        private IplImage GetMiddleFrame(CvCapture capture, int frames)
        {
            // Промежуточные компонентные изображения
		    IplImage imgRed;
		    IplImage imgGreen;
		    IplImage imgBlue;
		
            int width = (int) capture.GetCaptureProperty(CaptureProperty.FrameWidth);
            int height = (int) capture.GetCaptureProperty(CaptureProperty.FrameHeight);

		    CvSize size = new CvSize(width, height);
		
		    imgRed = new IplImage(size, BitDepth.U8, 1);
		    imgGreen = new IplImage(size, BitDepth.U8, 1);
		    imgBlue = new IplImage(size, BitDepth.U8, 1);
		
		    // Создаем структуры для окончательной картинки
		    IplImage imgResultRed = new IplImage(size, BitDepth.U8, 1);
		    IplImage imgResultGreen = new IplImage(size, BitDepth.U8, 1);
		    IplImage imgResultBlue = new IplImage(size, BitDepth.U8, 1);
		
		    IplImage imgResult = new IplImage(size, BitDepth.U8, 3);
		
		    // В этих массивах будем суммировать компонентные веса
		    int[,] theSumRed = new int[width, height];
            int[,] theSumGreen = new int[width, height];
            int[,] theSumBlue = new int[width, height];

		    // Инициализируем массивы 0
		    for (int x = 0; x < width; x++){
			    for (int y = 0; y < height; y++){
				    theSumBlue[x, y] = 0;
				    theSumGreen[x, y] = 0;
				    theSumRed[x, y] = 0;
			    }
		    }

            for (int i = 0; i < frames; i++)
            {
                // Захватываем кадр
                frame = capture.QueryFrame();
                // Разделяем кадр на отдельные RGB компоненты
                Cv.Split(frame, imgRed, imgGreen, imgBlue, null);

                // поканально суммируем значения для каждой точки изображения
                for (int y = 0; y < size.Height; y++)
                {
                    for (int x = 0; x < size.Width; x++)
                    {
                        theSumRed[x, y] += (int) Cv.GetReal2D(imgRed, y, x);
                        theSumGreen[x, y] += (int) Cv.GetReal2D(imgGreen, y, x);
                        theSumBlue[x, y] += (int) Cv.GetReal2D(imgBlue, y, x);
                    }
                }
            }

            for (int y = 0; y < size.Height; y++)
            {
                for (int x = 0; x < size.Width; x++)
                {
                    // Находим среднее значение
                    theSumRed[x, y] = (int) ((float)theSumRed[x, y] / (float)frames);
                    theSumGreen[x, y] = (int) ((float)theSumGreen[x, y] / (float)frames);
                    theSumBlue[x, y] = (int) ((float)theSumBlue[x, y] / (float)frames);
                    // Создаём компоненты для итоговой картинки
                    Cv.SetReal2D(imgResultRed, y, x, theSumRed[x, y]);
                    Cv.SetReal2D(imgResultGreen, y, x, theSumGreen[x, y]);
                    Cv.SetReal2D(imgResultBlue, y, x, theSumBlue[x, y]);
                }
            }

            // Объединяем каналы в окончательную картинку
            Cv.Merge(imgResultRed, imgResultGreen, imgResultBlue, null, imgResult);

            /*if (clarity) {
				float kernel[9];
				//float	kernel[9] = {0.1111, -0.8889, 0.1111, -0.8889, 4.1111, -0.8889, 0.1111, -0.8889, 0.1111};
						
				// Ядро свёртки для увеличения чёткости
				kernel[0] = -0.1;
				kernel[1] = -0.1;
				kernel[2] = -0.1;
						
				kernel[3] = -0.1;	// [-0.1] [-0.1] [-0.1]
				kernel[4] = 2;		// [-0.1] [  2\1.8 ] [-0.1] 
				kernel[5] = -0.1;	// [-0.1] [-0.1] [-0.1]
						
				kernel[6] = -0.1;
				kernel[7] = -0.1;
				kernel[8] = -0.1;
				// Матрица
				CvMat kernel_matrix = cvMat(3,3,CV_32FC1,kernel);
				// Рабочая копия кадра
				IplImage *src = NULL;
				src = cvCloneImage(imgResult);
				// Повышаем чёткость
				cvFilter2D(src, imgResult, &kernel_matrix, cvPoint(-1,-1));
				cvReleaseImage(&src);
			}*/
            return imgResult;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                capture = new CvCapture(NumDev);
            }
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

            IplImage tmp = null;
            IplImage silh = null;
            IplImage pre_frame = null;
            bool moving = false;
            bool pre_moving = false;
            bool getfaces = false;

            while (true)
            {
                capture.GrabFrame(); //Захватываем кадр
                frame = capture.RetrieveFrame();

                if (pre_frame == null && silh == null)
                {
                    silh = new IplImage(frame.Size, frame.Depth, frame.NChannels);
                    pre_frame = frame.Clone();
                    continue;
                }

                using (IplImage gray = new IplImage(frame.Size, BitDepth.U8, 1))
                {
                    Cv.AbsDiff(frame, pre_frame, silh);
                    Cv.CvtColor(silh, gray, ColorConversion.BgrToGray); //Преобразование img в тоны серого gray
                    double count = Cv.Threshold(gray, gray, 100, 255, ThresholdType.Binary);

                    moving = false;
                    for (int x = 0; x < gray.Size.Width; x++)
                    {
                        for (int y = 0; y < gray.Size.Height; y++)
                        {
                            CvScalar se = gray.Get2D(y, x);
                            if (se.Val0 > 0)
                            {
                                moving = true;
                                break;
                            }
                        }
                        if (moving) break;
                    }
                    if (pre_moving == moving)
                    {
                        getfaces = moving;
                        Text = "Движение: " + moving;
                    }
                    pre_moving = moving;

                    if (getfaces) {
                        tmp = frame.Clone();

                        CvSeq<CvAvgComp> faces = GetFaces(tmp);

                        for (int i = 0; i < faces.Total; i++)
                        {
                            CvRect r = faces[i].Value.Rect;
                            tmp.Rectangle(r, colors[0]);
                        }

                        pictureBox1.Image = BitmapConverter.ToBitmap(tmp);
                        pictureBox1.Refresh();
                    }

                    pre_frame.Dispose();
                    pre_frame = frame.Clone();

                    Application.DoEvents();
                    if (Stop) break;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                capture = new CvCapture(NumDev);
            }
            catch
            {
                MessageBox.Show("[!] Error: Can't open camera.");
            }

            if (capture == null) //Не удалось соединиться с камерой !
                return;

            Stop = false;
            btStop.Enabled = true;

            tbOut.AppendText("[i] Подключение к камере:\r\n     " + camera1Combo.Items[NumDev] + "\r\n");

            IplImage frame = null;

            while (!Stop)
            {
                if (frame != null)
                {
                    frame.Dispose();
                    frame = null;
                }

                frame = GetMiddleFrame(capture, 7);

                pictureBox1.Image = BitmapConverter.ToBitmap(frame);
                pictureBox1.Refresh();

                Application.DoEvents();
            }
        }

    }
}
