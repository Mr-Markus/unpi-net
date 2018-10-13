using BinarySerialization;
using System;

namespace UnpiNet
{
    /// <summary>
    /// TI Unified NPI Packet Format
    /// SOF(1) + Length(2/1) + Type/Sub(1) + Cmd(1) + Payload(N) + FCS(1)
    ///  
    /// Source: http://processors.wiki.ti.com/index.php/Unified_Network_Processor_Interface
    /// </summary>
    public class Packet
    {
        [Ignore()]
        private byte _SOF = 0xfe;

        public Packet()
        {
            
        }

        public Packet(MessageType type, SubSystem subSystem, byte commandId, byte[] payload = null)
        {
            Type = type;
            SubSystem = subSystem;
            Cmd1 = commandId;
            Payload = payload != null ? payload : new byte[0];

            if(payload != null)
            {
                Length = (byte)payload.Length;

                CalcFcs();
            }
        }

        /// <summary>
        /// Start of Frame(SOF) is set to be 0xFE (254)
        /// </summary>
        [FieldOrder(0)]
        public byte SOF
        {
            get
            {
                return _SOF;
            }
            set
            {
                _SOF = value;
            }
        }

        [FieldOrder(1)]
        public byte Length { get; set; }

        [FieldOrder(2)]
        [FieldBitLength(3)]
        public MessageType Type { get; set; }

        [FieldOrder(3)]
        [FieldBitLength(5)]
        public SubSystem SubSystem { get; set; }

        /// <summary>
        /// CMD0 is a 1 byte field that contains both message type and subsystem information 
        /// Bits[8-6]: Message type, see the message type section for more info.
        /// Bits[5-1]: Subsystem ID field, used to help NPI route the message to the appropriate place.
        /// 
        /// Source: http://processors.wiki.ti.com/index.php/NPI_Type_SubSystem
        /// </summary>
        [Ignore()]
        public byte Cmd0
        {
            get
            {
                return (byte)(((int)Type << 5) | ((int)SubSystem));
            }
            private set
            {
                Type = (MessageType)(value & 0xE0);
                SubSystem = (SubSystem)(value & 0x1F);
            }
        }

        /// <summary>
        /// CMD1 is a 1 byte field that contains the opcode of the command being sent
        /// </summary>
        [FieldOrder(4)]
        public byte Cmd1 { get; set; }

        /// <summary>
        /// Payload is a variable length field that contains the parameters defined by the 
        /// command that is selected by the CMD1 field. The length of the payload is defined by the length field.
        /// </summary>
        [FieldOrder(5)]
        [FieldLength(nameof(Length))]
        //[FieldChecksum(nameof(FrameCheckSequence), Mode = ChecksumMode.Xor)]
        public byte[] Payload { get; set; }

        /// <summary>
        /// Frame Check Sequence (FCS) is calculated by doing a XOR on each bytes of the frame in the order they are 
        /// send/receive on the bus (the SOF byte is always excluded from the FCS calculation): 
        ///     FCS = LEN_LSB XOR LEN_MSB XOR D1 XOR D2...XOR Dlen
        /// </summary>
        [FieldOrder(6)]
        public byte FrameCheckSequence { get; set; }

        //Self calculated checksum for check if incoming FrameSequenz is correct
        [Ignore()]
        public byte Checksum
        {
            get
            {
                byte[] preBuffer = new byte[3];

                preBuffer[0] = (byte)this.Length;
                preBuffer[1] = this.Cmd0;
                preBuffer[2] = this.Cmd1;

                return this.checksum(preBuffer, this.Payload);
            }
        }

        private byte checksum(byte[] buf1, byte[] buf2)
        {
            var fcs = (byte)0x00;
            var buf1_len = buf1.Length;
            var buf2_len = buf2 != null ? buf2.Length : 0;

            for (int i = 0; i < buf1_len; i += 1)
            {
                fcs ^= buf1[i];
            }

            if (buf2 != null)
            {
                for (int i = 0; i < buf2_len; i += 1)
                {
                    fcs ^= buf2[i];
                }
            }

            return fcs;
        }

        public void CalcFcs()
        {
            byte[] preBuffer = new byte[3];

            preBuffer[0] = (byte)this.Length;
            preBuffer[1] = this.Cmd0;
            preBuffer[2] = this.Cmd1;

            this.FrameCheckSequence = this.checksum(preBuffer, this.Payload);
        }
    }
}
