using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PCV_windows_
{
    public partial class MainWindow : Window
    {
        private static string remoteAddress;
        private static string localAddress;
        private static string remotePort;
        private static int localPort;
        private IPEndPoint ipep;
        private Socket SrvSock;
        private IPEndPoint sender;
        private EndPoint Remote;
        private Thread listenerThread, GetAddressThread, controlThread;
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        public MainWindow()
        {
            InitializeComponent();
            InitNetState();
            SrvSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            ipep = new IPEndPoint(IPAddress.Parse(localAddress), localPort);
            Remote = new IPEndPoint(IPAddress.Any, 0);
            SrvSock.Bind(ipep);
            GetAddressThread = new Thread(new ThreadStart(GetAddress));
            GetAddressThread.Start();
            CancelButton.IsEnabled = false;
            StartButton.IsEnabled = true;
            isOn.Source = new BitmapImage(new Uri(@"img/status-off.png", UriKind.RelativeOrAbsolute));
        }
        public void InitNetState()
        {
            Random rnd = new Random();
            localPort = rnd.Next(49152, 65535 + 1);
            localAddress = (Dns.GetHostByName(Dns.GetHostName()).AddressList[0]).ToString();
        }
        public void Listener()
        {
            while (true)
            {
                int recv;
                byte[] data = new byte[1024];
                recv = SrvSock.ReceiveFrom(data, ref Remote);
                String[] inf = Encoding.UTF8.GetString(data, 0, recv).Split('@');
                String[] arr = new String[2];
                int i = 0;
                foreach (string info in inf)
                {
                    arr[i] = info;
                    i++;
                }
                remoteAddress = arr[0];
                remotePort = arr[1];
                try
                {
                    sender = new IPEndPoint(IPAddress.Parse(remoteAddress), int.Parse(remotePort));
                }
                catch (FormatException) { }
                if (data != null)
                {
                    controlThread = new Thread(new ThreadStart(ListenControl));
                    controlThread.Start();
                    Thread.Sleep(1000);
                    while (true)
                    {
                        Image pr = TakeScreenShot(Screen.PrimaryScreen);
                        Bitmap bmp = new Bitmap(pr);
                        byte[] bytes = ConvertToByte(bmp);
                        List<byte[]> lst = CutMsg(bytes);
                        for (int j = 0; j < lst.Count; j++)
                        {
                            SrvSock.SendTo(lst[j], sender);
                        }
                        Thread.Sleep(40);
                    }
                }
                Thread.Sleep(500);
            }
        }
        private void ListenControl()
        {
            while (true)
            {
                int recv;
                byte[] data = new byte[1024];
                recv = SrvSock.ReceiveFrom(data, ref Remote);
                if (Encoding.UTF8.GetString(data, 0, recv).Contains("M"))
                {
                    String[] inf = Encoding.UTF8.GetString(data, 0, recv).Split('M');
                    String[] arr = new String[2];
                    int i = 0;
                    foreach (string info in inf)
                    {
                        arr[i] = info;
                        i++;
                    }
                    try
                    {
                        string[] cord_X = arr[0].Split(',');
                        string[] cord_Y = arr[1].Split(',');
                        string cordX = cord_X[0];
                        string cordY = cord_Y[0];
                        if (cordX.Contains("-"))
                        {
                            double timeX = double.Parse(cordX);
                            timeX = (timeX / 10) * (-1);
                            cordX = timeX.ToString();
                            string[] timeCordX = cordX.Split(',');
                            cordX = timeCordX[0];
                        }
                        else
                        {
                            double timeX = double.Parse(cordX);
                            timeX = (timeX / 10) * (-1);
                            cordX = timeX.ToString();
                            string[] timeCordX = cordX.Split(',');
                            cordX = timeCordX[0];
                        }
                        if (cordY.Contains("-"))
                        {
                            double timeY = double.Parse(cordY);
                            timeY = (timeY / 10) * (-1);
                            cordY = timeY.ToString();
                            string[] timeCordY = cordY.Split(',');
                            cordY = timeCordY[0];
                        }
                        else
                        {
                            double timeY = double.Parse(cordY);
                            timeY = (timeY / 10) * (-1);
                            cordY = timeY.ToString();
                            string[] timeCordY = cordY.Split(',');
                            cordY = timeCordY[0];
                        }
                        System.Drawing.Point defPnt = new System.Drawing.Point();
                        NativeMethods.GetCursorPos(ref defPnt);
                        string[] current_X = (defPnt.X).ToString().Split(',');
                        string[] current_Y = (defPnt.Y).ToString().Split(',');
                        string currentX = current_X[0];
                        string currentY = current_Y[0];
                        NativeMethods.SetCursorPos(int.Parse(cordX) + int.Parse(currentX), int.Parse(cordY) + int.Parse(currentY));
                    }
                    catch (FormatException) { }
                    catch (NullReferenceException) { }
                    catch (InvalidOperationException) { }
                    catch (EntryPointNotFoundException) { }
                }
                else if (Encoding.UTF8.GetString(data, 0, recv).Contains("LClick"))
                {
                    NativeMethods.mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    NativeMethods.mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                }
                else if (Encoding.UTF8.GetString(data, 0, recv).Contains("RClick"))
                {
                    NativeMethods.mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                    NativeMethods.mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                }
            }
        }
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
        private byte[] ConvertToByte(Bitmap bmp)
        {
            MemoryStream memoryStream = new MemoryStream();
            ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
            EncoderParameters myEncoderParameters = new EncoderParameters(1);
            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 50L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            bmp = new Bitmap(bmp, 800, 600);
            bmp.Save(memoryStream, jpgEncoder, myEncoderParameters);
            return memoryStream.ToArray();
        }
        private Bitmap TakeScreenShot(Screen currentScreen)
        {
            Bitmap bmpScreenShot = new Bitmap(currentScreen.Bounds.Width, currentScreen.Bounds.Height, System.Drawing.Imaging.PixelFormat.Format16bppRgb555);
            Graphics gScreenShot = Graphics.FromImage(bmpScreenShot);
            gScreenShot.CopyFromScreen(currentScreen.Bounds.X, currentScreen.Bounds.Y, 0, 0, currentScreen.Bounds.Size, CopyPixelOperation.SourceCopy);
            System.Drawing.Point defPnt = new System.Drawing.Point();
            NativeMethods.GetCursorPos(ref defPnt);
            Image cursor = Image.FromFile(Path.GetFullPath(@"..\\Release") + "\\img\\cursor.png");
            gScreenShot.DrawImage(cursor.GetThumbnailImage(26, 36, null, IntPtr.Zero), defPnt.X, defPnt.Y);
            return bmpScreenShot;
        }
        private List<byte[]> CutMsg(byte[] bt)
        {
            int Lenght = bt.Length;
            byte[] temp;
            List<byte[]> msg = new List<byte[]>();
            MemoryStream memoryStream = new MemoryStream();
            memoryStream.Write(BitConverter.GetBytes((short)((Lenght / 65500) + 1)), 0, 2);
            memoryStream.Write(bt, 0, bt.Length);
            memoryStream.Position = 0;
            while (Lenght > 0)
            {
                temp = new byte[65500];
                memoryStream.Read(temp, 0, 65500);
                msg.Add(temp);
                Lenght -= 65500;
            }
            return msg;
        }
        public void GetAddress()
        {
            while (true)
            {
                localAddress = (Dns.GetHostByName(Dns.GetHostName()).AddressList[0]).ToString();
                address.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    address.Text = localAddress.ToString();
                }));
                port.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    port.Text = localPort.ToString();
                }));
                if (localAddress.ToString() == "127.0.0.1")
                {
                    TypeCon.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        TypeCon.Source = new BitmapImage(new Uri(@"img/local-host.png", UriKind.RelativeOrAbsolute));
                    }));
                }
                else
                {
                    TypeCon.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        TypeCon.Source = new BitmapImage(new Uri(@"img/wi-fi-or-internet-con.png", UriKind.RelativeOrAbsolute));
                    }));
                }
                Thread.Sleep(5000);
            }
        }
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            CancelButton.IsEnabled = true;
            StartButton.IsEnabled = false;
            isOn.Source = new BitmapImage(new Uri(@"img/status-on.png", UriKind.RelativeOrAbsolute));
            listenerThread = new Thread(new ThreadStart(Listener));
            listenerThread.Start();
        }
        private void CloseForm(object sender, EventArgs e)
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelButton.IsEnabled = false;
            StartButton.IsEnabled = true;
            isOn.Source = new BitmapImage(new Uri(@"img/status-off.png", UriKind.RelativeOrAbsolute));
            try
            {
                listenerThread.Abort();
                controlThread.Abort();
            }
            catch (NullReferenceException) { }
        }
    }
}
public partial class NativeMethods
{
    [DllImportAttribute("user32.dll", EntryPoint = "SetCursorPos")]
    [return: MarshalAsAttribute(UnmanagedType.Bool)]
    public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(ref System.Drawing.Point lpPoint);
    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
}
