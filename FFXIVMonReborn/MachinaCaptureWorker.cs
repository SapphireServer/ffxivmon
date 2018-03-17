using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FFXIVMonReborn.DataModel;
using FFXIVMonReborn.Views;
using Machina;
using Machina.FFXIV;

namespace FFXIVMonReborn
{
    public class MachinaCaptureWorker
    {
        [Flags]
        public enum ConfigFlags
        {
            None                   = 0,
            StripHeaderActors      = 1 << 0,
            DontUsePacketTimestamp = 1 << 1
        }

        private readonly XivMonTab _myTab;
        private readonly TCPNetworkMonitor.NetworkMonitorType _monitorType;
        private readonly ConfigFlags _configFlags;

        private volatile bool _shouldStop;

        public MachinaCaptureWorker(XivMonTab window, TCPNetworkMonitor.NetworkMonitorType monitorType, ConfigFlags flags)
        {
            this._myTab = window;
            this._monitorType = monitorType;
            this._configFlags = flags;
        }

        private void MessageReceived(long epoch, byte[] message, int set)
        {
            var res = Parse(message);

            var item = new PacketEntry { IsVisible = true, ActorControl = -1, Data = message, Message = res.header.MessageType.ToString("X4"), Direction = "S",
                Category = set.ToString(), Timestamp = Util.UnixTimeStampToDateTime(res.header.Seconds).ToString(@"MM\/dd\/yyyy HH:mm:ss"), Size = res.header.MessageLength.ToString(), 
                Set = set, RouteID = res.header.RouteID.ToString(), PacketUnixTime = res.header.Seconds, SystemMsTime = Millis() };

            if (_configFlags.HasFlag(ConfigFlags.DontUsePacketTimestamp))
            {
                item.Timestamp = DateTime.Now.ToString(@"MM\/dd\/yyyy HH:mm:ss.fff tt");
            }

            _myTab.Dispatcher.Invoke(new Action(() => { _myTab.AddPacketToListView(item); }));
        }

        private void MessageSent(long epoch, byte[] message, int set)
        {
            var res = Parse(message);

            var item = new PacketEntry { IsVisible = true, ActorControl = -1, Data = message, Message = res.header.MessageType.ToString("X4"), Direction = "C",
                Category = set.ToString(), Timestamp = Util.UnixTimeStampToDateTime(res.header.Seconds).ToString(@"MM\/dd\/yyyy HH:mm:ss"), Size = res.header.MessageLength.ToString(),
                Set = set, RouteID = res.header.RouteID.ToString(), PacketUnixTime = res.header.Seconds, SystemMsTime = Millis() };

            if (_configFlags.HasFlag(ConfigFlags.DontUsePacketTimestamp))
            {
                item.Timestamp = DateTime.Now.ToString(@"MM\/dd\/yyyy HH:mm:ss.fff tt");
            }

            _myTab.Dispatcher.Invoke(new Action(() => { _myTab.AddPacketToListView(item); }));
        }

        private static ParseResult Parse(byte[] data)
        {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            FFXIVMessageHeader head = (FFXIVMessageHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(FFXIVMessageHeader));
            handle.Free();

            ParseResult result = new ParseResult();
            result.header = head;
            result.data = data;

            return result;
        }

        public void Run()
        {
            FFXIVNetworkMonitor monitor = new FFXIVNetworkMonitor();
            monitor.MonitorType = _monitorType;
            monitor.MessageReceived = (long epoch, byte[] message, int set) => MessageReceived(epoch, message, set);
            monitor.MessageSent = (long epoch, byte[] message, int set) => MessageSent(epoch, message, set);
            monitor.Start();

            while (!_shouldStop)
            {
                // So don't burn the cpu while doing nothing
                Thread.Sleep(1);
            }

            Console.WriteLine("MachinaCaptureWorker: Terminating");
            monitor.Stop();
        }

        public void Stop()
        {
            _shouldStop = true;
        }

        internal class ParseResult
        {
            public FFXIVMessageHeader header;
            public byte[] data;
        }
        
        private long Millis() {
            return (long.MaxValue + DateTime.Now.ToBinary()) / 10000;
        }
    }
}
