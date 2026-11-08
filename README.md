# NofyMe

Lembretes discretos que aparecem na hora certa — sem manual.

## O que faz

- **Pop-up na tela**: cartão com barra de tempo, “Adiar 10 min” e “Dispensar”. Fecha sozinho no tempo definido em **Configurações → Tempo na tela** (1 a 60 s; padrão 6 s). O canto de entrada é escolhido em **Posição do pop-up**.
- **Nativa do Windows**: toast do sistema.
- **Balão compacto**: uma única bolha na cor do tema, só com a mensagem. Os cantos são arredondados e o canto de origem salta para fora, apontando para o canto da tela. Sem roubar o foco e sem título. O canto (superior/inferior, esquerdo/direito) é escolhido em **Configurações → Posição do balão**.
- **Quando disparar**: ao iniciar o Windows, a cada X minutos/segundos **ou** em um horário fixo (um ou outro).
- **Temas**: Entardecer, Café, Floresta e Pêssego.
- **Bandeja**: fechar a janela mantém o NofyMe ativo. Clique duplo no ícone para abrir.

## Requisitos

- Windows 10 (1809+) ou Windows 11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) — só para rodar pelo código-fonte. Os pacotes em `dist` já são autocontidos.

## Instalar ou usar portátil

| Arquivo | Como usar |
| --- | --- |
| `dist\NofyMe-Setup-1.0.0.exe` | Instala por usuário (sem administrador), cria atalhos e abre o app. |
| `dist\NofyMe-1.0.0-portatil.zip` | Extraia e execute `NofyMe.exe`. Não precisa instalar. |

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
| `settings.json` | tema, opções, posições e tempo do pop-up |
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
