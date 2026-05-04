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
        card.setAttribute('aria-checked', checkbox.checked.toString());
    };

    const getOpeningTimesElements = (card) => ({
        container: card.querySelector('[data-opening-times-display]'),
        text: card.querySelector('[data-opening-times-text]')
    });

    const setOpeningTimesDisplay = (card, openingTimes) => {
        const { container, text } = getOpeningTimesElements(card);
        if (!container || !text) {
            return;
        }

        if (openingTimes && openingTimes.trim()) {
            text.textContent = openingTimes;
            container.classList.remove('d-none');
        } else {
            text.textContent = '';
            container.classList.add('d-none');
        }
    };

    const restoreInitialOpeningTimes = () => {
        supplierCards.forEach(card => {
            setOpeningTimesDisplay(card, card.dataset.openingTimesInitial || '');
        });
    };

    const getOpeningTimesForDay = (card, dayOfWeek) => {
        const generalOpeningTimes = card.dataset.openingTimesGeneral || '';

        switch (dayOfWeek) {
            case 0:
                return card.dataset.openingTimesSunday || generalOpeningTimes;
            case 1:
                return card.dataset.openingTimesMonday || generalOpeningTimes;
            case 2:
                return card.dataset.openingTimesTuesday || generalOpeningTimes;
            case 3:
                return card.dataset.openingTimesWednesday || generalOpeningTimes;
            case 4:
                return card.dataset.openingTimesThursday || generalOpeningTimes;
            case 5:
                return card.dataset.openingTimesFriday || generalOpeningTimes;
            case 6:
                return card.dataset.openingTimesSaturday || generalOpeningTimes;
            default:
                return generalOpeningTimes;
        }
    };

    const getDayOfWeekFromTourDate = (tourDate) => {
        if (!tourDate) {
            return null;
        }

        const datePart = tourDate.split('T')[0];
        const dateParts = datePart.split('-').map(Number);
        if (dateParts.length !== 3 || dateParts.some(Number.isNaN)) {
            return null;
        }

        const [year, month, day] = dateParts;
        const parsedDate = new Date(year, month - 1, day);
        return Number.isNaN(parsedDate.getTime()) ? null : parsedDate.getDay();
    };

    const applyTourDateOpeningTimes = (tourDate) => {
        const dayOfWeek = getDayOfWeekFromTourDate(tourDate);
        if (dayOfWeek === null) {
            restoreInitialOpeningTimes();
            return;
        }

        supplierCards.forEach(card => {
            setOpeningTimesDisplay(card, getOpeningTimesForDay(card, dayOfWeek));
        });
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

    const toggleSupplierCheckbox = (card) => {
        const checkbox = card.querySelector('.supplier-checkbox');
        checkbox.checked = !checkbox.checked;
        handleCheckboxChange(checkbox);
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
                toggleSupplierCheckbox(card);
            });

            card.addEventListener('keydown', event => {
                if (event.key !== ' ' && event.key !== 'Space' && event.key !== 'Spacebar' && event.key !== 'Enter') {
                    return;
                }

                event.preventDefault();
                toggleSupplierCheckbox(card);
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
    let partnerConfirmationUrl = null;
    const bookingIdInput = document.getElementById('BookingId');
    const bookingStatusMessage = document.getElementById('bookingStatusMessage');
    const bookingStatusIcon = document.getElementById('bookingStatusIcon');
    const bookingStatusText = document.getElementById('bookingStatusText');
    const bookingConfirmedModalElement = document.getElementById('bookingConfirmedModal');
    const viewTourDetailsLink = document.getElementById('viewTourDetailsLink');
    const partnerConfirmationPrompt = document.getElementById('partnerConfirmationPrompt');
    const partnerConfirmationMessage = document.getElementById('partnerConfirmationMessage');
    const partnerConfirmationLink = document.getElementById('partnerConfirmationLink');
    const bookingConfirmedModal = bookingConfirmedModalElement ? bootstrap.Modal.getOrCreateInstance(bookingConfirmedModalElement) : null;
    // Tracks the booking key for the confirmation modal that is currently displayed.
    let activeConfirmedBookingKey = null;
    // Tracks the booking key for the most recently dismissed confirmation modal to prevent it from being shown again for the same booking.
    let dismissedConfirmedBookingKey = null;

    if (bookingConfirmedModalElement) {
        bookingConfirmedModalElement.addEventListener('hidden.bs.modal', () => {
            dismissedConfirmedBookingKey = activeConfirmedBookingKey;
            activeConfirmedBookingKey = null;
        });
    }

    const showBookingMessage = (alertClass, iconClass, message) => {
        if (bookingStatusMessage && bookingStatusIcon && bookingStatusText) {
            bookingStatusMessage.className = `alert ${alertClass} mb-4`;
            bookingStatusIcon.className = `bi ${iconClass}`;
            bookingStatusText.textContent = ` ${message}`;
            bookingStatusMessage.classList.remove('d-none');
        }
    };

    const showBookingSuccess = (message, bookingId) => {
        clearBookingMessages(false);
        validatedBookingId = bookingId;
        if (bookingIdInput) {
            bookingIdInput.value = bookingId;
        }
        showBookingMessage('alert-success', 'bi-check-circle-fill', message);
    };

    const showBookingWarning = (message) => {
        clearBookingMessages();
        showBookingMessage('alert-warning', 'bi-exclamation-triangle-fill', message);
    };

    const showPartnerConfirmationPrompt = (message, confirmationUrl) => {
        clearBookingMessages();
        partnerConfirmationUrl = confirmationUrl;
        if (partnerConfirmationPrompt && partnerConfirmationMessage && partnerConfirmationLink) {
            partnerConfirmationMessage.innerHTML = `<i class="bi bi-info-circle-fill"></i> ${message}`;
            partnerConfirmationLink.href = confirmationUrl;
            partnerConfirmationPrompt.classList.remove('d-none');
        }
    };

    const clearBookingMessages = (resetOpeningTimes = true) => {
        if (bookingStatusMessage) {
            bookingStatusMessage.classList.add('d-none');
        }
        if (partnerConfirmationPrompt) {
            partnerConfirmationPrompt.classList.add('d-none');
        }
        // Clear validated booking ID
        validatedBookingId = null;
        partnerConfirmationUrl = null;
        if (bookingIdInput) {
            bookingIdInput.value = '';
        }
        if (resetOpeningTimes) {
            restoreInitialOpeningTimes();
        }
    };

    const showBookingConfirmedModal = (email, purchaseCode, qrToken) => {
        const normalisedEmail = email?.trim().toLowerCase();
        const normalisedPurchaseCode = purchaseCode?.trim();
        const confirmedBookingKey = `${normalisedEmail}|${normalisedPurchaseCode}|${qrToken}`;

        if (bookingConfirmedModal && bookingConfirmedModalElement && viewTourDetailsLink) {
            if (dismissedConfirmedBookingKey === confirmedBookingKey) {
                return;
            }

            if (activeConfirmedBookingKey === confirmedBookingKey && bookingConfirmedModalElement.classList.contains('show')) {
                return;
            }

            viewTourDetailsLink.href = `/Home/FoodTourCode?qrToken=${qrToken}`;
            activeConfirmedBookingKey = confirmedBookingKey;
            dismissedConfirmedBookingKey = null;
            bookingConfirmedModal.show();
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

            if (data.requiresPartnerConfirmation) {
                const encodedReference = encodeURIComponent(purchaseCode);
                const encodedEmail = encodeURIComponent(email);
                const confirmationUrl = `/Home/PartnerBookingConfirmation?bookingReference=${encodedReference}&customerEmail=${encodedEmail}`;
                showPartnerConfirmationPrompt(data.message, confirmationUrl);
            } else if (data.found) {
                if (data.isCancelled) {
                    showBookingWarning(data.message);
                } else if (data.isConfirmed) {
                    applyTourDateOpeningTimes(data.tourDate);
                    showBookingConfirmedModal(email, purchaseCode, data.qrToken);
                } else {
                    showBookingSuccess(data.message, data.bookingId);
                    applyTourDateOpeningTimes(data.tourDate);
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
                        updateCardVisualState(card, checkbox);
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
                } else if (partnerConfirmationUrl) {
                    showPartnerConfirmationPrompt('We\'ve located your booking through one of our partner sites.  Please confirm your email address to continue.', partnerConfirmationUrl);
                    emailInput.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    emailInput.focus();
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
