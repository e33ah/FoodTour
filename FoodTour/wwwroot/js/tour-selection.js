document.addEventListener('DOMContentLoaded', () => {
    // Constants
    const MAX_SELECTIONS = parseInt(document.querySelector('[data-max-selections]')?.dataset.maxSelections || '0');
    const ERROR_TIMEOUT_MS = 5000;

    // Cached selectors
    const checkboxes = document.querySelectorAll('.supplier-checkbox');
    const supplierCards = document.querySelectorAll('.supplier-card');
    const createTourBtn = document.getElementById('createTourBtn');

    let messageTimeout;
    let currentMessage = null;

    // Utility functions
    const updateButtonState = () => {
        const checkedCount = document.querySelectorAll('.supplier-checkbox:checked').length;
        if (createTourBtn) {
            createTourBtn.disabled = checkedCount !== MAX_SELECTIONS;
        }
    };

    const updateCardVisualState = (card, checkbox) => {
        card.classList.toggle('selected', checkbox.checked);
    };

    const showMaxSelectionMessage = (card) => {
        const messageElement = document.createElement('div');
        messageElement.className = 'alert alert-warning mt-2 mb-3';
        messageElement.innerHTML = `<strong>Maximum selections reached!</strong><br>You must select exactly ${MAX_SELECTIONS} tasting experiences for your tour.`;
        card.parentNode.insertBefore(messageElement, card.nextSibling);
        currentMessage = messageElement;
        clearTimeout(messageTimeout);
        messageTimeout = setTimeout(() => {
            if (messageElement.parentNode) messageElement.remove();
            currentMessage = null;
        }, ERROR_TIMEOUT_MS);
    };

    const handleCheckboxChange = (checkbox) => {
        const checkedCount = document.querySelectorAll('.supplier-checkbox:checked').length;
        const card = checkbox.closest('.supplier-card');
        if (currentMessage) {
            currentMessage.remove();
            currentMessage = null;
        }
        if (checkedCount > MAX_SELECTIONS) {
            checkbox.checked = false;
            showMaxSelectionMessage(card);
        }
        updateCardVisualState(card, checkbox);
        updateButtonState();
    };

    // Event listeners for supplier selection
    const setupCheckboxes = () => {
        checkboxes.forEach(checkbox => {
            checkbox.addEventListener('change', () => handleCheckboxChange(checkbox));
        });
    };

    const setupSupplierCards = () => {
        supplierCards.forEach(card => {
            card.addEventListener('click', event => {
                if (event.target.closest('.supplier-checkbox')) return;
                const checkbox = card.querySelector('.supplier-checkbox');
                checkbox.checked = !checkbox.checked;
                handleCheckboxChange(checkbox);
            });
        });
    };

    // Email validation helpers
    const emailInput = document.getElementById('customerEmail');

    const isValidEmail = (email) => {
        // Simple email regex for client-side validation
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    };

    const showEmailError = (message) => {
        clearEmailError();
        if (emailInput) {
            const formGroup = emailInput.closest('.form-group');
            if (formGroup) {
                const errorElement = document.createElement('div');
                errorElement.className = 'text-danger small mt-1 email-validation-error';
                errorElement.textContent = message;
                errorElement.setAttribute('tabindex', '-1');
                formGroup.parentNode.insertBefore(errorElement, formGroup.nextSibling);
            }
        }
    };

    const clearEmailError = () => {
        if (emailInput) {
            const formGroup = emailInput.closest('.form-group');
            if (formGroup) {
                const nextSibling = formGroup.nextSibling;
                if (nextSibling && nextSibling.classList && nextSibling.classList.contains('email-validation-error')) {
                    nextSibling.remove();
                }
            }
        }
    };

    // Purchase code validation helpers
    const purchaseCodeInput = document.getElementById('purchaseCode');

    const showPurchaseCodeError = (message) => {
        clearPurchaseCodeError();
        if (purchaseCodeInput) {
            const formGroup = purchaseCodeInput.closest('.form-group');
            if (formGroup) {
                const errorElement = document.createElement('div');
                errorElement.className = 'text-danger small mt-1 purchase-code-validation-error';
                errorElement.textContent = message;
                errorElement.setAttribute('tabindex', '-1');
                formGroup.parentNode.insertBefore(errorElement, formGroup.nextSibling);
                purchaseCodeInput.focus();
            }
        }
    };

    const clearPurchaseCodeError = () => {
        if (purchaseCodeInput) {
            const formGroup = purchaseCodeInput.closest('.form-group');
            if (formGroup) {
                const nextSibling = formGroup.nextSibling;
                if (nextSibling && nextSibling.classList && nextSibling.classList.contains('purchase-code-validation-error')) {
                    nextSibling.remove();
                }
            }
        }
    };

    // Booking check functionality
    let validatedBookingId = null;
    const bookingIdInput = document.getElementById('BookingId');

    const showBookingSuccess = (message, bookingId) => {
        clearBookingMessages();
        validatedBookingId = bookingId;
        if (bookingIdInput) {
            bookingIdInput.value = bookingId;
        }
        if (purchaseCodeInput) {
            const card = purchaseCodeInput.closest('.card');
            if (card) {
                const successElement = document.createElement('div');
                successElement.className = 'alert alert-success mb-4 booking-message';
                successElement.innerHTML = `<i class="bi bi-check-circle-fill"></i> ${message}`;
                card.parentNode.insertBefore(successElement, card.nextSibling);
            }
        }
    };

    const showBookingWarning = (message) => {
        clearBookingMessages();
        if (purchaseCodeInput) {
            const card = purchaseCodeInput.closest('.card');
            if (card) {
                const warningElement = document.createElement('div');
                warningElement.className = 'alert alert-warning mb-4 booking-message';
                warningElement.innerHTML = `<i class="bi bi-exclamation-triangle-fill"></i> ${message}`;
                card.parentNode.insertBefore(warningElement, card.nextSibling);
            }
        }
    };

    const clearBookingSuccess = () => {
        const existingMessage = document.querySelector('.booking-success-message');
        if (existingMessage) {
            existingMessage.remove();
        }
    };

    const clearBookingMessages = () => {
        const existingMessage = document.querySelector('.booking-message');
        if (existingMessage) {
            existingMessage.remove();
        }
        // Also clear old success messages for backwards compatibility
        clearBookingSuccess();
        // Clear validated booking ID
        validatedBookingId = null;
        if (bookingIdInput) {
            bookingIdInput.value = '';
        }
    };

    const checkBookingExists = async () => {
        const email = emailInput?.value.trim();
        const purchaseCode = purchaseCodeInput?.value.trim();

        // Clear any previous messages
        clearBookingMessages();
        clearEmailError();
        clearPurchaseCodeError();

        // Only check if both fields have values
        if (!email || !purchaseCode) {
            return;
        }

        // Validate email format
        if (!isValidEmail(email)) {
            return;
        }

        try {
            const response = await fetch('/api/foodtour/checkbooking', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    customerEmail: email,
                    purchaseCode: purchaseCode
                })
            });

            if (!response.ok) {
                console.error('Booking check failed:', response.status);
                return;
            }

            const data = await response.json();

            if (data.found) {
                if (data.isCancelled) {
                    showBookingWarning(data.message);
                } else if (data.bookingId) {
                    showBookingSuccess(data.message, data.bookingId);
                } else {
                    showBookingWarning('Booking found but missing booking reference. Please contact support.');
                }
            } else {
                showBookingWarning(data.message);
            }
        } catch (error) {
            console.error('Error checking booking:', error);
        }
    };

    // Debounce function to avoid too many API calls
    let bookingCheckTimeout;
    const debouncedBookingCheck = () => {
        clearTimeout(bookingCheckTimeout);
        bookingCheckTimeout = setTimeout(checkBookingExists, 500);
    };

    // Add form validation on submit
    const tourForm = document.querySelector('form[data-max-selections]');
    if (tourForm) {
        const restorePostedSelections = () => {
            const selectedSuppliersJson = tourForm.dataset.selectedSuppliers;
            if (!selectedSuppliersJson || selectedSuppliersJson.trim() === '') {
                return;
            }

            let selectedSuppliers;
            try {
                selectedSuppliers = JSON.parse(selectedSuppliersJson);
            } catch {
                return;
            }

            if (!Array.isArray(selectedSuppliers)) {
                return;
            }

            selectedSuppliers.forEach(id => {
                const checkbox = document.getElementById(`supplier-${id}`);
                if (checkbox) {
                    checkbox.checked = true;
                    const card = checkbox.closest('.supplier-card');
                    if (card) {
                        card.classList.add('selected');
                    }
                }
            });

            updateButtonState();
        };

        tourForm.addEventListener('submit', function (event) {
            // Check if booking has been validated
            if (!validatedBookingId) {
                event.preventDefault();

                // Focus on the first missing or invalid field
                if (!emailInput || !emailInput.value.trim()) {
                    showEmailError('Please enter your email address to validate your booking.');
                    emailInput.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    emailInput.focus();
                } else if (!isValidEmail(emailInput.value.trim())) {
                    showEmailError('Please enter a valid email address.');
                    emailInput.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    emailInput.focus();
                } else if (!purchaseCodeInput || !purchaseCodeInput.value.trim()) {
                    showPurchaseCodeError('Please enter your booking reference to validate your booking.');
                    purchaseCodeInput.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    purchaseCodeInput.focus();
                } else {
                    // Both fields have values but booking not found
                    showBookingWarning('We couldn\'t find your booking. Please check your email and booking reference are correct.');
                    emailInput.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    emailInput.focus();
                }

                return false;
            }
        });

        restorePostedSelections();
    }

    // Initialise
    setupCheckboxes();
    setupSupplierCards();
    updateButtonState();

    // Check if email and purchase code are pre-populated (from qrToken navigation)
    if (emailInput?.value.trim() && purchaseCodeInput?.value.trim()) {
        checkBookingExists();
    }

    // Clear purchase code and email error messages as soon as the user types or changes the value
    if (purchaseCodeInput) {
        purchaseCodeInput.addEventListener('input', () => {
            clearPurchaseCodeError();
            clearBookingMessages();
            debouncedBookingCheck();
        });
        purchaseCodeInput.addEventListener('change', () => {
            clearPurchaseCodeError();
            clearBookingMessages();
            debouncedBookingCheck();
        });
    }

    if (emailInput) {
        emailInput.addEventListener('input', () => {
            clearEmailError();
            clearBookingMessages();
            debouncedBookingCheck();
        });
        emailInput.addEventListener('change', () => {
            clearEmailError();
            clearBookingMessages();
            debouncedBookingCheck();
        });
    }
});
