using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetMQ;
using NetMQ.Sockets;

namespace MCSControl
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            /*Trace.WriteLine("Connecting to hello world server…");
            using (var requester = new RequestSocket())
            {
                requester.Connect("tcp://localhost:5555");

                int requestNumber;
                for (requestNumber = 0; requestNumber != 10; requestNumber++)
                {
                    Trace.WriteLine("Sending Hello {0}...", requestNumber.ToString());
                    requester.SendFrame("Client Request");
                    string str = requester.ReceiveFrameString();
                    Trace.WriteLine("Received World {0}", str);
                }
            }*/
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
