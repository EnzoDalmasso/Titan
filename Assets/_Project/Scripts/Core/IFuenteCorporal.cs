namespace Titan.Core
{
    // Contrato de todo lo que entrega cuerpos: sensor, grabacion o teclado
    // El gameplay depende solo de esto y nunca sabe de donde vienen los datos
    public interface IFuenteCorporal
    {
        // Copia el ultimo cuadro en destino. Devuelve falso si todavia no hay ninguno
        // Se llama desde Update: siempre en el hilo principal
        bool IntentarObtenerUltimoCuadro(CuadroCorporal destino);
    }
}
