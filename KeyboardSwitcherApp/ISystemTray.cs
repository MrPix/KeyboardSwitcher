namespace KeyboardLayoutSwitcher;

public interface ISystemTray
{
    void SetIcon(string tooltip, Action onDoubleClick);
    void SetContextMenu(IEnumerable<(string label, Action onClick)> items);
    void Show();
    void Hide();
    void Dispose();
    void UpdateTooltip(string tooltip);
}
