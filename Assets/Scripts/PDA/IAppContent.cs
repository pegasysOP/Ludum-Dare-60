// how an app's content hooks into its FullscreenApp window
public interface IAppContent
{
    // return true if back was used up, e.g. closing a sub view
    bool Back();

    void OnAppOpened();

    void OnAppClosed();
}
