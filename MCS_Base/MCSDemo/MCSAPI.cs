using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using PalletCheck;
using System;
using System.Threading.Tasks;

namespace MCSControl
{
    public class MCSMessage
    {
        public MCSCommand Command { get; set; }
        public string Parameter1 { get; set; }
        public string Parameter2 { get; set; }

        // You can also add methods to serialize to/from JSON if needed.
    }
    public class MCSAPI
    {
        private RequestSocket requester;
        const String address = "tcp://localhost:6666";

        public MCSAPI()
        {
            try
            {
                requester = new RequestSocket();
                requester.Connect(address);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting: {ex.Message}");
            }
        }

        public void sendCaptureId(string captureId)
        {
            var connectMessage = new MCSMessage
            {
                Command = MCSCommand.CaptureId,
                Parameter1 = captureId,
            };
            SendCommand(connectMessage);
        }

        private string SendCommand(MCSMessage message, int timeoutMilliseconds = 2000)
        {
            if (requester != null)
            {
                try
                {
                    var jsonMessage = JsonConvert.SerializeObject(message);
                    requester.SendFrame(jsonMessage);

                    var receiveTask = Task.Run(() => requester.ReceiveFrameString());
                    var timeoutTask = Task.Delay(timeoutMilliseconds);

                    var completedTask = Task.WhenAny(receiveTask, timeoutTask).Result;
                    if (completedTask == receiveTask)
                    {
                        return receiveTask.Result;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception e)
                {
                    return null;
                }
            }

            return null;
        }
    }
}
