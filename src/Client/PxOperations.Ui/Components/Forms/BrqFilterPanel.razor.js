const listeners = new WeakMap();

export function attach(root, dotNetReference) {
  // Reanexar sem soltar o listener anterior deixaria um órfão em document,
  // invocado para sempre com uma referência .NET já descartada.
  detach(root);

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
