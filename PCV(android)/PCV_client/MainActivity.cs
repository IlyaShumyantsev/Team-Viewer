using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content;
using System;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace PCV_client
{
    [Activity(Label = "PC Viewer", MainLauncher = true, Theme = "@android:style/Theme.Black.NoTitleBar", Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        static string remoteAddress;
        static string localAddress;
        static string remotePort;
        static string localPort;
        UdpClient server;
        IPEndPoint ipep, senderDat;
        Socket SrvSock;
        IPHostEntry hostname;
        EditText ipEdit, portEdit;
        Button connectButton, clearButton;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            hostname = Dns.GetHostEntry(Dns.GetHostName());
            GetIpAndCreateSocket();
            SetContentView(Resource.Layout.Main);
            InitInterfaceElements();
            connectButton.Click += (object sender, System.EventArgs e) =>
            {
                remoteAddress = ipEdit.Text;
                remotePort = portEdit.Text;
                PortGeneration();
                try
                {
                    ipep = new IPEndPoint(IPAddress.Parse(localAddress), int.Parse(localPort));
                    SrvSock.Bind(ipep);
                }
                catch (Exception) { }
                try
                {
                    senderDat = new IPEndPoint(IPAddress.Parse(remoteAddress), int.Parse(remotePort));
                    byte[] data = new byte[1024];
                    data = Encoding.UTF8.GetBytes(localAddress + "@" + localPort);
                    SrvSock.SendTo(data, senderDat);
                    Intent intent = new Intent(this, typeof(ContentPage));
                    intent.PutExtra("localAddress", localAddress);
                    intent.PutExtra("localPort", localPort);
                    intent.PutExtra("remotePort", remotePort);
                    intent.PutExtra("remoteAddress", remoteAddress);
                    intent.PutExtra("localPort", localPort);
                    StartActivity(intent);
                }
                catch (Exception) { }
            };
            clearButton.Click += (object sender, System.EventArgs e) =>
            {
                ipEdit.Text = "";
                portEdit.Text = "";
            };
        }
        public void InitInterfaceElements()
        {
            connectButton = FindViewById<Button>(Resource.Id.connectButton);
            clearButton = FindViewById<Button>(Resource.Id.clearButton);
            ipEdit = FindViewById<EditText>(Resource.Id.ipEdit);
            portEdit = FindViewById<EditText>(Resource.Id.portEdit);
        }
        public void GetIpAndCreateSocket()
        {
            hostname = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ipS in hostname.AddressList)
            {
                if (ipS.AddressFamily == AddressFamily.InterNetwork)
                {
                    localAddress = ipS.ToString();
                    break;
                }
            }
            SrvSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }
        public void PortGeneration()
        {
            while (true)
            {
                if (localPort == null || localPort == remotePort)
                {
                    Random rnd = new Random();
                    localPort = rnd.Next(49152, 65535 + 1).ToString();
                    if (localPort != remotePort)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }
}
