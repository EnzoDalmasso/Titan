using System.IO;
using NUnit.Framework;
using Titan.Core;
using UnityEngine;

namespace Titan.Kinect.Tests
{
    // Prueba el lector del protocolo sin sensor: arma los paquetes igual que el puente
    public class ProtocoloPuenteTests
    {
        private CuadroCorporal cuadro;

        [SetUp]
        public void Preparar()
        {
            cuadro = new CuadroCorporal();
        }

        [Test]
        public void ConCuerpo_ConvierteEjesYLeeEstados()
        {
            byte[] paquete = ArmarPaquete(hayCuerpo: true, marcaTiempoMs: 1500);

            Assert.IsTrue(ProtocoloPuente.Leer(paquete, paquete.Length, cuadro));
            Assert.IsTrue(cuadro.hayCuerpo);
            Assert.AreEqual(1.5, cuadro.tiempo, 1e-9);

            // La mano derecha se armo en (0.3, 1.2, 2.0) del Kinect: en Unity la z se invierte
            ArticulacionCorporal mano = cuadro.Obtener(Articulacion.ManoDerecha);
            Assert.AreEqual(new Vector3(0.3f, 1.2f, -2f), mano.posicion);
            Assert.AreEqual(EstadoArticulacion.Inferida, mano.estado);
            Assert.AreEqual(EstadoArticulacion.Rastreada, cuadro.Obtener(Articulacion.Cabeza).estado);
        }

        [Test]
        public void SinCuerpo_EsValidoYQuedaVacio()
        {
            byte[] paquete = ArmarPaquete(hayCuerpo: false, marcaTiempoMs: 0);

            Assert.IsTrue(ProtocoloPuente.Leer(paquete, paquete.Length, cuadro));
            Assert.IsFalse(cuadro.hayCuerpo);
            Assert.AreEqual(EstadoArticulacion.NoRastreada, cuadro.Obtener(Articulacion.Cabeza).estado);
        }

        [Test]
        public void OtraVersion_SeDescarta()
        {
            byte[] paquete = ArmarPaquete(hayCuerpo: true, marcaTiempoMs: 0);
            paquete[2] = ProtocoloPuente.Version + 1;

            Assert.IsFalse(ProtocoloPuente.Leer(paquete, paquete.Length, cuadro));
        }

        [Test]
        public void TamanioIncorrecto_SeDescarta()
        {
            byte[] paquete = ArmarPaquete(hayCuerpo: true, marcaTiempoMs: 0);

            Assert.IsFalse(ProtocoloPuente.Leer(paquete, paquete.Length - 1, cuadro));
        }

        [Test]
        public void EstadoInvalido_SeDescartaYDejaElCuadroVacio()
        {
            byte[] paquete = ArmarPaquete(hayCuerpo: true, marcaTiempoMs: 0);
            paquete[ProtocoloPuente.TamanioEncabezado + 12] = 7;

            Assert.IsFalse(ProtocoloPuente.Leer(paquete, paquete.Length, cuadro));
            Assert.IsFalse(cuadro.hayCuerpo);
        }

        // Arma un paquete igual que Program.cs del puente: todas rastreadas menos la mano derecha
        private static byte[] ArmarPaquete(bool hayCuerpo, long marcaTiempoMs)
        {
            var paquete = new byte[ProtocoloPuente.TamanioPaquete];
            using (var escritor = new BinaryWriter(new MemoryStream(paquete)))
            {
                escritor.Write((byte)'T');
                escritor.Write((byte)'K');
                escritor.Write(ProtocoloPuente.Version);
                escritor.Write((byte)(hayCuerpo ? 1 : 0));
                escritor.Write(42);
                escritor.Write(marcaTiempoMs);

                for (int i = 0; i < Articulaciones.Cantidad; i++)
                {
                    bool esManoDerecha = i == (int)Articulacion.ManoDerecha;
                    escritor.Write(esManoDerecha ? 0.3f : 0f);
                    escritor.Write(esManoDerecha ? 1.2f : 1f);
                    escritor.Write(esManoDerecha ? 2f : 2.5f);
                    escritor.Write((byte)(esManoDerecha ? EstadoArticulacion.Inferida : EstadoArticulacion.Rastreada));
                }
            }

            return paquete;
        }
    }
}
