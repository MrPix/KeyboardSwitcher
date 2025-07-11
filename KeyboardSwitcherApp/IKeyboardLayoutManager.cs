namespace KeyboardLayoutSwitcher;

public interface IKeyboardLayoutManager
{
    IReadOnlyList<IntPtr> GetAvailableLayouts();
    IntPtr GetCurrentLayout();
    void ActivateLayout(IntPtr layout);
    string GetLayoutName(IntPtr layout);
}
