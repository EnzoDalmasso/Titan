# Configura este clon del repo para trabajar en Titán. Se corre una vez, después de clonar:
#   powershell -ExecutionPolicy Bypass -File Tools\configurar-git.ps1
# Todo se guarda en la configuración local del repo (.git/config); no toca la configuración global.

$ErrorActionPreference = 'Stop'
$versionUnity = '2022.3.62f3'

# Hay que ejecutarlo desde dentro del repo para que git config afecte a este clon.
$raiz = git rev-parse --show-toplevel
if (-not $?) { throw 'Ejecutá este script desde dentro del repositorio de Titán.' }
Set-Location $raiz

# 1) Hooks versionados: bloquean commits fuera de tarea/* y push directo a main/develop.
git config core.hooksPath .githooks
Write-Host '✔ Hooks activados (core.hooksPath = .githooks)'

# 2) Git LFS: registra los filtros si todavía no se hizo en esta PC.
git lfs install --skip-repo | Out-Null
Write-Host '✔ Git LFS listo'

# 3) Smart Merge: busca UnityYAMLMerge.exe en las rutas habituales de Unity Hub.
$candidatos = @(
    "$env:ProgramFiles\Unity\Hub\Editor\$versionUnity\Editor\Data\Tools\UnityYAMLMerge.exe",
    "D:\Unity\Hub\Editor\$versionUnity\Editor\Data\Tools\UnityYAMLMerge.exe",
    "D:\Program Files\Unity\Hub\Editor\$versionUnity\Editor\Data\Tools\UnityYAMLMerge.exe"
)
$herramienta = $candidatos | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $herramienta) {
    Write-Warning "No encontré UnityYAMLMerge.exe de Unity $versionUnity."
    Write-Warning 'Configuralo a mano con los comandos del README (sección Smart Merge).'
    exit 1
}

# Git necesita barras normales y la ruta entre comillas simples por los espacios.
$ruta = $herramienta -replace '\\', '/'

# Driver: lo usa Git solo en los archivos marcados con merge=unityyamlmerge en .gitattributes.
git config merge.unityyamlmerge.name 'Unity SmartMerge (UnityYAMLMerge)'
git config merge.unityyamlmerge.driver "'$ruta' merge -h -p --force %O %B %A %A"
git config merge.unityyamlmerge.recursive binary

# Herramienta manual: para resolver con 'git mergetool' lo que Smart Merge no pudo.
# PowerShell 5.1 descarta las comillas dobles al llamar a programas externos;
# con \" llegan intactas a git y las rutas con espacios funcionan.
git config mergetool.unityyamlmerge.cmd ("'$ruta' merge -p " + '\"$BASE\" \"$REMOTE\" \"$LOCAL\" \"$MERGED\"')
git config mergetool.unityyamlmerge.trustExitCode false

Write-Host "✔ Smart Merge configurado: $herramienta"
