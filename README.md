# NofyMe

Lembretes discretos que aparecem na hora certa — sem manual.

**[Baixar a versão mais recente](https://github.com/LaCharlynn/NofyMe/releases/latest)** — instalador ou portátil, sem precisar instalar o .NET.

## O que faz

- **Pop-up na tela**: cartão com barra de tempo e “Dispensar”. Fecha sozinho no tempo do próprio lembrete (1 a 60 s) ou, se ele não definir, no padrão de **Configurações → Tempo na tela** (padrão 6 s).
- **Nativa do Windows**: toast do sistema.
- **Balão compacto**: uma única bolha na cor do tema, só com a mensagem. Os cantos são arredondados e o canto de origem salta para fora, apontando para o canto da tela. Sem roubar o foco e sem título. O canto é escolhido em cada lembrete (padrão: inferior esquerdo).
- **Por lembrete**: cada notificação define o próprio tempo na tela e o canto (sem escolha, usa inferior esquerdo).
- **Quando disparar**: ao iniciar o Windows, a cada X minutos/segundos **ou** em um horário fixo (um ou outro). O chip mostra a unidade escolhida — 90 s não vira “1 min”.
- **Temas**: Entardecer, Café, Floresta e Pêssego.
- **Janela redimensionável**: arraste as bordas ou os cantos.
- **Bandeja**: fechar a janela mantém o NofyMe ativo. Clique duplo no ícone para abrir.

## Requisitos

- Windows 10 (1809+) ou Windows 11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) — só para rodar pelo código-fonte. Os pacotes em `dist` já são autocontidos.

## Instalar ou usar portátil

| Arquivo | Como usar |
| --- | --- |
| `NofyMe-Setup-1.1.1.exe` | Instala por usuário (sem administrador), cria atalhos e abre o app. |
| `NofyMe-1.1.1-portatil.zip` | Extraia e execute `NofyMe.exe`. Não precisa instalar. |

Os dois ficam em [Releases](https://github.com/LaCharlynn/NofyMe/releases) e também em `dist` depois de gerados.

Os dois são autocontidos: não exigem .NET instalado. A desinstalação **não apaga** os lembretes.

## Rodar pelo código

```powershell
dotnet run --project NofyMe
```

## Dados

Ficam em `%APPDATA%\NofyMe`:

| Arquivo | Conteúdo |
| --- | --- |
| `notifications.json` | notificações criadas |
| `settings.json` | tema, opções e tempo padrão do pop-up (cada lembrete pode sobrepor) |
| `history.json` | últimas 200 exibições |

Gravação atômica e UTF-8 sem BOM. Arquivo corrompido não derruba o app.

## Testes

```powershell
dotnet test
```

## Gerar os pacotes

```powershell
# portátil (autocontido, arquivo único)
dotnet publish NofyMe -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o dist\portable

# instalador (Inno Setup 6)
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" installer\NofyMe.iss
```
