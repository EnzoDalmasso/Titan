using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Microsoft.Kinect;

// Puente entre el Kinect v1 y Unity: lee el esqueleto con el SDK 1.8 y lo envia por UDP a esta misma PC
// Formato binario version 1, documentado en Tools/KinectBridge/README.md y leido por ProtocoloPuente en Unity
internal static class Programa
{
    private const byte Version = 1;
    private const int Puerto = 5005;
    private const int CantidadArticulaciones = 20;
    private const int TamanioPaquete = 16 + CantidadArticulaciones * 13;

    private static void Main()
    {
        KinectSensor sensor = null;
        foreach (KinectSensor candidato in KinectSensor.KinectSensors)
        {
            if (candidato.Status == KinectStatus.Connected)
            {
                sensor = candidato;
                break;
            }
        }

        if (sensor == null)
        {
            Console.WriteLine("No se encontro el Kinect");
            Console.ReadLine();
            return;
        }

        // Suavizado del SDK con los valores que probo Julian en el prototipo
        sensor.SkeletonStream.Enable(new TransformSmoothParameters
        {
            Smoothing = 0.5f,
            Correction = 0.5f,
            Prediction = 0.5f,
            JitterRadius = 0.05f,
            MaxDeviationRadius = 0.04f
        });

        var esqueletos = new Skeleton[sensor.SkeletonStream.FrameSkeletonArrayLength];
        var paquete = new byte[TamanioPaquete];
        var escritor = new BinaryWriter(new MemoryStream(paquete));
        var udp = new UdpClient();
        var destino = new IPEndPoint(IPAddress.Loopback, Puerto);
        int numeroCuadro = 0;

        sensor.SkeletonFrameReady += (emisor, evento) =>
        {
            using (SkeletonFrame cuadro = evento.OpenSkeletonFrame())
            {
                if (cuadro == null)
                {
                    return;
                }

                cuadro.CopySkeletonDataTo(esqueletos);
                Skeleton cuerpo = null;
                foreach (Skeleton esqueleto in esqueletos)
                {
                    if (esqueleto.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        cuerpo = esqueleto;
                        break;
                    }
                }

                // Encabezado: firma, version, si hay cuerpo, numero de cuadro y marca de tiempo del sensor
                escritor.Seek(0, SeekOrigin.Begin);
                escritor.Write((byte)'T');
                escritor.Write((byte)'K');
                escritor.Write(Version);
                escritor.Write((byte)(cuerpo != null ? 1 : 0));
                escritor.Write(numeroCuadro++);
                escritor.Write(cuadro.Timestamp);

                // Articulaciones en el orden de JointType: posicion en metros y estado
                for (int i = 0; i < CantidadArticulaciones; i++)
                {
                    if (cuerpo == null)
                    {
                        escritor.Write(0f);
                        escritor.Write(0f);
                        escritor.Write(0f);
                        escritor.Write((byte)0);
                        continue;
                    }

                    Joint articulacion = cuerpo.Joints[(JointType)i];
                    escritor.Write(articulacion.Position.X);
                    escritor.Write(articulacion.Position.Y);
                    escritor.Write(articulacion.Position.Z);
                    escritor.Write((byte)articulacion.TrackingState);
                }

                escritor.Flush();
                udp.Send(paquete, paquete.Length, destino);
            }
        };

        sensor.Start();
        Console.WriteLine("Kinect enviando a UDP " + Puerto + " (protocolo version " + Version + "). Enter para salir.");
        Console.ReadLine();
        sensor.Stop();
    }
}
