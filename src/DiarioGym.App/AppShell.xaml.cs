using DiarioGym.Views;

namespace DiarioGym;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("editor", typeof(EditorRutinaPage));
    }
}
