# Prototipo de Julián

Primer prototipo con el Kinect real, hecho por Julián Vico en un proyecto de Unity aparte (2022.3.62f3, pipeline Built-in). Se guarda acá **solo como referencia**: está fuera de `Assets/`, así que Unity no lo compila.

No se copia al proyecto tal cual porque:

- usa el Input viejo (`Input.GetKeyDown`) y el proyecto solo tiene Input System;
- guarda las articulaciones en un `Dictionary<string, Vector3>`, escala las posiciones (`escala = 3`) y no informa el estado de cada articulación, así que no cumple `IFuenteCorporal`;
- parsea texto y crea objetos en cada frame (genera basura);
- usa arte con licencia del Asset Store, que no se puede subir a un repo público.

El puente que manda el esqueleto por UDP está en [`Tools/KinectBridge`](../../Tools/KinectBridge).

## Scripts

| Script | Qué hace | Sirve para |
|---|---|---|
| `KinectManos.cs` | Recibe el UDP del puente en un hilo aparte y guarda el último mensaje con `lock` | Base de `FuenteKinect` (M1) |
| `AvatarJugador.cs` | Mueve un modelo humanoide para que cada hueso apunte como el del jugador | Titán que copia al jugador (M2) |
| `CamaraJugador.cs` | Cámara detrás del pecho del jugador | Cámara en tercera persona (M2) |
| `DetectorGolpes.cs` | Detecta un golpe por la velocidad de la mano respecto del pecho, con enfriamiento | Gesto de golpe (M3) |
| `JuegoBoxeo.cs` | Minijuego de golpear blancos; empieza con las dos manos arriba de la cabeza | Gesto de brazos en alto (M4) |
| `JuegoPelea.cs` | Minijuego con espada y escudo contra un enemigo que lanza bolas de fuego | Frenar rocas con el cuerpo (M4) |

## Datos que confirma

- El SDK 1.8 lee el sensor y el puente UDP a `127.0.0.1:5005` funciona. Según Julián, usar el SDK directo desde Unity falló.
- Conversión de coordenadas que usó: `(x, y, -z)` desde el espacio del Kinect. Falta confirmarla con la prueba de levantar la mano derecha.
- Suavizado del SDK que usó: `Smoothing 0.5`, `Correction 0.5`, `Prediction 0.5`, `JitterRadius 0.05`, `MaxDeviationRadius 0.04`.
