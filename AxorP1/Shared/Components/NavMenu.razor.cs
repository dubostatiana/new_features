using System.Security.AccessControl;
using AxorP1.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Syncfusion.Blazor.Navigations;

namespace AxorP1.Shared.Components
{
    public class NavMenuBase : MainComponent<NavMenu>, IDisposable
    {
        protected SfSidebar? SidebarRef;
        protected SidebarType Type = SidebarType.Push;

        // Lock the Sidebar in open state
        protected bool SidebarLocked = false;

        // CSS class (open or close)
        protected string ToggleClass = "close";
        // Specify the value of Sidebar component state (open/close).
        protected bool SidebarToggle { get; set; }

        protected override void OnInitialized()
        {
           base.OnInitialized();

            // Handle LocationChanged Event
            NavigationManager.LocationChanged += HandleLocationChanged;
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await ChangeSidebarType();

                try
                {
                    // Set dotNet reference to access NavMenu component when window is resized
                    var dotNetReference = DotNetObjectReference.Create(this);
                    await JSRuntime.InvokeVoidAsync("OnResize", dotNetReference);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        // Window OnResize event
        [JSInvokable] 
        public async Task OnBrowserResize()
        {
            await ChangeSidebarType();
        }

        // Change Sidebar Type dynamically
        public async Task ChangeSidebarType()
        {
            try
            {
                // If small screen, change the type of the Sidebar to Over
                var width = await JSRuntime.InvokeAsync<int>("getWidth");
                var type = width > 600 ? SidebarType.Push : SidebarType.Over;

                if (Type != type)
                {
                    // If Sidebar Type is Over and it is lock
                    if (type == SidebarType.Over && SidebarLocked)
                    {
                        // Unlock and close the Sidebar
                        SidebarLocked = false;
                        SidebarToggle = false;
                    }

                    Type = type;
                    StateHasChanged();

                    Logger.LogInformation("Sidebar Type has changed");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
            }

        }

        // Close Sidebar when location changed
        private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            Logger.LogInformation($"URL of new location: {e.Location}");

            // If Sidebar is open
            if (SidebarRef.IsOpen)
            {
                // If Sidebar is not lock
                if (SidebarLocked == false)
                {
                    // Close the Sidebar
                    SidebarToggle = false;
                    StateHasChanged();
                }
            }
        }

        // Implement IDisposable
        public void Dispose()
        {
            // Unhooking HandleLocationChanged method
            NavigationManager.LocationChanged -= HandleLocationChanged;
        }

        // Event handler for Clicked event on Toggle Button 
        protected void ToggleSidebar(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
        {
            // If Sidebar is open
            if (SidebarToggle == true)
            {
                if(Type != SidebarType.Over) // Dont lock Sidebar if Type is Over
                {
                    // Lock/unlock the Sidebar
                    SidebarLocked = !SidebarLocked;
                }
            }

            // If Sidebar is not lock
            if (SidebarLocked == false)
            {
                // Open/close the Sidebar 
                SidebarToggle = !SidebarToggle;
                // Update the css class
                ToggleClass = SidebarToggle ? "open" : "close";
            }
            else
            {   // If Sidebar is lock
                // Adding the locked class
                ToggleClass += " locked";
            }

            StateHasChanged();
        }

        // Method to handle the close event
        public void OnClose(Syncfusion.Blazor.Navigations.EventArgs args)
        {
            // If Sidebar is not lock
            if (SidebarLocked == false)
            {
                // Update the css class
                ToggleClass = "close";
            }
            else
            {
                // If Sidebar is lock
                // Prevent the Sidebar from closing
                args.Cancel = true;
            }
        }

    }
}

