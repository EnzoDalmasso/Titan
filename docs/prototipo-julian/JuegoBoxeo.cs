using UnityEngine;
using System.Collections.Generic;

public class JuegoBoxeo : MonoBehaviour
{
    public KinectManos kinect;
    public float duracion = 60f;        // segundos de partida
    public float intervalo = 1.2f;      // segundos entre blancos
    public float vidaBlanco = 2.5f;     // cuánto dura cada blanco
    public float radioGolpe = 0.20f;    // en metros reales: tolerancia para acertar
    public float distanciaAlPecho = 0.55f;
    public float tamanoBlanco = 0.18f;  // en metros reales

    enum Estado { Espera, Jugando, Fin }
    Estado estado = Estado.Espera;

    class Blanco { public GameObject go; public bool derecha; public float muereEn; }
    List<Blanco> blancos = new List<Blanco>();

    int puntos;
    float inicio, proximoSpawn, tiempoManosArriba;
    GUIStyle estilo;

    void Update()
    {
        switch (estado)
        {
            case Estado.Espera:
                if (ManosArriba() || Input.GetKeyDown(KeyCode.Space)) Empezar();
                break;

            case Estado.Jugando:
                if (Time.time - inicio >= duracion) { Terminar(); break; }
                if (Time.time >= proximoSpawn) { CrearBlanco(); proximoSpawn = Time.time + intervalo; }
                for (int i = blancos.Count - 1; i >= 0; i--)
                    if (Time.time > blancos[i].muereEn) Quitar(i);
                break;

            case Estado.Fin:
                if (ManosArriba() || Input.GetKeyDown(KeyCode.Space)) Empezar();
                break;
        }
    }

    // Gesto de inicio: las dos manos más arriba de la cabeza durante 1 segundo
    bool ManosArriba()
    {
        var j = kinect.joints;
        bool arriba = kinect.hayCuerpo
            && j.TryGetValue("Head", out var c)
            && j.TryGetValue("HandRight", out var d)
            && j.TryGetValue("HandLeft", out var iz)
            && d.y > c.y && iz.y > c.y;

        if (!arriba) { tiempoManosArriba = 0; return false; }
        tiempoManosArriba += Time.deltaTime;
        return tiempoManosArriba > 1f;
    }

    void Empezar()
    {
        for (int i = blancos.Count - 1; i >= 0; i--) Quitar(i);
        puntos = 0;
        inicio = Time.time;
        proximoSpawn = Time.time + 1f;
        tiempoManosArriba = 0;
        estado = Estado.Jugando;
    }

    void Terminar()
    {
        for (int i = blancos.Count - 1; i >= 0; i--) Quitar(i);
        tiempoManosArriba = -2f;   // evita reiniciar enseguida sin querer
        estado = Estado.Fin;
    }

    void CrearBlanco()
    {
        if (!kinect.joints.TryGetValue("ShoulderCenter", out var pecho)) return;
        float e = kinect.escala;

        var offset = new Vector3(Random.Range(-0.35f, 0.35f),
                                 Random.Range(-0.30f, 0.20f),
                                 distanciaAlPecho);

        bool derecha = Random.value > 0.5f;
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<Collider>());
        go.transform.position = pecho + offset * e;
        go.transform.localScale = Vector3.one * tamanoBlanco * 2f * e;
        go.GetComponent<Renderer>().material.color = derecha ? Color.red : Color.cyan;

        blancos.Add(new Blanco { go = go, derecha = derecha, muereEn = Time.time + vidaBlanco });
    }

    void Quitar(int i)
    {
        Destroy(blancos[i].go);
        blancos.RemoveAt(i);
    }

    // Se llaman desde los eventos de DetectorGolpes
    public void GolpeDerecha(Vector3 dir) { Intentar(true, "HandRight"); }
    public void GolpeIzquierda(Vector3 dir) { Intentar(false, "HandLeft"); }

    void Intentar(bool derecha, string mano)
    {
        if (estado != Estado.Jugando) return;
        if (!kinect.joints.TryGetValue(mano, out var pos)) return;

        float radio = radioGolpe * kinect.escala;
        int mejor = -1;
        float mejorDist = radio;

        for (int i = 0; i < blancos.Count; i++)
        {
            if (blancos[i].derecha != derecha) continue;   // cada mano, su color
            float d = Vector3.Distance(pos, blancos[i].go.transform.position);
            if (d < mejorDist) { mejorDist = d; mejor = i; }
        }

        if (mejor >= 0) { puntos++; Quitar(mejor); }
    }

    void OnGUI()
    {
        if (estilo == null)
        {
            estilo = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold };
            estilo.normal.textColor = Color.white;
        }

        float w = Screen.width;
        switch (estado)
        {
            case Estado.Espera:
                GUI.Label(new Rect(w / 2 - 330, 60, 700, 120), "Levantá las dos manos\npara empezar", estilo);
                break;
            case Estado.Jugando:
                float resta = Mathf.Max(0, duracion - (Time.time - inicio));
                GUI.Label(new Rect(w / 2 - 200, 20, 400, 60),
                          "Puntos: " + puntos + "    Tiempo: " + resta.ToString("F0"), estilo);
                break;
            case Estado.Fin:
                GUI.Label(new Rect(w / 2 - 330, 60, 700, 150),
                          "¡Fin! Puntos: " + puntos + "\nLevantá las manos para repetir", estilo);
                break;
        }
    }
}