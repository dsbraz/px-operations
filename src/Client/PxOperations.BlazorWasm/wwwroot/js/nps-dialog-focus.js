// Devolver o foco ao gatilho é a única parte da acessibilidade do diálogo que
// o Blazor não alcança sozinho: quando o diálogo fecha, o elemento que o abriu
// já saiu da árvore de render que o C# enxerga. Guardamos a referência aqui, no
// momento em que o diálogo abre, e restauramos no fechamento.
let trigger = null;

export function capture() {
    trigger = document.activeElement;
}

export function restore() {
    // Pode ter saído do DOM junto com o card que o continha — nesse caso não há
    // para onde voltar, e forçar o foco no body seria pior do que não fazer nada.
    if (trigger && document.contains(trigger)) {
        trigger.focus();
    }

    trigger = null;
}
