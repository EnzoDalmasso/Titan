using UnityEngine;
using System.Collections.Generic;

public class JuegoPelea : MonoBehaviour
{
    public KinectManos kinect;

    [Header("Jugador (metros reales)")]
    public int vidasMax = 5;
    public float espadaLargo = 0.5f;
    public float escudoAncho = 0.45f;
    public float escudoAlto = 0.55f;
    public float radioEscudo = 0.28f;

    [Header("Enemigo")]
    public int vidaEnemigo = 12;
    public float radioEnemigo = 0.35f;
    public float intervaloAtaque = 2.2f;    // entre bolas de fuego (de lejos)
    public float avisoAtaque = 0.7f;
    public float velProyectil = 2.2f;
    public float tiempoAturdido = 1.8f;
    public float ventanaGolpe = 0.5f;
    public float profundidadEscudo = 0.40f;
    public float planoImpacto = 0.25f;

    [Header("IA")]
    public float distLejos = 2.2f;          // distancia al disparar (m)
    public float distCerca = 0.85f;         // distancia al pelear (m)
    public float velAcercar = 1.6f;         // m/s
    public float velAlejar = 2.2f;          // m/s
    public Vector2 tiempoLejos = new Vector2(7f, 10f);  // cuánto dispara antes de cargar
    public float tiempoCercaMax = 9f;       // cuánto aguanta cerca como máximo
    public int golpesParaRetirarse = 3;     // golpes que recibe cerca antes de huir
    public float intervaloPuno = 2.4f;
    public float avisoPuno = 0.6f;
    public float radioPuno = 0.35f;         // tolerancia del escudo contra el puño
    public bool cargaAlBloquear = true;     // si bloqueás una bola, se enfurece y carga

    [Header("Personaje")]
    public GameObject prefabEnemigo;
    public float alturaEnemigo = 1.8f;
    public float offsetAltura = 0.55f;

    enum Estado { Espera, Jugando, Victoria, Derrota }
    Estado estado = Estado.Espera;

    enum Fase { Lejos, Acercando, Cerca, Alejando }
    Fase fase = Fase.Lejos;

    class Proyectil { public GameObject go, marca; public Vector3 dir; }
    List<Proyectil> proyectiles = new List<Proyectil>();

    GameObject espada, escudo, enemigo, modelo, marcaPuno;
    Renderer escudoRend;
    Animator anim;
    HashSet<string> parametros = new HashSet<string>();
    Color colorEscudo = new Color(0.2f, 0.45f, 0.95f);

    Vector3 dirEspada = Vector3.forward;
    int vidas, hpEnemigo, golpesCerca;
    float dist, finFase;
    float proximoAtaque, aturdidoHasta, ventanaRestante;
    float proximoPuno, punoImpactoEn;
    bool punoEnCurso, avisandoPrev;
    float flashEnemigo, flashEscudo, flashDano;
    float tiempoManosArriba, bloqueoHasta;
    GUIStyle estilo, estiloChico;

    void Start()
    {
        espada = Crear(PrimitiveType.Cube, new Color(0.85f, 0.85f, 0.95f));
        escudo = Crear(PrimitiveType.Cube, colorEscudo);
        escudoRend = escudo.GetComponent<Renderer>();

        enemigo = new GameObject("Enemigo");
        modelo = Instantiate(prefabEnemigo, enemigo.transform);
        modelo.transform.localRotation = Quaternion.Euler(0, 180, 0);
        anim = modelo.GetComponent<Animator>();
        foreach (var p in anim.parameters) parametros.Add(p.name);
        enemigo.SetActive(false);
    }

    GameObject Crear(PrimitiveType tipo, Color color)
    {
        var go = GameObject.CreatePrimitive(tipo);
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().material.color = color;
        return go;
    }

    // Animator "a prueba de parámetros faltantes"
    void Trig(string n) { if (parametros.Contains(n)) anim.SetTrigger(n); }
    void SetBool(string n, bool v) { if (parametros.Contains(n)) anim.SetBool(n, v); }

    void Update()
    {
        ActualizarArmas();

        if (estado == Estado.Jugando)
            ActualizarCombate();
        else if (Time.time > bloqueoHasta && (ManosArriba() || Input.GetKeyDown(KeyCode.Space)))
            Empezar();

        flashEnemigo = Mathf.MoveTowards(flashEnemigo, 0, 5f * Time.deltaTime);
        flashEscudo = Mathf.MoveTowards(flashEscudo, 0, 4f * Time.deltaTime);
        flashDano = Mathf.MoveTowards(flashDano, 0, 3f * Time.deltaTime);
    }

    // ---------- Espada y escudo ----------
    void ActualizarArmas()
    {
        var j = kinect.joints;
        if (!kinect.hayCuerpo
            || !j.TryGetValue("HandRight", out var manoD)
            || !j.TryGetValue("ElbowRight", out var codoD)
            || !j.TryGetValue("HandLeft", out var manoI))
        {
            espada.SetActive(false);
            escudo.SetActive(false);
            return;
        }
        espada.SetActive(true);
        escudo.SetActive(true);
        float e = kinect.escala;

        Vector3 d = (manoD - codoD).normalized;
        dirEspada = Vector3.Slerp(dirEspada, d, 12f * Time.deltaTime).normalized;

        espada.transform.position = manoD + dirEspada * (espadaLargo * 0.5f * e);
        espada.transform.rotation = Quaternion.LookRotation(dirEspada);
        espada.transform.localScale = new Vector3(0.07f, 0.07f, espadaLargo) * e;

        escudo.transform.position = manoI;
        escudo.transform.localScale = new Vector3(escudoAncho, escudoAlto, 0.05f) * e;
        escudoRend.material.color = Color.Lerp(colorEscudo, Color.white, flashEscudo);
    }

    bool EspadaToca(Vector3 centro, float radio)
    {
        if (!kinect.joints.TryGetValue("HandRight", out var mano)) return false;
        float e = kinect.escala;
        for (float t = 0.4f; t <= 1.01f; t += 0.3f)
        {
            Vector3 punto = mano + dirEspada * (espadaLargo * t * e);
            if (Vector3.Distance(punto, centro) < radio) return true;
        }
        return false;
    }

    // Evento de DetectorGolpes (mano derecha)
    public void GolpeEspada(Vector3 dir)
    {
        if (estado == Estado.Jugando) ventanaRestante = ventanaGolpe;
    }

    // ---------- Combate ----------
    void ActualizarCombate()
    {
        if (!kinect.hayCuerpo || !kinect.joints.TryGetValue("ShoulderCenter", out var pecho)) return;
        float e = kinect.escala;
        float t = Time.time;
        bool aturdido = t < aturdidoHasta;

        // IA: decide fase, movimiento y ataques
        if (ActualizarFase(t, aturdido, pecho)) return;

        // Posición y escala del monstruo
        Vector3 objetivo = pecho + new Vector3(Mathf.Sin(t * 0.8f) * 0.15f, -0.1f, dist) * e;
        enemigo.transform.position = Vector3.Lerp(enemigo.transform.position, objetivo, 4f * Time.deltaTime);
        modelo.transform.localScale = Vector3.one * (alturaEnemigo * e / 1.8f) * (1f + 0.08f * flashEnemigo);
        modelo.transform.localPosition = Vector3.down * (alturaEnemigo * offsetAltura * e);

        SetBool("Aturdido", aturdido);
        SetBool("Corriendo", !aturdido && (fase == Fase.Acercando || fase == Fase.Alejando));

        // Golpe de espada
        ventanaRestante -= Time.deltaTime;
        if (ventanaRestante > 0 && EspadaToca(enemigo.transform.position, radioEnemigo * e))
        {
            ventanaRestante = 0;
            hpEnemigo -= aturdido ? 2 : 1;
            flashEnemigo = 1f;
            if (fase == Fase.Cerca) golpesCerca++;
            CancelarPuno(t, 1.0f);              // un golpe interrumpe el puñetazo
            if (hpEnemigo <= 0) { Terminar(Estado.Victoria); return; }
            Trig("Golpe");
        }

        // Proyectiles
        for (int i = proyectiles.Count - 1; i >= 0; i--)
        {
            var p = proyectiles[i];
            p.go.transform.position += p.dir * velProyectil * e * Time.deltaTime;

            if (Vector3.Distance(p.go.transform.position, escudo.transform.position) < radioEscudo * e)
            {
                QuitarProyectil(i);
                Aturdir(t);
                if (cargaAlBloquear && fase == Fase.Lejos) finFase = Mathf.Min(finFase, aturdidoHasta);
            }
            else if (p.go.transform.position.z <= pecho.z + planoImpacto * e)
            {
                QuitarProyectil(i);
                vidas--;
                flashDano = 1f;
                if (vidas <= 0) { Terminar(Estado.Derrota); return; }
            }
        }
    }

    void Aturdir(float t)
    {
        aturdidoHasta = t + tiempoAturdido;
        proximoAtaque = aturdidoHasta + 0.8f;
        CancelarPuno(t, tiempoAturdido + 0.8f);
        flashEscudo = 1f;
    }

    // ---------- IA ----------
    // Devuelve true si la partida terminó en este frame
    bool ActualizarFase(float t, bool aturdido, Vector3 pecho)
    {
        float dt = Time.deltaTime;

        switch (fase)
        {
            case Fase.Lejos:
                if (!aturdido)
                {
                    dist = Mathf.MoveTowards(dist, distLejos, velAlejar * dt);

                    bool avisando = t > proximoAtaque - avisoAtaque;
                    if (avisando && !avisandoPrev) Trig("Ataque");
                    avisandoPrev = avisando;

                    if (t >= proximoAtaque)
                    {
                        Lanzar(pecho);
                        proximoAtaque = t + Mathf.Lerp(intervaloAtaque * 0.55f, intervaloAtaque, (float)hpEnemigo / vidaEnemigo);
                    }
                    if (t >= finFase) CambiarFase(Fase.Acercando);
                }
                break;

            case Fase.Acercando:
                if (!aturdido)
                {
                    dist = Mathf.MoveTowards(dist, distCerca, velAcercar * dt);
                    if (dist <= distCerca + 0.01f) CambiarFase(Fase.Cerca);
                }
                break;

            case Fase.Cerca:
                if (!aturdido)
                {
                    if (!punoEnCurso && t >= proximoPuno) IniciarPuno(pecho, t);
                    if (punoEnCurso && t >= punoImpactoEn && ResolverPuno(t)) return true;
                }
                if (golpesCerca >= golpesParaRetirarse || t >= finFase)
                {
                    CancelarPuno(t, 0f);
                    CambiarFase(Fase.Alejando);
                }
                break;

            case Fase.Alejando:
                if (!aturdido)
                {
                    dist = Mathf.MoveTowards(dist, distLejos, velAlejar * dt);
                    if (dist >= distLejos - 0.01f) CambiarFase(Fase.Lejos);
                }
                break;
        }
        return false;
    }

    void CambiarFase(Fase f)
    {
        fase = f;
        avisandoPrev = false;
        float t = Time.time;
        if (f == Fase.Lejos)
        {
            finFase = t + Random.Range(tiempoLejos.x, tiempoLejos.y);
            proximoAtaque = t + 1.2f;
        }
        else if (f == Fase.Cerca)
        {
            finFase = t + tiempoCercaMax;
            golpesCerca = 0;
            proximoPuno = t + 0.8f;
        }
    }

    void IniciarPuno(Vector3 pecho, float t)
    {
        float e = kinect.escala;
        punoEnCurso = true;
        punoImpactoEn = t + avisoPuno;
        Trig("Puno");

        marcaPuno = Crear(PrimitiveType.Sphere, Color.yellow);
        marcaPuno.transform.localScale = Vector3.one * 0.12f * e;
        marcaPuno.transform.position = pecho + new Vector3(Random.Range(-0.25f, 0.25f),
                                                           Random.Range(-0.20f, 0.10f), 0.40f) * e;
    }

    // true si el puñetazo te mató
    bool ResolverPuno(float t)
    {
        punoEnCurso = false;
        bool bloqueado = marcaPuno != null &&
            Vector3.Distance(escudo.transform.position, marcaPuno.transform.position) < radioPuno * kinect.escala;
        if (marcaPuno != null) Destroy(marcaPuno);
        marcaPuno = null;
        proximoPuno = t + intervaloPuno;

        if (bloqueado)
        {
            Aturdir(t);
            return false;
        }
        vidas--;
        flashDano = 1f;
        if (vidas <= 0) { Terminar(Estado.Derrota); return true; }
        return false;
    }

    void CancelarPuno(float t, float pausa)
    {
        if (!punoEnCurso) return;
        punoEnCurso = false;
        if (marcaPuno != null) Destroy(marcaPuno);
        marcaPuno = null;
        proximoPuno = t + pausa;
    }

    void Lanzar(Vector3 pecho)
    {
        float e = kinect.escala;
        Vector3 destino = pecho + new Vector3(Random.Range(-0.3f, 0.3f),
                                              Random.Range(-0.25f, 0.15f),
                                              profundidadEscudo) * e;

        var bola = Crear(PrimitiveType.Sphere, new Color(1f, 0.4f, 0f));
        bola.transform.localScale = Vector3.one * 0.18f * e;
        bola.transform.position = enemigo.transform.position;

        var marca = Crear(PrimitiveType.Sphere, Color.yellow);
        marca.transform.localScale = Vector3.one * 0.08f * e;
        marca.transform.position = destino;

        proyectiles.Add(new Proyectil
        {
            go = bola,
            marca = marca,
            dir = (destino - bola.transform.position).normalized
        });
    }

    void QuitarProyectil(int i)
    {
        Destroy(proyectiles[i].go);
        Destroy(proyectiles[i].marca);
        proyectiles.RemoveAt(i);
    }

    // ---------- Estados ----------
    bool ManosArriba()
    {
        var j = kinect.joints;
        bool arriba = kinect.hayCuerpo
            && j.TryGetValue("Head", out var cab)
            && j.TryGetValue("HandRight", out var d)
            && j.TryGetValue("HandLeft", out var iz)
            && d.y > cab.y && iz.y > cab.y;

        if (!arriba) { tiempoManosArriba = 0; return false; }
        tiempoManosArriba += Time.deltaTime;
        return tiempoManosArriba > 1f;
    }

    void Empezar()
    {
        if (!kinect.hayCuerpo || !kinect.joints.TryGetValue("ShoulderCenter", out var pecho)) return;
        for (int i = proyectiles.Count - 1; i >= 0; i--) QuitarProyectil(i);
        CancelarPuno(Time.time, 0f);

        vidas = vidasMax;
        hpEnemigo = vidaEnemigo;
        dist = distLejos;
        enemigo.SetActive(true);
        enemigo.transform.position = pecho + new Vector3(0, -0.1f, dist) * kinect.escala;
        anim.Rebind();
        anim.Update(0f);
        CambiarFase(Fase.Lejos);
        proximoAtaque = Time.time + 2f;
        aturdidoHasta = 0;
        ventanaRestante = 0;
        tiempoManosArriba = 0;
        estado = Estado.Jugando;
    }

    void Terminar(Estado nuevo)
    {
        for (int i = proyectiles.Count - 1; i >= 0; i--) QuitarProyectil(i);
        CancelarPuno(Time.time, 0f);
        SetBool("Corriendo", false);
        SetBool("Aturdido", false);
        if (nuevo == Estado.Victoria) Trig("Muerte");
        else if (nuevo == Estado.Derrota) Trig("Victoria");
        estado = nuevo;
        bloqueoHasta = Time.time + 3f;
        tiempoManosArriba = 0;
    }

    // ---------- Pantalla ----------
    void OnGUI()
    {
        if (estilo == null)
        {
            estilo = new GUIStyle(GUI.skin.label) { fontSize = 36, fontStyle = FontStyle.Bold };
            estilo.normal.textColor = Color.white;
            estiloChico = new GUIStyle(estilo) { fontSize = 26 };
            estiloChico.normal.textColor = Color.yellow;
        }
        float w = Screen.width;

        if (flashDano > 0)
        {
            GUI.color = new Color(1, 0, 0, flashDano * 0.4f);
            GUI.DrawTexture(new Rect(0, 0, w, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        switch (estado)
        {
            case Estado.Espera:
                GUI.Label(new Rect(w / 2 - 330, 60, 700, 120), "Levantá las dos manos\npara empezar", estilo);
                break;

            case Estado.Jugando:
                GUI.Label(new Rect(w / 2 - 100, 20, 300, 50), "Vidas: " + vidas, estilo);
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(w / 2 - 202, 78, 404, 24), Texture2D.whiteTexture);
                GUI.color = Color.red;
                GUI.DrawTexture(new Rect(w / 2 - 200, 80, 400f * Mathf.Max(0, hpEnemigo) / vidaEnemigo, 20), Texture2D.whiteTexture);
                GUI.color = Color.white;

                string consejo = (fase == Fase.Lejos) ? "¡Cubrite con el escudo!"
                               : (fase == Fase.Acercando) ? "¡Se acerca!"
                               : (fase == Fase.Cerca) ? "¡Atacalo con la espada!" : "";
                GUI.Label(new Rect(w / 2 - 250, 115, 600, 40), consejo, estiloChico);
                break;

            case Estado.Victoria:
                GUI.Label(new Rect(w / 2 - 330, 60, 700, 150), "¡Ganaste!\nLevantá las manos para repetir", estilo);
                break;

            case Estado.Derrota:
                GUI.Label(new Rect(w / 2 - 330, 60, 700, 150), "Perdiste...\nLevantá las manos para reintentar", estilo);
                break;
        }
    }
}