using System;
using Titan.Core;
using UnityEngine;

namespace Titan.Kinect
{
    // Formato binario de los paquetes UDP del puente (Tools/KinectBridge/README.md)
    // Si cambia el formato, se sube la version en el puente y aca
    public static class ProtocoloPuente
    {
        public const byte Version = 1;
        public const int TamanioEncabezado = 16;    // 'T', 'K', version, hayCuerpo, numeroCuadro (int), marcaTiempoMs (long)
        public const int BytesPorArticulacion = 13; // x, y, z (float) y estado (byte)
        public const int TamanioPaquete = TamanioEncabezado + Articulaciones.Cantidad * BytesPorArticulacion;

        // Llena destino con un paquete. Devuelve falso si el paquete no es valido
        // Convierte del espacio del Kinect al del contrato: metros y ejes de Unity, (x, y, -z)
        public static bool Leer(byte[] datos, int largo, CuadroCorporal destino)
        {
            if (largo != TamanioPaquete || datos[0] != (byte)'T' || datos[1] != (byte)'K' || datos[2] != Version)
            {
                return false;
            }

            destino.Limpiar();
            destino.hayCuerpo = datos[3] != 0;
            destino.tiempo = BitConverter.ToInt64(datos, 8) / 1000.0;
            if (!destino.hayCuerpo)
            {
                return true;
            }

            int posicion = TamanioEncabezado;
            for (int i = 0; i < Articulaciones.Cantidad; i++)
            {
                byte estado = datos[posicion + 12];
                if (estado > (byte)EstadoArticulacion.Rastreada)
                {
                    destino.Limpiar();
                    return false;
                }

                float x = BitConverter.ToSingle(datos, posicion);
                float y = BitConverter.ToSingle(datos, posicion + 4);
                float z = BitConverter.ToSingle(datos, posicion + 8);
                destino.Establecer((Articulacion)i, new Vector3(x, y, -z), (EstadoArticulacion)estado);
                posicion += BytesPorArticulacion;
            }

            return true;
        }
    }
}
