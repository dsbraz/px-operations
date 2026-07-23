# Redesign do módulo de NPS — Operations PX (BRQ)

Pacote autossuficiente para revisão. **Abra `index.html` no navegador** para começar.

| Arquivo | O que é |
|---|---|
| `index.html` | Capa: o que muda, como revisar e o que a decisão destrava. Ponto de entrada. |
| `painel.html` | Painel navegável (Coleta · Resultados · Respostas). |
| `formulario.html` | Formulário público do respondente. |
| `PRD.md` | Decisões (D), requisitos (F) e fases de entrega. |
| `FLUXOS.md` | Mapeamento técnico: capacidades (B), arquivos e migrações. |
| `assets/` | Design system BRQ (CSS, JS, fontes) usado pelos mockups. |

Tudo abre **offline**, sem servidor nem instalação. Os mockups carregam a pasta
`assets/` que acompanha o pacote; os dados exibidos vêm de um seed determinístico
de desenvolvimento (os números são os que a API calcula sobre esse seed).
