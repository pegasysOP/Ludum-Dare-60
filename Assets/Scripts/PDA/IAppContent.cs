// for components inside an app window that want back and close without subclassing FullscreenApp
public interface IAppContent
{
    // return true if back was used up, e.g. closing a sub view
    bool Back();

    void OnAppClosed();
}
