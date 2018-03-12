using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SendLink
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ConnectedWindow : Window
    {
        private String clientName;
        Socket mSocket;
        SQLiteHelper sqliteHelper;

        public ConnectedWindow(String clientName)
        {
            InitializeComponent();
            this.clientName = clientName;
            Loaded += MyWindow_Loaded;
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            sqliteHelper = new SQLiteHelper();
            mSocket = SocketHandler.getSocket();
            String clientIp = (IPAddress.Parse(((IPEndPoint)mSocket.RemoteEndPoint).Address.ToString())).ToString();

            // Update widgetText
            clientLabel.Text = clientName;
            hideProgressBar();
            

            // Start socket reading
            Thread thdHandler = new Thread(new ThreadStart(ReadingThread));
            thdHandler.IsBackground = true;
            thdHandler.Start();
        }

        private void SendToServer(String message)
        {
            showProgressBar();
            Task<Boolean> taskA = Task<Boolean>.Run(() =>
            {
                try
                {
                    NetworkStream networkStream = new NetworkStream(mSocket);
                    StreamWriter writer = new StreamWriter(networkStream);
                    writer.Write(message + Environment.NewLine);
                    writer.Flush();
                    Console.WriteLine("Sent:" + message);
                    return true;
                } catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }
                
            });
            taskA.Wait();
            Task.Delay(1500).ContinueWith(_ =>
            {
                hideProgressBar();
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (taskA.Result)
                    {
                        showSnackbar("Sent successfully", 2000);
                    }
                    else
                    {
                        showSnackbar("Failed to send", 2000);
                    }
                }));
           
            });
            
        }

        public void ReadingThread()
        {
            NetworkStream networkStream = new NetworkStream(mSocket);
            bool terminationCodeReceived = false;

            String s;
            try
            {
                while (!terminationCodeReceived)
                {
                    StreamReader sr = new StreamReader(networkStream);
                    if ((s = sr.ReadLine()) != null)
                    {
                        
                        Console.WriteLine("Received: " + s);
                        
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                Paste newPaste = new Paste
                                (
                                    s,
                                    DateTime.Now,
                                    clientName
                                );
                                listView.Items.Insert(0, newPaste);
                                sqliteHelper.InsertPaste(newPaste);
                            }));
                       
                    } else
                    {
                        DisconnectAndShowMain();
                    }
                }
            } catch (IOException e){
                Console.WriteLine("Unable to reach remote endpoint");
                
            }
        }

        private void DisconnectAndShowMain()
        {
            mSocket.Close();
            Console.WriteLine("Socket closed");
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageBox.Show(this, "Disconnected from " + clientName, "Disconnected");
                MainWindow mw = new MainWindow();
                this.Close();
                mw.ShowDialog();
                
            }
            ));
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            DisconnectAndShowMain();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            String inputText = TextBox.Text;

            if(inputText.Length != 0)
            {
                SendToServer(inputText);
                // Clean textbox once sent
                TextBox.Text = "";
            }
            
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                SendButton_Click(this, new RoutedEventArgs());
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            mSocket.Close();
            base.OnClosing(e);
        }

        private void hideProgressBar()
        {
            this.Dispatcher.Invoke((Action)(() => {
                SendingProgressBar.Visibility = Visibility.Hidden;
            }));
        }

        private void showProgressBar()
        {
            this.Dispatcher.Invoke((Action)(() => {
                SendingProgressBar.Visibility = Visibility.Visible;
            }));
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
            if (pasteString.StartsWith("http://") || pasteString.StartsWith("https://"))
            {
                Process.Start(pasteString);
            }
            else
            {
                showSnackbar("Invalid url, unable to open", 2000);
            }
        }
    }
}
