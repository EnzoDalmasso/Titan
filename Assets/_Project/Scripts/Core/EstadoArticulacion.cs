namespace Titan.Core
{
    // Confiabilidad de la posicion de una articulacion. Mismo orden que el SDK de Kinect
    // NoRastreada va primero porque es el valor por defecto
    public enum EstadoArticulacion
    {
        NoRastreada, // El sensor no la encuentra
        Inferida, // El sensor la estima. No sirve para gestos
        Rastreada // El sensor la ve
    }
}
