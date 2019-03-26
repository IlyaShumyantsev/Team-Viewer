using Android.App;
using Android.OS;
using Android.Widget;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using Android.Graphics;
using Android.Content.PM;
using Android.Views;

namespace PCV_client
{
    [Activity(Label = "PC Viewer", Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen", Icon = "@drawable/icon", ScreenOrientation = ScreenOrientation.UserLandscape)]
    class ContentPage : Activity, View.IOnTouchListener
    {
        ImageView connectionStatusImg;
        static string remoteAddress;
        static string localAddress;
        static string localPort;
        static string remotePort;
        IPEndPoint sender;
        Socket SrvSock;
        IPEndPoint ipep;
        EndPoint Remote;
        IPHostEntry hostname;
        float lastPointY, lastPointX;
        float widthInPx, heightInPx;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ContentPage);
            connectionStatusImg = FindViewById<ImageView>(Resource.Id.contentImg);
            SetNetState();
            Thread listenerThread = new Thread(new ThreadStart(Listener));
            listenerThread.Start();
            GetMethrics();
            connectionStatusImg.SetOnTouchListener(this);
        }
        public void SetNetState()
        {
            localAddress = Intent.GetStringExtra("localAddress");
            remoteAddress = Intent.GetStringExtra("remoteAddress");
            localPort = Intent.GetStringExtra("localPort");
            remotePort = Intent.GetStringExtra("remotePort");
            ipep = new IPEndPoint(IPAddress.Parse(localAddress), int.Parse(localPort));
            SrvSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sender = new IPEndPoint(IPAddress.Parse(remoteAddress), int.Parse(remotePort));
            Remote = new IPEndPoint(IPAddress.Any, int.Parse(remotePort));
            hostname = Dns.GetHostEntry(Dns.GetHostName());
            SrvSock.Bind(ipep);
        }
        public void GetMethrics()
        {
            var metrics = Resources.DisplayMetrics;
            widthInPx = metrics.WidthPixels;
            heightInPx = metrics.HeightPixels;
        }
        public bool OnTouch(View v, MotionEvent e)
        {
            switch (e.Action)
            {
                case MotionEventActions.Down:
                    {
                        lastPointY = e.GetY();
                        lastPointX = e.GetX();
                        if (lastPointX >= widthInPx - (widthInPx / 10) && lastPointY > heightInPx - (heightInPx/2))
                        {
                            byte[] data = new byte[1024];
                            data = System.Text.Encoding.UTF8.GetBytes("LClick");
                            SrvSock.SendTo(data, sender);
                        }
                        else if(lastPointX >= widthInPx - (widthInPx / 10) && lastPointY < heightInPx - (heightInPx / 2))
                        {
                            byte[] data = new byte[1024];
                            data = System.Text.Encoding.UTF8.GetBytes("RClick");
                            SrvSock.SendTo(data, sender);
                        }
                        return true;
                    }
                case MotionEventActions.Move:
                    {
                        var currentPossitionX = e.GetX();
                        var currentPossitionY = e.GetY();
                        var deltaX = lastPointX - currentPossitionX;
                        var deltaY = lastPointY - currentPossitionY;
                        byte[] data = new byte[1024];
                        data = System.Text.Encoding.UTF8.GetBytes(deltaX + "M" + deltaY);
                        SrvSock.SendTo(data, sender);
                        return true;
                    }
                default:
                    {
                        return v.OnTouchEvent(e);
                    }
            }
        }
        public void BuildImg(object obj)
        {
            MemoryStream memoryStream = new MemoryStream();
            memoryStream = (MemoryStream)obj;
            Bitmap bmp;
            try
            {
                bmp = BitmapFactory.DecodeByteArray(memoryStream.ToArray(), 0, (int)memoryStream.Length);
                RunOnUiThread(() =>
                {
                    connectionStatusImg.SetImageBitmap(bmp);
                });
            }
            catch { }
            memoryStream.Close();
        }
        public void Listener()
        {
            int countMsg;
            while (true)
            {
                byte[] data = new byte[65500];
                SrvSock.ReceiveFrom(data, ref Remote);
                MemoryStream memoryStream = new MemoryStream();
                memoryStream.Write(data, 2, data.Length - 2);
                countMsg = data[0] - 1; 
                if(countMsg < 10)
                {
                    Thread ImgBuilder = new Thread(new ParameterizedThreadStart(BuildImg));
                    for (int i = 0; i < countMsg; i++)
                    {
                        byte[] bt = new byte[65500];
                        SrvSock.ReceiveFrom(bt, ref Remote);
                        memoryStream.Write(bt, 0, bt.Length);
                    }
                    ImgBuilder.Start(memoryStream);
                }
            }
        }
    }
}