using UnityEngine;
using UnityEngine.Events;

[System.Serializable] public class GolpeEvent : UnityEvent<Vector3> { }

[DefaultExecutionOrder(100)]
public class DetectorGolpes : MonoBehaviour
{
    public KinectManos kinect;
    public string mano = "HandRight";     // "HandLeft" para la otra
    public float umbral = 6f;             // velocidad mínima (unidades/seg)
    public float cooldown = 0.4f;         // segundos entre golpes
    public GolpeEvent alGolpear;          // recibe la dirección del golpe

    public float velocidadActual;         // para ver y ajustar en el Inspector

    const float DT = 1f / 30f;            // el Kinect envía a 30 FPS
    Vector3 posPrev;
    bool tienePrev;
    int msgPrev = -1;
    float ultimoGolpe = -10f;

    void Update()
    {
        if (!kinect.hayCuerpo) { tienePrev = false; return; }
        if (kinect.numeroMensaje == msgPrev) return;   // sin dato nuevo
        msgPrev = kinect.numeroMensaje;

        if (!kinect.joints.TryGetValue(mano, out var m)) return;
        if (!kinect.joints.TryGetValue("ShoulderCenter", out var h)) return;

        // Posición relativa al pecho: no importa dónde se pare la persona
        Vector3 rel = m - h;

        if (tienePrev)
        {
            Vector3 vel = (rel - posPrev) / DT;
            velocidadActual = Mathf.Lerp(velocidadActual, vel.magnitude, 0.5f);

            if (velocidadActual > umbral && Time.time - ultimoGolpe > cooldown)
            {
                ultimoGolpe = Time.time;
                Debug.Log($"GOLPE {mano}  vel={velocidadActual:F1}  dir={vel.normalized}");
                alGolpear.Invoke(vel.normalized);
            }
        }
        posPrev = rel;
        tienePrev = true;
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, mano == "HandRight" ? 10 : 30, 400, 20),
                  mano + " vel: " + velocidadActual.ToString("F1"));
    }
}