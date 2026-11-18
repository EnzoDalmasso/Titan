using System;
using UnityEngine;

namespace Titan.Core
{
    // Foto del cuerpo del jugador en un instante
    // Se crea una vez y se reutiliza: se rellena en cada frame sin hacer new
    public class CuadroCorporal
    {
        public bool hayCuerpo;  // Falso si no hay nadie frente al sensor
        public double tiempo;   // Segundos en que se tomo el cuadro, para calcular velocidades

        private readonly ArticulacionCorporal[] articulaciones = new ArticulacionCorporal[Articulaciones.Cantidad];

        // Devuelve una copia de la articulacion pedida
        public ArticulacionCorporal Obtener(Articulacion articulacion)
        {
            return articulaciones[(int)articulacion];
        }

        // Lo usan las fuentes para rellenar el cuadro
        public void Establecer(Articulacion articulacion, Vector3 posicion, EstadoArticulacion estado)
        {
            articulaciones[(int)articulacion] = new ArticulacionCorporal(posicion, estado);
        }

        // Copia todos los datos de otro cuadro sin generar basura
        public void CopiarDesde(CuadroCorporal otro)
        {
            hayCuerpo = otro.hayCuerpo;
            tiempo = otro.tiempo;
            Array.Copy(otro.articulaciones, articulaciones, Articulaciones.Cantidad);
        }

        // Deja el cuadro vacio: sin cuerpo y todo NoRastreada
        public void Limpiar()
        {
            hayCuerpo = false;
            tiempo = 0;
            Array.Clear(articulaciones, 0, Articulaciones.Cantidad);
        }
    }
}
