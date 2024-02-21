using Newtonsoft.Json;
using AxorP1.Class;
using AxorP1.Components;
using AxorP1.Services;
using AxorP1.Shared.Components.Panels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Syncfusion.Blazor.Layouts;
using Syncfusion.Blazor.Maps;
using static AxorP1.Shared.Components.Panels.ChartComponent;
using static AxorP1.Shared.Components.Panels.GridComponent<AxorP1.Class.Station>;
using static AxorP1.Shared.Components.Panels.PieChartComponent;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Data;
using Syncfusion.Blazor.Buttons;

namespace AxorP1.Pages
{
    public class IndexBase : MainComponent<Index>, IDisposable
    {
        protected List<Dictionary<string, object>> LayoutPreference = new List<Dictionary<string, object>>(); // Dashboard Panels preference
        protected Dictionary<string, DynamicComponent> componentsReferences = new Dictionary<string, DynamicComponent>(); // List of DynamicComponent references

        // MultiSelect component
        protected SfMultiSelect<List<DropdownData>, DropdownData>? MultiSelectRef;
        protected List<DropdownData> DropdownItems = new List<DropdownData>();
        protected List<DropdownData> SelectedPanels = new List<DropdownData>();
        protected List<DropdownData> InitialSelectedPanels = new List<DropdownData>();

        protected List<PanelObject> PanelData = new List<PanelObject>(); // List of all Dashboard Panels 
        protected List<PanelObject> InitialPanelData = new List<PanelObject>(); // List of initially selected Dashboard Panels 

        // DashboardLayout attributes
        protected SfDashboardLayout? DashboardLayout;
        protected bool IsDashboardVisible = false;
        protected int DashboardWidth = 0;
        public bool IsDisposed = true;
        public bool? IsStacked = null;
        public int Columns = 4;
        public static double[] Spacing = new double[] { 10, 10 };

        // Manage updates on panels
        protected string PanelToRemove = string.Empty;
        protected string PanelToAdd = string.Empty;
        protected List<PanelModel> PanelToUpdateList = new List<PanelModel>();
        protected bool ResetLayoutRequest;
        protected bool IsDialogVisible;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if(firstRender) // Dashboard not created yet
            {
                InitializePanelData();              // Initialize Panel Objects
                await InitializeLayoutPreference(); // Initialize LayoutPreference to keep persistance
                SetInitialDropdownData();           // Set initial selected panels in MultiSelect component
                InitializeInitialPanelData();
            }

            if(!firstRender && !IsDisposed)
            {
                // Remove a panel
                if (PanelToRemove != string.Empty)
                {
                    var id = PanelToRemove;
                    PanelToRemove = string.Empty;
                    await RemovePanel(id);              // Remove panel
                    await RefreshDashboard();
                    await UpdateLayoutPreference();     // Update LayoutPreference
                }
            
               // Add a panel
               if(PanelToAdd != string.Empty)
               {
                    var id = PanelToAdd;
                    PanelToAdd = string.Empty;
                    await AddPanel(id); // Add the panel
                    await Task.Delay(500);
                    await RefreshDashboard();
                    PanelsStateChanged();  // Authorize the render of the component
                    await UpdateLayoutPreference(); // Update LayoutPreference
               }
            
               // Update a list of panels preferences
               if(PanelToUpdateList.Count != 0)
               {
                    var list = PanelToUpdateList.ToList();
                    PanelToUpdateList.Clear();
                    await UpdateLayoutPreference(list);
               }

               // Reset panels preferences
               if (ResetLayoutRequest)
               {
                    ResetLayoutRequest = false;
                    IsDashboardVisible = false; // Hide Dashboard

                    foreach (var panel in LayoutPreference.Where(obj => Convert.ToBoolean(obj["Visible"]) == false).ToList())
                    {
                       await AddPanel(panel["Id"].ToString()); // Add hidden panels
                    }
                    await Task.Delay(100);
                    await RefreshDashboard();
                    await MultiSelectRef?.RefreshDataAsync(); 
                    await ResetLayoutPreference(); // Reset LayoutPreference values
                    if (IsStacked == false)
                    {
                        ResetPanelsPosition();   // Move the panels to default position
                    }
                    PanelsStateChanged();       // Authorize the render of the components
                    await Task.Delay(500);
                    await RefreshAllPanelsAsync();
                    await Task.Delay(500);

                    IsDashboardVisible = true; // Show Dashboard
               }
            }
        }

        // Dashboard event Created
        public async void Created(Object args)
        {
            Logger.LogInformation($"Main Dashboard created");
            IsDisposed = false;
            ThemeProvider.MainDashboard = this; // Keep instance

            // Starting live update of the Dashboard data every 5s
            timer.Elapsed += async (sender, e) => await NewDataAsync();
            timer.AutoReset = true;
            timer.Enabled = true;

            // Call OnContainerResize to make the dashboard visible when created
            try
            {  // Set dotNet reference to access Index component when Dashboard container is resized
                var dotNetReference = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("OnContainerResize", dotNetReference, "SfMainDashboardLayout");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
            }

        }
       
        // Implement IDisposable
        public void Dispose()
        {
            Logger.LogInformation($"Main Dashboard disposed");

            // Dispose components
            ThemeProvider.MainDashboard = null;
            IsDisposed = true;
        }

        // Dashboard ResizeObserver
        [JSInvokable]
        public async Task OnContainerResize(int width, int height)
        {
            int previousWidth = DashboardWidth;

            if (DashboardWidth != width && !IsDisposed)   // If width changed
            {
                DashboardWidth = width;
                await IsLayoutStackedAsync();  // Check if layout is stacked

                var dif = Math.Abs(DashboardWidth - previousWidth);

                if (previousWidth != 0 && dif >= 10 )    // If is not the first render
                {
                    await Task.Delay(100);
                    await RefreshAllPanelsAsync(); // Refresh all the panels
                }
                else if(previousWidth == 0)   // If Dashboard first render
                {
                    await Task.Delay(500);
                    await RefreshDashboard();
                    PanelsStateChanged();      // Initiate components that need to be notified to render

                    IsDashboardVisible = true; // Make the Dashboard visible
                    StateHasChanged();
                }
            }
            else // If height changed
            {
                await Task.Delay(100);
                await RefreshDashboard();
            }
        }

        // Save LayoutPreference
        protected async Task SaveLayoutPreference()
        {
            try
            {
                await LocalStorage.SetItemAsync("MainDashboardLayout", LayoutPreference);
                Logger.LogInformation("LayoutPreference saved");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Initialize LayoutPreference 
        protected async Task InitializeLayoutPreference()
        {
            try
            {   // Get MainDashboardLayout panels preference
                var storage = await LocalStorage.GetItemAsync<string>("MainDashboardLayout");
                // Convert Json to C# object
                var preference = storage is null ? null : JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(storage);

                if (preference != null && preference.Count != 0) // If user preference is set
                {
                    foreach (var panel in preference)
                    {
                        LayoutPreference.Add(new Dictionary<string, object> // Initialize LayoutPreference with preference values
                        {   { "Id", panel["Id"] },
                            { "SizeX", panel["SizeX"] },
                            { "SizeY", panel["SizeY"] },
                            { "Column", panel["Column"] },
                            { "Row", panel["Row"] },
                            { "Visible", panel["Visible"] }
                        });
                    }
                }
                else // If no preference yet
                {
                    await ResetLayoutPreference(); // Initialize LayoutPreference with default values
                }

                Logger.LogInformation("LayoutPreference initialized");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Reset LayoutPreference with default values
        protected async Task ResetLayoutPreference()
        {
            LayoutPreference.Clear();

            foreach (var panel in PanelData)
            {
                LayoutPreference.Add( new Dictionary<string, object> {   
                    { "Id", panel.Id },
                    { "SizeX", panel.SizeX },
                    { "SizeY", panel.SizeY },
                    { "Column", panel.Column },
                    { "Row", panel.Row },
                    { "Visible", true }
                });
            }
            await SaveLayoutPreference(); // Save preference
        }

        // Update LayoutPreference
        protected async Task UpdateLayoutPreference(List<PanelModel>? changedPanels = null)
        {
            try 
            {   if (changedPanels == null) // Panel was added or removed
                {
                    // Get Syncfusion DashBoardLayout preference
                    var storage = await LocalStorage.GetItemAsync<string>("SfMainDashboardLayout");
                    // Convert Json to C# object
                    var preference = storage is null ? null : JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(storage);

                    for (var index = 0; index < PanelData.Count(); index++)
                    {
                        var data = preference is null ? null : preference.FirstOrDefault(p => p["Id"].ToString() == PanelData[index].Id);

                        if (SelectedPanels.Any(p => p.Value == PanelData[index].Id)) // Panel is Visible
                        {
                            // Update LayoutPreference with new values
                            LayoutPreference[index]["Visible"] = true;

                            if(IsStacked == false && data != null) // Avoid saving values when layout is stacked (we don't want this state to persist)
                            {
                                LayoutPreference[index]["SizeX"] = Convert.ToInt32(data["SizeX"]);
                                LayoutPreference[index]["SizeY"] = Convert.ToInt32(data["SizeY"]);
                                LayoutPreference[index]["Column"] = Convert.ToInt32(data["Column"]);
                                LayoutPreference[index]["Row"] = Convert.ToInt32(data["Row"]);
                            }
                        }
                        else // Is not Visible
                        {
                            LayoutPreference[index]["Visible"] = false;
                            LayoutPreference[index]["SizeX"] = 1;
                            LayoutPreference[index]["SizeY"] = 1;
                            LayoutPreference[index]["Column"] = 0;
                            LayoutPreference[index]["Row"] = 0;
                        }
                    }
                }
                else // Panel has changed size or position
                {
                    foreach (var panel in changedPanels)
                    {
                        string id = panel.Id.Split('#')[0] ; // Trim unique id (Format: PanelObject.id # integer)
                        int index = LayoutPreference.FindIndex(obj => obj["Id"].ToString() == id); // Find the index of the panel

                        if (index != -1)
                        {   // Update LayoutPreference with new values
                            LayoutPreference[index]["SizeX"] = panel.SizeX;
                            LayoutPreference[index]["SizeY"] = panel.SizeY;
                            LayoutPreference[index]["Column"] = panel.Column;
                            LayoutPreference[index]["Row"] = panel.Row;
                        }
                    }
                }
                await SaveLayoutPreference(); // Save preference
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        
        // Initialize DropdownData objects
        public void SetInitialDropdownData()
        {
            DropdownItems.Clear();  SelectedPanels.Clear(); InitialSelectedPanels.Clear();
            var VisiblePanels = LayoutPreference.Where(obj => Convert.ToBoolean(obj["Visible"]));

            foreach (var data in PanelData)
            {
               var isVisible =  VisiblePanels.Any(p => p["Id"].ToString() == data.Id);

               DropdownItems.Add(new DropdownData() { Text = data.Title, Value = data.Id }); // All MultiSelect items

                if (isVisible) // The panel has to be checked and visible
                {
                    SelectedPanels.Add(new DropdownData() { Text = data.Title, Value = data.Id });         // Initially checked items 
                    InitialSelectedPanels.Add(new DropdownData() { Text = data.Title, Value = data.Id });  // Initially visible panels
                }
            }
            DropdownItems = DropdownItems.OrderBy(l => l.Text).ToList();  // Place in alphabetical order
            StateHasChanged();
        }

        // OnValueSelect event of MultiSelect 
        public void OnValueSelect(SelectEventArgs<DropdownData> args)
        {
            var id = args.ItemData.Value;
            PanelToAdd = id;  // Add panel OnAfterRender          
        }

        // OnValueRemove event of MultiSelect 
        public void OnValueRemove(RemoveEventArgs<DropdownData> args)
        {
            var id = args.ItemData.Value;
            PanelToRemove = id; // Remove panel OnAfterRender
        }

        //  Reset button, OnClick event on confirm dialog
        public void OnResetLayoutClick()
        {
            IsDialogVisible = false;
            ResetLayoutRequest = true; // Reset layout OnAfterRender
        }

        // OnClick event of Close panel button 
        public async void OnClosePanelClick(string id)
        {
            PanelToRemove = id; // Remove panel OnAfterRender
            await Task.Delay(100);
            await MultiSelectRef?.RefreshDataAsync(); // Refresh selection
        }


        // Verify if the DashboardLayout panels are stacked
        public async Task IsLayoutStackedAsync()
        {
            try
            {  // Check if the layout is stacked based on the screen width
                var width = await JSRuntime.InvokeAsync<int>("getWidth");
                bool IsLayoutStacked = (width <= MaxWidth) ? true : false;

                if (IsStacked != IsLayoutStacked)       // If the value has changed
                {
                    IsStacked = IsLayoutStacked;
                    if (IsStacked == true)              // If the layout is now stacked
                    {
                        Logger.LogInformation("Layout is stacked.");
                        foreach (var panel in componentsReferences)
                        {
                            DashboardLayout?.ResizePanelAsync(panel.Key, 1, 1); // Set each panel to a standard size
                        }
                    }
                    else
                    {
                        Logger.LogInformation("Layout is not stacked.");
                        ResetPanelsPosition(); // Reset each panel to original size 
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Move panels to their original position
        public void ResetPanelsPosition()
        {
            var orderedlist = LayoutPreference.Where(obj => Convert.ToBoolean(obj["Visible"]))
                                              .OrderBy(obj => Convert.ToInt32(obj["Row"]))
                                              .ThenBy(obj => Convert.ToInt32(obj["Column"])).ToList(); // Order visible panels

            for (int i = 0; i < 3; i++) // Doing foreach loop many times, to be sure every panel is in the good position
            {
                // Set each panel to its original state to manually keep the persistance
                foreach (var panel in orderedlist)
                {
                    // Find unique id of the panel (Format: PanelObject.id # integer)
                    var id = componentsReferences.FirstOrDefault(panelRef => panelRef.Key.Split('#')[0] == panel["Id"].ToString()).Key;

                    if (id != null && panel.TryGetValue("SizeX", out var sizeX) && panel.TryGetValue("SizeY", out var sizeY))
                    {
                        DashboardLayout?.ResizePanelAsync(id.ToString(), Convert.ToInt32(sizeX), Convert.ToInt32(sizeY)); // Apply the original size to the panel
                    }
                    if (id != null && panel.TryGetValue("Column", out var column) && panel.TryGetValue("Row", out var row))
                    {
                        DashboardLayout?.MovePanelAsync(id.ToString(), Convert.ToInt32(row), Convert.ToInt32(column)); // Apply the original position to the panel
                    }
                }
            }
            Logger.LogInformation("Panel's position reseted.");
        }

        // Dashboard event Changed
        public void Changed(Syncfusion.Blazor.Layouts.ChangeEventArgs args)
        {
            // Manually keep the panels size and position persistance when layout is not stacked
            if (args.IsInteracted && args.ChangedPanels.Count > 0 && IsStacked == false)
            {
                PanelToUpdateList = args.ChangedPanels;
            }
        }

        // Dashboard event OnResizeStop
        public void OnResizeStop(Syncfusion.Blazor.Layouts.ResizeArgs args)
        {
            if(args.IsInteracted)
            {
                RefreshPanel(args.Id);  // Refresh the panel
            }
        }

        // Add Panel
        public async Task AddPanel(string id)
        {
            PanelObject? data = PanelData.FirstOrDefault(p => p.Id == id);             // Selected PanelObject
            DropdownData? selection = DropdownItems.FirstOrDefault(p => p.Value == id);// Selected Dropdown item

            if (data != null && selection != null)
            {
                var timestamp = DateTimeOffset.Now.ToString("HHmmssffff");
                var uniqueID = $"{data.Id}#{timestamp}" ; // Create unique id (Format: PanelObject.id # integer)

                PanelModel newPanel = new PanelModel         // Create new PanelModel
                { 
                    Id = uniqueID, SizeX = 1, SizeY = 1, MinSizeX = data.MinSizeX, MinSizeY = data.MinSizeY, Column = 0, Row = 0, AllowDragging = true, Enabled = true,
                    Header = builder =>   // Header RenderFragment
                    {
                        builder.OpenElement(0, "div");
                        // Title span
                        builder.OpenElement(1, "span");
                        builder.AddContent(2, data.Title);
                        builder.CloseElement(); 
                        // Close button span
                        builder.OpenElement(3, "span");
                        builder.AddAttribute(4, "class", "btn btnClosePanel");
                        builder.AddAttribute(5, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => OnClosePanelClick(data.Id))); 
                        // SfIcon for the close button
                        builder.OpenComponent<SfIcon>(6);
                        builder.AddAttribute(7, "Name", IconName.Close);
                        builder.CloseComponent(); 
                        builder.CloseElement(); 
                        builder.CloseElement(); 
                    },
                    Content = builder =>  // Content RenderFragment
                    {
                        builder.OpenElement(0, "div");
                        builder.AddAttribute(1, "class", "panel");
                        // Dynamic Component
                        builder.OpenComponent<DynamicComponent>(2);
                        builder.AddAttribute(3, "Type", data.ComponentType);
                        builder.AddAttribute(4, "Parameters", data.Parameters);
                        builder.AddComponentReferenceCapture(5, reference => 
                        { 
                            if (reference is DynamicComponent componentRef) 
                            {
                                componentsReferences[uniqueID] = componentRef; // Add Reference to componentsReferences
                            } 
                        });
                        builder.CloseComponent();
                        builder.CloseElement();
                    }
                };

                try
                {
                    SelectedPanels.Add(selection);               // Update selection
                    await DashboardLayout?.AddPanelAsync(newPanel); // Add panel to the dashboard
                    StateHasChanged();

                    Logger.LogInformation($"{uniqueID} added");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        // Remove Panel
        public async Task RemovePanel(string id)
        {   
            try
            {   
                DropdownData? selection = SelectedPanels.FirstOrDefault(p => p.Value == id); // Selected Dropdown item
                string? uniqueID = componentsReferences.FirstOrDefault(panelRef => panelRef.Key.Split('#')[0] == id).Key; // Find unique id (Format: PanelObject.id # integer)
               
                if (selection != null && uniqueID != null)
                {
                    SelectedPanels.Remove(selection);                  // Update selection
                    componentsReferences.Remove(uniqueID);            // Remove component reference
                    await DashboardLayout?.RemovePanelAsync(uniqueID); // Remove dashboard panel 
                    StateHasChanged();

                    Logger.LogInformation($"{uniqueID} removed");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Refresh DashboardLayout
        public async Task RefreshDashboard()
        {
            try
            {
                if (DashboardLayout != null && !IsDisposed) { await DashboardLayout?.RefreshAsync(); } 
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Refresh the panel content
        public void RefreshPanel(string id)
        {
            if (componentsReferences.ContainsKey(id))
            {
                var component = componentsReferences[id].Instance; // Get the component instance corresponding to the panel
                                                                   // Check the type of the component and perform a refresh
                     if (component is ChartComponent chartComponent)             { chartComponent.Refresh(); }
                else if (component is GridComponent<Station> gridComponent)      { gridComponent.Refresh(); }
                else if (component is MapComponent<StationMapData> mapComponent) { mapComponent.Refresh(); }
                else if (component is PieChartComponent pieComponent)            { pieComponent.Refresh(); }
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
            // Sets IsParentCreated to True and call StateHasChanged()
            foreach (var componentRef in componentsReferences.Values)
            {
                var component = componentRef.Instance; // Get the component instance corresponding to the panel
                                                       // Check the type of the component 
                     if (component is ChartComponent chartComponent)            { chartComponent.StateChanged(); }
                else if (component is MapComponent<StationMapData> mapComponent){ mapComponent.StateChanged(); }
                else if (component is PieChartComponent pieComponent)           { pieComponent.StateChanged(); }
            }
        }


        // Update the DataSource
        protected async Task NewDataAsync()
        {
            await UpdateDataSourceAsync(); // Update data
            await InvokeAsync(() => { StateHasChanged(); });
        }

        // Initialize Dashboard PanelObjects 
        public void InitializePanelData()
        {
            // PanelData contains all the PanelObjects
            PanelData = new List<PanelObject>()
            {
                // CANADA MAP
               new PanelObject() { Id = "panelMap", Column = 3, Row = 5, SizeX = 1, SizeY = 1, Title = "Localisation", ComponentType = typeof(MapComponent<StationMapData>),
                   Parameters = new Dictionary<string, object>
                   {
                       { "MapId", "Map" },
                       { "OnMarkerClickEvent", new EventCallback<MarkerClickEventArgs>(this, OnMarkerClickEvent) },
                       { "MarkerAttributes", new Dictionary<string, object>()
                          {
                             {"Visible", true },
                             {"DataSource", DataProvider.GetMapDetails() },
                             {"LatitudeValuePath", nameof(StationMapData.Latitude) },
                             {"LongitudeValuePath", nameof(StationMapData.Longitude) },
                          }
                       },
                       {"MarkerToolTipAttributes", new Dictionary<string, object>()
                           {
                              {"Visible", true },
                              {"ValuePath", nameof(StationMapData.Name) },
                           }
                       }
                   }
               },
               // GROUP T/A
               new PanelObject() { Id = "panelGroupTA", Column = 0, Row = 5, SizeX = 2, SizeY = 1, Title = "Groupe T/A", ComponentType = typeof(GridComponent<Station>),
                         Parameters = new Dictionary<string, object>
                         {
                             { "GridId", "gridGroupTA" },
                             { "DataSource", DataSource },
                             { "ColumnsList", new List<GridColumnsConfig>
                                {
                                    new GridColumnsConfig()
                                    {
                                        ColumnAttributes = new Dictionary<string, object>
                                        {
                                            {"Field", nameof(Station.Id) },
                                            {"HeaderText", "Centrale" },
                                            {"TextAlign", Syncfusion.Blazor.Grids.TextAlign.Right }
                                        },
                                    },
                                    new GridColumnsConfig()
                                    {
                                        ColumnAttributes = new Dictionary<string, object>
                                        {
                                            {"HeaderText", "Groupe 1" },
                                            {"TextAlign", Syncfusion.Blazor.Grids.TextAlign.Center }
                                        },
                                        Template = (context) =>
                                        {
                                                var station = context as Station;
                                                if (station != null)
                                                {
                                                    Boolean? group1 = (station.Groups.Count > 0) ? station.Groups[0].GroupTA : null;

                                                    if (group1.HasValue)
                                                    {
                                                        String id = (group1 == true) ? "actif" : "inactif";
                                                        String background = (group1 == true) ? "bg-red" : "bg-green";

                                                        return (builder) =>
                                                        {
                                                            builder.OpenElement(0, "div");
                                                            builder.OpenElement(1, "span");
                                                            builder.AddAttribute(2, "class", $"groupta {background}");
                                                            builder.AddContent(3, id);
                                                            builder.CloseElement(); // </span>
                                                            builder.CloseElement(); // </div>
                                                        };
                                                    }
                                                }

                                                return (builder) =>
                                                {
                                                    builder.AddContent(0, "");
                                                };
                                        }
                                    },
                                    new GridColumnsConfig()
                                    {
                                        ColumnAttributes = new Dictionary<string, object>
                                        {
                                            {"HeaderText", "Groupe 2" },
                                            {"TextAlign", Syncfusion.Blazor.Grids.TextAlign.Center }
                                        },
                                        Template = (context) =>
                                        {
                                                var station = context as Station;
                                                if (station != null)
                                                {
                                                    Boolean? group2 = (station.Groups.Count > 1) ? station.Groups[1].GroupTA : null;

                                                    if (group2.HasValue)
                                                    {
                                                        String id = (group2 == true) ? "actif" : "inactif";
                                                        String background = (group2 == true) ? "bg-red" : "bg-green";

                                                        return (builder) =>
                                                        {
                                                            builder.OpenElement(0, "div");
                                                            builder.OpenElement(1, "span");
                                                            builder.AddAttribute(2, "class", $"groupta {background}");
                                                            builder.AddContent(3, id);
                                                            builder.CloseElement(); // </span>
                                                            builder.CloseElement(); // </div>
                                                        };
                                                    }
                                                }

                                                return (builder) =>
                                                {
                                                    builder.AddContent(0, "");
                                                };
                                        }
                                    },
                                }
                             }
                         }
               },
               // DÉBIT TOTAL
                 new PanelObject() { Id = "panelTotalFlowRate", Column = 2, Row = 4, SizeX = 2, SizeY = 1, Title = "Débit total", ComponentType = typeof(ChartComponent),
                         Parameters = new Dictionary<string, object>
                         {
                             { "ChartId", "chartTotalFlowRate" },
                             { "LightPalette", ThemeProvider.GetColors('F') },
                             { "DarkPalette", ThemeProvider.GetColors('G')  },
                             { "XAxisAttributes", new Dictionary<string, object>
                                 {
                                     {"ValueType", Syncfusion.Blazor.Charts.ValueType.Category },
                                 }
                             },
                             { "YAxisAttributes", new Dictionary<string, object>
                                 {
                                     {"ValueType", Syncfusion.Blazor.Charts.ValueType.Double },
                                     {"Title", "m³/sec"},
                                 }
                             },
                             { "ToolTipAttributes", new Dictionary<string, object>
                                 {
                                     {"Enable", true },
                                     {"Format", "${point.x}: <b>${point.y} m³/s</b>" },
                                 }
                             },
                             { "ChartSeriesList", new List<ChartSeriesConfig>
                                 {
                                     new ChartSeriesConfig()
                                     {
                                         SeriesAttributes = new Dictionary<string, object>
                                         {
                                             {"DataSource", DataSource },
                                             {"XName", nameof(Station.Id) },
                                             {"YName", nameof(Station.TotalFlowRate) },
                                             {"Type", Syncfusion.Blazor.Charts.ChartSeriesType.Column },
                                         },
                                     },
                                 }
                             }
                         }
                 }, 
                 // DÉBIT DES GROUPES
                    new PanelObject() { Id = "panelGroupFlowRate", Column = 0, Row = 4, SizeX = 2, SizeY = 1, Title = "Débit des groupes", ComponentType = typeof(ChartComponent),
                         Parameters = new Dictionary<string, object>
                         {
                             { "ChartId", "chartGroupFlowRate" },
                             { "LightPalette", ThemeProvider.GetColors('C', 'F') },
                             { "DarkPalette", ThemeProvider.GetColors('C','G')  },
                             { "XAxisAttributes", new Dictionary<string, object>
                                 {
                                     {"ValueType", Syncfusion.Blazor.Charts.ValueType.Category },
                                 }
                             },
                             { "YAxisAttributes", new Dictionary<string, object>
                                 {
                                     {"ValueType", Syncfusion.Blazor.Charts.ValueType.Double },
                                     {"Title", "m³/sec"},
                                 }
                             },
                             { "ToolTipAttributes", new Dictionary<string, object>
                                 {
                                     {"Enable", true },
                                     {"Format", "${point.x}: <b>${point.y} m³/s</b>" },
                                 }
                             },
                             { "ChartSeriesList", new List<ChartSeriesConfig>
                                 {
                                     new ChartSeriesConfig()
                                     {
                                         SeriesAttributes = new Dictionary<string, object>
                                         {
                                             {"DataSource", DataProvider.GetAllGroups(DataSource, 1) },
                                             {"XName", nameof(Group.StationName) },
                                             {"YName", nameof(Group.FlowRate) },
                                             {"Name", "Groupe 1" },
                                             {"Type", Syncfusion.Blazor.Charts.ChartSeriesType.StackingColumn },
                                         },
                                     },
                                     new ChartSeriesConfig()
                                     {
                                         SeriesAttributes = new Dictionary<string, object>
                                         {
                                             {"DataSource", DataProvider.GetAllGroups(DataSource, 2) },
                                             {"XName", nameof(Group.StationName) },
                                             {"YName", nameof(Group.FlowRate) },
                                             {"Name", "Groupe 2" },
                                             {"Type", Syncfusion.Blazor.Charts.ChartSeriesType.StackingColumn },
                                         },
                                     },
                                 }
                             }
                         }
                    }, 
                    // NIVEAU AMONT ET AVAL
                     new PanelObject() { Id = "panelStreamLevel", Column = 0, Row = 3, SizeX = 2, SizeY = 1, Title = "Niveau amont et aval", ComponentType = typeof(ChartComponent),
                          Parameters = new Dictionary<string, object>
                          {
                              { "ChartId", "chartStreamLevel" },
                              { "LightPalette", ThemeProvider.GetColors('C', 'F') },
                              { "DarkPalette", ThemeProvider.GetColors('C','G')  },
                              { "XAxisAttributes", new Dictionary<string, object>
                                  {
                                      {"ValueType", Syncfusion.Blazor.Charts.ValueType.Category },
                                  }
                              },
                              { "YAxisAttributes", new Dictionary<string, object>
                                  {
                                      {"ValueType", Syncfusion.Blazor.Charts.ValueType.Double },
                                      {"Title", "m"},
                                  }
                              },
                              { "ToolTipAttributes", new Dictionary<string, object>
                                  {
                                      {"Enable", true },
                                      {"Format", "${point.x}: <b>${point.y} m</b>" },
                                  }
                              },
                              { "ChartSeriesList", new List<ChartSeriesConfig>
                                  {
                                      new ChartSeriesConfig()
                                      {
                                          SeriesAttributes = new Dictionary<string, object>
                                          {
                                              {"DataSource", DataSource },
                                              {"XName", nameof(Station.Id) },
                                              {"YName", nameof(Station.UpstreamLevel) },
                                              {"Name", "Amont" },
                                              {"ColumnSpacing", 0.1 },
                                              {"Type", Syncfusion.Blazor.Charts.ChartSeriesType.Column },
                                          },
                                      },
                                      new ChartSeriesConfig()
                                      {
                                          SeriesAttributes = new Dictionary<string, object>
                                          {
                                              {"DataSource", DataSource },
                                              {"XName", nameof(Station.Id) },
                                              {"YName", nameof(Station.DownstreamLevel) },
                                              {"Name", "Aval" },
                                              {"ColumnSpacing", 0.1 },
                                              {"Type", Syncfusion.Blazor.Charts.ChartSeriesType.Column },
                                          },
                                      },
                                  }
                              }
                          }
                     },
                      // HAUTEUR DE CHUTE
                      new PanelObject() { Id = "panelFallHeight", Column = 2, Row = 5, SizeX = 1, SizeY = 1, Title = "Hauteur de chute", ComponentType = typeof(ChartComponent),
                         Parameters = new Dictionary<string, object>
                         {
                             { "ChartId", "chartFallHeight" },
                             { "LightPalette", ThemeProvider.GetColors('H') },
                             { "DarkPalette", ThemeProvider.GetColors('G') },
                             { "XAxisAttributes", new Dictionary<string, object>
                                 {
                                     {"ValueType", Syncfusion.Blazor.Charts.ValueType.Category },
                                 }
                             },
                             { "YAxisAttributes", new Dictionary<string, object>
                                 {
                                     {"ValueType", Syncfusion.Blazor.Charts.ValueType.Double },
                                     {"Title", "m"},
                                 }
                             },
                             { "ToolTipAttributes", new Dictionary<string, object>
                                 {
                                     {"Enable", true },
                                     {"Format", "${point.x}: <b>${point.y} m</b>" },
                                 }
                             },
                             { "ChartSeriesList", new List<ChartSeriesConfig>
                                 {
                                     new ChartSeriesConfig()
                                     {
                                         SeriesAttributes = new Dictionary<string, object>
                                         {
                                             {"DataSource", DataSource },
                                             {"XName", nameof(Station.Id) },
                                             {"YName", nameof(Station.FallHeight) },
                                             {"Type", Syncfusion.Blazor.Charts.ChartSeriesType.StepLine },
                                         },
                                     },
                                 }
                             }
                         }
                     },
                   // DIFFERENTIEL DE GRILLE
                   new PanelObject() { Id = "panelGridDifferential", Column = 2, Row = 3, SizeX = 2, SizeY = 1, Title = "Différentiel de grille", ComponentType = typeof(ChartComponent),
                         Parameters = new Dictionary<string, object>
                         {
                             { "ChartId", "chartGridDifferential" },
                             { "LightPalette", ThemeProvider.GetColors('C', 'D', 'G', 'F') },
                             { "DarkPalette", ThemeProvider.GetColors('C','D', 'G', 'F')  },
                             { "XAxisAttributes", new Dictionary<string, object>
                                 {
                                     {"ValueType", Syncfusion.Blazor.Charts.ValueType.Category },
                                 }
                             },
                             { "YAxisAttributes", new Dictionary<string, object>
                                 {
                                     {"ValueType", Syncfusion.Blazor.Charts.ValueType.Double },
                                     {"Title", "m"},
                                 }
                             },
                             { "ToolTipAttributes", new Dictionary<string, object>
                                 {
                                     {"Enable", true },
                                     {"Format", "${point.x}: <b>${point.y} m</b>" },
                                 }
                             },
                             { "ChartSeriesList", new List<ChartSeriesConfig>
                                 {
                                     new ChartSeriesConfig()
                                     {
                                         SeriesAttributes = new Dictionary<string, object>
                                         {
                                             {"DataSource", DataProvider.GetAllGroups(DataSource, 1) },
                                             {"XName", nameof(Group.StationName) },
                                             {"YName", nameof(Group.FineGridDifferential) },
                                             {"Name", "Groupe 1 - Fines" },
                                             {"Type", Syncfusion.Blazor.Charts.ChartSeriesType.Column },
                                             {"ColumnSpacing", 0.1 }
                                         },
                                     },
                                     new ChartSeriesConfig()
                                     {
                                         SeriesAttributes = new Dictionary<string, object>
                                         {
                                             {"DataSource", DataProvider.GetAllGroups(DataSource, 1) },
                                             {"XName", nameof(Group.StationName) },
                                             {"YName", nameof(Group.CoarseGridDifferential) },
                                             {"Name", "Groupe 1 - Grossières" },
                                             {"Type", Syncfusion.Blazor.Charts.ChartSeriesType.Column },
                                             {"ColumnSpacing", 0.1 }
                                         },
                                     },
                                     new ChartSeriesConfig()
                                     {
                                         SeriesAttributes = new Dictionary<string, object>
                                         {
                                             {"DataSource", DataProvider.GetAllGroups(DataSource, 2) },
                                             {"XName", nameof(Group.StationName) },
                                             {"YName", nameof(Group.FineGridDifferential) },
                                             {"Name", "Groupe 2 - Fines" },
                                             {"Type", Syncfusion.Blazor.Charts.ChartSeriesType.Column },
                                             {"ColumnSpacing", 0.1 }
                                         },
                                     },
                                     new ChartSeriesConfig()
                                     {
                                        SeriesAttributes = new Dictionary<string, object>
                                        {
                                            {"DataSource", DataProvider.GetAllGroups(DataSource, 2) },
                                            {"XName", nameof(Group.StationName) },
                                            {"YName", nameof(Group.CoarseGridDifferential) },
                                            {"Name", "Groupe 2 - Grossières" },
                                            {"Type", Syncfusion.Blazor.Charts.ChartSeriesType.Column },
                                            {"ColumnSpacing", 0.1 }
                                        },
                                     },

                                 }
                             }
                         }
                   },
                 // TOTAL PRODUCTION
                 new PanelObject() { Id = "panelTotalProduction", Column = 0, Row = 2, SizeX = 2, SizeY = 1, Title = "Production centrale", ComponentType = typeof(ChartComponent),
                     Parameters = new Dictionary<string, object>
                     {
                         { "ChartId", "chartTotalProduction" },
                         { "LightPalette", ThemeProvider.GetColors('H') },
                         { "DarkPalette", ThemeProvider.GetColors('G') },
                         { "XAxisAttributes", new Dictionary<string, object>
                             {
                                 {"ValueType", Syncfusion.Blazor.Charts.ValueType.Category },
                             }
                         },
                         { "YAxisAttributes", new Dictionary<string, object>
                             {
                                 {"ValueType", Syncfusion.Blazor.Charts.ValueType.Double },
                                 {"Title", "mW"},
                             }
                         },
                         { "ToolTipAttributes", new Dictionary<string, object>
                             {
                                 {"Enable", true },
                                 {"Format", "${point.x}: <b>${point.y} mW</b>" },
                             }
                         },
                         { "ChartSeriesList", new List<ChartSeriesConfig>
                             {
                                 new ChartSeriesConfig()
                                 {
                                     SeriesAttributes = new Dictionary<string, object>
                                     {
                                         {"DataSource", DataSource },
                                         {"XName", nameof(Station.Id) },
                                         {"YName", nameof(Station.CentralProduction) },
                                         {"Type", Syncfusion.Blazor.Charts.ChartSeriesType.Spline },
                                     },
                                 },
                             }
                         }
                     }
                 }, 
                 // GROUPS PRODUCTION
                 new PanelObject() { Id = "panelGroupProduction", Column = 2, Row = 2, SizeX = 2, SizeY = 1, Title = "Production des groupes", ComponentType = typeof(ChartComponent),
                     Parameters = new Dictionary<string, object>
                     {
                         { "ChartId", "chartGroupProduction" },
                         { "LightPalette", ThemeProvider.GetColors('H', 'A') },
                         { "DarkPalette", ThemeProvider.GetColors('G','C')  },
                         { "XAxisAttributes", new Dictionary<string, object>
                             {
                                 {"ValueType", Syncfusion.Blazor.Charts.ValueType.Category },
                             }
                         },
                         { "YAxisAttributes", new Dictionary<string, object>
                             {
                                 {"ValueType", Syncfusion.Blazor.Charts.ValueType.Double },
                                 {"Title", "mW"},
                             }
                         },
                         { "ToolTipAttributes", new Dictionary<string, object>
                             {
                                 {"Enable", true },
                                 {"Format", "${point.x}: <b>${point.y} mW</b>" },
                             }
                         },
                         { "ChartSeriesList", new List<ChartSeriesConfig>
                             {
                                 new ChartSeriesConfig()
                                 {
                                     SeriesAttributes = new Dictionary<string, object>
                                     {
                                         {"DataSource", DataProvider.GetAllGroups(DataSource, 1) },
                                         {"XName", nameof(Group.StationName) },
                                         {"YName", nameof(Group.Production) },
                                         {"Type", Syncfusion.Blazor.Charts.ChartSeriesType.Spline },
                                         {"Name", "Groupe 1" }
                                     }
                                 },
                                 new ChartSeriesConfig()
                                 {
                                     SeriesAttributes = new Dictionary<string, object>
                                     {
                                         {"DataSource", DataProvider.GetAllGroups(DataSource, 2) },
                                         {"XName", nameof(Group.StationName) },
                                         {"YName", nameof(Group.Production) },
                                         {"Type", Syncfusion.Blazor.Charts.ChartSeriesType.Spline },
                                         {"Name", "Groupe 2" }
                                     },
                                 },
                             }
                         }
                     }
                 },
                 // STATISTIQUE DES PERFORMANCES DE PRODUCTION
                 new PanelObject() { Id = "panelPieChart", Column = 3, Row = 0, SizeX = 1, SizeY = 1, Title = "Performances de production", ComponentType = typeof(PieChartComponent),
                         Parameters = new Dictionary<string, object>
                         {
                             { "PieChartId", "pieChart" },
                             { "LightPalette", ThemeProvider.GetColors('A', 'B', 'C', 'E', 'G', 'H', 'I', 'J') },
                             { "DarkPalette", ThemeProvider.GetColors('A', 'B', 'C', 'E', 'G', 'H', 'I', 'J')  },
                             { "SeriesAttributes", new Dictionary<string, object>()
                                {
                                    { "DataSource", DataProvider.GetProductionStatistics(DataSource)},
                                    { "XName", nameof(PieData.Name) },
                                    { "YName", nameof(PieData.Percentage) },
                                    { "Name", "Production" }
                                }
                             }
                         }
                 },  
                  // AVANCEMENT PRODUCTION
                 new PanelObject(){ Id = "panelTargetProduction", Column = 0, Row = 0, SizeX = 3, SizeY = 1, Title = "Avancement production", ComponentType = typeof(GridComponent<Station>),
                    Parameters = new Dictionary<string, object>
                    {
                        { "GridId", "gridTargetProduction" },
                        { "DataSource", DataSource },
                        { "ColumnsList", new List<GridColumnsConfig>
                            {
                                new GridColumnsConfig()
                                {
                                    ColumnAttributes = new Dictionary<string, object>
                                    {
                                        {"Field", nameof(Station.Id) },
                                        {"HeaderText", "Centrale" },
                                        {"TextAlign", Syncfusion.Blazor.Grids.TextAlign.Right }
                                    },
                                },
                                new GridColumnsConfig() // Section mensuel
                                {
                                    ColumnAttributes = new Dictionary<string, object> { {"HeaderText", "Mensuel (mW)"}, {"TextAlign", Syncfusion.Blazor.Grids.TextAlign.Center } },
                                    ChildColumns = new List<GridColumnsConfig>
                                    {
                                        new GridColumnsConfig()
                                        {
                                            ColumnAttributes = new Dictionary<string, object>
                                            {
                                                {"Field", nameof(Station.MonthlyProductionActual) },
                                                {"HeaderText", "Réalisation" },
                                                {"TextAlign", Syncfusion.Blazor.Grids.TextAlign.Center },
                                                {"Format", "0" }
                                            },
                                        },
                                        new GridColumnsConfig()
                                        {
                                            ColumnAttributes = new Dictionary<string, object>
                                            {
                                                {"Field", nameof(Station.MonthlyProductionTarget) },
                                                {"HeaderText", "Objectif" },
                                                {"TextAlign", Syncfusion.Blazor.Grids.TextAlign.Center },
                                                {"Format", "0" }
                                            },
                                        },
                                        new GridColumnsConfig()
                                        {
                                            ColumnAttributes = new Dictionary<string, object>
                                            {
                                                {"Field", nameof(Station.MonthlyPercentage) },
                                                {"HeaderText", "%" },
                                                {"TextAlign", Syncfusion.Blazor.Grids.TextAlign.Center },
                                                {"Format", "0%" },
                                            },
                                        },
                                    }
                                },
                                new GridColumnsConfig() // Section annuel
                                {
                                    ColumnAttributes = new Dictionary<string, object> { {"HeaderText", "Annuel (mW)"}, {"TextAlign", Syncfusion.Blazor.Grids.TextAlign.Center } },
                                    ChildColumns = new List<GridColumnsConfig>
                                    {
                                        new GridColumnsConfig()
                                        {
                                            ColumnAttributes = new Dictionary<string, object>
                                            {
                                                {"Field", nameof(Station.AnnualProductionActual) },
                                                {"HeaderText", "Réalisation" },
                                                {"TextAlign", Syncfusion.Blazor.Grids.TextAlign.Center },
                                                {"Format", "0" }
                                            },
                                        },
                                        new GridColumnsConfig()
                                        {
                                            ColumnAttributes = new Dictionary<string, object>
                                            {
                                                {"Field", nameof(Station.AnnualProductionTarget) },
                                                {"HeaderText", "Objectif" },
                                                {"TextAlign", Syncfusion.Blazor.Grids.TextAlign.Center },
                                                {"Format", "0" }
                                            },
                                        },
                                        new GridColumnsConfig()
                                        {
                                            ColumnAttributes = new Dictionary<string, object>
                                            {
                                                {"Field", nameof(Station.AnnualPercentage) },
                                                {"HeaderText", "%" },
                                                {"TextAlign", Syncfusion.Blazor.Grids.TextAlign.Center },
                                                {"Format", "0%" }
                                            },
                                        },
                                    }
                                },
                            }
                        }
                    },
                 },
                 // AVANCEMENT PRODUCTION MENSUELLE 
                    new PanelObject() { Id = "panelTargetProductionMonth", Column = 0, Row = 1, SizeX = 2, SizeY = 1, Title = "Avancement production mensuelle", ComponentType = typeof(ChartComponent),
                          Parameters = new Dictionary<string, object>
                          {
                              { "ChartId", "chartTargetProductionMonth" },
                              { "LightPalette", ThemeProvider.GetColors('C', 'F') },
                              { "DarkPalette", ThemeProvider.GetColors('C','G')  },
                              { "XAxisAttributes", new Dictionary<string, object>
                                  {
                                      {"ValueType", Syncfusion.Blazor.Charts.ValueType.Category },
                                  }
                              },
                              { "YAxisAttributes", new Dictionary<string, object>
                                  {
                                      {"ValueType", Syncfusion.Blazor.Charts.ValueType.Double },
                                      {"Title", "mW"},
                                  }
                              },
                              { "ToolTipAttributes", new Dictionary<string, object>
                                  {
                                      {"Enable", true },
                                      {"Format", "${series.name}: <b>${point.y} mW</b>" },
                                      {"Shared", true },
                                  }
                              },
                              { "ChartSeriesList", new List<ChartSeriesConfig>
                                  {
                                      new ChartSeriesConfig()
                                      {
                                          SeriesAttributes = new Dictionary<string, object>
                                          {
                                              {"DataSource", DataSource },
                                              {"XName", nameof(Station.Id) },
                                              {"YName", nameof(Station.MonthlyProductionTarget) },
                                              {"Name", "Objectif mensuel" },
                                              {"Opacity", 0.5 },
                                              {"Type", Syncfusion.Blazor.Charts.ChartSeriesType.SplineArea },
                                          },
                                          MarkerAttributes = new Dictionary<string, object>
                                          {
                                              { "Visible", false }
                                          },
                                          LabelAttributes = new Dictionary<string, object>
                                          {
                                              { "Visible", false },
                                          },
                                      },
                                      new ChartSeriesConfig()
                                      {
                                          SeriesAttributes = new Dictionary<string, object>
                                          {
                                              {"DataSource", DataSource },
                                              {"XName", nameof(Station.Id) },
                                              {"YName", nameof(Station.MonthlyProductionActual) },
                                              {"Name", "Réalisation mensuelle" },
                                              {"Opacity", 0.5 },
                                              {"Type", Syncfusion.Blazor.Charts.ChartSeriesType.SplineArea },
                                          },
                                          MarkerAttributes = new Dictionary<string, object>
                                          {
                                              { "Visible", false }
                                          },
                                          LabelAttributes = new Dictionary<string, object>
                                          {
                                              { "Visible", false },
                                          },
                                      },
                                   }
                              }
                          }
                     },
                     // AVANCEMENT PRODUCTION ANUELLE 
                     new PanelObject() { Id = "panelTargetProductionYear", Column = 2, Row = 1, SizeX = 2, SizeY = 1, Title = "Avancement production annuelle", ComponentType = typeof(ChartComponent),
                          Parameters = new Dictionary<string, object>
                          {
                              { "ChartId", "chartTargetProductionYear" },
                              { "LightPalette", ThemeProvider.GetColors('C', 'F') },
                              { "DarkPalette", ThemeProvider.GetColors('C','G')  },
                              { "XAxisAttributes", new Dictionary<string, object>
                                  {
                                      {"ValueType", Syncfusion.Blazor.Charts.ValueType.Category },
                                  }
                              },
                              { "YAxisAttributes", new Dictionary<string, object>
                                  {
                                      {"ValueType", Syncfusion.Blazor.Charts.ValueType.Double },
                                      {"Title", "mW"},
                                  }
                              },
                              { "ToolTipAttributes", new Dictionary<string, object>
                                  {
                                      {"Enable", true },
                                      {"Format", "${series.name}: <b>${point.y} mW</b>" },
                                      {"Shared", true },
                                  }
                              },
                              { "ChartSeriesList", new List<ChartSeriesConfig>
                                  {
                                      new ChartSeriesConfig()
                                      {
                                          SeriesAttributes = new Dictionary<string, object>
                                          {
                                              {"DataSource", DataSource },
                                              {"XName", nameof(Station.Id) },
                                              {"YName", nameof(Station.AnnualProductionTarget) },
                                              {"Name", "Objectif annuel" },
                                              {"Opacity", 0.5 },
                                              {"Type", Syncfusion.Blazor.Charts.ChartSeriesType.SplineArea },
                                          },
                                          MarkerAttributes = new Dictionary<string, object>
                                          {
                                              { "Visible", false }
                                          },
                                          LabelAttributes = new Dictionary<string, object>
                                          {
                                              { "Visible", false },
                                          },
                                      },
                                      new ChartSeriesConfig()
                                      {
                                          SeriesAttributes = new Dictionary<string, object>
                                          {
                                              {"DataSource", DataSource },
                                              {"XName", nameof(Station.Id) },
                                              {"YName", nameof(Station.AnnualProductionActual) },
                                              {"Name", "Réalisation annuelle" },
                                              {"Opacity", 0.5 },
                                              {"Type", Syncfusion.Blazor.Charts.ChartSeriesType.SplineArea },
                                          },
                                          MarkerAttributes = new Dictionary<string, object>
                                          {
                                              { "Visible", false }
                                          },
                                          LabelAttributes = new Dictionary<string, object>
                                          {
                                              { "Visible", false },
                                          },
                                      },
                                   }
                              }
                          }
                     },
                     // Add other PanelObjects here
                  
            };
        }

        public void InitializeInitialPanelData()
        {
            if (InitialSelectedPanels.Any())
            {
                // InitalPanelData contains only the PanelObjects that are initialy visible
                InitialPanelData = PanelData.Where(panels => InitialSelectedPanels.Any(selectedpanels => selectedpanels.Value == panels.Id)).ToList();
            }
        }

        // MapComponent Marker OnClick event
        public void OnMarkerClickEvent(MarkerClickEventArgs args)
        {
            if (args.Data.TryGetValue("Name", out var name)) // Get StationMapData.Name field
            {
                var StationName = DataProvider.stationNames.FirstOrDefault(obj => obj.Value == name); // Find index of element
                if (String.IsNullOrEmpty(StationName.Key) || String.IsNullOrEmpty(StationName.Value) ) { return; }

                NavigationManager.NavigateTo($"station/{StationName.Key}"); // Navigate to station page
                Logger.LogInformation($"Navigate to {StationName.Value} page");
            }
        }

    }
}
