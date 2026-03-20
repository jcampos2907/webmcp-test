/**
 * Form Editor drag-and-drop module.
 * Supports multiple block containers. Fields can only be reordered within their block.
 * Calls back into Blazor via DotNetObjectReference with block key + ordered IDs.
 */
window.FormEditorDnD = {
    _dotNetRef: null,
    _containers: [],
    _dragEl: null,
    _sourceContainer: null,
    _placeholder: null,

    init: function (containerSelector, dotNetRef) {
        this.dispose();
        this._dotNetRef = dotNetRef;
        this._containers = document.querySelectorAll(containerSelector);
        if (!this._containers.length) return;

        this._placeholder = document.createElement('div');
        this._placeholder.className = 'border-2 border-dashed border-indigo-300 rounded-lg h-12 my-1 transition-all';

        var self = this;
        this._containers.forEach(function (container) {
            if (container.dataset.fixed === 'true') return;
            self._attachListeners(container);
        });
    },

    _attachListeners: function (container) {
        var self = this;

        container.addEventListener('dragstart', function (e) {
            var card = e.target.closest('[data-field-id]');
            if (!card) return;
            self._dragEl = card;
            self._sourceContainer = container;
            card.classList.add('opacity-50');
            e.dataTransfer.effectAllowed = 'move';
            e.dataTransfer.setData('text/plain', card.dataset.fieldId);
        });

        container.addEventListener('dragend', function () {
            if (self._dragEl) {
                self._dragEl.classList.remove('opacity-50');
                self._dragEl = null;
            }
            self._sourceContainer = null;
            if (self._placeholder.parentNode) {
                self._placeholder.parentNode.removeChild(self._placeholder);
            }
        });

        container.addEventListener('dragover', function (e) {
            // Only allow drops within the same container
            if (self._sourceContainer !== container) return;
            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';

            var afterEl = self._getDragAfterElement(container, e.clientY);
            if (afterEl) {
                container.insertBefore(self._placeholder, afterEl);
            } else {
                container.appendChild(self._placeholder);
            }
        });

        container.addEventListener('drop', function (e) {
            if (self._sourceContainer !== container) return;
            e.preventDefault();
            if (self._placeholder.parentNode) {
                self._placeholder.parentNode.removeChild(self._placeholder);
            }
            if (!self._dragEl) return;

            var afterEl = self._getDragAfterElement(container, e.clientY);
            if (afterEl) {
                container.insertBefore(self._dragEl, afterEl);
            } else {
                container.appendChild(self._dragEl);
            }

            // Collect new order for this block
            var blockKey = container.dataset.block || '';
            var ids = [];
            var cards = container.querySelectorAll('[data-field-id]');
            for (var i = 0; i < cards.length; i++) {
                ids.push(cards[i].dataset.fieldId);
            }

            if (self._dotNetRef) {
                self._dotNetRef.invokeMethodAsync('OnFieldReordered', blockKey, ids);
            }
        });
    },

    _getDragAfterElement: function (container, y) {
        var cards = container.querySelectorAll('[data-field-id]:not(.opacity-50)');
        var closest = null;
        var closestOffset = Number.NEGATIVE_INFINITY;

        for (var i = 0; i < cards.length; i++) {
            var box = cards[i].getBoundingClientRect();
            var offset = y - box.top - box.height / 2;
            if (offset < 0 && offset > closestOffset) {
                closestOffset = offset;
                closest = cards[i];
            }
        }
        return closest;
    },

    dispose: function () {
        this._dotNetRef = null;
        this._containers = [];
        this._dragEl = null;
        this._sourceContainer = null;
    }
};
