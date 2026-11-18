# Titán

Juego de acción y defensa en 3D para un jugador. Con el cuerpo, mediante un sensor Kinect, el jugador controla a un titán de piedra que defiende su pueblo de oleadas de soldados y catapultas.

- Concepto: [docs/Documento_de_Concepto_Titan.docx](docs/Documento_de_Concepto_Titan.docx)
- Flujo de Git: [docs/GIT.md](docs/GIT.md)
- Estado de las tareas: [docs/ROADMAP.md](docs/ROADMAP.md)
- Reglas para Claude Code: [CLAUDE.md](CLAUDE.md)

## Requisitos

| Herramienta | Versión | Notas |
|---|---|---|
| Windows | 10 | |
| Unity | **2022.3.62f3** exacta | Instalala desde Unity Hub → Installs → Install Editor → Archive |
| Git | 2.40 o superior | |
| Git LFS | 3.x | Viene incluido con Git for Windows |
| GitHub CLI (`gh`) | 2.x | `winget install GitHub.cli` y después `gh auth login` |
| .NET SDK | 8 o superior | Para el puente de Kinect (`Tools/KinectBridge`) |
| Kinect for Windows SDK | 1.8 | Solo si tenés el sensor (Kinect Xbox 360, modelo 1414) |

## Puesta en marcha

1. Cloná el repo en una ruta **sin tildes** (los espacios sí funcionan):
   ```bash
   git clone https://github.com/EnzoDalmasso/Titan.git
   ```
2. Configurá el clon. Este paso es **obligatorio** y se hace una sola vez:
   ```bash
   powershell -ExecutionPolicy Bypass -File Tools\configurar-git.ps1
   ```
   El script activa los hooks, registra Git LFS y configura Smart Merge. Si preferís hacerlo a mano, seguí la sección siguiente.
3. Abrí la carpeta del repo desde Unity Hub (Add → Add project from disk) con Unity 2022.3.62f3.
4. Probá en tu escena de `Assets/_Project/Scenes/Sandbox/`.

## Configuración manual

### Hooks
```bash
git config core.hooksPath .githooks
```
Bloquean commits fuera de `tarea/*`, mensajes fuera del formato Conventional Commits y push directo a `main` y `develop`. Además le pasan sus eventos a Git LFS.

### Git LFS
```bash
git lfs install
```

### Smart Merge (UnityYAMLMerge)
Resuelve conflictos en escenas, prefabs y otros YAML de Unity entendiendo su estructura. Ajustá la ruta si Unity Hub está instalado en otro lugar:
```bash
git config merge.unityyamlmerge.name "Unity SmartMerge (UnityYAMLMerge)"
git config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data/Tools/UnityYAMLMerge.exe' merge -h -p --force %O %B %A %A"
git config merge.unityyamlmerge.recursive binary
git config mergetool.unityyamlmerge.cmd "'C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p \"\$BASE\" \"\$REMOTE\" \"\$LOCAL\" \"\$MERGED\""
git config mergetool.unityyamlmerge.trustExitCode false
```
Estos comandos están escritos para Git Bash. El driver se usa solo en los archivos marcados con `merge=unityyamlmerge` en `.gitattributes`. Si Smart Merge no puede resolver un conflicto, abrilo con `git mergetool --tool=unityyamlmerge`.

Para verificar la configuración:
```bash
git config --local --get-regexp "hookspath|unityyamlmerge"
```

## Estructura

```
Assets/_Project/
  Scripts/<Modulo>/   Core, Kinect, Gestos, Personaje, Enemigos, Pueblo, Oleadas, Flujo, Composicion
  Tests/EditMode/     Pruebas de la lógica pura
  Scenes/             Main.unity y Sandbox/ (una escena por programador)
Tools/                Scripts del repo y puente de Kinect
docs/                 Concepto, flujo de Git y roadmap
.githooks/            Hooks versionados
```

## Equipo

- Enzo Dalmasso: Gestos y Personaje.
- Julián Vico: Enemigos, Pueblo, Oleadas y Kinect (tiene el sensor).
