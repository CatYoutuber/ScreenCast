using System;
using System.IO;
using System.Net;
using System.Drawing;
using System.Threading;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Speech.Synthesis;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ScreenCast
{
    internal class Program
    {
        static HttpListener listener = new HttpListener();
        static HttpListenerContext ctx;
        static HttpListenerRequest request;
        static HttpListenerResponse response;
        static SpeechSynthesizer synthesizer = new SpeechSynthesizer();
        static readonly bool NoTTS = false; //dlya otladki
        static void Main(string[] args)
        {
            synthesizer.SelectVoice(synthesizer.GetInstalledVoices()[Properties.Settings.Default.VoiceIndex].VoiceInfo.Name);
            synthesizer.SetOutputToDefaultAudioDevice();
            synthesizer.Rate = 2;

            listener.Prefixes.Add("http://+:8080/");
            listener.Prefixes.Add("http://*:8080/");
            new Thread(new ThreadStart(NetworkThread)).Start();
            if (!NoTTS)
            {
                synthesizer.Speak("Сервер запущен на порту 8080");

                List<IPAddress> adresses = new List<IPAddress>();
                foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    if (ip.AddressFamily == AddressFamily.InterNetwork && ip.ToString().StartsWith("192.168."))
                        adresses.Add(ip);
                while (true)
                {
                    synthesizer.SpeakAsync("Список локальных адресов компьютера, " + adresses.Count + " штук. Нажмите ВВОД");
                    if (Console.ReadKey().Key == ConsoleKey.Escape)
                    {
                        synthesizer.SpeakAsyncCancelAll();
                        break;
                    }
                    foreach (IPAddress ip in adresses)
                        synthesizer.Speak(ip.ToString().Replace(".", " "));
                    synthesizer.Speak("Чтобы прослушать ещё раз нажмите ВВОД, или другую клавишу чтобы начать работу");
                    if (Console.ReadKey().Key != ConsoleKey.Enter) break;
                }
            }
        }
        protected static void NetworkThread()
        {
            listener.Start();
            while (true)
			{
                try
                {
                    ctx = listener.GetContext();
                    request = ctx.Request;
                    response = ctx.Response;
                    Stream os = response.OutputStream;
                    Console.WriteLine("New Request");
                    Console.WriteLine("Path: " + request.Url.ToString() + "\n");

                    if (request.Url.AbsolutePath.Contains("favicon"))
                    {
                        Properties.Resources.desktop_icon.Save(os, ImageFormat.Png);
                        os.Close();
                        continue;
                    }
                    if (!request.Url.AbsolutePath.Contains("img"))
                    {
                        StreamWriter sw = new StreamWriter(os);
                        sw.WriteLine(Properties.Resources.main);
                        sw.Close();
                        os.Close();
                    }
                    else
                    {
                        double ss = 0.5f;
                        bool parsed = true;
                        if (request.QueryString.Get("ss") != null)
                            parsed = double.TryParse(request.QueryString.Get("ss"), out ss);
                        if (parsed)
                            getFromScreen((float)ss).Save(os, ImageFormat.Jpeg);
                        else
                            SystemIcons.Error.ToBitmap().Save(os, ImageFormat.Png);
                        os.Close();
                    }
                } catch (Exception ex) { 
                    Console.WriteLine(ex.ToString()); synthesizer.SpeakAsync(ex.Message); }
			}
        }
        static Image getFromScreen(float shrinkScale)
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            Image img = new Bitmap(bounds.Width,bounds.Height);
            Graphics g = Graphics.FromImage(img);
            g.CopyFromScreen(0, 0, bounds.X, bounds.Y, bounds.Size, CopyPixelOperation.SourceCopy);

            Win32.CURSORINFO pci = new Win32.CURSORINFO();
            pci.cbSize = Marshal.SizeOf(typeof(Win32.CURSORINFO));
            Win32.GetCursorInfo(ref pci);

            IntPtr hDC = g.GetHdc();
            Win32.DrawIconEx(hDC, pci.ptScreenPos.x, pci.ptScreenPos.y, pci.hCursor, 0, 0, 0, IntPtr.Zero, 3); //DI_NOMAL = 0x0003
            g.ReleaseHdc();

            //g.DrawImage(Properties.Resources.cursor_on_the_cheap, Cursor.Position);
            return resizeImage(img,new Size((int)(bounds.Width * shrinkScale), (int)(bounds.Height * shrinkScale)));
        }
        static Image resizeImage(Image imgToResize, Size size)
        {
            Image img = new Bitmap(size.Width, size.Height);
            Graphics g = Graphics.FromImage(img);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.CompositingMode = CompositingMode.SourceCopy;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.DrawImage(imgToResize, 0, 0, size.Width, size.Height);
            g.Dispose();
            return img;
        }
    }
}
