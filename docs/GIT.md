# Flujo de Git de Titán

## Ramas

| Rama | Para qué | Cómo recibe cambios |
|---|---|---|
| `main` | Versiones estables, cada una con etiqueta (`v0.1.0`, …) | PR desde `develop`, **merge commit**, revisa el otro programador |
| `develop` | Integración del equipo. Rama por defecto en GitHub | PR desde `modulo/*`, **merge commit**, revisa el otro programador |
| `modulo/<nombre>` | Integra un módulo. Ej.: `modulo/personaje` | PR desde `tarea/*`, **squash** (tareas de integración: **merge commit**) |
| `tarea/<modulo>/<descripcion>` | Trabajo concreto. Ej.: `tarea/personaje/golpe-mano` | Commits directos. Es la única rama donde se commitea |

Recorrido: `tarea/*` → `modulo/*` → `develop` → `main`.

No uses `modulo/personaje/golpe`: Git no permite que convivan una rama `modulo/personaje` y otra `modulo/personaje/golpe`, porque la primera tendría que ser archivo y carpeta a la vez.

Nombres de rama: minúsculas, números y guiones. El hook rechaza cualquier otra cosa.

## Qué protege cada capa

- **Hooks locales** (`.githooks/`, se activan con `Tools/configurar-git.ps1`):
  - `pre-commit`: solo deja commitear en `tarea/*`.
  - `commit-msg`: exige Conventional Commits.
  - `pre-push`: bloquea el push a `main` y `develop`.
  - Los demás hooks le pasan los eventos a Git LFS.
- **Reglas de GitHub** (no se pueden saltear con `--no-verify`):
  - `main` y `develop`: PR con 1 aprobación, solo merge commit, sin force push ni borrado.
  - `modulo/*`: PR obligatorio, squash o merge commit, sin force push ni borrado.

## Commits

Formato Conventional Commits, en español:

```
tipo(alcance): descripción en presente
```

- Tipos: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`.
- El alcance es el módulo: `core`, `kinect`, `gestos`, `personaje`, `enemigos`, `pueblo`, `oleadas`, `flujo`, `setup`.

Ejemplos: `feat(personaje): agrega golpe de mano` · `fix(enemigos): corrige soldado que atraviesa la muralla`.

El título de un PR de tarea usa el mismo formato, porque con squash se convierte en el commit del módulo.

## Recetas

### Empezar una tarea
```bash
git switch modulo/personaje
git pull --ff-only
git switch -c tarea/personaje/golpe-mano
```

### Subir la tarea y abrir el PR al módulo
```bash
git push -u origin tarea/personaje/golpe-mano
gh pr create --base modulo/personaje --title "feat(personaje): agrega golpe de mano"
```
Se mergea con **Squash and merge**.

### Actualizar un módulo desde develop (tarea de integración)
Antes de abrir el PR de un módulo hacia `develop`, el módulo tiene que tener lo último de `develop`. Los conflictos se resuelven acá y no en el PR.
```bash
git switch modulo/personaje
git pull --ff-only
git switch -c tarea/personaje/integrar-develop
git fetch origin
git merge origin/develop        # resolvé los conflictos y git commit
git push -u origin tarea/personaje/integrar-develop
gh pr create --base modulo/personaje --title "chore(personaje): integra develop"
```
Este PR se mergea con **Create a merge commit**, nunca con squash. El squash borra el registro de que `develop` ya está integrado, y el mismo conflicto vuelve a aparecer en el próximo merge.

### Llevar un módulo a develop
```bash
gh pr create --base develop --head modulo/personaje --title "feat(personaje): golpe de mano y movimiento"
```
Lo revisa el otro programador y se mergea con **Create a merge commit**.

### Publicar una versión
1. PR de `develop` a `main`, con merge commit, revisado por el otro programador.
2. Después del merge, etiquetar el commit de `main`:
```bash
git switch main
git pull --ff-only
git tag -a v0.1.0 -m "M1: Kinect funcionando"
git push origin v0.1.0
```

## Cambios en Core

`Core` tiene los contratos que usan los dos programadores. Cada cambio en Core va en `modulo/core` y entra a `develop` **antes** que el trabajo que lo usa, para que el otro lo reciba cuanto antes. Si un cambio en Core rompe el código del otro, se avisa en el PR.

## Escenas y prefabs

- Una persona por vez modifica una misma escena o prefab. Avisá antes de tocar uno compartido.
- Cada uno prueba en su escena de `Assets/_Project/Scenes/Sandbox/`.
- `Main.unity` solo se modifica en una tarea dedicada (ej.: `tarea/flujo/escena-principal`).
- Si aparece un conflicto en un `.unity` o `.prefab`, Git usa Smart Merge automáticamente. Lo que no pueda resolver se abre con `git mergetool --tool=unityyamlmerge`.

## Prohibido

- `git push --force` y cualquier reescritura de historia compartida (`rebase` o `commit --amend` de algo ya subido).
- Commits en `main`, `develop` o `modulo/*`.
- `--no-verify` para saltear los hooks.
