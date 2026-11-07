/* globals feather:false */

(function() {
    'use strict';

    document.addEventListener('click', function (event) {
        var button = event.target.closest('[data-confirm]');

        if (button && !window.confirm(button.dataset.confirm)) {
            event.preventDefault();
        }
    });

    feather.replace();
})();