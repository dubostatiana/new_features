// Get window inner width
function getWidth() {
    return (window.innerWidth)
}

// Invoke OnBrowserResize method when window is resized
function OnResize(reference) {
    // Set the dotNet reference
    var dotNetReference = reference;
    var debounceTimer;

    // Define the onResize handler with debouncing
    window.onresize = () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            // Call OnBrowserResize method on blazor component after resize is complete
            dotNetReference.invokeMethodAsync('OnBrowserResize');
        }, 100); // Adjust the delay as needed
    };
}

// Invoke OnContainerResize when the container element is resized
function OnContainerResize(reference, id) {
    // Set the dotNet reference
    var dotNetReference = reference;
    var debounceTimer;

    // Get the container div by its id
    var container = document.getElementById(id);

    // Create a ResizeObserver to observe the container for size changes with debouncing
    var resizeObserver = new ResizeObserver(entries => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            const entry = entries[0];
            if (entry) {
                const width = Math.round(entry.contentRect.width);
                const height = Math.round(entry.contentRect.height);
                // Now we pass the width and height as parameters to the method
                dotNetReference.invokeMethodAsync('OnContainerResize', width, height);
            }
        }, 100); // delay 
    });

    // Start observing the specified container div for size changes
    resizeObserver.observe(container);
}

// Change the stylesheet of the app
function changeTheme(newLink) {

    let existingLink = document.getElementById('theme-link');

    if (!existingLink) {
        existingLink = document.createElement('link');
        existingLink.id = 'theme-link';
        existingLink.rel = 'stylesheet';
        document.head.appendChild(existingLink);
    }

    existingLink.href = newLink;
}

// The page scrolls to the top
function resetScrollPosition() {
    let page = document.getElementById("content");
    page.scrollTo(0, 0);
}

// Invoke FullScreenChanged method when full screen changed
function OnFullScreenChange(reference, elementId) {
    var element = elementId == undefined ? document : document.getElementById(elementId);
    // Set the dotNet reference
    var dotNetReference = reference;

    // Define the onFullScreenChange handler
    element.onfullscreenchange = () => {
        dotNetReference.invokeMethodAsync('FullScreenChanged');
    };
}

// Full Screen mode
function enterFullScreen(elementId) { // Enter
    var element = elementId == undefined ? document.documentElement : document.getElementById(elementId);

    if (element.requestFullscreen) {
        element.requestFullscreen();
    } else if (element.mozRequestFullScreen) { // Firefox
        element.mozRequestFullScreen();
    } else if (element.webkitRequestFullscreen) { // Chrome, Safari, Opera
        element.webkitRequestFullscreen();
    } else if (element.msRequestFullscreen) { // IE/Edge
        element.msRequestFullscreen();
    }
}

function exitFullScreen() { // Exit
    if (document.exitFullscreen) {
        document.exitFullscreen();
    } else if (document.mozCancelFullScreen) { // Firefox
        document.mozCancelFullScreen();
    } else if (document.webkitExitFullscreen) { // Chrome, Safari, Opera
        document.webkitExitFullscreen();
    } else if (document.msExitFullscreen) { // IE/Edge
        document.msExitFullscreen();
    }
}

// Return full screen state
function isFullScreenOn() {
    return (
        document.fullscreenElement ||       // Standard property
        document.webkitFullscreenElement || // Chrome, Safari and Opera property
        document.mozFullScreenElement ||    // Firefox property
        document.msFullscreenElement        // IE/Edge property
    ) ? true : false;
}
