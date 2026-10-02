using AtolyeRecete.App.Views;

namespace AtolyeRecete.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Sekmelerde olmayan, push ile açılan sayfalar
		Routing.RegisterRoute("IngredientEditPage", typeof(IngredientEditPage));
		Routing.RegisterRoute("RecipeEditPage", typeof(RecipeEditPage));
	}
}
