document.addEventListener('DOMContentLoaded', () => {
    // Constants
    const MAX_SELECTIONS = parseInt(document.querySelector('[data-max-selections]')?.dataset.maxSelections || '0');
    const DATE_FORMAT = 'Y-m-d';
    const ALT_DATE_FORMAT = 'd/m/Y';
    const TOUR_MONTH_START = 3; // March
    const TOUR_MONTH_END = 10; // October
    const SUNDAY = 0;
    const ERROR_TIMEOUT_MS = 5000;

    // Cached selectors
    const checkboxes = document.querySelectorAll('.supplier-checkbox');
    const supplierCards = document.querySelectorAll('.supplier-card');
    const createTourBtn = document.getElementById('createTourBtn');
    const tourDateInput = document.getElementById('tourDate');

    let messageTimeout;
    let currentMessage = null;

    // Utility functions
    const getTomorrow = () => {
        const now = new Date();
        return new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1);
    };

    const getTourDateRange = () => {
        const now = new Date();
        const currentYear = now.getFullYear();
        const marchFirst = new Date(currentYear, TOUR_MONTH_START - 1, 1);
        const octoberLast = new Date(currentYear, TOUR_MONTH_END - 1, 31);
        let minDate = getTomorrow() > marchFirst ? getTomorrow() : marchFirst;
        let maxDate = octoberLast;
        if (now > octoberLast) {
            const nextYear = currentYear + 1;
            minDate = new Date(nextYear, TOUR_MONTH_START - 1, 1);
            maxDate = new Date(nextYear, TOUR_MONTH_END - 1, 31);
        }
        return { minDate, maxDate };
    };

    const parseDate = (dateString) => {
        if (!dateString) return null;
        if (dateString.includes('/')) {
            const [day, month, year] = dateString.split('/').map(Number);
            return new Date(year, month - 1, day);
        }
        return new Date(dateString + 'T00:00:00');
    };

    const isFutureDate = (date) => {
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        return date > today;
    };

    const isValidTourDate = (dateString) => {
        const date = parseDate(dateString);
        if (!date || !isFutureDate(date)) return false;
        const month = date.getMonth() + 1;
        const dayOfWeek = date.getDay();
        return dayOfWeek !== SUNDAY && month >= TOUR_MONTH_START && month <= TOUR_MONTH_END;
    };

    const showDateError = (message) => {
        clearDateError();
        if (tourDateInput) {
            // Find the .form-group containing the date input
            const formGroup = tourDateInput.closest('.form-group');
            if (formGroup) {
                const errorElement = document.createElement('div');
                errorElement.className = 'text-danger small mt-1 date-validation-error';
                errorElement.textContent = message;
                errorElement.setAttribute('tabindex', '-1');
                formGroup.parentNode.insertBefore(errorElement, formGroup.nextSibling);
                errorElement.focus();
            }
        }
    };

    const clearDateError = () => {
        if (tourDateInput) {
            const formGroup = tourDateInput.closest('.form-group');
            if (formGroup) {
                // Remove any error message directly after the form-group
                const nextSibling = formGroup.nextSibling;
                if (nextSibling && nextSibling.classList && nextSibling.classList.contains('date-validation-error')) {
                    nextSibling.remove();
                }
            }
        }
    };

    const updateButtonState = () => {
        const checkedCount = document.querySelectorAll('.supplier-checkbox:checked').length;
        if (createTourBtn) {
            createTourBtn.disabled = checkedCount !== MAX_SELECTIONS;
        }
    };

    const validateTourDate = () => {
        if (!tourDateInput || !tourDateInput.value) {
            clearDateError();
            updateButtonState();
            return;
        }
        const date = parseDate(tourDateInput.value);
        if (!date || !isFutureDate(date)) {
            showDateError('Please select a date in the future.');
        } else if (date.getDay() === SUNDAY) {
            showDateError('Tours are not available on Sundays. Please select a different date.');
        } else if (date.getMonth() + 1 < TOUR_MONTH_START || date.getMonth() + 1 > TOUR_MONTH_END) {
            showDateError('Tours are only available from March to October. Please select a date within this period.');
        } else {
            clearDateError();
        }
        updateButtonState();
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

    // Event listeners
    const setupDateInput = () => {
        const { minDate, maxDate } = getTourDateRange();
        if (tourDateInput && typeof flatpickr !== 'undefined') {
            flatpickr(tourDateInput, {
                minDate,
                maxDate,
                locale: { firstDayOfWeek: 1 },
                dateFormat: DATE_FORMAT,
                altInput: true,
                altFormat: ALT_DATE_FORMAT,
                disable: [date => date.getDay() === SUNDAY],
                onChange: validateTourDate,
                onReady: () => { if (tourDateInput.value) validateTourDate(); }
            });
        } else if (tourDateInput) {
            tourDateInput.min = minDate.toISOString().split('T')[0];
            tourDateInput.max = maxDate.toISOString().split('T')[0];
            tourDateInput.addEventListener('change', validateTourDate);
            tourDateInput.addEventListener('blur', validateTourDate);
        }
    };

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
    const emailInput = document.querySelector('input[type="email"][name$="CustomerEmail"]');

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
    const purchaseCodeInput = document.querySelector('input[name$="PurchaseCode"]');

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

    // Add form validation on submit
    const tourForm = document.querySelector('form[data-max-selections]');
    if (tourForm) {
        tourForm.addEventListener('submit', function (event) {
            let hasError = false;
            // Validate tour date before submitting
            if (!tourDateInput || !tourDateInput.value || !isValidTourDate(tourDateInput.value)) {
                event.preventDefault();
                showDateError('Please select a valid tour date before creating your tour.');
                tourDateInput.focus();
                hasError = true;
            } else {
                clearDateError();
            }
            // Validate email before submitting
            if (!emailInput || !emailInput.value.trim() || !isValidEmail(emailInput.value.trim())) {
                event.preventDefault();
                showEmailError('Please enter a valid email address.');
                hasError = true;
            } else {
                clearEmailError();
            }
            // Validate purchase code before submitting
            if (!purchaseCodeInput || !purchaseCodeInput.value.trim()) {
                event.preventDefault();
                showPurchaseCodeError('Please enter your purchase code to continue.');
                hasError = true;
            } else {
                clearPurchaseCodeError();
            }
            // Prevent form submission if any validation failed
            if (hasError) {
                event.preventDefault();
            }
        });
    }

    // Initialise
    setupDateInput();
    setupCheckboxes();
    setupSupplierCards();
    updateButtonState();

    // Clear purchase code and email error messages as soon as the user types or changes the value
    if (purchaseCodeInput) {
        purchaseCodeInput.addEventListener('input', clearPurchaseCodeError);
        purchaseCodeInput.addEventListener('change', clearPurchaseCodeError);
    }
    if (emailInput) {
        emailInput.addEventListener('input', clearEmailError);
        emailInput.addEventListener('change', clearEmailError);
    }
});
