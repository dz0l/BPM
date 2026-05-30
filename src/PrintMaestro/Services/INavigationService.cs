namespace PrintMaestro.Services;

public interface INavigationService
{
    void NavigateToSettings();
}

public sealed class NavigationService : INavigationService
{
    public void NavigateToSettings()
    {
        // MVP: навигация будет расширена на этапе полного UI.
    }
}
