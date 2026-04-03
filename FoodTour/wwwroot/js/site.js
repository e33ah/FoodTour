// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Cookie consent handling
document.addEventListener('DOMContentLoaded', function() {
    const cookieBanner = document.getElementById('cookie-consent-banner');

    if (cookieBanner) {
        const acceptButton = document.getElementById('cookie-consent-accept');
        const declineButton = document.getElementById('cookie-consent-decline');

        acceptButton.addEventListener('click', function() {
            setCookie('cookie-consent', 'accepted', 365);
            hideBanner();
        });

        declineButton.addEventListener('click', function() {
            setCookie('cookie-consent', 'declined', 365);
            hideBanner();
        });

        function hideBanner() {
            cookieBanner.classList.add('fade-out');
            setTimeout(function() {
                cookieBanner.remove();
            }, 300);
        }

        function setCookie(name, value, days) {
            const date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
            const expires = "expires=" + date.toUTCString();
            document.cookie = name + "=" + value + ";" + expires + ";path=/;SameSite=Lax";
        }
    }
});

