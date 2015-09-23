using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Data;
using System.Threading;
using GenArt.AST;
using GenArt.Classes;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using test;

namespace WpfApplication1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //Settings = Serializer.DeserializeSettings();
            if (Settings == null)
                Settings = new Settings();
        }
        public static Settings Settings; //loads settings
        private DnaDrawing currentDrawing; //creates a drawing

        private double errorLevel = double.MaxValue;
        private int generation;
        private DnaDrawing guiDrawing;
        private bool isRunning = true;
        private DateTime lastRepaint = DateTime.MinValue;
        private int lastSelected;
        private TimeSpan repaintIntervall = new TimeSpan(0, 0, 0, 0, 0);
        private int repaintOnSelectedSteps = 3;
        private int selected;
        private System.Drawing.Color[,] sourceColors;

        private Thread thread;

        public string VersionString = " - Beta 0.8 - Joseph Garrone, Matthew Lake";
        public string InputFile;
        public string OutputFile;
        public string[] OptimiseArray;
        public string[] OptimiseArrayDictionary;

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(VersionString + ". Currently in development! Email bugs to bugs.fireoak@outlook.com", "About");
        }

        private void exitMenu_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void selectInput_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.InitialDirectory = "";
            //ofd.DefaultExt = ""; // Default file extension
            //ofd.Filter = ""; // Filter files by extension
            // Show open file dialog box
            Nullable<bool> result = ofd.ShowDialog();
            // Process open file dialog box results 
            if (result == true)
            {
                // Open document 
                InputFile = ofd.FileName;
            }

                writeConsolas("Input file: " + InputFile);
                writeConsolas("Output file: " + OutputFile);
        }

        private void selectOutput_Click(object sender, EventArgs e)
        {
            //Set output file.
            {
                //SaveFileDialog sfd = new SaveFileDialog();
                //DialogResult dlgResult = sfd.ShowDialog();

                //if (dlgResult == System.Windows.Forms.DialogResult.OK)
                //{
                //    InputFile = sfd.FileName;
                //    OutputFile = sfd.FileName.Remove(sfd.FileName.LastIndexOf("\\")) + "\\Image Compression Output.txt";
                //}

                //writeConsolas("Input file: " + InputFile);
                //writeConsolas("Output file: " + OutputFile);
            }
        }
        public void writeConsolas(String inputText)
        {
            consolas.AppendText("[" + DateTime.Now.ToLongTimeString() + "]: " + inputText + "\r");
        }

        private void startCompression_Click(object sender, RoutedEventArgs e)
        {
            if (System.IO.Path.GetExtension(InputFile) == ".eica")
            {
                decompress();
            }
            else
            {
                compress();
            }
        }


        public void compress()
        {
            //initial stuff
            DateTime startTime = DateTime.Now;
            writeConsolas("Reading from input file: " + InputFile);

            Bitmap ImageBitmap = new Bitmap(InputFile);
            int Iwidth = ImageBitmap.Width;
            int Iheight = ImageBitmap.Height;
            int[,] ImageArray = new int[Iwidth * Iheight,4];
            //Array.Resize(ref ImageArray, (Iwidth * Iheight) - 1); //zero based so -1

            writeConsolas("ImageArray created with with " + ImageArray.Length + " elements");

            Tools.MaxHeight = ImageBitmap.Height;
            Tools.MaxWidth = ImageBitmap.Width;

            int r = 0;
            int g = 0;
            int b = 0;

            int total = 0;

            for (int x = 0; x < ImageBitmap.Width; x++)
            {
                for (int y = 0; y < ImageBitmap.Height; y++)
                {
                    System.Drawing.Color clr = ImageBitmap.GetPixel(x, y);
                    r += clr.R;
                    g += clr.G;
                    b += clr.B;
                    total++;
                }
            }

            //Calculate average
            r /= total;
            g /= total;
            b /= total;

            Tools.avgColour = System.Drawing.Color.FromArgb(r, g, b);

            //fill array with pixel data
            int arraycounter = 0;
            try
            {
                for (int y = 0; y < Iheight; y++)
                {
                    for (int x = 0; x < Iwidth; x++)
                    {
                        ImageArray[arraycounter, 0] = ImageBitmap.GetPixel(x, y).A;
                        ImageArray[arraycounter, 1] = ImageBitmap.GetPixel(x, y).R;
                        ImageArray[arraycounter, 2] = ImageBitmap.GetPixel(x, y).G;
                        ImageArray[arraycounter, 3] = ImageBitmap.GetPixel(x, y).B;
                        arraycounter++;
                    }
                }
                writeConsolas("ImageArray successfully filled with GetPixel() data in: " + (DateTime.Now - startTime));
            }
            catch (Exception e)
            {
                writeConsolas("Error filling ImageArray: " + e.Message.ToString());
            }

            writeConsolas("evolution started");
            isRunning = true;
            StartEvolution(ImageBitmap);

        }

        private DnaDrawing GetNewInitializedDrawing()
        {
            var drawing = new DnaDrawing();
            drawing.Init();
            return drawing;
        }

        private void StartEvolution(Bitmap input)
        {
            Pixel[] sourcePixels = SetupSourceColorMatrix(input);
            if (currentDrawing == null)
                currentDrawing = GetNewInitializedDrawing();
            lastSelected = 0;
            int freq = 0;
            double target= (32 * 32 * 3D * (input.Height ) * (input.Width ));
            double minTarget = (64 * 64 * 3D * (input.Height) * (input.Width));
            writeConsolas("target: " + target);

            DateTime startTime2 = DateTime.Now;
            while (isRunning)
            {
                DnaDrawing newDrawing;
                lock (currentDrawing)
                {
                    newDrawing = currentDrawing.Clone();
                }
                newDrawing.Mutate();

                if (newDrawing.IsDirty) //idk - this deceides whether or not to change the drawing i guess
                {

                    generation++;

                    NewFitnessCalculator test = new NewFitnessCalculator();
                    double newErrorLevel = test.GetDrawingFitness(newDrawing, sourcePixels);
                    test.Dispose();

                    if ((errorLevel <= target) || (((DateTime.Now - startTime2).TotalSeconds > 60)&&(newErrorLevel <=minTarget)))
                    {
                        writeConsolas("shapes done: " + newErrorLevel + "in: " + (DateTime.Now - startTime2));
                        DateTime startTime3 = DateTime.Now;
                        isRunning = false;
                        int[,] DiffArray = new int[Tools.MaxWidth * Tools.MaxHeight, 4];
                        int[,] RefArray = new int[Tools.MaxWidth * Tools.MaxHeight, 5];
                        Bitmap _bmp;
                        Graphics _g;
                        _bmp = new System.Drawing.Bitmap(Tools.MaxWidth, Tools.MaxHeight);
                        _g = Graphics.FromImage(_bmp);
                        Renderer.Render(newDrawing, _g, 1);
                        BitmapData bd = _bmp.LockBits(new System.Drawing.Rectangle(0, 0, Tools.MaxWidth, Tools.MaxHeight), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        int arraycounter = 0;
                        unchecked
                        {
                            unsafe
                            {
                                fixed (Pixel* psourcePixels = sourcePixels)
                                {
                                    Pixel* p1 = (Pixel*)bd.Scan0.ToPointer();
                                    Pixel* p2 = psourcePixels;
                                    for (int i = sourcePixels.Length; i > 0; i--, p1++, p2++)
                                    {
                                        int a = p2->A - p1->A;
                                        int r = p2->R - p1->R;
                                        int g = p2->G - p1->G;
                                        int b = p2->B - p1->B;
                                        DiffArray[arraycounter, 0] = a;
                                        DiffArray[arraycounter, 1] = r;
                                        DiffArray[arraycounter, 2] = g;
                                        DiffArray[arraycounter, 3] = b;
                                        arraycounter++;
                                    }
                                }
                            }
                        }
                        writeConsolas("DiffArray done: " + newErrorLevel + "in: " + (DateTime.Now - startTime3));
                        //RefArray = startReference(DiffArray);
                        for (int w = 0, e=0; w<RefArray.Length/5;w++)
                        {
                            if (((RefArray[w, 0] != RefArray[w - RefArray[w, 4], 0]) || (RefArray[w, 1] != RefArray[w - RefArray[w, 4], 1]) || (RefArray[w, 2] != RefArray[w - RefArray[w, 4], 2]) || (RefArray[w - RefArray[w, 4], 3] != RefArray[w, 3])))
                            {
                                //Console.WriteLine(w + ":" + (e));
                                e++;
                            }
                        }
                        Console.WriteLine("returned");

                        _bmp.UnlockBits(bd);
                        double total= 0D;
                        for (int i = 0; i < DiffArray.Length/4; i++)
                        {
                            total += Math.Abs(DiffArray[i, 0]);
                            total += Math.Abs(DiffArray[i, 1]);
                            total += Math.Abs(DiffArray[i, 2]);
                            total += Math.Abs(DiffArray[i, 3]);
                        }
                        writeConsolas("done with average difference: " + total / DiffArray.Length);
                        _bmp.Save("hopefully.png", System.Drawing.Imaging.ImageFormat.Png);
                        //byte[] result = {0,1};
                        //Console.WriteLine(result[0]);

                        List<BitArray> BitList = new List<BitArray>();
                        BitList.Add(WriteBits(Tools.MaxWidth, 16));
                        Console.WriteLine(Tools.MaxWidth.ToString());
                        BitList.Add(WriteBits(Tools.MaxHeight, 16));
                        BitList.Add(WriteBits(Tools.avgColour.R, 8));
                        BitList.Add(WriteBits(Tools.avgColour.G, 8));
                        BitList.Add(WriteBits(Tools.avgColour.B, 8));
                        BitList.Add(WriteBits(newDrawing.Polygons.Count, 10));
                        int q=0;
                        foreach (DnaPolygon polygon in newDrawing.Polygons)
                        {
                            BitList.Add(WriteBits(polygon.Points.Count, 5));
                            BitList.Add(WriteBits(polygon.Brush.Alpha, 8));
                            BitList.Add(WriteBits(polygon.Brush.Red, 8));
                            BitList.Add(WriteBits(polygon.Brush.Green, 8));
                            BitList.Add(WriteBits(polygon.Brush.Blue, 8));  
                            foreach (DnaPoint point in polygon.Points)
                            {
                                BitList.Add(WriteBits(point.X, 12));
                                BitList.Add(WriteBits(point.Y, 12));
                            }
                            Console.WriteLine(q.ToString());
                            q++;
                        }
                        //BitList.Add(WriteBits(Tools.MaxWidth, 1));
                        for (int k = 0, c = 0; k < DiffArray.Length / 4; k++, c++)
                        {
                            if (RefArray[k, 4] != 0)
                            {
                                BitList.Add(WriteBits(1, 1));
                                BitList.Add(WriteBits(RefArray[k, 4],11));
                            }
                            else
                            {
                                BitList.Add(WriteBits(0, 1));
                                //BitList.Add(WriteBits(neg(DiffArray[k, 0]), 1));
                                //BitList.Add(WriteBits(BitSize(DiffArray[k, 0]), 2));
                                //BitList.Add(WriteBits(neg(DiffArray[k, 1]), 1)); //hopefully i don't need to comment this
                                //BitList.Add(WriteBits(BitSize(DiffArray[k, 1]), 2)); 
                                //BitList.Add(WriteBits(neg(DiffArray[k, 2]), 1));
                                //BitList.Add(WriteBits(BitSize(DiffArray[k, 2]), 2));
                                //BitList.Add(WriteBits(neg(DiffArray[k, 3]), 1));
                                //BitList.Add(WriteBits(BitSize(DiffArray[k, 3]), 2));
                                BitList.Add(WriteBits(DiffArray[k, 0]+256, 9));//BitSize(DiffArray[k, 0]) + 4));
                                BitList.Add(WriteBits(DiffArray[k, 1]+256, 9));//BitSize(DiffArray[k, 1]) + 4));
                                BitList.Add(WriteBits(DiffArray[k, 2]+256, 9));//BitSize(DiffArray[k, 2]) + 4));
                                BitList.Add(WriteBits(DiffArray[k, 3]+256, 9));//BitSize(DiffArray[k, 3]) + 4));
                            }
                        }
                        Console.WriteLine("done writing to bits");
                        int totalBitCount = 0;
                        foreach (BitArray array in BitList)
                        {
                            totalBitCount += array.Count;
                        }
                        BitArray allBits = new BitArray(totalBitCount);
                        int abcounter = 0;
                        foreach (BitArray array in BitList)
                        {
                            foreach (Boolean bit in array)
                            {
                                allBits[abcounter] = bit;
                                abcounter++;
                            }
                        }
                        byte[] result = ToByteArray(allBits);
                        Console.WriteLine(result.Length.ToString());
                        QuickLZ qlz = new QuickLZ();
                        byte[] compressed = qlz.Compress(result);
                        byte[] decompressed = qlz.Decompress(compressed);
                        //byte[] result = new byte[RefArray.Length * 5 * sizeof(int)];
                        //Buffer.BlockCopy(RefArray, 0, result, 0, result.Length/5);
                        File.WriteAllBytes("output.eica", compressed);
                        File.WriteAllBytes("output2.eica", decompressed);

                        Bitmap _bmp2 = new System.Drawing.Bitmap(Tools.MaxWidth, Tools.MaxHeight);
                        int arraycounter2 = 0;
                        for (int y = 0; y < Tools.MaxHeight; y++)
                        {
                            for (int x = 0; x < Tools.MaxWidth; x++)
                            {
                                System.Drawing.Color c1 = _bmp.GetPixel(x, y);
                                int a = c1.A + DiffArray[arraycounter2, 0];
                                int r = c1.R + DiffArray[arraycounter2, 1];
                                int g = c1.G + DiffArray[arraycounter2, 2];
                                int b = c1.B + DiffArray[arraycounter2, 3];
                                System.Drawing.Color c3 = System.Drawing.Color.FromArgb(a, r, g, b);
                                _bmp2.SetPixel(x, y, c3);
                                arraycounter2++;
                            }
                        }
                        _bmp2.Save("success.png", System.Drawing.Imaging.ImageFormat.Png);
                        writeConsolas("saving done: " + newErrorLevel + "in: " + (DateTime.Now - startTime3));

                        //for (int k = 0; k < RefArray.Length / 5; k++)
                        //{
                        //    if (RefArray[k, 4] != 0)
                        //    {
                        //        int whatervertest = RefArray[k, 4];
                        //        RefArray[k, 0] = RefArray[k - Math.Abs(RefArray[k, 4]), 0];
                        //        RefArray[k, 1] = RefArray[k - Math.Abs(RefArray[k, 4]), 1];
                        //        RefArray[k, 2] = RefArray[k - Math.Abs(RefArray[k, 4]), 2];
                        //        RefArray[k, 3] = RefArray[k - Math.Abs(RefArray[k, 4]), 3];
                        //    }
                        //}

                        //Bitmap _bmp2 = new System.Drawing.Bitmap(Tools.MaxWidth, Tools.MaxHeight);
                        //int arraycounter2 = 0;
                        //for (int y = 0; y < Tools.MaxHeight; y++)
                        //{
                        //    for (int x = 0; x < Tools.MaxWidth; x++)
                        //    {
                        //        System.Drawing.Color c1 = _bmp.GetPixel(x, y);
                        //        int a = c1.A + RefArray[arraycounter2, 0];
                        //        int r = c1.R + RefArray[arraycounter2, 1];
                        //        int g = c1.G + RefArray[arraycounter2, 2];
                        //        int b = c1.B + RefArray[arraycounter2, 3];
                        //        System.Drawing.Color c3 = System.Drawing.Color.FromArgb(a, r, g, b);
                        //        _bmp2.SetPixel(x, y, c3);
                        //        arraycounter2++;
                        //    }
                        //}
                        //_bmp2.Save("success.png", System.Drawing.Imaging.ImageFormat.Png);
                    }
                    if (newErrorLevel <= errorLevel)
                    {
                        selected++;
                        lock (currentDrawing)
                        {
                            currentDrawing = newDrawing;
                        }
                        errorLevel = newErrorLevel;

                        if (freq == 100)
                        {
                            writeConsolas("next gen created with error level: " + errorLevel);
                            freq = 0;
                        }
                        freq++;
                    }
                }
                //else, discard new drawing
            }
            return;
        }

        public int[,] startReference(int[,] input)
        {
            int[,] referencedArray = new int[input.Length / 4, 5];
            for (int k = 0; k < input.Length / 4; k++)
            {
                referencedArray[k, 0] = input[k, 0];
                referencedArray[k, 1] = input[k, 1];
                referencedArray[k, 2] = input[k, 2];
                referencedArray[k, 3] = input[k, 3];
            }
            Console.WriteLine(input.Length / 4);
            List<Task> tasks = new List<Task>();
            for (int i = 0; i <= input.Length/4; i += 2048)
            {
                int iCopy = i;
                if ((iCopy == 0) && (input.Length / 4 < 2048))
                {
                    int[,] res = new int[input.Length / 4, 5];
                    int[,] arrayToReference = new int[input.Length / 4, 5];
                    Array.Clear(arrayToReference, 0, arrayToReference.Length);
                    ArrayCopy(input, iCopy, arrayToReference, 0, input.Length / 4);
                    Task<Tuple<int[,], int>> test = Task.Factory.StartNew(() => addReferences(arrayToReference, iCopy));
                    tasks.Add(test);
                    tasks.Add(test.ContinueWith(t => { ArrayCopyAgain(t.Result.Item1, 0, referencedArray, t.Result.Item2, input.Length / 4); /*Console.WriteLine(t.Result.Item2 + "last");*/}));
                }
                else if (iCopy == 0) 
                {
                    int[,] res = new int[2048, 5];
                    int[,] arrayToReference = new int[2048, 5];
                    Array.Clear(arrayToReference, 0, arrayToReference.Length);
                    ArrayCopy(input, iCopy, arrayToReference, 0, 2048);
                    Task<Tuple<int[,], int>> test = Task.Factory.StartNew(() => addReferences(arrayToReference, iCopy));
                    tasks.Add(test);
                    tasks.Add(test.ContinueWith(t => { ArrayCopyAgain(t.Result.Item1, 0, referencedArray, t.Result.Item2, 2048); /*Console.WriteLine(t.Result.Item2);*/ }));
                }
                else if (input.Length / 4 - iCopy < 2048)
                {
                    int[,] res = new int[2048 + (input.Length / 4 - iCopy), 5];
                    int[,] arrayToReference = new int[2048 + (input.Length / 4 - iCopy), 5];
                    Array.Clear(arrayToReference, 0, arrayToReference.Length);
                    ArrayCopy(input, iCopy - 2048, arrayToReference, 0, 2048 + (input.Length / 4 - iCopy));
                    Task<Tuple<int[,], int>> test = Task.Factory.StartNew(() => addReferences(arrayToReference, iCopy));
                    tasks.Add(test);
                    tasks.Add(test.ContinueWith(t => { ArrayCopyAgain(t.Result.Item1, 2048, referencedArray, t.Result.Item2, (input.Length/4 - t.Result.Item2)); /*Console.WriteLine(t.Result.Item2 +"last");*/ }));
                }
                else
                {
                    int[,] res = new int[4096, 5];
                    Array.Clear(res, 0, res.Length);
                    int[,] arrayToReference = new int[4096, 5];
                    Array.Clear(arrayToReference, 0, arrayToReference.Length);
                    ArrayCopy(input, iCopy - 2048, arrayToReference, 0, 4096);
                    Task<Tuple<int[,], int>> test = Task.Factory.StartNew(() => addReferences(arrayToReference, iCopy));
                    tasks.Add(test);
                    tasks.Add(test.ContinueWith(t => { ArrayCopyAgain(t.Result.Item1, 2048, referencedArray, t.Result.Item2, 2048); /*Console.WriteLine(t.Result.Item2);*/ }));
                }
            }
            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("returning");
            for (int k = 0, c = 0; k < referencedArray.Length / 5; k++)
            {
                if (referencedArray[k, 1] != 0)
                {
                    //Console.WriteLine(referencedArray[k, 4].ToString() + "yes!" + (c).ToString());
                    c++;
                }
            }
            return referencedArray;
        }

        public Tuple<int[,],int> addReferences(int[,] input, int index)
        {
            int c = 0;
            if (input.Length/5 == 2048)
            {
                for (int i = 0; i < 2048; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if ((input[i, 0] == input[j, 0]) && (input[i, 1] == input[j, 1]) && (input[i, 2] == input[j, 2]) && (input[i, 3] == input[j, 3]))
                        {
                            input[i, 4] = (i - j);
                            if (!((input[i, 0] == input[i - input[i, 4], 0]) && (input[i, 1] == input[i - input[i, 4], 1]) && (input[i, 2] == input[i - input[i, 4], 2]) && (input[i - input[i, 4], 3] == input[i, 3])))
                            {
                                Console.WriteLine( ": nope ");
                            }
                            //c++;
                            //Console.WriteLine(c);
                            break;
                        }
                    }
                }
            }
            else if ((input.Length/5 < 4096) && (input.Length/5 > 2048))
            {
                for (int i = 2048; i < input.Length / 5; i++)
                {
                    for (int j = 0; j < 2048; j++)
                    {
                        if ((input[i, 0] == input[j, 0]) && (input[i, 1] == input[j, 1]) && (input[i, 2] == input[j, 2]) && (input[i, 3] == input[j, 3]))
                        {
                            input[i, 4] = (i - j);
                            if (!((input[i, 0] == input[i - input[i, 4], 0]) && (input[i, 1] == input[i - input[i, 4], 1]) && (input[i, 2] == input[i - input[i, 4], 2]) && (input[i - input[i, 4], 3] == input[i, 3])))
                            {
                                Console.WriteLine( ": nope ");
                            }
                            //c++;
                            Console.WriteLine("hi");
                            break;
                        }
                    }
                }
            }
            else if (input.Length/5 < 2048)
            {
                for (int i = 0; i < input.Length / 5; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if ((input[i, 0] == input[j, 0]) && (input[i, 1] == input[j, 1]) && (input[i, 2] == input[j, 2]) && (input[i, 3] == input[j, 3]))
                        {
                            input[i, 4] = (i - j);
                            if (!((input[i, 0] == input[i - input[i, 4], 0]) && (input[i, 1] == input[i - input[i, 4], 1]) && (input[i, 2] == input[i - input[i, 4], 2]) && (input[i - input[i, 4], 3] == input[i, 3])))
                            {
                                Console.WriteLine(": nope ");
                            }
                            //c++;
                            //Console.WriteLine(c);
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int i = 2048; i < 4096; i++)
                {
                    for (int j = 0; j < 2048; j++)
                    {
                        if ((input[i, 0] == input[j, 0]) && (input[i, 1] == input[j, 1]) && (input[i, 2] == input[j, 2]) && (input[i, 3] == input[j, 3]))
                        {
                            input[i, 4] = (i - j);
                            if (!((input[i, 0] == input[i - input[i, 4], 0]) && (input[i, 1] == input[i - input[i, 4], 1]) && (input[i, 2] == input[i - input[i, 4], 2]) && (input[i - input[i, 4], 3] == input[i, 3])))
                            {
                                Console.WriteLine( ": nope ");
                            }
                            //c++;
                            //Console.WriteLine(c);
                            break;
                        }
                    }
                }
            }
            return new Tuple<int[,],int>(input,index);
        }

        public void ArrayCopy(int[,] input, int start, int[,] destination, int deStart, int length)
        {
            for (int i = start, j=deStart; i < length; i++, j++)
            {
                destination[j, 0] = input[i, 0];
                destination[j, 1] = input[i, 1];
                destination[j, 2] = input[i, 2];
                destination[j, 3] = input[i, 3];
            }
        }

        public void ArrayCopyAgain(int[,] input, int start, int[,] destination, int deStart, int length)
        {
            for (int i = start, j = deStart; i < (length+start); i++, j++)
            {
                destination[j, 4] = input[i, 4];
            }
        }

        public void bitArrayCopy(BitArray input, int start,BitArray destination, int deStart, int length)
        {
            for (int i = start, j = deStart; i < (length + start); i++, j++)
            {
                destination[j] = input[i];
            }
        }

        public int BitSize(int input)
        {
            if (Math.Abs(input) > 127) return 3;
            if (Math.Abs(input) > 63) return 2;
            if (Math.Abs(input) > 31) return 1;
            if (Math.Abs(input) <= 31) return 0;
            else return 0;
        }
        public int neg(int input)
        {
            if (input >= 0) return 1;
            if (input < 0) return 0;
            else return 1;
        }

        public BitArray WriteBits(int x, int length)
        {
            BitArray ba = new BitArray(length);
            int xx = Math.Abs(x);
            string ss = Convert.ToString(xx, 2); //Convert to binary in a string
            int[] bitss = ss.PadLeft(length, '0') // Add 0's from left
                         .Select(cc => int.Parse(cc.ToString())) // convert each char to int
                         .ToArray(); // Convert IEnumerable from select to Array
            for (int i = 0; i < length; i++)
            {
                if (bitss[i] == 1) ba[i] = true;
                if (bitss[i] == 0) ba[i] = false;
            }
            return ba;
        }

        public byte[] ToByteArray(BitArray bits)
        {
            int numBytes = bits.Count / 8;
            if (bits.Count % 8 != 0) numBytes++;

            byte[] bytes = new byte[numBytes];
            /*int byteIndex = 0, bitIndex = 0;

            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i])
                    bytes[byteIndex] |= (byte)(1 << (7 - bitIndex));

                bitIndex++;
                if (bitIndex == 8)
                {
                    bitIndex = 0;
                    byteIndex++;
                }
            }*/
            bits.CopyTo(bytes, 0);
            return bytes;
        }

        public void decompress()
        {
            QuickLZ qlz = new QuickLZ();
            byte[] decompressed = qlz.Decompress(File.ReadAllBytes(InputFile));
            Console.WriteLine("dec" + decompressed.Length.ToString());
            BitArray bits = new BitArray(decompressed);
            Console.WriteLine("bits" + bits.Length.ToString());
            int progress = 0;
            BitArray width = new BitArray(16); //Max width
            bitArrayCopy(bits, progress, width, 0, 16);
            Tools.MaxWidth = unwritebits(width); //create maxwidth
            progress += 16;
            BitArray height = new BitArray(16); //Max height
            bitArrayCopy(bits, progress, height, 0, 16);
            Tools.MaxHeight = unwritebits(height); //create maxheight
            progress += 16;
            BitArray avgreds = new BitArray(8); //red colour average
            bitArrayCopy(bits, progress, avgreds, 0, 8);
            int avgred = unwritebits(avgreds);
            progress += 8;
            BitArray avggreens = new BitArray(8); //green colour average
            bitArrayCopy(bits, progress, avggreens, 0, 8);
            int avggreen = unwritebits(avggreens);
            progress += 8;
            BitArray avgblues = new BitArray(8); //blue colour average
            bitArrayCopy(bits, progress, avgblues, 0, 8);
            int avgblue = unwritebits(avgblues);
            progress += 8;
            Tools.avgColour = System.Drawing.Color.FromArgb(avgred, avggreen, avgblue); //create average colour
            DnaDrawing newDrawing = new DnaDrawing(); //Create dnadrawing
            //newDrawing.Init();
            BitArray polynumbers = new BitArray(10); //number of polygons
            bitArrayCopy(bits, progress, polynumbers, 0, 10);
            int polynumber = unwritebits(polynumbers);
            progress += 10;
            newDrawing.Polygons = new List<DnaPolygon>();
            for (int i = 0; i < polynumber; i++)
            {
                Console.WriteLine(i);
                DnaPolygon polygon = new DnaPolygon(); //create dnapolygon
                polygon.Points = new List<DnaPoint>();
                BitArray pointnumbers = new BitArray(5); //number of points
                bitArrayCopy(bits, progress, pointnumbers, 0, 5);
                int pointnumber = unwritebits(pointnumbers);
                progress += 5;
                BitArray alphas = new BitArray(8); //alpha colour
                bitArrayCopy(bits, progress, alphas, 0, 8);
                int alpha = unwritebits(alphas);
                progress += 8;
                BitArray reds = new BitArray(8); //red colour
                bitArrayCopy(bits, progress, reds, 0, 8);
                int red = unwritebits(reds);
                progress += 8;
                BitArray greens = new BitArray(8); //green colour
                bitArrayCopy(bits, progress, greens, 0, 8);
                int green = unwritebits(greens);
                progress += 8;
                BitArray blues = new BitArray(8); //blue colour
                bitArrayCopy(bits, progress, blues, 0, 8);
                int blue = unwritebits(blues);
                progress += 8;
                DnaBrush brush = new DnaBrush(); //create dnabrush
                brush.Alpha=alpha;
                brush.Red = red;
                brush.Green = green;
                brush.Blue=blue;
                polygon.Brush=brush; //assign brush
                for (int j = 0; j < pointnumber; j++)
                {
                    DnaPoint point = new DnaPoint();
                    BitArray Xs = new BitArray(12); //x position
                    bitArrayCopy(bits, progress, Xs, 0, 12);
                    int x = unwritebits(Xs);
                    progress += 12;
                    point.X = x;
                    BitArray Ys = new BitArray(12); //y position
                    bitArrayCopy(bits, progress, Ys, 0, 12);
                    int y = unwritebits(Ys);
                    progress += 12;
                    point.Y = y;
                    polygon.Points.Add(point);
                }
                newDrawing.Polygons.Add(polygon);
            }
            int[,] DiffArray = new int[Tools.MaxWidth * Tools.MaxWidth, 4];
            int[,] refArray = new int[Tools.MaxWidth * Tools.MaxWidth, 5];
            for (int i = 0; i < Tools.MaxWidth * Tools.MaxWidth; i++)
            {
                BitArray firsts = new BitArray(1); //normal or reference
                bitArrayCopy(bits, progress, firsts, 0, 1);
                int first = unwritebits(firsts);
                progress += 1;
                if (first == 1)
                {
                    BitArray reffs = new BitArray(11); //reference number
                    bitArrayCopy(bits, progress, reffs, 0, 11);
                    int reff = unwritebits(reffs);
                    progress += 11;
                    refArray[i,4]=reff;
                }
                else
                {
                    //BitArray asizes = new BitArray(3); //alpha size
                    //bitArrayCopy(bits, progress, asizes, 0, 3);
                    int asize = 9;//unwritebits(asizes);
                    //progress += 3;
                    //BitArray rsizes = new BitArray(3); //red size
                    //bitArrayCopy(bits, progress, rsizes, 0, 3);
                    int rsize = 9;//unwritebits(rsizes);
                    //progress += 3;
                    //BitArray gsizes = new BitArray(3); //green size
                    //bitArrayCopy(bits, progress, gsizes, 0, 3);
                    int gsize = 9;//unwritebits(gsizes);
                    //progress += 3;
                    //BitArray bsizes = new BitArray(3); //blue size
                    //bitArrayCopy(bits, progress, bsizes, 0, 3);
                    int bsize = 9;//unwritebits(bsizes);
                    //progress += 3;

                    BitArray a = new BitArray(asize); //alpha
                    bitArrayCopy(bits, progress, a, 0, asize);
                    int alpha = unwritebits(a);
                    progress += asize;
                    DiffArray[i, 0] = alpha-256;
                    BitArray r = new BitArray(rsize); //red
                    bitArrayCopy(bits, progress, r, 0, rsize);
                    int red = unwritebits(r);
                    progress += rsize;
                    DiffArray[i, 1] = red-256;
                    BitArray g = new BitArray(gsize); //green
                    bitArrayCopy(bits, progress, g, 0, gsize);
                    int green = unwritebits(g);
                    progress += gsize;
                    DiffArray[i, 2] = green-256;
                    BitArray b = new BitArray(bsize); //blue
                    bitArrayCopy(bits, progress, b, 0, bsize);
                    int blue = unwritebits(b);
                    progress += bsize;
                    DiffArray[i, 3] = blue-256;
                }
            }
            //int[,] fullArray = (int[,]) refArray.Clone();
            for (int k = 0; k < refArray.Length / 5; k++)
            {
                if (refArray[k, 4] != 0)
                {
                    DiffArray[k, 0] = refArray[k - refArray[k, 4], 0];
                    DiffArray[k, 1] = refArray[k - refArray[k, 4], 1];
                    DiffArray[k, 2] = refArray[k - refArray[k, 4], 2];
                    DiffArray[k, 3] = refArray[k - refArray[k, 4], 3];
                    Console.WriteLine(refArray[k, 1]);
                }
            }

            Console.WriteLine(newDrawing.Polygons.Count);
            Console.WriteLine("done with drawing");

            Bitmap _bmp;
            Graphics _g;
            _bmp = new System.Drawing.Bitmap(Tools.MaxWidth, Tools.MaxHeight);
            _g = Graphics.FromImage(_bmp);
            Renderer.Render(newDrawing, _g, 1);
            BitmapData bd = _bmp.LockBits(new System.Drawing.Rectangle(0, 0, Tools.MaxWidth, Tools.MaxHeight), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            _bmp.Save("finally.png", System.Drawing.Imaging.ImageFormat.Png);
            _bmp.UnlockBits(bd);

            Bitmap _bmp2 = new System.Drawing.Bitmap(Tools.MaxWidth, Tools.MaxHeight);
            int arraycounter2 = 0;
            for (int y = 0; y < Tools.MaxHeight; y++)
            {
                for (int x = 0; x < Tools.MaxWidth; x++)
                {
                    System.Drawing.Color c1 = _bmp.GetPixel(x, y);
                    int a = c1.A + DiffArray[arraycounter2, 0];
                    int r = c1.R + DiffArray[arraycounter2, 1];
                    int g = c1.G + DiffArray[arraycounter2, 2];
                    int b = c1.B + DiffArray[arraycounter2, 3];
                    System.Drawing.Color c3 = System.Drawing.Color.FromArgb(a, r, g, b);
                    _bmp2.SetPixel(x, y, c3);
                    arraycounter2++;
                }
            }
            _bmp2.Save("success.png", System.Drawing.Imaging.ImageFormat.Png);
            System.Diagnostics.Process.Start(@"success.png");
        }

        public int unwritebits(BitArray ba)
        {
            int[] bits = new int[ba.Count];
            int sum = 0;
            for (int i = (ba.Count-1); i >= 0; i--)
            {
                if (ba[i]) bits[i] = 1;
                if (!ba[i]) bits[i] = 0;
                sum += (int)Math.Pow(2 ,(Math.Abs(i - (ba.Count - 1)))) * bits[i];
                //Console.WriteLine(i.ToString() + ":" + (Math.Abs(i - (ba.Count - 1))).ToString() + "/" + ((int)Math.Pow(2, (Math.Abs(i - (ba.Count - 1)))) * bits[i]).ToString());
            }
            return sum;
        }

        //public void copyBack(int[,] input, int index)
        //{
        //    if ((index == 0) && (input.Length / 5 < 2048))
        //    {
        //        Array.Copy(input, 0, referencedArray, index, input.Length / 4);
        //    }
        //    else if (i == 0) 
        //    {
        //        Array.Copy(input, 0, referencedArray, index, 2048);
        //    }
        //    else if (input.Length/4 - i < 2048)
        //    {
        //        Array.Copy(input, 2048, referencedArray, index, (input.Length - i));
        //    }
        //    else
        //    {
        //        Array.Copy(input, 2048, referencedArray, index, 2048);
        //    }
        //}

        public Pixel[] SetupSourceColorMatrix(Bitmap sourceImage)
        {
            if (sourceImage == null)
                throw new NotSupportedException("A source image of Bitmap format must be provided");

            BitmapData bd = sourceImage.LockBits(
            new System.Drawing.Rectangle(0, 0, Tools.MaxWidth, Tools.MaxHeight),
            ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Pixel[] sourcePixels = new Pixel[Tools.MaxWidth * Tools.MaxHeight];
            unsafe
            {
                fixed (Pixel* psourcePixels = sourcePixels)
                {
                    Pixel* pSrc = (Pixel*)bd.Scan0.ToPointer();
                    Pixel* pDst = psourcePixels;
                    for (int i = sourcePixels.Length; i > 0; i--)
                        *(pDst++) = *(pSrc++);
                }
            }
            sourceImage.UnlockBits(bd);

            return sourcePixels;
        }

        private unsafe System.Drawing.Color GetPixel(BitmapData bmd, int x, int y)
        {
            byte* p = (byte*)bmd.Scan0 + y * bmd.Stride + 3 * x;
            return System.Drawing.Color.FromArgb(p[2], p[1], p[0]);
        }
    }
}

