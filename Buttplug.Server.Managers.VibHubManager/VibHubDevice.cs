using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using Buttplug.Devices;

namespace Buttplug.Server.Managers.VibHubManager
{
    class VibHubDevice : IButtplugDevice
    {
        private readonly uint _deviceNumber;
        private readonly Dictionary<Type, MessageAttributes>_msgTypes = new Dictionary<Type, MessageAttributes>();
        private readonly VibHubManager _manager;

        private readonly IButtplugLog _bpLogger;

        private readonly uint[] _speeds = { 0, 0, 0, 0 };

        public VibHubDevice(string deviceId, uint aDeviceNum, VibHubManager aManager, IButtplugLogManager aLogManager)
        {
            Identifier = deviceId;
            _deviceNumber = aDeviceNum;
            _manager = aManager;
            _bpLogger = aLogManager.GetLogger(GetType());

            _msgTypes.Add(typeof(StopDeviceCmd), new MessageAttributes());
            _msgTypes.Add(typeof(SingleMotorVibrateCmd), new MessageAttributes());
            _msgTypes.Add(typeof(VibrateCmd), new MessageAttributes(4));
        }

        public string Name => "VibHub Device";

        public string Identifier { get; }

        public IEnumerable<Type> AllowedMessageTypes => _msgTypes.Keys;

        public bool Connected { get; private set; } = true;

        public event EventHandler DeviceRemoved;
        public event EventHandler<MessageReceivedEventArgs> MessageEmitted;

        public Task<ButtplugMessage> ParseMessageAsync(ButtplugDeviceMessage aMsg, CancellationToken aToken = default)
        {
            if (!Connected)
            {
                throw new ButtplugDeviceException(_bpLogger, $"{Name} has disconnected and can no longer process messages.", aMsg.Id);
            }

            if (!_msgTypes.ContainsKey(aMsg.GetType()))
            {
                throw new ButtplugDeviceException(_bpLogger, $"{Name} cannot handle message of type {aMsg.GetType().Name}", aMsg.Id);
            }

            switch (aMsg)
            {
                case SingleMotorVibrateCmd cmd:
                {
                    var changed = false;
                    var newSpeed = Convert.ToUInt16(Math.Min(cmd.Speed * 255, 255));
                    for (var i = 0; i < _speeds.Length; i++)
                    {
                        if (_speeds[i] == newSpeed)
                        {
                            continue;
                        }

                        _speeds[i] = newSpeed;
                        changed = true;
                    }

                    if (changed)
                    {
                        _manager.sendPwm(_deviceNumber, _speeds);
                    }

                    break;
                }
                case StopDeviceCmd _:
                {
                    for (var i = 0; i < _speeds.Length; i++)
                    {
                        _speeds[i] = 0;
                    }

                    _manager.sendPwm(_deviceNumber, _speeds);
                    break;
                }
                case VibrateCmd cmd:
                {
                    var changed = false;
                    foreach (var vib in cmd.Speeds)
                    {
                        if (vib.Index >= 4)
                        {
                            continue;
                        }

                        var newSpeed = Convert.ToUInt16(Math.Min(vib.Speed * 255, 255));
                        if (_speeds[vib.Index] == newSpeed)
                        {
                            continue;
                        }

                        _speeds[vib.Index] = newSpeed;
                        changed = true;
                    }

                    if (changed)
                    {
                        _manager.sendPwm(_deviceNumber, _speeds);
                    }

                    break;
                }
            }

            return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
        }

        public Task InitializeAsync(CancellationToken aToken = default)
        {
            _manager.sendPwm(_deviceNumber, _speeds);
            return Task.FromResult(new Ok(ButtplugConsts.SystemMsgId));
        }

        public MessageAttributes GetMessageAttrs(Type aMsg)
        {
            return _msgTypes[aMsg];
        }

        public void Disconnect()
        {
            Connected = false;
            DeviceRemoved?.Invoke(this, EventArgs.Empty);
        }
    }
}
