document.addEventListener('DOMContentLoaded', () => {
    // Constants
    const ERROR_TIMEOUT_MS = 5000;

    // Get the maximum number of selections from the form's data attribute
    let maxSelections = parseInt(document.querySelector('[data-max-selections]')?.dataset.maxSelections || '0');

    const formEl = document.querySelector('form[data-max-selections]');
    const headingTemplateWithCount = formEl?.dataset.headingTemplate || 'Select {0} tasting experiences to create your personalised food tasting tour';
    const headingTemplateNoCount = formEl?.dataset.headingTemplateNoCount || 'Select your tasting experiences to create your personalised food tasting tour';

    const setMaxSelections = (value) => {
        maxSelections = parseInt(String(value) || '0');
        if (formEl) {
            formEl.setAttribute('data-max-selections', String(maxSelections));
        }
        // Update heading text to be more user-friendly when count unknown
        const heading = document.getElementById('selectionHeading');
        if (heading) {
            if (maxSelections > 0) {
                heading.textContent = headingTemplateWithCount.replace('{0}', String(maxSelections));
            } else {
                heading.textContent = headingTemplateNoCount;
            }
        }

        updateButtonState();
    };


    // Cached selectors
    const checkboxes = document.querySelectorAll('.supplier-checkbox');
    const supplierCards = document.querySelectorAll('.supplier-card');
    const createTourBtn = document.getElementById('createTourBtn');
    const supplierTagFilter = document.getElementById('supplierTagFilter');
    const supplierTagFilterIconButton = document.getElementById('supplierTagFilterIconButton');
    const supplierTagFilterIcon = document.getElementById('supplierTagFilterIcon');
    const supplierTagFilterStatus = document.getElementById('supplierTagFilterStatus');
    const supplierTagNoResults = document.getElementById('supplierTagNoResults');

    let messageTimeout;
    let currentMessage = null;

    // Whether supplier selection is locked until a booking is validated
    let selectionLocked = true;

    const showSelectionLockedMessage = () => {
        if (bookingStatusMessage && bookingStatusIcon && bookingStatusText) {
            showBookingMessage('alert-warning', 'bi-lock-fill', 'Please enter your email and booking reference to enable tasting selection.');
            // focus is handled inside showBookingMessage
        } else {
            // Fallback small inline message near the form
            if (!currentMessage) {
                const messageElement = document.createElement('div');
                messageElement.className = 'alert alert-warning mt-2 mb-3';
                messageElement.setAttribute('role', 'alert');
                messageElement.setAttribute('aria-live', 'polite');
                messageElement.setAttribute('aria-atomic', 'true');
                messageElement.setAttribute('tabindex', '-1');
                messageElement.textContent = 'Please enter your email and booking reference to enable tasting selection.';
                const mainForm = document.querySelector('form[data-max-selections]');
                if (mainForm) {
                    mainForm.parentNode.insertBefore(messageElement, mainForm);
                    currentMessage = messageElement;
                    messageElement.focus();
                    clearTimeout(messageTimeout);
                    messageTimeout = setTimeout(() => {
                        if (messageElement.parentNode) messageElement.remove();
                        currentMessage = null;
                    }, ERROR_TIMEOUT_MS);
                }
            } else {
                currentMessage.focus();
            }
        }
    };

    const lockSelection = () => {
        selectionLocked = true;
        checkboxes.forEach(cb => cb.disabled = true);
        supplierCards.forEach(card => {
            card.classList.add('selection-locked');
            card.setAttribute('tabindex', '-1');
            card.setAttribute('aria-disabled', 'true');
        });
        if (createTourBtn) createTourBtn.disabled = true;
    };

    const unlockSelection = () => {
        selectionLocked = false;
        checkboxes.forEach(cb => cb.disabled = false);
        supplierCards.forEach(card => {
            card.classList.remove('selection-locked');
            card.setAttribute('tabindex', '0');
            card.setAttribute('aria-disabled', 'false');
        });
        updateButtonState();
    };

    // Utility functions
    const updateButtonState = () => {
        if (selectionLocked) {
            if (createTourBtn) createTourBtn.disabled = true;
            return;
        }
        const checkedCount = document.querySelectorAll('.supplier-checkbox:checked').length;
        if (createTourBtn) {
            createTourBtn.disabled = checkedCount !== maxSelections;
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
        messageElement.setAttribute('role', 'alert');
        messageElement.setAttribute('aria-live', 'polite');
        messageElement.setAttribute('aria-atomic', 'true');
        messageElement.innerHTML = `<strong>Maximum selections reached!</strong><br>You must select exactly ${maxSelections} tasting experiences for your tour.`;
        card.parentNode.insertBefore(messageElement, card.nextSibling);
        currentMessage = messageElement;
        clearTimeout(messageTimeout);
        messageTimeout = setTimeout(() => {
            if (messageElement.parentNode) messageElement.remove();
            currentMessage = null;
        }, ERROR_TIMEOUT_MS);
    };

    const handleCheckboxChange = (checkbox) => {
        // Guard against programmatic or legacy-browser events while selection is locked
        if (selectionLocked) {
            checkbox.checked = false;
            return;
        }

        const checkedCount = document.querySelectorAll('.supplier-checkbox:checked').length;
        const card = checkbox.closest('.supplier-card');
        if (currentMessage) {
            currentMessage.remove();
            currentMessage = null;
        }
        if (checkedCount > maxSelections) {
            checkbox.checked = false;
            showMaxSelectionMessage(card);
        }
        updateCardVisualState(card, checkbox);
        updateButtonState();
    };

    const toggleSupplierCheckbox = (card) => {
        if (selectionLocked) {
            showSelectionLockedMessage();
            return;
        }

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

    const normaliseTag = (tag) => (tag || '').trim().toLowerCase();

    const getPluralSuffix = (value) => value === 1 ? '' : 's';

    const updateSupplierTagFilterIcon = (selectedTag) => {
        if (!supplierTagFilterIconButton || !supplierTagFilterIcon) {
            return;
        }

        if (selectedTag) {
            supplierTagFilterIcon.className = 'bi bi-x-circle';
            supplierTagFilterIconButton.disabled = false;
            supplierTagFilterIconButton.removeAttribute('tabindex');
            supplierTagFilterIconButton.setAttribute('aria-disabled', 'false');
            supplierTagFilterIconButton.setAttribute('aria-label', 'Clear type filter');
            supplierTagFilterIconButton.setAttribute('title', 'Clear type filter');
            return;
        }

        supplierTagFilterIcon.className = 'bi bi-funnel';
        supplierTagFilterIconButton.disabled = true;
        supplierTagFilterIconButton.setAttribute('tabindex', '-1');
        supplierTagFilterIconButton.setAttribute('aria-disabled', 'true');
        supplierTagFilterIconButton.setAttribute('aria-label', 'Filter tasting experiences by type');
        supplierTagFilterIconButton.setAttribute('title', 'Filter tasting experiences by type');
    };

    const updateSupplierTagFilterFeedback = (selectedTag, visibleCount, totalCount, hiddenSelectedCount) => {
        if (!supplierTagFilterStatus || !supplierTagNoResults) {
            return;
        }

        if (!selectedTag) {
            supplierTagFilterStatus.classList.add('d-none');
            supplierTagFilterStatus.textContent = '';
            supplierTagNoResults.classList.add('d-none');
            return;
        }

        if (visibleCount === 0) {
            supplierTagFilterStatus.classList.add('d-none');
            supplierTagFilterStatus.textContent = '';
            supplierTagNoResults.classList.remove('d-none');
            return;
        }

        supplierTagNoResults.classList.add('d-none');
        supplierTagFilterStatus.classList.remove('d-none');

        const hiddenSelectedSummary = hiddenSelectedCount > 0
            ? ` ${hiddenSelectedCount} selected item${getPluralSuffix(hiddenSelectedCount)} hidden by the filter.`
            : '';
        supplierTagFilterStatus.textContent = `${visibleCount} of ${totalCount} tasting experience${getPluralSuffix(totalCount)} shown.${hiddenSelectedSummary}`;
    };

    const applySupplierTagFilter = (selectedTag) => {
        const normalisedSelectedTag = normaliseTag(selectedTag);
        let visibleCardCount = 0;
        let hiddenSelectedCardCount = 0;

        supplierCards.forEach(card => {
            const checkbox = card.querySelector('.supplier-checkbox');

            if (!normalisedSelectedTag) {
                card.classList.remove('tag-filter-hidden');
                card.removeAttribute('aria-hidden');
                return;
            }

            const supplierTags = (card.dataset.supplierTags || '')
                .split('|')
                .map(normaliseTag)
                .filter(Boolean);

            const shouldShowCard = supplierTags.includes(normalisedSelectedTag);
            card.classList.toggle('tag-filter-hidden', !shouldShowCard);

            if (shouldShowCard) {
                card.removeAttribute('aria-hidden');
                visibleCardCount++;
            } else {
                card.setAttribute('aria-hidden', 'true');
                if (checkbox?.checked) {
                    hiddenSelectedCardCount++;
                }
            }
        });

        if (!normalisedSelectedTag) {
            visibleCardCount = supplierCards.length;
            hiddenSelectedCardCount = 0;
        }

        updateSupplierTagFilterFeedback(normalisedSelectedTag, visibleCardCount, supplierCards.length, hiddenSelectedCardCount);
        updateSupplierTagFilterIcon(normalisedSelectedTag);
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
    let activeBookingLookupKey = null;
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

    const createBookingLookupKey = (email, purchaseCode) => `${email?.trim().toLowerCase() || ''}|${purchaseCode?.trim() || ''}`;

    const getCurrentBookingLookupKey = () => createBookingLookupKey(emailInput?.value, purchaseCodeInput?.value);

    const showBookingMessage = (alertClass, iconClass, message) => {
        if (bookingStatusMessage && bookingStatusIcon && bookingStatusText) {
            bookingStatusMessage.className = `alert ${alertClass} mb-4`;
            bookingStatusIcon.className = `bi ${iconClass}`;
            bookingStatusText.textContent = ` ${message}`;
            bookingStatusMessage.classList.remove('d-none');
            // Only move focus if the user is not currently typing in a booking input field,
            // to avoid interrupting keystrokes during debounced background validation.
            const activeEl = document.activeElement;
            if (activeEl !== emailInput && activeEl !== purchaseCodeInput) {
                bookingStatusMessage.focus();
            }
        }
    };

    const showBookingSuccess = (message, bookingId, email, purchaseCode) => {
        clearBookingMessages(false, false);
        activeBookingLookupKey = createBookingLookupKey(email, purchaseCode);
        validatedBookingId = bookingId;
        if (bookingIdInput) {
            bookingIdInput.value = bookingId;
        }
        // Unlock selection now the booking is validated
        unlockSelection();
        showBookingMessage('alert-success', 'bi-check-circle-fill', message);
    };

    const showBookingWarning = (message, email, purchaseCode) => {
        clearBookingMessages(true, false);
        activeBookingLookupKey = createBookingLookupKey(email, purchaseCode);
        showBookingMessage('alert-warning', 'bi-exclamation-triangle-fill', message);
    };

    const showPartnerConfirmationPrompt = (message, confirmationUrl, email, purchaseCode) => {
        clearBookingMessages(true, false);
        activeBookingLookupKey = createBookingLookupKey(email, purchaseCode);
        partnerConfirmationUrl = confirmationUrl;
        if (partnerConfirmationPrompt && partnerConfirmationMessage && partnerConfirmationLink) {
            partnerConfirmationMessage.innerHTML = `<i class="bi bi-info-circle-fill"></i> ${message}`;
            partnerConfirmationLink.href = confirmationUrl;
            partnerConfirmationPrompt.classList.remove('d-none');
        }
    };

    const clearBookingMessages = (resetOpeningTimes = true, resetLookupState = true) => {
        if (bookingStatusMessage) {
            bookingStatusMessage.classList.add('d-none');
        }
        if (partnerConfirmationPrompt) {
            partnerConfirmationPrompt.classList.add('d-none');
        }
        if (resetLookupState) {
            activeBookingLookupKey = null;
            validatedBookingId = null;
            partnerConfirmationUrl = null;
            if (bookingIdInput) {
                bookingIdInput.value = '';
            }
            // Lock selection whenever lookup state is reset
            lockSelection();
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
        const bookingLookupKey = createBookingLookupKey(email, purchaseCode);

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
            if (bookingLookupKey !== getCurrentBookingLookupKey()) {
                return;
            }

            if (data.requiresPartnerConfirmation) {
                const encodedReference = encodeURIComponent(purchaseCode);
                const encodedEmail = encodeURIComponent(email);
                const confirmationUrl = `/Home/PartnerBookingConfirmation?bookingReference=${encodedReference}&customerEmail=${encodedEmail}`;
                showPartnerConfirmationPrompt(data.message, confirmationUrl, email, purchaseCode);
            } else if (data.found) {
                if (data.isCancelled) {
                    showBookingWarning(data.message, email, purchaseCode);
                } else if (data.isConfirmed) {
                    if (typeof data.tastingCount === 'number') setMaxSelections(data.tastingCount);
                    applyTourDateOpeningTimes(data.tourDate);
                    // Treat confirmed bookings as validated for selection
                    if (typeof data.bookingId !== 'undefined' && data.bookingId !== null) {
                        validatedBookingId = data.bookingId;
                        if (bookingIdInput) bookingIdInput.value = data.bookingId;
                    }
                    unlockSelection();
                    showBookingConfirmedModal(email, purchaseCode, data.qrToken);
                } else {
                    showBookingSuccess(data.message, data.bookingId, email, purchaseCode);
                    if (typeof data.tastingCount === 'number') setMaxSelections(data.tastingCount);
                    applyTourDateOpeningTimes(data.tourDate);
                }
            } else {
                showBookingWarning(data.message, email, purchaseCode);
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

    const handleBookingFieldInput = (clearError) => {
        clearError();
        clearBookingMessages();
        debouncedBookingCheck();
    };

    const handleBookingFieldChange = (clearError) => {
        clearError();

        if (getCurrentBookingLookupKey() === activeBookingLookupKey) {
            return;
        }

        clearBookingMessages();
        debouncedBookingCheck();
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

    if (supplierTagFilter) {
        supplierTagFilter.addEventListener('change', () => {
            applySupplierTagFilter(supplierTagFilter.value);
        });
    }

    if (supplierTagFilterIconButton && supplierTagFilter) {
        supplierTagFilterIconButton.addEventListener('click', () => {
            if (supplierTagFilter.value) {
                supplierTagFilter.value = '';
                applySupplierTagFilter('');
            }
            supplierTagFilter.focus();
        });
    }

    // Initialise
    setupCheckboxes();
    setupSupplierCards();
    // Lock selection until booking is validated by default
    lockSelection();
    // Ensure heading and button state use the initial maxSelections
    setMaxSelections(maxSelections);

    // Check if email and purchase code are pre-populated (from qrToken navigation)
    if (emailInput?.value.trim() && purchaseCodeInput?.value.trim()) {
        checkBookingExists();
    }

    // Clear purchase code and email error messages as soon as the user types or changes the value
    if (purchaseCodeInput) {
        purchaseCodeInput.addEventListener('input', () => {
            handleBookingFieldInput(clearPurchaseCodeError);
        });
        purchaseCodeInput.addEventListener('change', () => {
            handleBookingFieldChange(clearPurchaseCodeError);
        });
    }

    if (emailInput) {
        emailInput.addEventListener('input', () => {
            handleBookingFieldInput(clearEmailError);
        });
        emailInput.addEventListener('change', () => {
            handleBookingFieldChange(clearEmailError);
        });
    }
});
