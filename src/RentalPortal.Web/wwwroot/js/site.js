// Generic modal handling.
//
// Any element with data-modal-url opens the shared modal with the partial view returned from that URL.
// Forms inside the modal are submitted via fetch: a redirect response navigates the page (success, or
// an expired login sending the user to the login page), and any other successful response is
// re-rendered inside the modal (e.g. validation errors). Error responses show a message instead.
(function () {
    const modalElement = document.getElementById('app-modal');
    if (!modalElement) {
        return;
    }

    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    const content = modalElement.querySelector('.modal-content');
    const ajaxHeaders = { 'X-Requested-With': 'XMLHttpRequest' };
    const errorMessage = 'Something went wrong. Please try again.';

    function render(html) {
        content.innerHTML = html;
    }

    function renderError() {
        render(
            '<div class="modal-header">' +
            '<h5 class="modal-title">Error</h5>' +
            '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>' +
            '</div>' +
            '<div class="modal-body"><div class="alert alert-danger mb-0" role="alert"></div></div>');
        content.querySelector('.alert').textContent = errorMessage;
    }

    // Shows an error above the form without discarding what the user has typed.
    function showFormError(form) {
        let alert = form.querySelector('[data-modal-error]');
        if (!alert) {
            alert = document.createElement('div');
            alert.className = 'alert alert-danger';
            alert.setAttribute('role', 'alert');
            alert.setAttribute('data-modal-error', '');
            (form.querySelector('.modal-body') || form).prepend(alert);
        }
        alert.textContent = errorMessage;
    }

    document.addEventListener('click', async function (event) {
        const trigger = event.target.closest('[data-modal-url]');
        if (!trigger) {
            return;
        }

        event.preventDefault();

        try {
            const response = await fetch(trigger.dataset.modalUrl, { headers: ajaxHeaders });

            if (response.redirected) {
                window.location.href = response.url;
                return;
            }

            if (response.ok) {
                render(await response.text());
            } else {
                renderError();
            }
        } catch {
            renderError();
        }

        modal.show();
    });

    content.addEventListener('submit', async function (event) {
        const form = event.target.closest('form');
        if (!form) {
            return;
        }

        event.preventDefault();

        const submitButton = form.querySelector('[type="submit"]');
        if (submitButton) {
            submitButton.disabled = true;
        }

        try {
            const response = await fetch(form.action, {
                method: form.method || 'post',
                headers: ajaxHeaders,
                body: new FormData(form)
            });

            if (response.redirected) {
                window.location.href = response.url;
                return;
            }

            if (response.ok) {
                render(await response.text());
            } else {
                showFormError(form);
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
