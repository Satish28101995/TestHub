namespace TestHub;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // All top-level destinations (splash / login / home) are declared as
        // ShellContent in AppShell.xaml, so no Routing.RegisterRoute calls are
        // required here. Add Routing.RegisterRoute(...) entries only for
        // sub-pages that should be reachable via relative navigation.
    }
}
