using System.Collections.ObjectModel;
using AxorP1.Class;
using AxorP1.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Timer = System.Timers.Timer;

namespace AxorP1.Components
{
    public class MainComponent<T> : ComponentBase
    {
        [Inject] protected ILogger<T> Logger { get; set; }
        [Inject] protected NavigationManager NavigationManager { get; set; }
        [Inject] protected IJSRuntime JSRuntime { get; set; }
        [Inject] protected ILocalStorageService LocalStorage { get; set; }

        // Custom services
        [Inject] protected DataProvider DataProvider { get; set; }
        [Inject] protected ThemeProvider ThemeProvider { get; set; }

        // 5s timer 
        protected Timer timer = new Timer(5000);

        // DataSource List
        protected ObservableCollection<Station> DataSource = new ObservableCollection<Station>();
        protected ObservableCollection<Station> PastDataSource = new ObservableCollection<Station>();

        // App Settings
        protected static int MaxWidth = 799;
        protected static string MediaQuery { get { return "max-width:" + MaxWidth + "px"; } }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            // Initialize DataSource List
            if (DataSource.Count == 0)
            {
                await UpdateDataSourceAsync();
            }
        }

        // Update data source asynchronously
        protected async Task UpdateDataSourceAsync()
        {
            try
            {
                var data = await DataProvider.GetDataAsync();
                var dataSourceDict = DataSource.ToDictionary(station => station.Id);

                foreach (var station in data)
                {
                    if (dataSourceDict.ContainsKey(station.Id))
                    {
                        // Update the existing Station object in the DataSource
                        var index = DataSource.IndexOf(dataSourceDict[station.Id]);
                        DataSource[index] = station; // Replace the old object with the new one
                    }
                    else
                    {
                        // Add new Station to the DataSource and dictionary for future lookups
                        DataSource.Add(station);
                        dataSourceDict.Add(station.Id, station);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error Data source assignment: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Update past data source asynchronously
        protected async Task UpdatePastDataSourceAsync(string id)
        {
            try
            {
                PastDataSource = await DataProvider.GetPastDataAsync(id);

            }
            catch (Exception ex)
            {
                Logger.LogError($"Error Past Data source assignment: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
