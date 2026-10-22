using DiarioGym.ViewModels;

namespace DiarioGym.Views;

public partial class EditorRutinaPage : ContentPage
{
    public EditorRutinaPage(EditorRutinaViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
