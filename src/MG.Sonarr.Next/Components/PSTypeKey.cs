using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Components;

[StructLayout(LayoutKind.Auto)]
public readonly struct PSTypeKey
{
    private readonly Type? _type;

    public readonly Type KeyedType => _type ?? typeof(object);
}