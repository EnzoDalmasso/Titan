using Titan.Core;
using UnityEngine;

namespace Titan.Kinect
{
    // Dibuja una esfera por articulacion de cualquier fuente corporal, para depurar
    // Verde rastreada, amarillo inferida y oculta si no esta rastreada
    public class VisorEsqueleto : MonoBehaviour
    {
        // Unity no serializa interfaces: se arrastra el componente y se convierte en Awake
        [SerializeField] private MonoBehaviour componenteFuente;
        [SerializeField] private float tamanioEsfera = 0.08f;
        [SerializeField] private Color colorRastreada = Color.green;
        [SerializeField] private Color colorInferida = Color.yellow;

        private static readonly int idColorBase = Shader.PropertyToID("_BaseColor"); // Color del material Lit de URP

        private readonly CuadroCorporal cuadro = new CuadroCorporal();
        private readonly Transform[] esferas = new Transform[Articulaciones.Cantidad];
        private readonly Renderer[] renderers = new Renderer[Articulaciones.Cantidad];
        private MaterialPropertyBlock bloqueColor;
        private IFuenteCorporal fuente;

        private void Awake()
        {
            fuente = componenteFuente as IFuenteCorporal;
            if (fuente == null)
            {
                Debug.LogError("VisorEsqueleto: el componente asignado no es una IFuenteCorporal", this);
                enabled = false;
                return;
            }

            bloqueColor = new MaterialPropertyBlock();
            for (int i = 0; i < Articulaciones.Cantidad; i++)
            {
                GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                esfera.name = ((Articulacion)i).ToString();
                Destroy(esfera.GetComponent<Collider>()); // Es solo visual, no tiene que chocar con nada
                esfera.transform.SetParent(transform, false);
                esfera.transform.localScale = Vector3.one * tamanioEsfera;
                esfera.SetActive(false);
                esferas[i] = esfera.transform;
                renderers[i] = esfera.GetComponent<Renderer>();
            }
        }

        private void Update()
        {
            bool hayDatos = fuente.IntentarObtenerUltimoCuadro(cuadro) && cuadro.hayCuerpo;

            for (int i = 0; i < Articulaciones.Cantidad; i++)
            {
                ArticulacionCorporal articulacion = cuadro.Obtener((Articulacion)i);
                bool visible = hayDatos && articulacion.estado != EstadoArticulacion.NoRastreada;
                esferas[i].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                esferas[i].localPosition = articulacion.posicion;
                bloqueColor.SetColor(idColorBase, articulacion.EstaRastreada ? colorRastreada : colorInferida);
                renderers[i].SetPropertyBlock(bloqueColor);
            }
        }
    }
}
