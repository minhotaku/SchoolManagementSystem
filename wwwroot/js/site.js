// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Sidebar state persistence
// Apply sidebar state immediately before DOM content loaded 
// to prevent flickering during page transitions
// Enable transitions after page load to prevent flickering
document.addEventListener('DOMContentLoaded', function () {
    // Wait a moment to ensure everything is rendered
    setTimeout(function () {
        document.documentElement.classList.add('transitions-enabled');
    }, 100);
});