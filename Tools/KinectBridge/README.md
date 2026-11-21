# KinectBridge

Programa de consola que lee el esqueleto del Kinect v1 con el SDK 1.8 y lo envía por UDP a `127.0.0.1:5005`. En Unity lo recibe `FuenteKinect` (`Assets/_Project/Scripts/Kinect`).

Se usa un programa aparte porque cargar el SDK directo en Unity falló en el prototipo.

## Requisitos

- Kinect para Xbox 360 (modelo 1414) con su fuente de alimentación.
- Kinect for Windows SDK 1.8. El proyecto toma `Microsoft.Kinect.dll` de `C:\Program Files\Microsoft SDKs\Kinect\v1.8\Assemblies`.
- Visual Studio con .NET Framework 4.8.

## Uso

1. Abrí `KinectPuente.sln` y compilalo.
2. Ejecutalo. Tiene que mostrar `Kinect enviando a UDP 5005 (protocolo version 1)`.
3. Dale Play en Unity a una escena con `FuenteKinect`.

El orden no importa: si Unity arranca antes, espera los paquetes.

## Protocolo, versión 1

Un paquete por cuadro del sensor (30 por segundo), de 276 bytes, en little endian:

| Byte | Tipo | Contenido |
|---|---|---|
| 0–1 | 2 × `byte` | Firma `'T'`, `'K'` |
| 2 | `byte` | Versión del protocolo (`1`) |
| 3 | `byte` | `1` si hay un cuerpo rastreado, `0` si no |
| 4–7 | `int` | Número de cuadro, consecutivo (sirve para detectar paquetes perdidos) |
| 8–15 | `long` | Marca de tiempo del sensor en milisegundos |
| 16–275 | 20 × 13 bytes | Articulaciones en el orden de `JointType` |

Cada articulación ocupa 13 bytes: `x`, `y` y `z` como `float` en metros, en el espacio del Kinect, y el estado como `byte` (`0` no rastreada, `1` inferida, `2` rastreada). Si no hay cuerpo, las articulaciones van en cero.

La conversión a los ejes de Unity, `(x, y, -z)`, la hace `ProtocoloPuente` del lado de Unity.

Si cambia el formato, hay que subir la versión en `Program.cs` y en `ProtocoloPuente.cs`. Unity descarta los paquetes con otra versión y lo avisa en la consola.
