using AxorP1.Class;
using AxorP1.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Syncfusion.Blazor.Buttons;
using Syncfusion.Blazor.Navigations;

namespace AxorP1.Shared.Components
{
    public class ToolbarBase : MainComponent<Toolbar>
    {
        protected SfToolbar? ToolbarRef;

        protected bool IsDialogVisible = true; // Dialog is visible on first render

        // EventCallBack of Theme switcher
        protected EventCallback<ChangeEventArgs<bool?>> switchStateChanged;

        // Full screen toolbar item
        protected bool FullScreenIsOn;
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

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // EventCallback for SfSwitch component ValueChange
            switchStateChanged = EventCallback.Factory.Create<ChangeEventArgs<bool?>>(this, HandleSwitchStateChanged);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender) // On first render
            {
                try
                {
                    // Set dotNet reference to access Toolbar component when document FullScreen changed
                    var dotNetReference = DotNetObjectReference.Create(this);
                    await JSRuntime.InvokeVoidAsync("OnFullScreenChange", dotNetReference);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
                } 
            }
        }

        // Close the SfDialog
        public void CloseDialog()
        {
            IsDialogVisible = false;
        }

        // OnClick event on FullScreen item
        public async Task FullScreenRequest()
        {
            try
            {
                // Put/Remove full screen mode
                if (FullScreenIsOn) 
                    { await JSRuntime.InvokeVoidAsync("exitFullScreen"); }
                else 
                    { await JSRuntime.InvokeVoidAsync("enterFullScreen"); }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Document OnFullScreenChange event
        [JSInvokable]
        public async Task FullScreenChanged()
        {
            var isFullscreen = await JSRuntime.InvokeAsync<bool>("isFullScreenOn"); // Check full screen state
            FullScreenIsOn = isFullscreen; // Update flag to change the icon
            StateHasChanged();
        }

        // Handle Change event on theme switcher item  
        protected async Task HandleSwitchStateChanged(Syncfusion.Blazor.Buttons.ChangeEventArgs<bool?> args)
        {
            if (ThemeProvider.isDarkMode != args.Checked)
            {
                var darkTheme = (bool)args.Checked;
                await ThemeProvider.ChangeTheme(darkTheme); // Change theme
            }
        }
    }
}
