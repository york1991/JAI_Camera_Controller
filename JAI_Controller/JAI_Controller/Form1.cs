using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Jai_FactoryDotNET;
using System.Drawing.Imaging;

namespace JAI_Controller
{
    public partial class Form1 : Form
    {
        static Jai_FactoryDotNET.CFactory factory;
        static CCamera myCamera;

        CNode myGainNode;
        CNode myExposureNode;

        public Form1()
        {
            InitializeComponent();

            button1.Enabled = false;
            button2.Enabled = false;

            Jai_FactoryWrapper.EFactoryError error = Jai_FactoryWrapper.EFactoryError.Success;

            factory = new Jai_FactoryDotNET.CFactory();

            error = factory.Open("");

            SearchButton_Click(null, null);
        }
        /// <summary>
        /// 开启相机
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if(myCamera!=null)
            {
                myCamera.NewImageDelegate += new Jai_FactoryWrapper.ImageCallBack(HandleImage);
                myCamera.StartImageAcquisition(true, 5, pictureBox1.Handle);
                //myCamera.StartImageAcquisition(true, 5);

                button2.Enabled = true;
                button1.Enabled = false;
            }
        }
        /// <summary>
        /// 关闭相机
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            if (myCamera != null)
            {
                myCamera.StopImageAcquisition();
                myCamera.NewImageDelegate -= new Jai_FactoryWrapper.ImageCallBack(HandleImage);
            }

            button1.Enabled = true;
            button2.Enabled = false;
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            // Search for any new GigE cameras using Filter Driver
            factory.UpdateCameraList(Jai_FactoryDotNET.CFactory.EDriverType.FilterDriver);

            if (factory.CameraList.Count > 0)
            {
                // Open the camera
                myCamera = factory.CameraList[0];
                myCamera.Open();

                // Get the GainRaw GenICam Node
                myGainNode = myCamera.GetNode("GainRaw");
                if (myGainNode != null)
                {
                    gainTrackBar.Minimum = Convert.ToInt32(myGainNode.Min);
                    gainTrackBar.Maximum = Convert.ToInt32(myGainNode.Max);
                    //gainTrackBar.Value = Convert.ToInt32(myGainNode.Value);
                    gainTrackBar.TickFrequency = (gainTrackBar.Maximum - gainTrackBar.Minimum) / 20;
                    gainTextBox.Text = myGainNode.Value.ToString();                    
                }

                // Get the Exposure GenICam Node
                myExposureNode = myCamera.GetNode("ExposureTimeRaw");
                if (myExposureNode != null)
                {
                    exposureTrackBar.Minimum = Convert.ToInt32(myExposureNode.Min);
                    exposureTrackBar.Maximum = Convert.ToInt32(myExposureNode.Max);
                    //exposureTrackBar.Value = Convert.ToInt32(myExposureNode.Value);
                    exposureTrackBar.TickFrequency = (exposureTrackBar.Maximum - exposureTrackBar.Minimum) / 20;
                    exposureTextBox.Text = myExposureNode.Value.ToString();
                }


                button1.Enabled = true;
            }
            else
            {
                MessageBox.Show("No Cameras Found!");
            }
        }

        ColorPalette myMonoColorPalette = null;
        void HandleImage(ref Jai_FactoryWrapper.ImageInfo ImageInfo)
        {  
            Bitmap newImageBitmap = new Bitmap((int)ImageInfo.SizeX, (int)ImageInfo.SizeY,  
                    (int)ImageInfo.SizeX, System.Drawing.Imaging.PixelFormat.Format8bppIndexed,
                    ImageInfo.ImageBuffer);

            // Create a Monochrome palette (only once)
            if (myMonoColorPalette == null)
            {
                Bitmap monoBitmap = new Bitmap(1, 1, PixelFormat.Format8bppIndexed);
                myMonoColorPalette = monoBitmap.Palette;

                for (int i = 0; i < 256; i++)
                    myMonoColorPalette.Entries[i] = Color.FromArgb(i, i, i);
            }

            for (int i = 0; i < 256; i++)
                myMonoColorPalette.Entries[i] = Color.FromArgb(i, i, i);

            // Set the Monochrome Color Palette
            newImageBitmap.Palette = myMonoColorPalette;

            //=====================================================
            //2018-12-17
            byte[] imgData;
            int stride;
            int ImageChannel = Image.GetPixelFormatSize(newImageBitmap.PixelFormat) / 8;
            getByteFrmBitmap(newImageBitmap, out imgData, out stride);

            pictureBox2.Image = ToGrayBitmap(imgData, (int)ImageInfo.SizeX, (int)ImageInfo.SizeY);
            return;
        }

        //从Bitmap中获取图像数据并存入byte[]数组中
        private void getByteFrmBitmap(Bitmap bp, out byte[] rgbValues, out int stride)
        {
            Rectangle rect = new Rectangle(0, 0, bp.Width, bp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
            bp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bp.PixelFormat);

            stride = bmpData.Stride;
            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap. 
            int bytes = Math.Abs(bmpData.Stride) * bp.Height;
            //byte[] rgbValues = new byte[bytes];
            rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            // Unlock the bits.
            bp.UnlockBits(bmpData);
        }

        /// <summary>  
        /// 将一个字节数组转换为8bit灰度位图  
        /// </summary>  
        /// <param name="rawValues">显示字节数组</param>  
        /// <param name="width">图像宽度</param>  
        /// <param name="height">图像高度</param>  
        /// <returns>位图</returns>  
        public static Bitmap ToGrayBitmap(byte[] rawValues, int width, int height)
        {
            //// 申请目标位图的变量，并将其内存区域锁定  
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height),
             ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            //// 获取图像参数  
            int stride = bmpData.Stride;  // 扫描线的宽度  
            int offset = stride - width;  // 显示宽度与扫描线宽度的间隙  
            IntPtr iptr = bmpData.Scan0;  // 获取bmpData的内存起始位置  
            int scanBytes = stride * height;// 用stride宽度，表示这是内存区域的大小  

            //// 下面把原始的显示大小字节数组转换为内存中实际存放的字节数组  
            int posScan = 0, posReal = 0;// 分别设置两个位置指针，指向源数组和目标数组  
            byte[] pixelValues = new byte[scanBytes];  //为目标数组分配内存  

            for (int x = 0; x < height; x++)
            {
                //// 下面的循环节是模拟行扫描  
                for (int y = 0; y < width; y++)
                {
                    pixelValues[posScan++] = rawValues[posReal++];
                }
                posScan += offset;  //行扫描结束，要将目标位置指针移过那段“间隙”  
            }

            //// 用Marshal的Copy方法，将刚才得到的内存字节数组复制到BitmapData中  
            System.Runtime.InteropServices.Marshal.Copy(pixelValues, 0, iptr, scanBytes);
            bmp.UnlockBits(bmpData);  // 解锁内存区域  

            //// 下面的代码是为了修改生成位图的索引表，从伪彩修改为灰度  
            ColorPalette tempPalette;
            using (Bitmap tempBmp = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
            {
                tempPalette = tempBmp.Palette;
            }
            for (int i = 0; i < 256; i++)
            {
                tempPalette.Entries[i] = Color.FromArgb(i, i, i);
            }

            bmp.Palette = tempPalette;

            //// 算法到此结束，返回结果  
            return bmp;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (myCamera != null)
            {
                myCamera.Close();
                return;
            }
        }

        private void gainTrackBar_Scroll(object sender, EventArgs e)
        {
            // Set Value
            myGainNode.Value = gainTrackBar.Value;
            // Update the Text Control with the new value
            gainTextBox.Text = myGainNode.Value.ToString();
        }

        private void exposureTrackBar_Scroll(object sender, EventArgs e)
        {
            // Set Value
            myExposureNode.Value = exposureTrackBar.Value;
            // Update the Text Control with the new value
            exposureTextBox.Text = myExposureNode.Value.ToString();
        }  


    }
}
