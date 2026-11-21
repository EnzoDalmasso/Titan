using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Titan.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Titan.Kinect
{
    // Fuente corporal del sensor real: recibe por UDP los cuadros del puente (Tools/KinectBridge)
    // Un hilo aparte espera los paquetes para no bloquear el juego; el hilo principal copia el ultimo
    public class FuenteKinect : MonoBehaviour, IFuenteCorporal
    {
        [SerializeField] private int puerto = 5005;
        [SerializeField] private float segundosSinDatos = 0.5f; // Sin paquetes por mas tiempo, no hay cuadro

        private readonly object candado = new object();
        private readonly CuadroCorporal ultimoCuadro = new CuadroCorporal();   // Compartido, siempre con candado
        private readonly CuadroCorporal cuadroEntrante = new CuadroCorporal(); // Solo lo usa el hilo receptor
        private readonly byte[] bufer = new byte[1024];                         // Solo lo usa el hilo receptor

        private Socket socket;
        private Thread hilo;
        private volatile bool corriendo;
        private long marcaUltimoPaquete; // Stopwatch.GetTimestamp del ultimo paquete valido, 0 si no llego ninguno
        private int paquetesInvalidos;
        private bool avisoInvalidos;

        public bool IntentarObtenerUltimoCuadro(CuadroCorporal destino)
        {
            long marca = Interlocked.Read(ref marcaUltimoPaquete);
            if (marca == 0)
            {
                return false;
            }

            // Si el puente se cerro, no se devuelve un cuadro viejo como si fuera actual
            double segundos = (Stopwatch.GetTimestamp() - marca) / (double)Stopwatch.Frequency;
            if (segundos > segundosSinDatos)
            {
                return false;
            }

            lock (candado)
            {
                destino.CopiarDesde(ultimoCuadro);
            }

            return true;
        }

        private void OnEnable()
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Bind(new IPEndPoint(IPAddress.Loopback, puerto)); // Solo escucha en esta PC
            }
            catch (SocketException e)
            {
                Debug.LogError($"FuenteKinect: no se pudo abrir el puerto {puerto}: {e.Message}", this);
                socket?.Close();
                socket = null;
                enabled = false;
                return;
            }

            corriendo = true;
            hilo = new Thread(Recibir) { IsBackground = true, Name = "FuenteKinect" };
            hilo.Start(socket);
        }

        private void OnDisable()
        {
            corriendo = false;
            socket?.Close(); // Destraba el Receive que esta esperando en el hilo
            hilo?.Join(500);
            socket = null;
            hilo = null;
        }

        private void Update()
        {
            // Avisa una sola vez: suele ser un puente con otra version del protocolo
            if (!avisoInvalidos && Volatile.Read(ref paquetesInvalidos) > 0)
            {
                avisoInvalidos = true;
                Debug.LogWarning($"FuenteKinect: llegaron paquetes invalidos. Revisar que el puente use la version {ProtocoloPuente.Version} del protocolo", this);
            }
        }

        // Corre en el hilo receptor: nunca usa la API de Unity
        private void Recibir(object estado)
        {
            var socketHilo = (Socket)estado;
            while (corriendo)
            {
                int largo;
                try
                {
                    largo = socketHilo.Receive(bufer);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException)
                {
                    if (!corriendo)
                    {
                        return;
                    }

                    continue;
                }

                if (!ProtocoloPuente.Leer(bufer, largo, cuadroEntrante))
                {
                    Interlocked.Increment(ref paquetesInvalidos);
                    continue;
                }

                lock (candado)
                {
                    ultimoCuadro.CopiarDesde(cuadroEntrante);
                }

                Interlocked.Exchange(ref marcaUltimoPaquete, Stopwatch.GetTimestamp());
            }
        }
    }
}
