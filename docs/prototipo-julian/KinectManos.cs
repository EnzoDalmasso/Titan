using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class KinectManos : MonoBehaviour
{
    public Transform manoDerecha;
    public Transform manoIzquierda;
    public float escala = 3f;
    public int puerto = 5005;

    UdpClient udp;
    Thread hilo;
    volatile bool corriendo;
    readonly object candado = new object();
    string ultimoMensaje = "NONE";

    // Posiciones de articulaciones, disponibles para el resto del juego
    public Dictionary<string, Vector3> joints = new Dictionary<string, Vector3>();
    public bool hayCuerpo;

    //
    public volatile int numeroMensaje;

    public float margenAdelante = 0.10f;   // la mano debe estar al menos 10 cm delante del pecho
    public float alcanceMaximo = 0.9f;     // distancia máxima de la mano al pecho (metros)

    void Start()
    {
        udp = new UdpClient(puerto);
        corriendo = true;
        hilo = new Thread(Escuchar) { IsBackground = true };
        hilo.Start();
    }

    void Escuchar()
    {
        var ep = new IPEndPoint(IPAddress.Any, 0);
        while (corriendo)
        {
            try
            {
                var datos = udp.Receive(ref ep);
                lock (candado) ultimoMensaje = Encoding.ASCII.GetString(datos);
                numeroMensaje++;
            }
            catch { }
        }
    }

    void Update()
    {
        string msg;
        lock (candado) msg = ultimoMensaje;

        hayCuerpo = msg != "NONE";
        if (!hayCuerpo) return;

        foreach (var parte in msg.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = parte.Split(':');
            var n = kv[1].Split(',');
            joints[kv[0]] = Convertir(
                float.Parse(n[0], CultureInfo.InvariantCulture),
                float.Parse(n[1], CultureInfo.InvariantCulture),
                float.Parse(n[2], CultureInfo.InvariantCulture));
        }

        if (joints.TryGetValue("ShoulderCenter", out var pecho))
        {
            LimitarMano("HandRight", pecho);
            LimitarMano("HandLeft", pecho);
        }

        if (joints.ContainsKey("HandRight")) manoDerecha.position = joints["HandRight"];
        if (joints.ContainsKey("HandLeft")) manoIzquierda.position = joints["HandLeft"];
    }

    Vector3 Convertir(float x, float y, float z)
    {
        return new Vector3(x, y, -z) * escala;
    }

    void OnDestroy()
    {
        corriendo = false;
        udp?.Close();
    }

    void LimitarMano(string nombre, Vector3 pecho)
    {
        if (!joints.TryGetValue(nombre, out var mano)) return;

        // 1) No dejar que pase detrás del pecho (adelante = +Z en Unity)
        float zMin = pecho.z + margenAdelante * escala;
        if (mano.z < zMin) mano.z = zMin;

        // 2) Limitar el alcance máximo respecto al pecho
        Vector3 rel = mano - pecho;
        float maxDist = alcanceMaximo * escala;
        if (rel.magnitude > maxDist)
            mano = pecho + rel.normalized * maxDist;

        joints[nombre] = mano;
    }
}