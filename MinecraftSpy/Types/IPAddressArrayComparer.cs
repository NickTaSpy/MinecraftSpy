using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace MinecraftSpy;

public class IPAddressArrayComparer : IEqualityComparer<IPAddress[]>
{
    public static readonly IPAddressArrayComparer Instance = new();

    public bool Equals(IPAddress[]? x, IPAddress[]? y)
    {
        if (x == y)
            return true;

        if ((x is null && y is not null) || (x is not null && y is null))
            return false;

        if (x!.Length != y!.Length)
            return false;

        foreach (var ipx in x!)
        {
            bool found = false;

            foreach (var ipy in y!)
            {
                if (ipx.GetHashCode() != ipy.GetHashCode())
                    continue;

                if (ipx.Equals(ipy))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                return false;
        }

        return true;
    }

    public int GetHashCode([DisallowNull] IPAddress[] obj)
    {
        return 0;
    }
}
