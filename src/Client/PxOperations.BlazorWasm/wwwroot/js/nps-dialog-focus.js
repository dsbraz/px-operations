// Devolver o foco ao gatilho é a única parte da acessibilidade do diálogo que
// o Blazor não alcança sozinho: quando o diálogo fecha, o elemento que o abriu
// já saiu da árvore de render que o C# enxerga. Guardamos a referência aqui, no
// momento em que o diálogo abre, e restauramos no fechamento.
//
// PILHA, não uma variável só. Um diálogo pode abrir outro — o detalhe da coleta
// tem a ação "Gerar link" —, e nesse caso o fechar de um e o abrir do outro
// caem no MESMO lote de render: com um slot único, o capture do novo e o
// restore do antigo se atropelam e o foco acaba no body, que é exatamente o que
// este arquivo existe para evitar.
const triggers = [];

export function capture() {
    triggers.push(document.activeElement);
}

export function restore() {
    // Desce a pilha até achar um gatilho que ainda exista. Um diálogo aberto de
    // dentro de outro fecha os dois: o gatilho do topo some junto, e quem abriu
    // a sequência é quem deve receber o foco de volta.
    while (triggers.length > 0) {
        const trigger = triggers.pop();

        if (trigger && document.contains(trigger)) {
            trigger.focus();
            return;
        }
    }
}
