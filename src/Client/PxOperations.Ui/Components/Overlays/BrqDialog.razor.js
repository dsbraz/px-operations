const states = new WeakMap();

export function sync(dialog, open, dotNetReference, autofocusSelector) {
  let state = states.get(dialog);

  if (!state) {
    state = {
      dotNetReference,
      previousFocus: null,
      suppressClose: false,
      onClose: null,
      onCancel: null
    };

    state.onCancel = (event) => {
      event.preventDefault();
      if (dialog.open) {
        void state.dotNetReference.invokeMethodAsync("NotifyNativeCloseAsync");
      }
    };

    state.onClose = () => {
      restoreFocus(state);
      if (!state.suppressClose) {
        void state.dotNetReference.invokeMethodAsync("NotifyNativeCloseAsync");
      }
      state.suppressClose = false;
    };

    dialog.addEventListener("cancel", state.onCancel);
    dialog.addEventListener("close", state.onClose);
    states.set(dialog, state);
  } else {
    state.dotNetReference = dotNetReference;
  }

  if (open && !dialog.open) {
    state.previousFocus = document.activeElement;
    dialog.showModal();

    queueMicrotask(() => {
      const requested = autofocusSelector
        ? dialog.querySelector(autofocusSelector)
        : null;
      const fallback = dialog.querySelector(
        "button:not([disabled]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex='-1'])"
      );
      (requested ?? fallback)?.focus();
    });
  } else if (!open && dialog.open) {
    state.suppressClose = true;
    dialog.close();
  }
}

export function dispose(dialog) {
  const state = states.get(dialog);
  if (!state) {
    return;
  }

  dialog.removeEventListener("cancel", state.onCancel);
  dialog.removeEventListener("close", state.onClose);
  restoreFocus(state);
  states.delete(dialog);
}

function restoreFocus(state) {
  if (state.previousFocus instanceof HTMLElement && state.previousFocus.isConnected) {
    state.previousFocus.focus();
  }
  state.previousFocus = null;
}
