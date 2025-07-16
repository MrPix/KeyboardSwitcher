namespace KeyboardLayoutSwitcher.Core.Entities;

public class KeyboardLayout
{
    public IntPtr Handle { get; }
    public string Name { get; }
    public int LayoutId { get; }
    public DateTime LastUsed { get; private set; }
    public int UsageCount { get; private set; }

    public KeyboardLayout(IntPtr handle, string name, int layoutId)
    {
        Handle = handle;
        Name = name;
        LayoutId = layoutId;
        LastUsed = DateTime.MinValue;
        UsageCount = 0;
    }

    public void MarkAsUsed()
    {
        LastUsed = DateTime.Now;
        UsageCount++;
    }

    public override string ToString()
    {
        return $"{Name} (ID: {LayoutId:X4})";
    }
} 