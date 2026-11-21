using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
public class AvatarJugador : MonoBehaviour
{
    public KinectManos kinect;

    [Header("Aspecto")]
    public float multiplicadorTamano = 1.2f;   // >1 = más grande que vos (Titan)
    public bool ocultarCabeza = false;         // útil en primera persona

    [Header("Movimiento")]
    public float suavizado = 14f;              // más alto = más directo, más bajo = más suave
    public bool copiarPiernas = true;

    class Hueso
    {
        public Transform t;
        public Quaternion restRot;
        public Vector3 restDir;
        public string desde, hasta;
    }

    List<Hueso> huesos = new List<Hueso>();
    List<Transform> torso = new List<Transform>();
    List<Quaternion> torsoRest = new List<Quaternion>();
    Transform caderas, cabeza;
    Quaternion caderasRest, restFrameTorso, restFrameCadera;
    Renderer[] renders;
    float distHombrosBase, escalaBase, sinCuerpo;
    bool calibrado, visible = true, primeraVez = true;

    void Start()
    {
        var anim = GetComponent<Animator>();
        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;

        Transform B(HumanBodyBones b) { return anim.GetBoneTransform(b); }

        caderas = B(HumanBodyBones.Hips);
        cabeza = B(HumanBodyBones.Head);
        Transform cuello = B(HumanBodyBones.Neck) != null ? B(HumanBodyBones.Neck) : B(HumanBodyBones.Spine);

        // Marco de referencia del torso y la cadera en reposo (T-pose)
        Vector3 hombros = B(HumanBodyBones.RightUpperArm).position - B(HumanBodyBones.LeftUpperArm).position;
        Vector3 arriba = cabeza.position - caderas.position;
        restFrameTorso = Marco(hombros, arriba);
        restFrameCadera = Marco(B(HumanBodyBones.RightUpperLeg).position - B(HumanBodyBones.LeftUpperLeg).position, arriba);
        distHombrosBase = hombros.magnitude;

        caderasRest = caderas.rotation;
        foreach (var hb in new[] { HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest })
        {
            var t = B(hb);
            if (t != null) { torso.Add(t); torsoRest.Add(t.rotation); }
        }

        // Cabeza y cuello: inclinación según cuello -> cabeza
        Vector3 dirCuello = cabeza.position - cuello.position;
        Agregar(B(HumanBodyBones.Neck), dirCuello, "ShoulderCenter", "Head");
        Agregar(cabeza, dirCuello, "ShoulderCenter", "Head");

        // Brazos
        Agregar(B(HumanBodyBones.LeftUpperArm), B(HumanBodyBones.LeftLowerArm), "ShoulderLeft", "ElbowLeft");
        Agregar(B(HumanBodyBones.LeftLowerArm), B(HumanBodyBones.LeftHand), "ElbowLeft", "WristLeft");
        Agregar(B(HumanBodyBones.RightUpperArm), B(HumanBodyBones.RightLowerArm), "ShoulderRight", "ElbowRight");
        Agregar(B(HumanBodyBones.RightLowerArm), B(HumanBodyBones.RightHand), "ElbowRight", "WristRight");

        // Piernas
        if (copiarPiernas)
        {
            Agregar(B(HumanBodyBones.LeftUpperLeg), B(HumanBodyBones.LeftLowerLeg), "HipLeft", "KneeLeft");
            Agregar(B(HumanBodyBones.LeftLowerLeg), B(HumanBodyBones.LeftFoot), "KneeLeft", "AnkleLeft");
            Agregar(B(HumanBodyBones.LeftFoot), B(HumanBodyBones.LeftToes), "AnkleLeft", "FootLeft");
            Agregar(B(HumanBodyBones.RightUpperLeg), B(HumanBodyBones.RightLowerLeg), "HipRight", "KneeRight");
            Agregar(B(HumanBodyBones.RightLowerLeg), B(HumanBodyBones.RightFoot), "KneeRight", "AnkleRight");
            Agregar(B(HumanBodyBones.RightFoot), B(HumanBodyBones.RightToes), "AnkleRight", "FootRight");
        }

        anim.enabled = false;   // ahora el cuerpo lo mueve este script
        renders = GetComponentsInChildren<Renderer>();
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>()) smr.updateWhenOffscreen = true;
        Mostrar(false);
    }

    static Quaternion Marco(Vector3 derecha, Vector3 arriba)
    {
        return Quaternion.LookRotation(Vector3.Cross(derecha, arriba), arriba);
    }

    void Agregar(Transform hueso, Transform hijo, string desde, string hasta)
    {
        if (hueso == null || hijo == null) return;
        Agregar(hueso, hijo.position - hueso.position, desde, hasta);
    }

    void Agregar(Transform hueso, Vector3 dirReposo, string desde, string hasta)
    {
        if (hueso == null) return;
        huesos.Add(new Hueso
        {
            t = hueso,
            restRot = hueso.rotation,
            restDir = dirReposo.normalized,
            desde = desde,
            hasta = hasta
        });
    }

    void Mostrar(bool v)
    {
        visible = v;
        foreach (var r in renders) r.enabled = v;
    }

    void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.C)) calibrado = false;   // recalibrar tamaño a mano

        var j = kinect.joints;
        if (!kinect.hayCuerpo
            || !j.TryGetValue("ShoulderLeft", out var sI)
            || !j.TryGetValue("ShoulderRight", out var sD)
            || !j.TryGetValue("ShoulderCenter", out var sC)
            || !j.TryGetValue("HipCenter", out var cad)
            || !j.TryGetValue("HipLeft", out var hI)
            || !j.TryGetValue("HipRight", out var hD))
        {
            sinCuerpo += Time.deltaTime;
            if (visible && sinCuerpo > 0.5f) Mostrar(false);
            if (sinCuerpo > 1f) { calibrado = false; primeraVez = true; }
            return;
        }
        sinCuerpo = 0;

        if (!calibrado)
        {
            escalaBase = Vector3.Distance(sI, sD) / distHombrosBase;
            calibrado = true;
        }
        transform.localScale = Vector3.one * escalaBase * multiplicadorTamano;

        float k = primeraVez ? 1f : Mathf.Clamp01(suavizado * Time.deltaTime);

        // Cadera: posición y giro
        Vector3 arriba = sC - cad;
        Quaternion deltaCadera = Marco(hD - hI, arriba) * Quaternion.Inverse(restFrameCadera);
        caderas.position = Vector3.Lerp(caderas.position, cad, k);
        caderas.rotation = Quaternion.Slerp(caderas.rotation, deltaCadera * caderasRest, k);

        // Torso: giro y inclinación según hombros y columna
        Quaternion deltaTorso = Marco(sD - sI, arriba) * Quaternion.Inverse(restFrameTorso);
        for (int i = 0; i < torso.Count; i++)
            torso[i].rotation = Quaternion.Slerp(torso[i].rotation, deltaTorso * torsoRest[i], k);

        // Cuello, cabeza, brazos y piernas: cada hueso apunta como el tuyo
        foreach (var h in huesos)
        {
            if (!j.TryGetValue(h.desde, out var a) || !j.TryGetValue(h.hasta, out var b)) continue;
            Vector3 dir = b - a;
            if (dir.sqrMagnitude < 0.0001f) continue;
            Quaternion objetivo = Quaternion.FromToRotation(h.restDir, dir.normalized) * h.restRot;
            h.t.rotation = Quaternion.Slerp(h.t.rotation, objetivo, k);
        }

        if (cabeza != null && ocultarCabeza) cabeza.localScale = Vector3.zero;
        else if (cabeza != null) cabeza.localScale = Vector3.one;

        primeraVez = false;
        if (!visible) Mostrar(true);
    }
}