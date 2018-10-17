# unpi-net [![AppVeyor build status](https://ci.appveyor.com/api/projects/status/github/Mr-Markus/unpi-net?branch=master&svg=true)](https://ci.appveyor.com/project/Mr-Markus/unpi-net/branch/master) 
A .Net implementation of Texas Intrument's **Unified Network Processor Interface (UNPI)**

You want to communicate with Texas Instrument chips like CC253x (Zigbee) with .NET? Then unpi-net is the right library for you.

## Unified NPI Protocol

TI's Unified Network Processor Interface (NPI) is used for establishing a serial data link between a TI SoC and external MCUs or PCs. This is mainly used by TI's network processor solutions. NPI currently supports UART and SPI.

## Usage

```cs
Unpi unpi = new Unpi("COM3", 115200);
unpi.DataReceived += Unpi_DataReceived;
unpi.Opened += Unpi_Opened;
unpi.Closed += Unpi_Closed;

unpi.Open();
```

## Packet Format

| SOF | Byte 0 - 1 | Byte 2 | Byte 3 | Byte 4 - (LEN + 3) | FCS |
| :---: | :---: | :---: |:---: |:---: |:---: |
| Start of Frame  0xFF (254) | Length [13:0] Reserved [15:14] | Type/Sub System | Command ID | Payload | Frame Check Sequence |

The Unified NPI packetformat is explained below: 
* **Start of Frame(SOF)** is set to be 0xFE (254)
* **Length** field is 2 bytes long in little-endian format (so LSB first).
* **CMD0** is a 1 byte field that contains both message type and subsystem information 
  * Bits[7:5]: Message type, see the message type section for more info.
  * Bits[4:0]: Subsystem ID field, used to help NPI route the message to the appropriate place.
  * See the Message Types Table below for the Type and Subsystem ID field info
* **CMD1** is a 1 byte field that contains the opcode of the command being sent
* **Payload** is a variable length field that contains the parameters defined by the command that is selected by the CMD1 field. The length of the payload is defined by the length field.
* **Frame Check Sequence (FCS)** is calculated by doing a XOR on each bytes of the frame in the order they are send/receive on the bus (the SOF byte is always excluded from the FCS calculation): 
FCS = LEN_LSB XOR LEN_MSB XOR D1 XOR D2 ... XOR Dlen

### Message Types

| Code | Message Type |
| :---: | :---: |
| 0x01 | Synchronous Request (SREQ) |
| 0x02 | Asynchronous Request or Indication (AREQ/AIND) |
| 0x03 | Synchronous Response (SRESP) |

#### Synchronous Messages 
A Synchronous Request (SREQ) is a frame, defined by data content instead of the ordering of events of the physical interface, which is sent from the Host to NP where the next frame sent from NP to Host must be the Synchronous Response (SRESP) to that SREQ.

Note that once a SREQ is sent, the NPI interface blocks until a corresponding response(SRESP) is received.
#### Asynchronous Messages 
Asynchronous Request transfer initiated by Host Asynchronous Indication – transfer initiated by NP. 
Both types of asynchronous messages use the same message type bit field (0x02). The type of message (request/indication) depends on which processor initiated the transaction. 

Source:
[http://processors.wiki.ti.com/index.php/Unified_Network_Processor_Interface](http://processors.wiki.ti.com/index.php/Unified_Network_Processor_Interface)

## License
unpi-net is provided under [The MIT License](https://github.com/Mr-Markus/unpi-net/blob/master/LICENSE).

## Contributor

 [@Mr-Markus](https://github.com/Mr-Markus)
