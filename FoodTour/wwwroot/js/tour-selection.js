document.addEventListener('DOMContentLoaded', function () {
    const maxSelections = parseInt(document.querySelector('[data-max-selections]')?.dataset.maxSelections || '0');
    const checkboxes = document.querySelectorAll('.supplier-checkbox');
    const supplierCards = document.querySelectorAll('.supplier-card');
    const createTourBtn = document.getElementById('createTourBtn');
    const tourDateInput = document.getElementById('tourDate');
    let messageTimeout;
    let currentMessage = null;

    // Calculate tomorrow's date
    var now = new Date();
    var tomorrow = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1);

    if (tourDateInput && typeof flatpickr !== 'undefined') {
        const currentYear = now.getFullYear();
        const marchFirst = new Date(currentYear, 2, 1);
        const octoberLast = new Date(currentYear, 9, 31);
        
        let minDate = tomorrow > marchFirst ? tomorrow : marchFirst;
        let maxDate = octoberLast;
        
        if (now > octoberLast) {
            const nextYear = currentYear + 1;
            minDate = new Date(nextYear, 2, 1);
            maxDate = new Date(nextYear, 9, 31);
        }
        
        flatpickr(tourDateInput, {
            minDate: minDate,
            maxDate: maxDate,
            dateFormat: 'Y-m-d',
            altInput: true,
            altFormat: 'd/m/Y',
            disable: [
                function(date) {
                    return date.getDay() === 0;
                }
            ],
            onChange: function(selectedDates, dateStr, instance) {
                validateTourDate();
            },
            onReady: function(selectedDates, dateStr, instance) {
                if (tourDateInput.value) {
                    validateTourDate();
                }
            }
        });
    } else if (tourDateInput) {
        const currentYear = now.getFullYear();
        const marchFirst = new Date(currentYear, 2, 1);
        const octoberLast = new Date(currentYear, 9, 31);
        
        let minDate = tomorrow > marchFirst ? tomorrow : marchFirst;
        let maxDate = octoberLast;
        if (now > octoberLast) {
            const nextYear = currentYear + 1;
            minDate = new Date(nextYear, 2, 1);
            maxDate = new Date(nextYear, 9, 31);
        }
        tourDateInput.min = minDate.toISOString().split('T')[0];
        tourDateInput.max = maxDate.toISOString().split('T')[0];
        tourDateInput.addEventListener('change', validateTourDate);
        tourDateInput.addEventListener('blur', validateTourDate);
    }

    function isValidTourDate(dateString) {
        if (!dateString) return false;
        
        let date;
        if (dateString.includes('/')) {
            const parts = dateString.split('/');
            date = new Date(parts[2], parts[1] - 1, parts[0]);
        } else {
            date = new Date(dateString + 'T00:00:00');
        }
        
        // Only allow future dates
        var today = new Date();
        today.setHours(0,0,0,0);
        if (date <= today) {
            return false;
        }
        
        const month = date.getMonth() + 1;
        const dayOfWeek = date.getDay();
        
        if (dayOfWeek === 0) {
            return false;
        }
        
        if (month < 3 || month > 10) {
            return false;
        }
        
        return true;
    }

    function updateButtonState() {
        const checkedCount = document.querySelectorAll('.supplier-checkbox:checked').length;
        const hasValidDate = tourDateInput && isValidTourDate(tourDateInput.value);
        
        if (createTourBtn) {
            createTourBtn.disabled = checkedCount !== maxSelections || !hasValidDate;
        }
    }

    function showDateError(message) {
        const existingError = tourDateInput?.parentElement.querySelector('.date-validation-error');
        if (existingError) {
            existingError.remove();
        }
        
        if (tourDateInput) {
            const errorElement = document.createElement('div');
            errorElement.className = 'text-danger small mt-1 date-validation-error';
            errorElement.textContent = message;
            tourDateInput.parentElement.appendChild(errorElement);
        }
    }

    function clearDateError() {
        const existingError = tourDateInput?.parentElement.querySelector('.date-validation-error');
        if (existingError) {
            existingError.remove();
        }
    }

    function validateTourDate() {
        if (!tourDateInput) return;
        
        if (!tourDateInput.value) {
            clearDateError();
            updateButtonState();
            return;
        }
        
        let date;
        const dateString = tourDateInput.value;
        if (dateString.includes('/')) {
            const parts = dateString.split('/');
            date = new Date(parts[2], parts[1] - 1, parts[0]);
        } else {
            date = new Date(dateString + 'T00:00:00');
        }
        var today = new Date();
        today.setHours(0,0,0,0);
        if (date <= today) {
            showDateError('Please select a date in the future.');
            updateButtonState();
            return;
        }
        const month = date.getMonth() + 1;
        const dayOfWeek = date.getDay();
        if (dayOfWeek === 0) {
            showDateError('Tours are not available on Sundays. Please select a different date.');
            updateButtonState();
            return;
        }
        if (month < 3 || month > 10) {
            showDateError('Tours are only available from March to October. Please select a date within this period.');
            updateButtonState();
            return;
        }
        clearDateError();
        updateButtonState();
    }

    function updateCardVisualState(card, checkbox) {
        if (checkbox.checked) {
            card.classList.add('selected');
        } else {
            card.classList.remove('selected');
        }
    }

    function handleCheckboxChange(checkbox) {
        const checkedCount = document.querySelectorAll('.supplier-checkbox:checked').length;
        const card = checkbox.closest('.supplier-card');

        if (currentMessage) {
            currentMessage.remove();
            currentMessage = null;
        }

        if (checkedCount > maxSelections) {
            checkbox.checked = false;

            const messageElement = document.createElement('div');
            messageElement.className = 'alert alert-warning mt-2 mb-3';

            const strongElement = document.createElement('strong');
            strongElement.textContent = 'Maximum selections reached!';
            messageElement.appendChild(strongElement);

            const lineBreakElement = document.createElement('br');
            messageElement.appendChild(lineBreakElement);

            const infoText = document.createTextNode(`You must select exactly ${maxSelections} tasting experiences for your tour.`);
            messageElement.appendChild(infoText);
            card.parentNode.insertBefore(messageElement, card.nextSibling);
            currentMessage = messageElement;

            clearTimeout(messageTimeout);
            messageTimeout = setTimeout(function () {
                if (messageElement && messageElement.parentNode) {
                    messageElement.remove();
                    currentMessage = null;
                }
            }, 5000);
        }

        updateCardVisualState(card, checkbox);
        updateButtonState();
    }

    checkboxes.forEach(function (checkbox) {
        checkbox.addEventListener('change', function () {
            handleCheckboxChange(this);
        });
    });

    supplierCards.forEach(function (card) {
        card.addEventListener('click', function (event) {
            if (event.target.closest('.supplier-checkbox')) {
                return;
            }
            const checkbox = this.querySelector('.supplier-checkbox');
            checkbox.checked = !checkbox.checked;
            handleCheckboxChange(checkbox);
        });
    });
    
    updateButtonState();
});
