using Newtonsoft.Json;
using System.Net.Sockets;
using System.Text;

namespace MinecraftSpy;

// Info: https://wiki.vg/Server_List_Ping
public sealed class MinecraftServerPing
{
    private static readonly byte[] _id = new byte[] { 0x00 };
    private static readonly byte[] _localAddress = Encoding.UTF8.GetBytes("localhost");

    private TcpClient? _client;
    private NetworkStream? _stream;

    private readonly byte[] _buffer = new byte[short.MaxValue];
    private int _offset;

    public async Task<PingPayload> Ping(string host, short port, CancellationToken ct = default)
    {
        ClearBuffer();

        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port, ct);
            _stream = _client.GetStream();

            // Handshake
            WriteVarInt(760);
            WriteVarInt(_localAddress.Length);
            WriteBytes(_localAddress);
            WriteShort(port);
            WriteVarInt(1);
            await Flush(ct);

            // Status Request
            await Flush(ct);

            // Response
            await _stream.ReadAsync(_buffer, ct);

            int length = ReadVarInt();
            int packet = ReadVarInt();
            int jsonLength = ReadVarInt();

            var json = ReadString(jsonLength);
            var result = JsonConvert.DeserializeObject<PingPayload>(json);

            if (result is null)
            {
                throw new Exception("Could not read the json response.");
            }

            return result;
        }
        finally
        {
            _client?.Dispose();
        }
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
            WriteByte((byte)(value & 127 | 128));
            value = (int)((uint)value) >> 7;
        }
        WriteByte((byte)value);
    }

    private void WriteShort(short value)
    {
        WriteByte((byte)(value & 0xFF));
        WriteByte((byte)((value >> 8) & 0xFF));
    }

    private async Task Flush(CancellationToken ct = default)
    {
        if (!_client.Connected || _stream is null)
        {
            throw new InvalidOperationException("The TCP client is not connected or the stream is unavailable.");
        }

        int requestOffset = _offset;
        WriteVarInt(requestOffset + _id.Length);
        int lengthOffset = _offset;

        await _stream.WriteAsync(_buffer.AsMemory(requestOffset, lengthOffset - requestOffset), ct);
        await _stream.WriteAsync(_id, ct);

        if (requestOffset > 0)
        {
            await _stream.WriteAsync(_buffer.AsMemory(0, requestOffset), ct);
        }

        ClearBuffer();
    }

    private void ClearBuffer()
    {
        _offset = 0;
    }
    #endregion
}
