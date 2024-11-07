using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace MCSControl
{
    public class MCSMessage
    {
        public MCSCommand Command { get; set; }
        public string Parameter1 { get; set; }
        public string Parameter2 { get; set; }

        // You can also add methods to serialize to/from JSON if needed.
    }
    public enum MCSCommand
    {
        Connect,
        Disconnect,
        Start,
        Stop,
        GetLatestCaptureId,
        GetCaptureImages,
        GetStatus,
        GetCameraCount,
        GetParamSettings,
        UpdateSettings,
        AddCamera,
        RemoveCamera,
        GetRecordingRootDIR,
        SetRecordingRootDIR,
        GetStatusSettings,
    }
    public class MCSAPI
    {
        private RequestSocket requester;
        const String address = "tcp://localhost:5555";

        public MCSAPI()
        {
            requester = new RequestSocket();
        }

        public (bool result, string status) Connect()
        {
            try
            {
                requester.Connect(address);
                var connectMessage = new MCSMessage
                {
                    Command = MCSCommand.Connect
                };
                var response = SendCommand(connectMessage);
                var receivedMCSMessage = JsonConvert.DeserializeObject<MCSMessage>(response);
                return (true, receivedMCSMessage.Parameter1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting: {ex.Message}");
                return (false, null);
            }
        }

        public void Disconnect()
        {
            try
            {
                requester.Disconnect(address);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting: {ex.Message}");
            }
        }

        public void Start()
        {
            var message = new MCSMessage
            {
                Command = MCSCommand.Start
            };
            SendCommand(message);
        }

        public void Stop()
        {
            var message = new MCSMessage
            {
                Command = MCSCommand.Stop
            };
            SendCommand(message);
        }

        public string GetLatestCaptureId()
        {
            var message = new MCSMessage
            {
                Command = MCSCommand.GetLatestCaptureId
            };
            var response = SendCommand(message);
            var receivedMCSMessage = JsonConvert.DeserializeObject<MCSMessage>(response);
            return receivedMCSMessage.Parameter1;
        }

        public string GetStatus()
        {
            var message = new MCSMessage
            {
                Command = MCSCommand.GetStatus
            };
            var response = SendCommand(message);
            var receivedMCSMessage = JsonConvert.DeserializeObject<MCSMessage>(response);
            return receivedMCSMessage.Parameter1;
        }

        public (Image img1, Image img2) GetCaptureImages()
        {
            var message = new MCSMessage
            {
                Command = MCSCommand.GetCaptureImages
            };
            var response = SendCommand(message);
            return ConvertCaptureImages(response);
        }

        private string SendCommand(MCSMessage message)
        {
            if (requester != null)
            {
                var jsonMessage = JsonConvert.SerializeObject(message);
                requester.SendFrame(jsonMessage);

                string receivedMessage = requester.ReceiveFrameString();
                return receivedMessage; // May need further processing based on command type
            }

            return null;
        }

        private (Image img1, Image img2) ConvertCaptureImages(string response)
        {
            var receivedMCSMessage = JsonConvert.DeserializeObject<MCSMessage>(response);
            Image image1 = receivedMCSMessage.Parameter1 != null ? ConvertBase64ToImage(receivedMCSMessage.Parameter1) : null;
            Image image2 = receivedMCSMessage.Parameter2 != null ? ConvertBase64ToImage(receivedMCSMessage.Parameter2) : null;

            return (image1, image2);
        }

        private Image ConvertBase64ToImage(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                return Image.FromStream(ms);
            }
        }

        public string GetCameraCount()
        {
            var message = new MCSMessage
            {
                Command = MCSCommand.GetCameraCount
            };
            var response = SendCommand(message);
            var receivedMCSMessage = JsonConvert.DeserializeObject<MCSMessage>(response);
            return receivedMCSMessage.Parameter1;
        }

        public Dictionary<string, Dictionary<string, string>> GetParamSettings()
        {
            var message = new MCSMessage
            {
                Command = MCSCommand.GetParamSettings
            };
            var response = SendCommand(message);
            var receivedMCSMessage = JsonConvert.DeserializeObject<MCSMessage>(response);
            return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(receivedMCSMessage.Parameter1);
        }

        public void UpdateSettings(Dictionary<string, Dictionary<string, string>> Categories)
        {
            var message = new MCSMessage
            {
                Command = MCSCommand.UpdateSettings,
                Parameter1 = JsonConvert.SerializeObject(Categories)
            };
            SendCommand(message);
        }

        public void AddCamera()
        {
            var message = new MCSMessage
            {
                Command = MCSCommand.AddCamera
            };
            SendCommand(message);
        }

        public void RemoveCamera()
        {
            var message = new MCSMessage
            {
                Command = MCSCommand.RemoveCamera
            };
            SendCommand(message);
        }

        public string GetRecordingRootDIR()
        {
            var message = new MCSMessage
            {
                Command = MCSCommand.GetRecordingRootDIR
            };
            var response = SendCommand(message);
            var receivedMCSMessage = JsonConvert.DeserializeObject<MCSMessage>(response);
            return receivedMCSMessage.Parameter1;
        }

        public Dictionary<string, Dictionary<string, string>> GetStatusSettings()
        {
            var message = new MCSMessage
            {
                Command = MCSCommand.GetStatusSettings
            };
            var response = SendCommand(message);
            var receivedMCSMessage = JsonConvert.DeserializeObject<MCSMessage>(response);
            return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(receivedMCSMessage.Parameter1);
        }
    }
}
