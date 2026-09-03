using System.Collections.Generic;
using System.Linq;

namespace Miniscuplter;

public partial class Main
{
    public sealed class V08MaskDto
    {
        public string ObjectName { get; set; } = "";
        public float[] Values { get; set; } = System.Array.Empty<float>();
    }

    internal List<V08MaskDto> ExportV08Masks()
        => _v08Masks.Select(kv => new V08MaskDto { ObjectName = kv.Key, Values = kv.Value.ToArray() }).ToList();

    internal void ImportV08Masks(List<V08MaskDto>? masks)
    {
        _v08Masks.Clear();
        if (masks == null) return;
        foreach (var m in masks)
            if (!string.IsNullOrWhiteSpace(m.ObjectName) && m.Values != null)
                _v08Masks[m.ObjectName] = m.Values.ToArray();
    }
}
