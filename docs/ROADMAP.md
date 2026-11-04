# Roadmap

Estados: ⬜ pendiente · 🔄 en curso · 👀 en revisión (PR abierto) · ✅ hecho

Actualizá esta tabla en la misma tarea que cambia el estado.

## M0 — Repo, proyecto Unity, reglas y documentación

| Tarea | Responsable | Estado |
|---|---|---|
| Commit inicial, ramas `main`/`develop`/`modulo/setup` | Enzo | ✅ |
| Reglas de protección en GitHub | Enzo | ✅ |
| `tarea/setup/git-config`: gitignore, gitattributes con LFS, hooks y Smart Merge | Enzo | ✅ (#1) |
| `tarea/setup/documentacion`: CLAUDE.md, README, GIT.md, ROADMAP y concepto | Enzo | ✅ (#2) |
| `tarea/setup/proyecto-unity`: proyecto, URP, Input System, carpetas, asmdefs y escenas | Enzo | ✅ (#3) |
| PR `modulo/setup` → `develop` | Enzo (revisa Julián) | 👀 |
| Preparar PC: herramientas, clon y `configurar-git.ps1` | Julián | ⬜ |

## M1 — Kinect funcionando, esqueleto visible, grabación y reproducción

| Tarea | Responsable | Estado |
|---|---|---|
| Spike 1: verificar el sensor con el SDK 1.8 y el Developer Toolkit | Julián | ⬜ |
| Contratos de Core: `CuadroCorporal`, `IFuenteCorporal` | Enzo + Julián | ⬜ |
| Spike 2: puente C# x64 por UDP (`Tools/KinectBridge`) | Julián | ⬜ |
| Spike 3: comparar el puente con el SDK directo en Unity y elegir con evidencia | Julián | ⬜ |
| Spike 4: medir la latencia real | Julián | ⬜ |
| `FuenteKinect` | Julián | ⬜ |
| `FuenteSimulada` (teclado y mouse) | Enzo | ⬜ |
| Formato de grabación y `FuenteGrabada` | Enzo | ⬜ |
| Esqueleto visible con primitivas | Enzo | ⬜ |
| Grabar sesiones reales de cada gesto | Julián | ⬜ |

## M2 — El titán de placeholder copia al jugador

| Tarea | Responsable | Estado |
|---|---|---|
| Titán de primitivas que sigue al esqueleto, con interpolación de 30 a 60 Hz | Enzo | ⬜ |
| Cámara en tercera persona | Enzo | ⬜ |

## M3 — Golpe de mano que aplasta a un soldado

| Tarea | Responsable | Estado |
|---|---|---|
| Contratos entre Gestos/Personaje y Enemigos/Pueblo | Enzo + Julián | ⬜ |
| Detector del gesto de golpe, con pruebas EditMode | Enzo | ⬜ |
| Soldado que avanza, con pool | Julián | ⬜ |
| Aplastar un soldado con la mano | Enzo + Julián | ⬜ |

## M4 — Catapulta, roca y gesto de frenar

| Tarea | Responsable | Estado |
|---|---|---|
| Catapulta que lanza rocas, con pool | Julián | ⬜ |
| Detector del gesto de brazos en alto, con pruebas | Enzo | ⬜ |
| Frenar la roca | Enzo + Julián | ⬜ |
| Evaluar si el juego es divertido | Ambos | ⬜ |

## M5 — Murallas y vida del pueblo

| Tarea | Responsable | Estado |
|---|---|---|
| Vida del pueblo y eventos de daño | Julián | ⬜ |
| Detector del gesto de ambas manos hacia arriba, con pruebas | Enzo | ⬜ |
| Levantar murallas | Julián | ⬜ |

## M6 — Oleadas, victoria, derrota y flujo completo

| Tarea | Responsable | Estado |
|---|---|---|
| Progresión y dificultad de oleadas, con pruebas | Julián | ⬜ |
| Estados de partida y UI mínima | A definir | ⬜ |
| Escena principal integrada | A definir | ⬜ |
