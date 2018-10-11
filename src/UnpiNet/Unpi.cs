using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text;
using BinarySerialization;

namespace UnpiNet
{
    /// <summary>
    /// The unpi is the packet builder and parser for Texas Instruments Unified Network Processor Interface (UNPI) 
    /// used in RF4CE, BluetoothSmart, and ZigBee wireless SoCs. As stated in TI's wiki page:
    ///     TI's Unified Network Processor Interface (NPI) is used for establishing a serial data link between a TI SoC and 
    ///     external MCUs or PCs. This is mainly used by TI's network processor solutions.
    /// 
    /// The UNPI packet consists of sof, length, cmd0, cmd1, payload, and fcs fields.The description of each field 
    /// can be found in Unified Network Processor Interface.
    /// 
    /// It is noted that UNPI defines the length field with 2 bytes wide, but some SoCs use NPI in their real transmission (physical layer), 
    /// the length field just occupies a single byte. (The length field will be normalized to 2 bytes in the transportation layer of NPI stack.)
    /// 
    /// Source: http://processors.wiki.ti.com/index.php/Unified_Network_Processor_Interface?keyMatch=Unified%20Network%20Processor%20Interface&tisearch=Search-EN-Support
    /// 
    /// /*************************************************************************************************/
    /// /*** TI Unified NPI Packet Format                                                              ***/
    /// /***     SOF(1) + Length(2/1) + Type/Sub(1) + Cmd(1) + Payload(N) + FCS(1)                     ***/
    /// /*************************************************************************************************/
    /// </summary>
    public class Unpi
    {
        public SerialPort Port { get; set; }
        public int LenBytes;

        public event EventHandler<Packet> DataReceived;
        public event EventHandler<SerialErrorReceivedEventArgs> ErrorReceived;

        public event EventHandler Opened;
        public event EventHandler Closed;

        /// <summary>
        /// Create a new instance of the Unpi class.
        /// </summary>
        /// <param name="lenBytes">1 or 2 to indicate the width of length field. Default is 2.</param>
        /// <param name="stream">The transceiver instance, i.e. serial port, spi. It should be a duplex stream.</param>
        public Unpi(string port, int baudrate = 115200, int lenBytes = 2)
        {
            Port = new SerialPort(port, baudrate);

            Port.DataReceived += Port_DataReceived;
            Port.ErrorReceived += Port_ErrorReceived;

            LenBytes = lenBytes;
        }

        private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            ErrorReceived?.Invoke(sender, e);
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] data = new byte[Port.BytesToRead];
            Port.Read(data, 0, Port.BytesToRead);

            Receive(data);
        }

        public void Open()
        {
            if (Port != null)
            {
                Port.Open();

                Opened?.Invoke(this, EventArgs.Empty);
            } else
            {
                throw new NullReferenceException("Port is not created");
            }
        }

        public void Close()
        {
            if (Port.IsOpen)
            {
                Port.Close();

                Closed?.Invoke(this, EventArgs.Empty);
            }
        }

        public byte[] Send(int type, int subSystem, byte commandId, byte[] payload = null)
        {
            return Send((MessageType)type, (SubSystem)subSystem, commandId, payload);
        }

        public byte[] Send(MessageType type, SubSystem subSystem, byte commandId, byte[] payload = null)
        {
            Packet packet = new Packet(type, subSystem, commandId, payload);

            return Send(packet);
        }

        public byte[] Send(Packet packet)
        {
            packet.CalcFcs();

            var serializer = new BinarySerializer();

            using (MemoryStream stream = new MemoryStream())
            {
                serializer.Serialize(stream, packet);

                stream.Flush();

                byte[] data = stream.ToArray();

                return Send(data);
            }
        }

        public byte[] Send(byte[] data)
        {
            if (Port.IsOpen == false)
            {
                Port.Open();
            }
            
            Port.Write(data, 0, data.Length);

            return data;
        }

        public void Receive(byte[] buffer)
        {
            if(buffer == null || buffer.Length == 0)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if(buffer[0] != 0xfe) //Fix SOF
            {
                throw new FormatException("Buffer is not a vailid frame");
            }

            var serializer = new BinarySerializer();

            List<Packet> packets = new List<Packet>();

            using (MemoryStream stream = new MemoryStream(buffer))
            {
                packets.AddRange(serializer.Deserialize<List<Packet>>(stream));
            }
            foreach (Packet packet in packets)
            {            
                if(packet.FrameCheckSequence.Equals(packet.Checksum) == false)
                {
                    throw new Exception("Received FCS is not equal with new packet");
                }

                DataReceived?.Invoke(this, packet);
            }
        }        
    }
}
