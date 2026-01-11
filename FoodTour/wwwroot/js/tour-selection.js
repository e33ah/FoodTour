document.addEventListener('DOMContentLoaded', function () {
    const maxSelections = parseInt(document.querySelector('[data-max-selections]')?.dataset.maxSelections || '0');
    const checkboxes = document.querySelectorAll('.supplier-checkbox');
    const supplierCards = document.querySelectorAll('.supplier-card');
    const createTourBtn = document.getElementById('createTourBtn');
    let messageTimeout;
    let currentMessage = null;

    function updateButtonState() {
        const checkedCount = document.querySelectorAll('.supplier-checkbox:checked').length;
        if (createTourBtn) {
            createTourBtn.disabled = checkedCount !== maxSelections;
        }
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
});
