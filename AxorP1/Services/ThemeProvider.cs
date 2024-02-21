using AxorP1.Pages;
using AxorP1.Shared;
using Blazored.LocalStorage;
using Microsoft.JSInterop;

namespace AxorP1.Services
{
    public class ThemeProvider
    {
        // Dark mode is activated or not
        public bool? isDarkMode { get; set; }

        private static string DarkPath = "_content/Syncfusion.Blazor.Themes/fluent-dark.css";   // Dark theme CSS path
        private static string LightPath = "_content/Syncfusion.Blazor.Themes/fluent.css";       // Light theme CSS path
        private static Syncfusion.Blazor.Theme  DarkTheme = Syncfusion.Blazor.Theme.FluentDark; // Dark Syncfusion theme
        private static Syncfusion.Blazor.Theme LigthTheme = Syncfusion.Blazor.Theme.Fluent;    // Light Syncfusion theme

        public Syncfusion.Blazor.Theme AppTheme { get; set; } = LigthTheme; // Syncfusion components theme
        public string ThemeClass { // CSS class for the page
            get {
               return isDarkMode == true ? "dark" : "light";
            } 
        }

        private static Dictionary<char, string> Colors = new Dictionary<char, string>
        { // Color palette for the charts
            {'A', "#b86d42"},
            {'B', "#f6d44d"},
            {'C', "#a5ff41"},
            {'D', "#73b857"},
            {'E', "#007355"},
            {'F', "#0cb2bd"},
            {'G', "#7effff"},
            {'H', "#00a6ff"},
            {'I', "#a192e0"},
            {'J', "#ffa02d"}
        };

        // Register page instances 
        public MainLayout? MainLayout; // MainLayout
        public IndexBase? MainDashboard; // Index page
        public StationPageBase? StationDashboard; // StationPage page 

        private readonly IJSRuntime JSRuntime;
        private readonly ILogger<ThemeProvider> Logger;
		private readonly ILocalStorageService LocalStorage;

		public ThemeProvider(IJSRuntime jsRuntime, ILogger<ThemeProvider> logger, ILocalStorageService localStorage)
        {
            JSRuntime = jsRuntime;
            Logger = logger;
            LocalStorage = localStorage;
        }
    
        // Set the initial theme
        public async Task InitialTheme()
        {
            try 
            {  // Get user theme preference 
                var themePreference = await LocalStorage.GetItemAsync<string>("ThemePreference");
				var darkTheme = (themePreference == "dark");

                await ChangeTheme(darkTheme);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Set the theme
        public async Task ChangeTheme(bool? darkTheme)
        {
            if (darkTheme == null || isDarkMode == darkTheme) { return; }

            // Update isDarkMode
            isDarkMode = darkTheme;

            String link;

            if (isDarkMode == true)
            {
                link = DarkPath;      // Dark theme CSS path
                AppTheme = DarkTheme; // Dark Syncfusion theme
            }
            else
            {
                link = LightPath;      // Light theme CSS path
                AppTheme = LigthTheme; // Light Syncfusion theme
            }

            try 
            { 
                // Update stylesheet path
                await JSRuntime.InvokeVoidAsync("changeTheme", link);
                // Save user theme preference
				await LocalStorage.SetItemAsync("ThemePreference", ThemeClass);

                bool success = await RefreshComponents(); // Refresh components
                if (success)
                {
                    Logger.LogInformation("Theme changed successfully");
                }
                else
                {
                    Logger.LogError("MainLayout is null.");
                }

            }
			catch (Exception ex)
            {
                 Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Refresh components
        public async Task<bool> RefreshComponents()
        {
           if (MainLayout != null)
           {
                MainLayout?.StateChanged();

                // Refresh DashboardLayout panels
                if (MainDashboard != null)
                {
                    await MainDashboard?.RefreshAllPanelsAsync();
                }
                if (StationDashboard != null)
                {
                    await StationDashboard?.RefreshAllPanelsAsync();
                }

                return true;
           }
           else { return false; }
           
        }

        // Function to select colors based on provided keys
        public string[] GetColors(params char[] keys)
        {
            // Check if keys are provided
            if (keys.Length == 0)
            {
                // Return all color values
                return Colors.Values.ToArray();
            }

            // If keys are provided, return colors for those keys
            // Initialize a list to store the colors in the order of the keys
            List<string> orderedColors = new List<string>();

            // For each key, add the corresponding color to the list
            foreach (char key in keys)
            {
                if (Colors.TryGetValue(key, out string color))
                {
                    orderedColors.Add(color);
                }
            }
            return orderedColors.ToArray();
        }

    }
}
