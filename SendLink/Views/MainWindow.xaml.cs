using QRCoder;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SendLink
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ArrayList nSockets;
        private String clientIp;
        private String clientName;
        ConnectedWindow connectedWindow;
        TcpListener tcpListener;
        PasteListViewModel viewModel;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                Loaded += MyWindow_Loaded;
                viewModel = new PasteListViewModel();
                this.DataContext = viewModel;
                scrollViewer.ScrollChanged += OnScrollChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GenerateQR();
            nSockets = new ArrayList();
            
            Thread thdListener = new Thread(new ThreadStart(ListenerThread));

            thdListener.IsBackground = true;
            thdListener.Start();
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;
            if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
            {
                viewModel.LoadPastes(10);
            }
        }


        private void GenerateQR()
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode("SendLink://" + GetLocalIPAddress(), QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20, System.Drawing.Color.Black, System.Drawing.Color.White, SendLink.Properties.Resources.sendlink_roundicon);
            ImageSource imgsrc = BitmapToImageSource(qrCodeImage);
            QRimage.Source = imgsrc;
        }

        public static string GetLocalIPAddress()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var addr = ni.GetIPProperties().GatewayAddresses.FirstOrDefault();
                if (addr != null)
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        Console.WriteLine(ni.Name);
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                Console.WriteLine(ip.Address.ToString());
                                return ip.Address.ToString();
                            }
                        }
                    }
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        public void ListenerThread()
        {
            tcpListener = new TcpListener(IPAddress.Any, 8080);
            tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            tcpListener.Start();

            try
            {
                Socket handlerSocket = tcpListener.AcceptSocket();

                if (handlerSocket.Connected)
                {
                    tcpListener.Stop();
                    SocketHandler.setSocket(handlerSocket);
                    clientIp = (IPAddress.Parse(((IPEndPoint)handlerSocket.RemoteEndPoint).Address.ToString())).ToString();

                    // Send acknowledgement to client
                    NetworkStream networkStream = new NetworkStream(handlerSocket);
                    StreamWriter writer = new StreamWriter(networkStream);
                    writer.Write(Environment.MachineName + ": Connection accepted." + Environment.NewLine);
                    writer.Flush();

                    Console.WriteLine("Accepting connection from " + clientIp);

                    // Receive acknowledgement from client
                    StreamReader sr = new StreamReader(networkStream);
                    bool acknowledged = false;
                    while (!acknowledged)
                    {
                        if (sr.Peek() > 0)
                        {
                            String s = sr.ReadLine();
                            int separatorIndex = s.IndexOf(':');
                            if(separatorIndex != -1)
                            {
                                clientName = s.Substring(0, separatorIndex);
                            }
                            Console.WriteLine("Recevied: " + s);
                            acknowledged = true;
                        }
                    }

                    // Change window 
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        connectedWindow = new ConnectedWindow(clientName);
                        this.Close();
                        connectedWindow.ShowDialog();
                    }));
                }

            }
            catch (SocketException e)
            {
                if((e.SocketErrorCode == SocketError.Interrupted))
                {
                    Console.WriteLine("Socket interupted");
                
                }
            }
           

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            tcpListener.Stop();
            base.OnClosing(e);
        }


        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var pasteString = button.Tag.ToString();
            Clipboard.SetText(pasteString);
            showSnackbar("Copied " + pasteString, 2000);
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var pasteString = button.Tag.ToString();
            if(pasteString.StartsWith("http://") || pasteString.StartsWith("https://"))
            {
                Process.Start(pasteString);
            } else
            {
                showSnackbar("Invalid url, unable to open", 2000);
            }
        }

        private void showSnackbar(string message, int durationInMillis)
        {
            SnackBarMessage.Content = message;
            Snackbar.IsActive = true;
            Task.Delay(durationInMillis).ContinueWith(_ =>
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    Snackbar.IsActive = false;
                }));
            });
        }
    }

    public class DateFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            DateTime dt = DateTime.Parse(value.ToString());
            if(dt.Date == DateTime.Now.Date)
            {
                return "Today at " + dt.ToString("hh:mm tt");
            } else if ((dt - DateTime.Now.Date).TotalDays == 1){
                return "Yesterday at " + dt.ToString("hh:mm tt");
            } 
            return dt.ToString("d MMMM hh:mm tt");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

}
