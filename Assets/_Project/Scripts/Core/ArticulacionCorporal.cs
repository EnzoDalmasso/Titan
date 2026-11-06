using UnityEngine;

namespace Titan.Core
{
    // Una articulacion en un cuadro: donde esta y que tan confiable es
    // Es struct para que el arreglo de articulaciones no genere basura
    public struct ArticulacionCorporal
    {
        public Vector3 posicion;//En metros, ejes de Unity
        public EstadoArticulacion estado;

        public ArticulacionCorporal(Vector3 posicion, EstadoArticulacion estado)
        {
            this.posicion = posicion;
            this.estado = estado;
        }

        // Verdadero si la posicion sirve para detectar gestos
        public bool EstaRastreada => estado == EstadoArticulacion.Rastreada;
    }
}
