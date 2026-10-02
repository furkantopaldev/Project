using AtolyeRecete.App.Services;
using AtolyeRecete.App.ViewModels;
using AtolyeRecete.App.Views;
using AtolyeRecete.Data;
using AtolyeRecete.Data.Repositories;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace AtolyeRecete.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Veri katmanı
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "atolyerecete.db3");
		builder.Services.AddSingleton(new AtolyeDatabase(dbPath));
		builder.Services.AddSingleton<IIngredientRepository, IngredientRepository>();
		builder.Services.AddSingleton<IRecipeRepository, RecipeRepository>();
		builder.Services.AddSingleton<SeedService>();
		builder.Services.AddSingleton<StartupService>();

		// Ekranlar
		builder.Services.AddTransient<HomeViewModel>();
		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<IngredientsViewModel>();
		builder.Services.AddTransient<IngredientsPage>();
		builder.Services.AddTransient<IngredientEditViewModel>();
		builder.Services.AddTransient<IngredientEditPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
