namespace KeyboardLayoutSwitcher.Domain.Entities;

public class KeyboardLayout
{
    public IntPtr Handle { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public uint LayoutId { get; }

    public KeyboardLayout(IntPtr handle, string name, string displayName, uint layoutId)
    {
        Handle = handle;
        Name = name;
        DisplayName = displayName;
        LayoutId = layoutId;
    }

    public override bool Equals(object? obj)
    {
        return obj is KeyboardLayout layout && Handle.Equals(layout.Handle);
    }

    public override int GetHashCode()
    {
        return Handle.GetHashCode();
    }

    public override string ToString()
    {
        return DisplayName;
    }
}