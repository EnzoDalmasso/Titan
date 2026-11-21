using UnityEngine;

public class CamaraJugador : MonoBehaviour
{
    public KinectManos kinect;
    public Vector3 offset = new Vector3(0f, 0.6f, -1.4f); // metros reales respecto al pecho
    public float miraAdelante = 1.2f;                     // punto al que mira, delante del pecho
    public float suavizado = 3f;

    bool primera = true;

    void LateUpdate()
    {
        if (!kinect.hayCuerpo || !kinect.joints.TryGetValue("ShoulderCenter", out var pecho)) return;
        float e = kinect.escala;

        Vector3 pos = pecho + offset * e;
        Vector3 mira = pecho + Vector3.forward * miraAdelante * e;
        Quaternion rot = Quaternion.LookRotation(mira - pos);

        if (primera) { transform.SetPositionAndRotation(pos, rot); primera = false; return; }

        transform.position = Vector3.Lerp(transform.position, pos, suavizado * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, suavizado * Time.deltaTime);
    }
}