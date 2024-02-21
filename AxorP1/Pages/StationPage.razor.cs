using AxorP1.Class;
using AxorP1.Components;
using AxorP1.Shared.Components.Panels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Syncfusion.Blazor.Buttons;
using Syncfusion.Blazor.Charts;
using Syncfusion.Blazor.Layouts;
using static AxorP1.Shared.Components.Panels.ChartComponent;

namespace AxorP1.Pages
{
    public class StationPageBase : MainComponent<StationPage>, IDisposable
    {
        [Parameter] 
        public string id { get; set; }      // ID of the desired Station
        protected bool? WrongParam = null;  // ID not found
        protected bool ScrollToTop = true;  // The page scrolls to the top initialy
        protected Station Station { get; set; } // Desired Station object
     
        protected SfDashboardLayout? DashboardLayout;                   // DashboardLayout reference
        protected List<PanelObject> PanelData = new List<PanelObject>(); // List of all Dashboard Panels 
        protected bool IsDisposed = true;
        protected int DashboardWidth = 0;
        protected bool IsDialogVisible;    // History dialog visibility

        protected bool FullScreenIsOn; // Full screen button
        protected IconName FullScreenIcon
        {
            get
            {
                if (FullScreenIsOn)
                {
                    return IconName.ExitFullScreen;
                }
                else
                {
                    return IconName.FullScreen;
                }
            }
        }

        // List of Component references
        protected Dictionary<string, DynamicComponent> componentsReferences = new Dictionary<string, DynamicComponent>();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender) // On first render
            {
                try
                {
                    // Set dotNet reference to access StationPage component when #content FullScreen changed
                    var dotNetReference = DotNetObjectReference.Create(this);
                    await JSRuntime.InvokeVoidAsync("OnFullScreenChange", dotNetReference, "content");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
                }
            }

            if (ScrollToTop)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("resetScrollPosition"); // Reset scroll prosition 
                    ScrollToTop = false;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
                }
            }
            
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            Logger.LogInformation($"StationPage Created");
            ThemeProvider.StationDashboard = this; // Keep instance
            IsDisposed = false;

            // Reset flags
            ScrollToTop = true;
            WrongParam = null;

            await NewDataAsync();                // Update the DataSource
            InitializePanelData();               // Initialize Panel objects

            // Starting live update of the Dashboard data every 5s
            timer.Elapsed += async (sender, e) => await NewDataAsync();
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        protected async Task NewDataAsync()
        {
            await UpdateDataSourceAsync(); // Update the DataSource

            Station? station = DataSource.FirstOrDefault(obj => obj.Id == id); // Get Station object
            if (station != null)
            {
                Station = station;
                WrongParam = false;

                await UpdatePastDataSourceAsync(id); // Update PastDataSource List with the data of the wanted station 
            }
            else
            {
                WrongParam = true;
            }
            await InvokeAsync(() => { StateHasChanged(); });
        }

        // Implement IDisposable
        public void Dispose()
        {
            Logger.LogInformation($"StationPage disposed");

            // Dispose components
            CloseDialog();
            ThemeProvider.StationDashboard = null;
            IsDisposed = true;
        }

        // Dashboard event Created
        public async void Created(Object args)
        {
            // Call OnContainerResize to make the dashboard panels visible when created
            try
            {  // Set dotNet reference to access StationPage component when Dashboard container is resized
                var dotNetReference = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("OnContainerResize", dotNetReference, "SfStationDashboardLayout");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Dashboard ResizeObserver
        [JSInvokable]
        public async Task OnContainerResize(int width, int height)
        {
            if (IsDialogVisible)
            {
                int previousWidth = DashboardWidth;

                if(DashboardWidth != width) // If width changed
                {
                    DashboardWidth = width;

                    if (previousWidth != 0) // If is not the first render
                    {
                        await RefreshAllPanelsAsync(); // Refresh all the panels
                    }
                    else if(previousWidth == 0)  // If Dashboard first render
                    {
                        await Task.Delay(500);
                        await RefreshDashboard();
                        PanelsStateChanged();  // Initiate components that need to be notified to render
                    }
                       
                }
            }
        }

        // Refresh DashboardLayout
        public async Task RefreshDashboard()
        {
            try
            {
                if (DashboardLayout != null && IsDialogVisible)
                {
                    await DashboardLayout?.RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error trying to Refresh Station Dashboard : {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Refresh the content of Dashboard panels
        public void RefreshPanel(string id)
        {
            if(componentsReferences.ContainsKey(id) && IsDialogVisible) 
            {
                var component = componentsReferences[id].Instance; // Get the component reference corresponding to the panel
                
                if (component is RangeComponent rangeComponent) { rangeComponent.Refresh(); } // Check the type of the component and perform a refresh
            }
        }
        public async Task RefreshAllPanelsAsync() 
        {
            await RefreshDashboard(); // Refresh DashboardLayout

            foreach (var panelRef in componentsReferences) // Iterate through all panels and refresh their content
            {
                RefreshPanel(panelRef.Key);
            }
        }

        // StateHasChanged for all dashboard panels components
        public void PanelsStateChanged()
        {
            foreach (var componentRef in componentsReferences.Values) // Sets IsParentCreated to True and call StateHasChanged()
            {
                var component = componentRef.Instance; 

                // Check the type of the component
                if (component is RangeComponent rangeComponent){ rangeComponent.StateChanged();}
            }
        }

        // OnClick event on FullScreen button
        public async Task FullScreenRequest()
        {
            try
            {
                // Put/Remove full screen mode on #content element
                if (FullScreenIsOn)
                {  await JSRuntime.InvokeVoidAsync("exitFullScreen");  }
                else
                {  await JSRuntime.InvokeVoidAsync("enterFullScreen", "content"); }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // #content OnFullScreenChange event
        [JSInvokable]
        public void FullScreenChanged()
        {
            // Update flag to change the icon
            FullScreenIsOn = !FullScreenIsOn;
            StateHasChanged();
        }

        //  Open the SfDialog
        public void OpenDialog()
        {   
            if(!IsDialogVisible)
            {
                DashboardWidth = 0;
                componentsReferences.Clear();

                IsDialogVisible = true;
            }
        }

        //  OnClose event of the SfDialog
        public void CloseDialog()
        {
            IsDialogVisible = false;

            DashboardWidth = 0;
            componentsReferences.Clear();
        }


        // Initialize Dashboard PanelObjects 
        public void InitializePanelData()
        {
            PanelData = new List<PanelObject>()
            {
                // Production History
                new PanelObject() { Id = "panelRangeNavigator", Column = 0, Row = 0, SizeX = 2, SizeY = 1, Title = "Historique de production", ComponentType = typeof(RangeComponent),
                    Parameters = new Dictionary<string, object>
                    {
                        { "LightPalette", ThemeProvider.GetColors('F', 'D') },
                        { "DarkPalette", ThemeProvider.GetColors('F', 'D')  },
                        { "Toolbar", true },
                        { "FormatY", "mW" },
                        { "RangeSeriesAttributes", new Dictionary<string, object>()
                            {
                                { "DataSource",  PastDataSource },
                                { "XName", nameof(Class.Station.DateTime) },
                                { "YName", nameof(Class.Station.CentralProduction) },
                                { "Type", RangeNavigatorType.Area },
                            }
                        },
                        { "ChartSeriesList", new List<ChartSeriesConfig>()
                            {
                                new ChartSeriesConfig
                                {
                                    SeriesAttributes = new Dictionary<string, object>
                                    {
                                        ["DataSource"] = PastDataSource,
                                        ["Name"] = "Production",
                                        ["XName"] = nameof(Class.Station.DateTime),
                                        ["YName"] = nameof(Class.Station.CentralProduction),
                                        ["Width"] = (double)3,
                                        ["Type"] = ChartSeriesType.Spline,
                                    },
                                    TrendlineAttributes = new Dictionary<string, object>
                                    {
                                        ["Type"] = TrendlineTypes.Linear,
                                        ["Width"] = (double)1,
                                        ["Name"] = "Tendance générale",
                                        ["DashArray"] = "5,5"
                                    },
                                    LabelAttributes = new Dictionary<string, object> { [ "Visible"] = false },
                                    MarkerAttributes = new Dictionary<string, object> { [ "Visible"] = true }
                                },
                            }
                        },
                    }
                },
                // Add other PanelObjects here
            };
        }


    }
}