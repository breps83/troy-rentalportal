// Generic modal handling.
//
// Any element with data-modal-url opens the shared modal with the partial view returned from that URL.
// Forms inside the modal are posted with fetch. The server either returns the same partial with
// validation errors (re-rendered in place) or JSON { success: true }, in which case the modal closes
// and the element named by data-refresh-target is reloaded from data-refresh-url. If the JSON includes
// redirectUrl, the page navigates there instead.
//
// A redirect response (e.g. an expired login sending the user to the login page) navigates the whole
// page instead of rendering inside the modal. Error responses show a message instead.
(function () {
    const modalElement = document.getElementById('app-modal');
    if (!modalElement) {
        return;
    }

    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    const content = modalElement.querySelector('.modal-content');
    const ajaxHeaders = { 'X-Requested-With': 'XMLHttpRequest' };
   
    // What to reload after a successful save, captured from the data-refresh-url and
    // data-refresh-target attributes of the button that opened the modal. Set on each click.
    let refresh = null;

    function render(html) {
        content.innerHTML = html;

        const form = content.querySelector('form');
        if (form && window.jQuery && jQuery.validator && jQuery.validator.unobtrusive) {
            jQuery.validator.unobtrusive.parse(form);
        }
    }

    // Picks the message to show for a failed request. response is undefined for network errors.
    function getErrorMessage(response) {
        switch (response && response.status) {
            case 403:
                return 'You do not have permission to perform this action. Reload the page and try again.';
            case 409:
                return 'This record was changed by someone else. Reload the page and try again.';
            default:
                return 'Something went wrong. Please try again.';
        }
    }

    function renderError(response) {
        render(
            '<div class="modal-header">' +
            '<h5 class="modal-title">Error</h5>' +
            '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>' +
            '</div>' +
            '<div class="modal-body"><div class="alert alert-danger mb-0" role="alert"></div></div>');
        content.querySelector('.alert').textContent = getErrorMessage(response);
    }

    // Shows an error above the form without discarding what the user has typed.
    function showFormError(form, response) {
        let alert = form.querySelector('[data-modal-error]');
        if (!alert) {
            alert = document.createElement('div');
            alert.className = 'alert alert-danger';
            alert.setAttribute('role', 'alert');
            alert.setAttribute('data-modal-error', '');
            (form.querySelector('.modal-body') || form).prepend(alert);
        }
        alert.textContent = getErrorMessage(response);
    }

    // Follows a redirect with a full page navigation. Returns true if it did.
    function followRedirect(response) {
        if (response.redirected) {
            window.location.href = response.url;
            return true;
        }
        return false;
    }

    async function refreshTarget() {
        if (!refresh || !refresh.url || !refresh.target) {
            // Nothing to refresh in place; reload so the page reflects the change.
            window.location.reload();
            return;
        }

       
        const target = document.querySelector(refresh.target);
        try {
            const response = await fetch(refresh.url, { headers: ajaxHeaders });
            if (followRedirect(response)) {
                return;
            }
            if (target && response.ok) {
                target.innerHTML = await response.text();
                return;
            }
        } catch {
            // Fall through to a full reload.
        }

        // The save succeeded but the partial refresh failed; reload to show current data.
        window.location.reload();
    }

    document.addEventListener('click', async function (event) {
        const trigger = event.target.closest('[data-modal-url]');
        if (!trigger) {
            return;
        }

        event.preventDefault();
        
        refresh = { url: trigger.dataset.refreshUrl, target: trigger.dataset.refreshTarget };

        try {
            const response = await fetch(trigger.dataset.modalUrl, { headers: ajaxHeaders });
            if (followRedirect(response)) {
                return;
            }

            if (response.ok) {
                render(await response.text());
            } else {
                renderError(response);
            }
        } catch {
            renderError();
        }

        modal.show();
    });

    content.addEventListener('submit', async function (event) {
        const form = event.target;
        event.preventDefault();

        if (window.jQuery && jQuery(form).data('validator') && !jQuery(form).valid()) {
            return;
        }

        const submitButton = form.querySelector('[type="submit"]');
        if (submitButton) {
            submitButton.disabled = true;
        }

        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
                headers: ajaxHeaders
            });

            if (followRedirect(response)) {
                return;
            }

            if (!response.ok) {
                showFormError(form, response);
                return;
            }

            const contentType = response.headers.get('content-type') || '';
            if (contentType.includes('application/json')) {
                const data = await response.json();
                if (data.redirectUrl) {
                    window.location.href = data.redirectUrl;
                    return;
                }
                modal.hide();
                await refreshTarget();
            } else {
                render(await response.text());
            }
        } catch {
            showFormError(form);
        } finally {
            if (submitButton && submitButton.isConnected) {
                submitButton.disabled = false;
            }
        }
    });
})();

// Re-check a field that shows an error as soon as its value changes (paste, autofill, date picker,
// dropdown), not only when it loses focus. Otherwise the error disappears on the mousedown of the
// Save click, the layout shifts, and the click misses the button. Applies to every form, including
// the modals and the application page.
(function () {
    function revalidateChangedField(event) {
        const field = event.target;

        if (!field.form || !field.classList.contains('input-validation-error')) {
            return;
        }

        if (window.jQuery && jQuery(field.form).data('validator')) {
            jQuery(field).valid();
        }
    }

    document.addEventListener('input', revalidateChangedField);
    document.addEventListener('change', revalidateChangedField);
})();
