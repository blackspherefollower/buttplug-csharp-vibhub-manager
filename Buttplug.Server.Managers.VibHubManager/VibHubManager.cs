using System;
using System.IO;
using System.Linq;
using Buttplug.Core.Logging;
using SocketIOClient;

namespace Buttplug.Server.Managers.VibHubManager
{

    public class VibHubManager : TimedScanDeviceSubtypeManager
    {
        private SocketIO socket;

        public VibHubManager(IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
            BpLogger.Info("Loading VibHub Manager");

            //ToDo: Allow for custom ViBHub server address
            socket = new SocketIO("https://vibhub.io");
            socket.On("dev_online", OnDeviceOnline);
            socket.On("dev_offline", OnDeviceOffline);
            socket.On("aCustom", OnCustomMessage);

        }

        private void OnDeviceOnline(object data)
        {
            // Track registered device coming online
        }

        private void OnDeviceOffline(object data)
        {
            // Track registered device going offline
        }

        private void OnCustomMessage(object data)
        {
            if (data is string[])
            {
                //var task = data[0];
                //var val = data[1] ;
                //BpLogger.Trace("Message received. Id:", id, "SID:", sid, "Task", task, "Val", val);
            }

        }

        protected override void RunScan()
        {
            BpLogger.Info("VibHubManager start scanning");
            try
            {
                var devices = File.ReadAllLines(@"vibhub.txt");
                if (!devices.Any())
                {
                    return;
                }

                if (!socket.Connected)
                {
                    socket.ConnectAsync();
                    socket.EmitAsync("app", "Buttplug Server");
                }

                foreach (var dev in devices)
                {
                    if (dev.Length > 0)
                    {
                        socket.EmitAsync("hookup", data => HookupAck(data, dev), dev);
                    }
                }
            }
            catch (FileNotFoundException e)
            {
                BpLogger.Info(e.Message);
            }
            catch (Exception e)
            {
                BpLogger.Error(e.Message);
            }

            InvokeScanningFinished();
        }

        internal async void sendPwm(uint deviceNumber, uint[] pwm)
        {
            var data = deviceNumber.ToString("X2");
            data = pwm.Aggregate(data, (current, speed) => current + speed.ToString("X2"));
            await socket.EmitAsync("p", data);
        }

        private void HookupAck(SocketIOResponse data, string deviceID)
        {
            var devices = data.GetValue<string[]>();
            var deviceNum = devices.ToList().IndexOf(deviceID);
            if (deviceNum >= 0)
            {
                InvokeDeviceAdded(new DeviceAddedEventArgs(new VibHubDevice(deviceID, (uint)deviceNum, this, LogManager)));
            }
        }
    }
}
