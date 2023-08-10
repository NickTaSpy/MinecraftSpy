using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace MinecraftSpy;

public class ConnectionAddress
{
    public required IPAddress[] Addresses { get; set; }
    public required short Port { get; set; }
}

public class ConnectionAddressComparer : IEqualityComparer<ConnectionAddress>
{
    public static readonly ConnectionAddressComparer Instance = new();

    public bool Equals(ConnectionAddress? x, ConnectionAddress? y)
    {
        if (x == y)
            return true;

        if ((x is null && y is not null) || (x is not null && y is null))
            return false;

        if (x!.Port != y!.Port)
            return false;

        return IPAddressArrayComparer.Instance.Equals(x!.Addresses, y!.Addresses);
    }

    public int GetHashCode([DisallowNull] ConnectionAddress obj)
    {
        return 0;
    }
}