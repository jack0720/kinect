using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WebSocket4Net;
using Microsoft.Kinect;

namespace sender_csharp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        //[要変更]ここにWebsocket プロキシサーバのURLをセットします。
        private string serverURL = "ws://white.cs.inf.shizuoka.ac.jp:10808/";
        //[要変更]ここにチャンネル文字列（半角英数字・ブラウザ側と同じ文字列）をセットします
        private string channel = "aaa";

        private WebSocket websocket;
        private bool ready = false;

        KinectSensor kinect;

        public MainWindow()
        {
            InitializeComponent();

            //Kinectの初期化
            kinect = KinectSensor.KinectSensors[0];

            //イベントハンドラの登録
            kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(handler_SkeletonFrameReady);
            //骨格トラッキングの有効化
            kinect.SkeletonStream.Enable();

            kinect.Start();

            if (serverURL == "")
            {
                textBox1.Text = "URL不明！";
            }
            else
            {
                textBox1.Text = channel;
                websocket = new WebSocket(serverURL);
                websocket.Closed += new EventHandler(websocket_Closed);
                websocket.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(websocket_Error);
                websocket.MessageReceived += new EventHandler<MessageReceivedEventArgs>(websocket_MessageReceived);
                websocket.Opened += new EventHandler(websocket_Opened);
                websocket.Open();
            }
        }

        //WebSocketで文字列を送信するメソッド
        private void sendMessage(string cmd, string msg)
        {
            if (ready)
            {
                //channelを先頭に付けて送信
                websocket.Send(channel + ":" + cmd + "," + msg);
            }
        }
     
        private void websocket_Opened(object sender, EventArgs e)
        {
            //以下のブロックはスレッドセーフにGUIを扱うおまじない
            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                //ここにGUI関連の処理を書く。
                button1.IsEnabled = true;
                textBlock2.Text = "接続完了";
            }));

        }

        private void websocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            ready = false;
            //以下のブロックはスレッドセーフにGUIを扱うおまじない
            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                //ここにGUI関連の処理を書く。
                textBlock2.Text = "未接続";
                textBlock3.Text = "[error] " + e.Exception.Message + "\n";
                button1.IsEnabled = false;
            }));
            MessageBox.Show("予期しないエラーで通信が途絶しました。再接続には起動しなおしてください。");
        }

        private void websocket_Closed(object sender, EventArgs e)
        {
            ready = false;
            //以下のブロックはスレッドセーフにGUIを扱うおまじない
            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                //ここにGUI関連の処理を書く。
                textBlock2.Text = "未接続";
                textBlock3.Text = "[closed]\n";
                button1.IsEnabled = false;
            }));
            MessageBox.Show("サーバがコネクションを切断しました。");
        }

        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //  データ受信(サーバで当該チャンネルのモノのみ送る処理をしているが一応チェック)
            if (e.Message.IndexOf(channel+":") == 0) 
            {
                //チャンネル名などをメッセージから削除
                string msg = e.Message.Substring(e.Message.IndexOf(":")+1);
                //カンマ区切りを配列にする方法は↓
                string[] fields = msg.Split(new char[] { ',' });
                this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    //ここにGUI関連の処理を書く。
                    //配列をループで回してスラッシュを付けて表示
                    textBlock3.Text = "";
                    foreach (string field in fields) {
                        textBlock3.Text += field + "/";
                    }
                }));

            }
            else if (e.Message == channel + ";") 
            {
                //ハンドシェイク完了の受信
                ready = true;
                this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    textBlock2.Text = "ハンドシェイク完了";
                    button1.IsEnabled = false;
                }));
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(textBox1.Text, @"^[a-zA-Z0-9]+$"))
            {
                button1.IsEnabled = false;
                channel = textBox1.Text;
                //ハンドシェイクを送信
                websocket.Send(channel + ":");
            }
            else {
                MessageBox.Show("チャンネルは半角英数字のみ！");
            }
        }

            void handler_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
            {
                SkeletonFrame temp = e.OpenSkeletonFrame();
                if (temp != null)
                {
                        Skeleton[] skeletonData = new Skeleton[temp.SkeletonArrayLength];
                        temp.CopySkeletonDataTo(skeletonData);
                         foreach (Skeleton skeleton in skeletonData)
                        {
                            if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                            {
                                Joint right = skeleton.Joints[JointType.HandRight];
                                Console.WriteLine(right.Position.X + "    " + right.Position.Y + "    " + right.Position.Z + "\n");
                                    if(right.Position.Z < 1)
                                    {
                                    sendMessage("mouse", (300 + right.Position.X*300) + "," + (200 + right.Position.Y*-300));
                                    }
                            }
                        }
                    }
                }
            }
    }
