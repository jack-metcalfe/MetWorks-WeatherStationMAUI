This library exposes 3 static interfaces to create and parse UDP packets for the Tempest protocol.

IRawPacketRecordFactory
IRawPacketRecordTypedFactory
IPacketFactory

Each has two functions for creating and parsing packets with one supporting ReadOnlyMemory<char> and 
the other supporting Span<char> for high performance scenarios.

The first two interfaces create and parse raw UDP packets represented as byte arrays or memory segments.

The last interface creates and parses strongly typed packet records represented as C# classes or structs.