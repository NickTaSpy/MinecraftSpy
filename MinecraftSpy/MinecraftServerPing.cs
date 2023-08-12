using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MinecraftSpy;

// Info: https://wiki.vg/Server_List_Ping
public sealed class MinecraftServerPing
{
    private static readonly byte[] _id = new byte[] { 0x00 };
    private static readonly byte[] _localAddress = Encoding.UTF8.GetBytes("localhost");

    private readonly byte[] _buffer = new byte[short.MaxValue];
    private int _offset;

    public async Task<PingPayload> Ping(IPAddress[] addresses, short port, TimeSpan timeout, CancellationToken ct = default)
    {
        ClearBuffer();

        using var client = new TcpClient();

        using var timeoutCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCancellationToken.CancelAfter(timeout);

        await client.ConnectAsync(addresses, port, timeoutCancellationToken.Token);

        ct.ThrowIfCancellationRequested();
        using var stream = client.GetStream();

        // Handshake
        WriteVarInt(760);
        WriteVarInt(_localAddress.Length);
        WriteBytes(_localAddress);
        WriteShort(port);
        WriteVarInt(1);
        await Flush(stream, ct);

        // Status Request
        await Flush(stream, ct);

        // Response
        await stream.ReadAsync(_buffer, ct);
        ct.ThrowIfCancellationRequested();

        int length = ReadVarInt();
        int packet = ReadVarInt();
        int jsonLength = ReadVarInt();

        var json = ReadString(jsonLength);
        return JsonConvert.DeserializeObject<PingPayload>(json) ?? throw new Exception("Could not read the json response.");
    }

    #region Read/Write methods
    private byte ReadByte()
    {
        return _buffer[_offset++];
    }

    private int ReadVarInt()
    {
        var value = 0;
        var size = 0;
        int b;
        while (((b = ReadByte()) & 0x80) == 0x80)
        {
            value |= (b & 0x7F) << (size++ * 7);
            if (size > 5)
            {
                throw new IOException("This VarInt is an imposter!");
            }
        }
        return value | ((b & 0x7F) << (size * 7));
    }

    private string ReadString(int length)
    {
        var str = Encoding.UTF8.GetString(_buffer, _offset, length);
        _offset += length;
        return str;
    }

    private void WriteBytes(byte[] bytes)
    {
        Buffer.BlockCopy(bytes, 0, _buffer, _offset, bytes.Length);
        _offset += bytes.Length;
    }

    private void WriteByte(byte value)
    {
        _buffer[_offset++] = value;
    }

    private void WriteVarInt(int value)
    {
        while ((value & 128) != 0)
        {
            WriteByte((byte)((value & 127) | 128));
            value = (int)(uint)value >> 7;
        }
        WriteByte((byte)value);
    }

    private void WriteShort(short value)
    {
        WriteByte((byte)(value & 0xFF));
        WriteByte((byte)((value >> 8) & 0xFF));
    }

    private async Task Flush(NetworkStream stream, CancellationToken ct = default)
    {
        int requestOffset = _offset;
        WriteVarInt(requestOffset + _id.Length);
        int lengthOffset = _offset;

        await stream.WriteAsync(_buffer.AsMemory(requestOffset, lengthOffset - requestOffset), ct);
        await stream.WriteAsync(_id, ct);

        if (requestOffset > 0)
        {
            await stream.WriteAsync(_buffer.AsMemory(0, requestOffset), ct);
        }

        ClearBuffer();
        ct.ThrowIfCancellationRequested();
    }

    private void ClearBuffer()
    {
        _offset = 0;
    }
    #endregion
}
