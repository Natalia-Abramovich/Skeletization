using System.Drawing;
using System.Windows.Forms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
//using Cloo;
//using OpenCL.Net;

namespace Skeletization
{
    public partial class Form1 : Form
    {
        private const int _sizeOfInt = 4;
        private readonly Bitmap _image;
        private ToolStripProgressBar _toolStripProgressBar;

        public Graphics ImageGraphics { get; private set; }
        public Bitmap BitmapToBlackWhite2(Bitmap src)
        {
            // 1.
            double treshold = 0.6;

            // 2.
            //int treshold = 150;

            Bitmap dst = new Bitmap(src.Width, src.Height);

            for (int i = 0; i < src.Width; i++)
            {
                for (int j = 0; j < src.Height; j++)
                {
                    // 1.
                    dst.SetPixel(i, j, src.GetPixel(i, j).GetBrightness() < treshold ? System.Drawing.Color.Black : System.Drawing.Color.White);

                    // 2 (пактически тоже, что 1).
                    //System.Drawing.Color color = src.GetPixel(i, j);
                    //int average = (int)(color.R + color.B + color.G) / 3;
                    //dst.SetPixel(i, j, average < treshold ? System.Drawing.Color.Black : System.Drawing.Color.White);
                }
            }

            return dst;
        }

        /*  private int[,] ExecuteZhangSuenAlgorithmOpenCV(int[,] data)
          {
              //Установка параметров, инициализирующих видеокарты при работе. В Platforms[1] должен стоять индекс
              //указывающий на используемую платформу
              ComputeContextPropertyList Properties = new ComputeContextPropertyList(ComputePlatform.Platforms[1]);
              ComputeContext Context = new ComputeContext(ComputeDeviceTypes.All, Properties, null, IntPtr.Zero);

              //Текст програмы, исполняющейся на устройстве (GPU или CPU). Именно эта программа будет выполнять паралельные
              //вычисления и будет складывать вектора. Программа написанна на языке, основанном на C99 специально под OpenCL.
              string passage = @" 
              __kernel void passage(__global __read_only int* input, __global int* output, int h_b, int w_b, int first)
              {
                  int i = get_global_id(0);
                  int y = i % h_b;
                  int x = i / h_b;
                  if (x > 0 && x < w_b - 1 && y > 0 && y < h_b - 1)
                  {
                      int sum = input[x * h_b + y - 1] + input[(x + 1) * h_b + y - 1] + input[(x + 1) * h_b + y] + input[(x + 1) * h_b + y + 1] + input[x * h_b + y + 1]
                                  + input[(x - 1) * h_b + y + 1] + input[(x - 1) * h_b + y] + input[(x - 1) * h_b + y - 1];
                      int p = 0;
                      p += input[x * h_b + y - 1] == 0 ? input[(x + 1) * h_b + y - 1] : 0;
                      p += input[(x + 1) * h_b + y - 1] == 0 ? input[(x + 1) * h_b + y] : 0;
                      p += input[(x + 1) * h_b + y] == 0 ? input[(x + 1) * h_b + y + 1] : 0;
                      p += input[(x + 1) * h_b + y + 1] == 0 ? input[x * h_b + y + 1] : 0;
                      p += input[x * h_b + y + 1] == 0 ? input[(x - 1) * h_b + y + 1] : 0;
                      p += input[(x - 1) * h_b + y + 1] == 0 ? input[(x - 1) * h_b + y] : 0;
                      p += input[(x - 1) * h_b + y] == 0 ? input[(x - 1) * h_b + y - 1] : 0;
                      if (first != 0)
                      {
                          if (sum >= 2 && sum <= 6 && p == 1 &&
                              input[x * h_b + y - 1] * input[(x + 1) * h_b + y] * input[x * h_b + y + 1] == 0 &&
                              input[(x + 1) * h_b + y] * input[x * h_b + y + 1] * input[(x - 1) * h_b + y] == 0 && input[x * h_b + y] != 1)
                          {
                              output[i] = 1;
                          }
                      }
                      else
                      {
                          if (sum >= 2 && sum <= 6 && p == 1 &&
                              input[x * h_b + y - 1] * input[(x + 1) * h_b + y] * input[(x - 1) * h_b + y] == 0 &&
                              input[x * h_b + y - 1] * input[x * h_b + y + 1] * input[(x - 1) * h_b + y] == 0 && input[x * h_b + y] != 1)
                          {
                              output[i] = 1;
                          }
                      }
                  }
              }
              ";

              //Список устройств, для которых мы будем компилировать написанную в passage программу
              List<ComputeDevice> Devs = new List<ComputeDevice>();
              Devs.Add(ComputePlatform.Platforms[1].Devices[0]);
              //Компиляция программы
              ComputeProgram prog = null;
              try
              {
                  prog = new ComputeProgram(Context, passage);
                  prog.Build(Devs, "", null, IntPtr.Zero);
              }
              catch
              {
                  Console.WriteLine(@"Please, add normal exeption handling.");
              }
              int w_b = data.GetLength(0), h_b = data.GetLength(1);
              int count = 1;
              int[] output = new int[w_b * h_b];
              int[] input = new int[w_b * h_b];
              int first = 1;
              while (count != 0)
              {
                  count = 0;
                  System.Buffer.BlockCopy(data, 0, input, 0, w_b * h_b * _sizeOfInt);

                  ComputeKernel kernelVecSum = prog.CreateKernel("passage");

                  //Загрузка данных в указатели для дальнейшего использования.
                  ComputeBuffer<int> bufInput = new ComputeBuffer<int>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, input);
                  ComputeBuffer<int> bufOutput = new ComputeBuffer<int>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer, output);

                  //Объявляем какие данные будут использоваться в программе vecSum
                  kernelVecSum.SetMemoryArgument(0, bufInput);
                  kernelVecSum.SetMemoryArgument(1, bufOutput);
                  kernelVecSum.SetValueArgument(2, h_b);
                  kernelVecSum.SetValueArgument(3, w_b);
                  kernelVecSum.SetValueArgument(4, first);
                  //Создание програмной очереди. Не забудте указать устройство, на котором будет исполняться программа!
                  ComputeCommandQueue Queue = new ComputeCommandQueue(Context, Cloo.ComputePlatform.Platforms[1].Devices[0], Cloo.ComputeCommandQueueFlags.None);
                  //Старт. Execute запускает программу-ядро vecSum указанное колличество раз (v1.Length)
                  Queue.Execute(kernelVecSum, null, new long[] { input.Length }, null, null);
                  //Считывание данных из памяти устройства.
                  GCHandle arrCHandle = GCHandle.Alloc(output, GCHandleType.Pinned);
                  Queue.Read<int>(bufOutput, true, 0, input.Length, arrCHandle.AddrOfPinnedObject(), null);
                  first = 1 - first;
                  for (int x = 1; x < w_b - 1; x++)
                  {
                      for (int y = 1; y < h_b - 1; y++)
                      {
                          if (output[x * h_b + y] == 1)
                          {
                              data[x, y] = 1;
                              output[x * h_b + y] = 0;
                              count++;
                          }
                      }
                  }
              }
              return data;
          }*/

        private int[,] ExecuteZhangSuenAlgorithm(int[,] data)
        {
            int w_b = data.GetLength(0), h_b = data.GetLength(1);
            int count = 1;
            int[,] change = new int[w_b, h_b];
            bool first = true;
            while (count != 0)
            {
                count = 0;

                for (int x = 1; x < w_b - 1; x++)
                {
                    for (int y = 1; y < h_b - 1; y++)
                    {
                        int sum = data[x, y - 1] + data[x + 1, y - 1] + data[x + 1, y] + data[x + 1, y + 1] + data[x, y + 1] + data[x - 1, y + 1] + data[x - 1, y] + data[x - 1, y - 1];
                        int p = 0;
                        p += data[x, y - 1] == 0 ? data[x + 1, y - 1] : 0;
                        p += data[x + 1, y - 1] == 0 ? data[x + 1, y] : 0;
                        p += data[x + 1, y] == 0 ? data[x + 1, y + 1] : 0;
                        p += data[x + 1, y + 1] == 0 ? data[x, y + 1] : 0;
                        p += data[x, y + 1] == 0 ? data[x - 1, y + 1] : 0;
                        p += data[x - 1, y + 1] == 0 ? data[x - 1, y] : 0;
                        p += data[x - 1, y] == 0 ? data[x - 1, y - 1] : 0;
                        if (first)
                        {
                            if (sum >= 2 && sum <= 6 && p == 1 &&
                                data[x, y - 1] * data[x + 1, y] * data[x, y + 1] == 0 &&
                                data[x + 1, y] * data[x, y + 1] * data[x - 1, y] == 0 && data[x, y] != 1)
                            {
                                change[x, y] = 1;
                                count++;
                            }
                        }
                        else
                        {
                            if (sum >= 2 && sum <= 6 && p == 1 &&
                                data[x, y - 1] * data[x + 1, y] * data[x - 1, y] == 0 &&
                                data[x, y - 1] * data[x, y + 1] * data[x - 1, y] == 0 && data[x, y] != 1)
                            {
                                change[x, y] = 1;
                                count++;
                            }
                        }
                    }
                }
                first = !first;
                for (int x = 1; x < w_b - 1; x++)
                {
                    for (int y = 1; y < h_b - 1; y++)
                    {
                        if (change[x, y] == 1)
                        {
                            data[x, y] = 1;
                            change[x, y] = 0;
                        }
                    }
                }
            }
            return data;
        }

        private int[,] ExecuteTemplateAlgorithm(int[,] data)
        {
            int w_b = data.GetLength(0), h_b = data.GetLength(1);
            int[,] pattern = { {150, 1, 1, 0, 0, 1, 150, 0, 150},
                               {150, 0, 150, 0, 0, 1, 150, 1, 1},
                               {1, 1, 150, 1, 0, 0, 150, 0, 150},
                               {150, 0, 150, 1, 0, 0, 1, 1, 150},
                               {1, 1, 1, 0, 0, 0, 150, 0, 150},
                               {150, 0, 1, 0, 0, 1, 150, 0, 1},
                               {150, 0, 150, 0, 0, 0, 1, 1, 1},
                               {1, 0, 150, 1, 0, 0, 1, 0, 150}};
            int count = 1;
            int del = 0;
            int amount = 0;
            while (count != 0)
            {
                count = 0;
                for (int x = 1; x < w_b - 1; x++)
                {
                    for (int y = 1; y < h_b - 1; y++)
                    {
                        int[] neighborhood = {data[x - 1, y - 1],
                            data[x, y - 1],
                            data[x + 1, y - 1],
                            data[x - 1, y],
                            data[x, y],
                            data[x + 1, y],
                            data[x - 1, y + 1],
                            data[x, y + 1],
                            data[x + 1, y + 1]};

                        int flag = 0;
                        for (int z = 0; z < 8; z++)
                        {
                            for (int i = 0; i < 9; i++)
                            {
                                if (pattern[z, i] != 150 && neighborhood[i] != pattern[z, i])
                                {
                                    flag = 1;
                                    break;
                                }
                            }
                            if (flag == 0)
                            {
                                del = 1;
                                data[x, y] = 1;
                                break;
                            }
                            flag = 0;

                        }
                    }
                }
                amount++;
                if (del == 1) { count = 1; del = 0; }
                else count = 0;
            }
            System.Console.WriteLine(amount);
            return data;
        }

        public Form1()
        {
            //картинка
            var pictureBox1 = new PictureBox();
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            _image = (Bitmap)Image.FromFile(@"C:\Users\Natali\Desktop\zzz.jpg");
            Controls.Add(pictureBox1);
            _image = BitmapToBlackWhite2(_image); //бинаризация

            var imagePixels = new int[_image.Width, _image.Height];
            for (var x = 1; x < _image.Width - 1; x++)
            {
                for (var y = 1; y < _image.Height - 1; y++)
                {
                    imagePixels[x, y] = _image.GetPixel(x, y).B == 255 ? 1 : 0;
                }
            }

            ExecuteZhangSuenAlgorithm(imagePixels);

            for (var x = 0; x < _image.Width; x++)
            {
                for (var y = 0; y < _image.Height; y++)
                {
                    _image.SetPixel(x, y,
                        imagePixels[x, y] == 1 ? System.Drawing.Color.White : System.Drawing.Color.Black);
                }
            }

            _toolStripProgressBar = new ToolStripProgressBar { Enabled = false };
            var prog = new ToolStripProgressBar();


            //     skelet(ref ms1, ref _image, prog);

            //****
            pictureBox1.Location = new Point(20, 30);

            pictureBox1.Size = new System.Drawing.Size(300, 300);
            pictureBox1.Image = (Image)_image;
            InitializeComponent();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}