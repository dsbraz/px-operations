const listeners = new WeakMap();

export function attach(root, dotNetReference) {
  const listener = (event) => {
    if (!root.contains(event.target)) {
      void dotNetReference.invokeMethodAsync("CloseFromOutsideAsync");
    }
  };

  document.addEventListener("pointerdown", listener, true);
  listeners.set(root, listener);
}

export function detach(root) {
  const listener = listeners.get(root);
  if (!listener) return;
  document.removeEventListener("pointerdown", listener, true);
  listeners.delete(root);
}
