using System;
using System.IO;
using System.Net;
using System.Drawing;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Speech.Synthesis;
using System.Drawing.Drawing2D;
using System.Security.Principal;
using System.Collections.Generic;

namespace ScreenCast
{
    internal class Program
    {
        static HttpListener listener = new HttpListener();
        static HttpListenerContext ctx;
        static HttpListenerRequest request;
        static HttpListenerResponse response;
        static SpeechSynthesizer synthesizer = new SpeechSynthesizer();
        static void Main(string[] args)
        {
            if(!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                Process.Start(new ProcessStartInfo(Assembly.GetExecutingAssembly().Location) { Verb = "runas" });
                Environment.Exit(0);
            }
            synthesizer.SelectVoice(synthesizer.GetInstalledVoices()[Properties.Settings.Default.VoiceIndex].VoiceInfo.Name);
            synthesizer.SetOutputToDefaultAudioDevice();
            synthesizer.Rate = 2;

            listener.Prefixes.Add("http://+:8080/");
            listener.Prefixes.Add("http://*:8080/");
            listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            new Thread(new ThreadStart(NetworkThread)).Start();
            synthesizer.Speak("Сервер запущен на порту 8080");
            while(true)
            {
                List<IPAddress> adresses = new List<IPAddress>();
                foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    if (ip.AddressFamily == AddressFamily.InterNetwork && ip.ToString().StartsWith("192.168."))
                        adresses.Add(ip);
                synthesizer.SpeakAsync("Список локальных адресов компьютера, " + adresses.Count + " штук.");
                if(Console.ReadKey().Key == ConsoleKey.Escape) 
                { 
                    synthesizer.SpeakAsyncCancelAll(); 
                    break; 
                }
                foreach(IPAddress ip in adresses)
                    synthesizer.Speak(ip.ToString().Replace("."," "));
                synthesizer.Speak("Чтобы прослушать ещё раз нажмите ВВОД, или другую клавишу чтобы начать работу");
                if (Console.ReadKey().Key != ConsoleKey.Enter) break;
            }
        }
        protected static void NetworkThread()
        {
            listener.Start();
            while (true)
			{
				ctx = listener.GetContext();
				request = ctx.Request;
				response = ctx.Response;
				Stream os = response.OutputStream;
                Console.WriteLine("New Request");
                Console.WriteLine("User: " + request.UserHostAddress);
                Console.WriteLine("Path: " + request.Url.ToString() + "\n");
                if(request.Url.AbsolutePath.Contains("favicon"))
                {
                    Properties.Resources.desktop_icon.Save(os,ImageFormat.Png);
                    os.Close();
                    continue;
                }
                double ss = 0.5f;
                string query = request.Url.Query;
                bool parsed = true;
                if (query.Contains("?ss=")) 
                    parsed = double.TryParse(query.Substring("?ss=".Length), out ss);
                if (parsed)
                    getFromScreen((float)ss).Save(os,ImageFormat.Jpeg);
                else
                    SystemIcons.Error.ToBitmap().Save(os,ImageFormat.Png);
				os.Close();
			}
        }
        static Image getFromScreen(float shrinkScale)
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            Image img = new Bitmap(bounds.Width,bounds.Height);
            Graphics g = Graphics.FromImage(img);
            g.CopyFromScreen(0, 0, bounds.X, bounds.Y, bounds.Size,CopyPixelOperation.SourceCopy);
            g.DrawImage(Properties.Resources.cursor_on_the_cheap, Cursor.Position);
            return resizeImage(img,new Size((int)(bounds.Width * shrinkScale), (int)(bounds.Height * shrinkScale)),InterpolationMode.NearestNeighbor);
        }
        static Image resizeImage(Image imgToResize, Size size,InterpolationMode im)
        {
            Image img = new Bitmap(size.Width, size.Height);
            Graphics g = Graphics.FromImage(img);
            g.InterpolationMode = im;
            g.CompositingMode = CompositingMode.SourceCopy;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.DrawImage(imgToResize, 0, 0, size.Width, size.Height);
            g.Dispose();
            return img;
        }
    }
}
