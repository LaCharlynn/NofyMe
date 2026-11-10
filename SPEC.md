# SPEC.md — NofyMe

**Versão:** 1.1.3 · **Atualizado em:** 10/11/2026

App desktop Windows para notificações personalizadas (pop-up na tela, notificações
nativas do Windows e balão compacto), com gatilhos de tempo/horário e opção de
iniciar junto com o Windows.

Feito em C# / WPF (.NET 8), alvo `net8.0-windows10.0.19041.0`, mínimo Windows 10
1809 (build 17763). Distribuído como instalador por usuário ou versão portátil
autocontida (x64, sem exigir .NET instalado).

---

## 1. Comportamento principal (o que acontece quando o usuário usa)

### 1.1 Janela principal (960×600, redimensionável, sem moldura)

- Tamanho inicial **960×600**; mínimo **840×560**; máximo limitado à área de
  trabalho da tela.
- Redimensionável pelas **8 alças invisíveis** (laterais, topo/base e cantos).
  `ResizeMode="NoResize"`: as alças são `Thumb` transparentes + `WindowResizer`,
  então não existe a borda branca padrão do Windows em volta da janela.
- Sem barra de título do sistema: cantos arredondados (raio 18, recorte 17) e
  fundo transparente. A faixa superior e a barra lateral arrastam a janela.
- Barra lateral (230 px) com:
  - marca **NofyMe** (sino + nome);
  - navegação **Notificações / Histórico / Configurações**;
  - no rodapé: seletor de tema (4 bolinhas), interruptor **"Iniciar com o
    Windows"** e interruptor **"Notificar ao ligar"**.
- Área central com cabeçalho: título da página, "N ativas" (só em Notificações)
  e botão **+ Nova notificação** (só em Notificações).
- Fechar a janela **esconde** o app para a bandeja; ele continua disparando.
  Sair de verdade é pelo menu do ícone: **Abrir NofyMe / Sair**.
- **Ao iniciar com o Windows**, o app abre **oculto** (só a bandeja) e dispara
  os lembretes "Ao iniciar o Windows" normalmente. O argumento `--startup` é
  gravado na entrada do Registro; cliques normais no `.exe` mostram a janela.
- Instância única: abrir o `.exe` de novo traz a janela existente para a frente.

### 1.2 Criar / editar notificação (modal 560×780, sem rolagem)

Abre centralizado no dono. Título "Nova notificação" ou "Editar notificação".
Medido: conteúdo **671 px**, área visível **677 px**, sobra 7 px — **sem barra de
rolagem** e com Salvar/Cancelar sempre visíveis.

Entradas:

- **Título** — obrigatório; vazio → erro inline "Escreva um título para a
  notificação.".
- **Mensagem** — opcional.
- **Exibir como** (um de): `Pop-up` (janela própria), `Nativa` (toast do Windows)
  ou `Balão` (bolha compacta).
- **Na tela** — cartão exibido apenas para Pop-up e Balão:
  - **Tempo do pop-up** (só Pop-up): 1–60 s; **em branco usa o padrão de
    Configurações** (6 s). Fora do intervalo → erro "Use um tempo de 1 a 60 s na
    tela.".
  - **Canto na tela**: 4 setas (↖ ↗ ↙ ↘), sem botão "Padrão". O padrão é
    **inferior esquerdo** (↙ já marcado).
- **Disparar quando** (1 a 3 combinados):
  - `Ao iniciar o Windows` — dispara na abertura do app, se **"Notificar ao
    ligar"** estiver ligado;
  - `Após determinado tempo` — repete a cada X **min** (1–1440) ou X **seg**
    (1–86400); a unidade escolhida é preservada no chip ("120 s" não vira
    "2 min");
  - `Em determinado horário` — `HH:mm` (00:00–23:59) + chips dos dias
    (D S T Q Q S S); os 7 marcados equivalem a "Todos os dias".
- Nenhum gatilho marcado → erro inline "Marque quando a notificação deve
  aparecer.".
- **Cancelar** fecha sem salvar; **Salvar** valida, grava em JSON e recarrega a
  lista.

Saída: cartão na lista com título, modo ("Pop-up na tela" etc.), chip do gatilho
(ex.: "Seg–Sex, 09:00", "A cada 45 min", "Ao iniciar o Windows") e interruptor
ativo/inativo.

Na lista:

- **clique no cartão** → abre o mesmo modal em modo edição;
- **lixeira** → exclui com aviso "Notificação excluída." e botão **Desfazer**
  por 6 s; o clique não abre a edição;
- **interruptor** → liga/desliga sem abrir a edição; religar reconta o intervalo
  e, se desligado, limpa o "já disparou hoje" do horário fixo.

### 1.3 Disparo (agendador)

- Verificação a cada **10 s**; cai para **1 s** enquanto existir algum lembrete
  ativo com gatilho "a cada X" em menos de 60 s.
- `Ao iniciar o Windows`: dispara **1× por sessão** do app, respeitando a opção
  global "Notificar ao ligar".
- `A cada X`: o primeiro disparo conta a partir da abertura do app; depois, do
  disparo anterior. Ligar/desligar o interruptor do cartão reinicia a contagem.
- `Em determinado horário`: dispara quando `agora ≥ horário` **no dia da semana
  escolhido** e ainda não disparou naquele dia (`LastAtTimeDate` por item).
  Abrir o app depois do horário, no dia certo, dispara "atrasado" — decisão
  ainda em aberto (§5).
- Cada disparo entra no **Histórico** (título, mensagem, modo e hora; mais
  recentes primeiro; máximo **200** registros).

### 1.4 Formas de exibição

- **Pop-up**: janela própria **404×182** (cartão de 380 px de largura + margem da sombra),
  sem borda, `Topmost`, sem roubar o foco. Entra deslizando da lateral do canto
  escolhido. Empilha com outros pop-ups (passo vertical **194 px**, vão **24
  px**) a partir do canto. Tem barra de progresso na base, cuja duração é o tempo
  do lembrete, e um único botão:
  - **Dispensar** — fecha. Não existe mais "Adiar 10 min" (removido na 1.1.1).

  Some sozinho ao fim do tempo (1–60 s; padrão 6 s).
- **Nativa**: toast do Windows via `Microsoft.Toolkit.Uwp.Notifications`. Se a
  chamada falhar (ex.: AUMID ausente), cai automaticamente para o balão compacto.
  Modo Foco/política podem apenas ocultar o toast, sem erro.
- **Balão compacto**: janela própria, sem foco, com **uma bolha sólida na cor de
  destaque** do tema e só a mensagem (sem mensagem, usa o título). Três cantos
  arredondados e o canto de origem saliente com **cauda de 4 px**, apontando
  para o canto da tela. Largura máxima 420 px. Fecha sozinho em **5 s** ou com
  um clique. Só existe um por vez: um novo fecha o anterior.

### 1.5 Configurações

- **Iniciar com o Windows** — grava/remove a linha "NofyMe" em
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (por usuário, sem admin),
  com o argumento `--startup` para abrir em segundo plano. Entradas antigas sem
  o argumento são migradas na instalação/ao abrir o app.
- **Notificar ao ligar** — permite que gatilhos "Ao iniciar o Windows" disparem.
- **Tempo na tela** — padrão do pop-up, 1–60 s (padrão **6 s**); cada lembrete
  pode sobrescrever.
- **Temas** — 4 cartões com prévia; trocar re-pinta tudo na hora e salva.
- Não existem mais cartões de "Posição do pop-up" nem "Posição do balão".

### 1.6 Temas

Paletas do design (`Interface\`), com todos os tokens: fundo, painéis, bordas,
textos (primário/secundário/apagado), acento, acento suave, item ativado, campos
e chips.

| Tema | Painel | Acento | Texto do balão |
|---|---|---|---|
| Entardecer | `#25232C` | `#7C5CF0` | `#FFFFFF` |
| Café | `#2E241E` | `#D98E5B` | `#2B1A10` |
| Floresta | `#212A24` | `#8DB596` | `#12201A` |
| Pêssego | `#FFF9F3` | `#B4472F` | `#FFFFFF` |

O acento do Pêssego foi escurecido de `#C85A41` para `#B4472F` para o texto
branco do balão/botões ficar em **5,41:1** (antes 4,20:1). O sino do pop-up usa
`AccentSoft` (6,04 / 6,50 / 7,50 / 3,36:1 sobre o item ativado).

---

## 2. Entradas / Saídas resumidas

| Ação | Entrada | Saída |
|---|---|---|
| Salvar/editar notificação | título, mensagem, modo, tempo/canto, 1–3 gatilhos | JSON em `%AppData%\NofyMe\notifications.json` + cartão na lista |
| Excluir | lixeira do cartão | item removido + "Desfazer" por 6 s |
| Disparo | relógio do sistema | pop-up / toast / balão + registro em `history.json` |
| Tema | clique no cartão/bolinha | interface repintada + `settings.json` |
| Iniciar com o Windows | interruptor | chave `HKCU\...\Run` + `settings.json` |
| Redimensionar | alças invisíveis | nova geometria salva só em memória (não persiste entre sessões) |

Arquivos de dados (criados sob demanda) em `%AppData%\NofyMe`:
`notifications.json`, `settings.json`, `history.json`.

- Gravação atômica (`.tmp` → `FlushToDisk` → substituição), UTF-8 sem BOM.
- JSON corrompido/ausente → app abre vazio, sem crash.
- `IntervalMinutes` antigo é lido e convertido para segundos; nunca é regravado.
- Chaves antigas `BalloonPosition`/`PopupPosition` em `settings.json` são
  ignoradas.

---

## 3. Casos de borda e testes (36 casos)

`NofyMe.Tests` — **36/36 aprovados**:

- **Agendador e chips (21 casos; 16 métodos, 2 deles `Theory`)**: horário já
  passou hoje dispara 1×; não
  repete no mesmo dia; dia da semana não marcado não dispara; dias vazio = todos;
  intervalo 0/negativo vira 1 s; chips "Seg–Sex, 09:00", "Fim de semana,
  10:00", "Ter, 22:00", "Todos os dias, 07:30", combinação de gatilhos e
  unidade respeitada ("120 s" ≠ "2 min").
- **Persistência (9)**: roundtrip de notificações com tempo/canto próprios;
  roundtrip de configurações; canto inválido/vazio cai em inferior esquerdo;
  arquivos ausentes; JSON corrompido; UTF-8 sem BOM; lembrete sem ajustes
  próprios mantém padrões; migração de `IntervalMinutes`; não regravar
  `IntervalMinutes`.
- **Redimensionamento (5)**: arrastar direita/esquerda, canto inferior direito,
  tamanho mínimo 840×560 e limite da área de trabalho.
- **Fumaça (1)**: sanidade do projeto de testes.

Evidência: `dotnet test` → "Com falha: 0, Aprovado: 36, Total: 36".

---

## 4. Evidências de execução (VERIFICADO)

- Build: `dotnet build` → **0 erros / 0 avisos**.
- Diálogo: janela 560×780; conteúdo 671 px / área 677 px; **sem barra de
  rolagem** (prévia renderizada em memória).
- Janela principal: `ResizeMode="NoResize"` + alças; redimensionamento validado
  manualmente. Moldura branca eliminada (`window == client`, inset 0).
- Temas: pop-up e balão renderizados nos 4 temas; cores resolvidas batem com a
  paleta (`popup_painel`, `balao_preenchimento`, `balao_texto`).
- Pacotes 1.1.3 gerados:
  - `dist\NofyMe-Setup-1.1.3.exe` — 53.089.745 bytes;
  - `dist\NofyMe-1.1.3-portatil.zip` — 72.196.887 bytes.
- Instalação silenciosa testada: 1.1.2 → 1.1.3, código de saída 0, app **não**
  abriu. Nenhum dado foi apagado pelo instalador: `notifications.json` seguiu
  vazio e o histórico foi preservado.
- **Início com o Windows em segundo plano**: o portátil 1.1.3 executado com
  `--startup` ficou 10 s + 30 s sem janela (`MainWindowHandle = 0`) e disparou
  normalmente; cliques normais no `.exe` continuam mostrando a janela.
- **Desempenho em segundo plano** (portátil Release, oculto, ocioso):
  - CPU: **0,016 s em 30 s** → **0,003 % de 16 núcleos**;
  - memória: **138,2 MB** de working set / **83,4 MB** privada, com variação de
    **0,1 MB** no período (sem crescimento contínuo);
  - 16 threads e 656 handles estáveis.
  Conclusão: não está pesado; o consumo é compatível com um app WPF em bandeja,
  sem uso relevante de CPU enquanto ocioso.
- Publicação: tag `v1.1.3`, branch `main` sincronizada.
  Release: https://github.com/LaCharlynn/NofyMe/releases/tag/v1.1.3
  (instalador e portátil anexados e verificados).

---

## 5. DESCONHECIDO / decisões ainda em aberto

1. **Catch-up do horário**: disparar a nota "atrasada" ao abrir o app
   (comportamento atual) ou ignorar e esperar o próximo dia? → decidir com o
   dono do projeto.
2. **Toasts nativos**: exibição, som e auto-dismiss dependem do Windows do
   usuário (Modo Foco/política podem esconder; por isso existe o pop-up próprio).
3. **Contraste do texto fraco no Pêssego** (`#A69286` sobre `#FFF9F3` = 2,84:1):
   abaixo de 4,5:1. É rótulo secundário ("agora", legendas); ajustar só se o
   dono quiser mexer na paleta clara.
4. **Persistência do tamanho da janela**: o redimensionamento vale na sessão;
   não é gravado em `settings.json`.

---

*Spec gerada a partir do comportamento acordado no chat e do design aprovado
(`Interface\Janela principal.png`, `Nova notificação.png`, `Notificações.png`,
`Temas.png`). Nome oficial do produto: **NofyMe**.*
