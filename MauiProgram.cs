using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MauiAquariumBLE.Services;

namespace MauiAquariumBLE;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
        // Initialise the toolkit
        builder.UseMauiApp<App>().UseMauiCommunityToolkit();

        builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

        builder.Services.AddSingleton<BluetoothService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
