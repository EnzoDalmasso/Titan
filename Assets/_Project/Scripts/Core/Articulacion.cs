namespace Titan.Core
{
    // Articulaciones del esqueleto. Mismo orden que el SDK de Kinect; se usa como indice de arreglo
    public enum Articulacion
    {
        CaderaCentro,
        Columna,
        HombroCentro,
        Cabeza,
        HombroIzquierdo,
        CodoIzquierdo,
        MunecaIzquierda,
        ManoIzquierda,
        HombroDerecho,
        CodoDerecho,
        MunecaDerecha,
        ManoDerecha,
        CaderaIzquierda,
        RodillaIzquierda,
        TobilloIzquierdo,
        PieIzquierdo,
        CaderaDerecha,
        RodillaDerecha,
        TobilloDerecho,
        PieDerecho
    }

    public static class Articulaciones
    {
        // Cantidad de valores de Articulacion. Actualizar si cambia el enum
        public const int Cantidad = 20;
    }
}
