# SPEC.md — NofyMe

App desktop Windows para notificações personalizadas (pop-up na tela, notificações
nativas do Windows e balão da bandeja), com gatilhos de tempo/horário e opción de
iniciar junto com o Windows.

Feito em C# / WPF (.NET 8), rodando como aplicativo em `bin\Debug\...\NofyMe.exe`.
Estrutura aprovada ao longo do projeto: pop-up = janela própria; nativas = pacote
oficial `Microsoft.Toolkit.Uwp.Notifications`; balão = ícone da bandeja do app.

---

## 1. Comportamento principal (o que acontece quando o usuário usa)

### 1.1 Janela principal (960×600, sem borda padrão, cantos arredondados)
- Barra lateral com: marca "NofyMe" (sino + nome), navegação **Notificações /
  Histórico / Configurações** e, no rodapé: seletor de tema (4 bolinhas:
  Entardecer roxo, Café laranja, Floresta verde, Pêssego claro),
  interruptor **"Iniciar com o Windows"** e interruptor **"Notificar ao ligar"**.
- Área central com cabeçalho (título da página, "N ativas" apenas em Notificações,
  botão **+ Nova notificação** apenas na página Notificações).
- Fechar a janela **esconde** o app para a bandeja (o app continua disparando).
  Sai de verdade pelo menu do ícone na bandeja ("Sair").

### 1.2 Criar notificação (modal "Nova notificação", 560×640)
Entradas:
- **Título** (obrigatório; vazio → erro inline "Escreva um título...").
- **Mensagem** (opcional).
- **Exibir como** (um de): `Pop-up` (janela própria), `Nativa` (toast do Windows),
  `Balão` (balloon tip da bandeja).
- **Disparar quando** (1 a 3 combinados):
  - `Ao iniciar o Windows` — dispara quando o app é aberto, se a opção global
    "Notificar ao ligar" estiver ligada;
  - `Após determinado tempo` — repete a cada X minutos (aceito 1–1440; valores
    fora desse intervalo concentram-se nesses extremos);
  - `Em determinado horário` — hora no formato `HH:mm` + dias da semana (chips
    D S T Q Q S S; os 7 marcados = "Todos os dias").
- Se nenhum gatilho marcado → erro inline "Marque ao menos um 'Disparar quando'.".
- **Cancelar** fecha sem salvar; **Salvar** grava em JSON e recarrega a lista.

Saída: cartão novo na lista com título, modo ("Pop-up na tela" etc.), chip do
gatilho (ex.: "Seg–Sex, 09:00", "A cada 45 min", "Ao iniciar o Windows") e
interruptor ativo/inativo.

### 1.3 Disparo (agendador)
- Verificação a cada 10 segundos, enquanto o app estiver aberto.
- `Ao iniciar o Windows`: dispara 1× por sessão do app (apertado à opção global).
- `A cada X min`: intervalo **relativo à abertura do app** (recomeça ao abrir;
  desligar/ligar o interruptor do cartão reconta o intervalo).
- `Em determinado horário`: dispara quando `agora ≥ horário` **no dia da semana
  escolhido** e ainda não disparou **naquele dia** (há guardar registro do último
  dia disparado por item). Abrir o app depois do horário no dia certo dispara
  "atrasado" ("catch-up"): **decisão a confirmar com o dono do projeto** (ver §5).
- Cada disparo é registrado no **Histórico** (título, mensagem, modo, hora; máximo
  de 200 registros; mais antigos são descartados).

### 1.4 Formas de exibição
- **Pop-up**: janela sem borda (430×150) no canto inferior direito da área de
  trabalho, desliza da direita, empilha se houver mais de uma (passo vertical
  fixo), barra de progresso na base (6 s) e dois botões:
  **Adiar 10 min** (reagenda o disparo) e **Dispensar** (fecha). Fecha sozinha
  após 6 s.
- **Nativa**: toast do Windows via pacote oficial; se o sistema recusar (ex.:
  Modo Foco/política), cai automaticamente para o balão.
- **Balão**: balloon tip do ícone do NofyMe na bandeja.

### 1.5 Temas
4 paletas (cel: tokens exatos do design recebido): fundo, painéis, bordas, textos
(primário/secundário/apagado), acento, item ativado, campos e chips. Trocar em
qualquer momento re-pinta tudo em tempo real (nada precisa reabrir). O tema
escolhido fica salvo.

### 1.6 Iniciar com o Windows
- Interruptor grava/remove a linha "NofyMe" na chave **HKCU** do Registro
  (`Software\Microsoft\Windows\CurrentVersion\Run`), por usuário (não pede admin).
- Estado inicial do interruptor lido do Registro+JSON ao abrir as Configurações.

---

## 2. Entradas / Saídas resumidas

| Ação | Entrada | Saída |
|---|---|---|
| Salvar notificação | título, mensagem, modo, 1–3 gatilhos | JSON em `%AppData%\NofyMe\notifications.json` + cartão na lista |
| Disparo | relógio do sistema | pop-up / toast / balão + registro no `history.json` |
| Tema | clique na bolinha | interface repintada + `settings.json` |
| Iniciar com o Windows | interruptor | chave `HKCU\...\Run` + `settings.json` |

Arquivos de dados (criados sob demanda): `notifications.json`, `settings.json`,
`history.json`, todos em `%AppData%\NofyMe`.

## 3. Casos de borda (testados em NofyMe.Tests — 22 testes)
- Horário já passou hoje e ainda não disparou → dispara na próxima checagem (1×).
- Mesmo dia, já disparou → **não** disparar de novo.
- Dia da semana não marcado → não dispara.
- Dias vazio (= todos os dias) → dispara em qualquer dia.
- Intervalo 0/negativo → comporta-se como 1 min (concentrado em 1–1440).
- Chip do gatilho monta textos: "Seg–Sex, 09:00", "Fim de semana, 10:00",
  "Ter, 22:00", "Todos os dias, 07:30" e combinação de múltiplos gatilhos.
- Arquivo JSON corrompido ou ausente → app abre vazio, sem crash.
- Rascunho de notificação sem título ou sem gatilho → erro inline, não salva.

Evidências dos testes: `dotnet test` → "Aprovado! — Com falha: 0, Aprovado: 22".

## 4. Evidências de execução (VERIFICADO)
- Build: `dotnet build` → "Compilação com êxito. 0 Erro(s)".
- App abre: processo NofyMe.exe vivo após 5 s (PID conferido).
- Tela principal e modal "Nova notificação" foram fotografados automaticamente
  e conferidos contra o design de referência (pasta `Interface\`).
- Pop-up disparou sozinha no 2.º boot do app apóes salvar item com gatilho
  "Ao iniciar o Windows" (janela 430×150 no canto inferior direito).

## 5. DESCONHECIDO / decisões ainda em aberto
1. **Catch-up do horário**: disparar nota "atrasada" ao abrir o app (comportamento
   atual) ou ignorar e esperar o próximo dia? → decidir com o dono.
2. **Editar/excluir notificações**: ainda não existe (só criar e ligar/desligar).
3. **Ícone do app (.exe) e atalho no menu Iniciar**: default genérico; ícone da
   bandeja é desenhado em runtime.
4. **Toast "auto-dismiss" e o som nativo** dependem do Windows do usuário
   (Modo Foco pode esconder toasts nativas — por isso o app tem o pop-up próprio).
5. Publicar instalador único (ex.: `dotnet publish` + empacotamento) — não feito.

---

*Spec gerada a partir do compassado acordado no chat e do design aprovado
(`Interface\Janela principal.png`, `Nova notificação.png`, `Notificações.png`,
`Temas.png`, HTML de referência). Excluir/editar notificações e catch-up são as
próximas conversas.*
