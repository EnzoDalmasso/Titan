using Titan.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Titan.Kinect
{
    // Fuente corporal para probar sin sensor: arma un esqueleto y lo mueve con teclado y mouse
    // Corre antes que el resto para que el gameplay lea el cuadro de este mismo frame
    [DefaultExecutionOrder(-100)]
    public class FuenteSimulada : MonoBehaviour, IFuenteCorporal
    {
        [SerializeField] private float frecuencia = 30f;            // Cuadros por segundo, igual que el Kinect v1
        [SerializeField] private float velocidadCuerpo = 1.5f;      // Metros por segundo al caminar con WASD
        [SerializeField] private float sensibilidadMouse = 0.002f;  // Metros que se mueve la mano por pixel
        [SerializeField] private float sensibilidadRueda = 0.0005f; // Metros hacia adelante por paso de rueda
        [SerializeField] private float alcanceBrazo = 0.65f;        // Distancia maxima de la mano al hombro

        // Mano respecto de su hombro con los brazos en alto
        private static readonly Vector3 manoArribaIzquierda = new Vector3(-0.05f, 0.6f, 0f);
        private static readonly Vector3 manoArribaDerecha = new Vector3(0.05f, 0.6f, 0f);

        // Postura de pie, en metros desde el punto del piso entre los pies
        private static readonly Vector3[] poseBase = CrearPoseBase();

        private readonly CuadroCorporal ultimoCuadro = new CuadroCorporal();
        private bool hayCuadro;
        private double proximoCuadro;
        private bool hayCuerpo = true;
        private bool brazosArriba;

        // Manos respecto de su hombro; arrancan con los brazos caidos
        private Vector3 posicionCuerpo;
        private Vector3 manoIzquierda = new Vector3(-0.05f, -0.57f, 0.05f);
        private Vector3 manoDerecha = new Vector3(0.05f, -0.57f, 0.05f);

        public bool IntentarObtenerUltimoCuadro(CuadroCorporal destino)
        {
            if (!hayCuadro)
            {
                return false;
            }

            destino.CopiarDesde(ultimoCuadro);
            return true;
        }

        private void Update()
        {
            LeerControles();

            // El input se lee en cada frame, pero los cuadros salen a la frecuencia del sensor
            double ahora = Time.timeAsDouble;
            if (ahora < proximoCuadro)
            {
                return;
            }

            ArmarCuadro(ahora);
            hayCuadro = true;

            // Si el juego se trabo, no se intenta recuperar los cuadros perdidos
            proximoCuadro += 1.0 / frecuencia;
            if (proximoCuadro < ahora)
            {
                proximoCuadro = ahora + 1.0 / frecuencia;
            }
        }

        private void LeerControles()
        {
            Keyboard teclado = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (teclado == null || mouse == null)
            {
                return;
            }

            if (teclado.bKey.wasPressedThisFrame)
            {
                hayCuerpo = !hayCuerpo;
            }

            // Espacio mantenido: brazos en alto, sin perder la posicion que tenian las manos
            brazosArriba = teclado.spaceKey.isPressed;

            Vector3 direccion = Vector3.zero;
            if (teclado.wKey.isPressed) direccion.z += 1f;
            if (teclado.sKey.isPressed) direccion.z -= 1f;
            if (teclado.dKey.isPressed) direccion.x += 1f;
            if (teclado.aKey.isPressed) direccion.x -= 1f;
            posicionCuerpo += direccion.normalized * (velocidadCuerpo * Time.deltaTime);

            Vector2 delta = mouse.delta.ReadValue();
            float rueda = mouse.scroll.ReadValue().y;
            Vector3 movimiento = new Vector3(delta.x * sensibilidadMouse, delta.y * sensibilidadMouse, rueda * sensibilidadRueda);

            // Boton izquierdo mueve la mano izquierda y boton derecho la derecha
            if (mouse.leftButton.isPressed)
            {
                manoIzquierda = Vector3.ClampMagnitude(manoIzquierda + movimiento, alcanceBrazo);
            }

            if (mouse.rightButton.isPressed)
            {
                manoDerecha = Vector3.ClampMagnitude(manoDerecha + movimiento, alcanceBrazo);
            }
        }

        private void ArmarCuadro(double tiempo)
        {
            ultimoCuadro.Limpiar();
            ultimoCuadro.tiempo = tiempo;
            ultimoCuadro.hayCuerpo = hayCuerpo;
            if (!hayCuerpo)
            {
                return;
            }

            for (int i = 0; i < Articulaciones.Cantidad; i++)
            {
                ultimoCuadro.Establecer((Articulacion)i, posicionCuerpo + poseBase[i], EstadoArticulacion.Rastreada);
            }

            ArmarBrazo(Articulacion.HombroIzquierdo, Articulacion.CodoIzquierdo, Articulacion.MunecaIzquierda,
                Articulacion.ManoIzquierda, brazosArriba ? manoArribaIzquierda : manoIzquierda);
            ArmarBrazo(Articulacion.HombroDerecho, Articulacion.CodoDerecho, Articulacion.MunecaDerecha,
                Articulacion.ManoDerecha, brazosArriba ? manoArribaDerecha : manoDerecha);
        }

        // Ubica codo, muneca y mano sobre la recta del hombro a la mano
        private void ArmarBrazo(Articulacion hombro, Articulacion codo, Articulacion muneca, Articulacion mano, Vector3 manoDesdeHombro)
        {
            Vector3 posicionHombro = ultimoCuadro.Obtener(hombro).posicion;
            ultimoCuadro.Establecer(codo, posicionHombro + manoDesdeHombro * 0.5f, EstadoArticulacion.Rastreada);
            ultimoCuadro.Establecer(muneca, posicionHombro + manoDesdeHombro * 0.9f, EstadoArticulacion.Rastreada);
            ultimoCuadro.Establecer(mano, posicionHombro + manoDesdeHombro, EstadoArticulacion.Rastreada);
        }

        private static Vector3[] CrearPoseBase()
        {
            var pose = new Vector3[Articulaciones.Cantidad];
            pose[(int)Articulacion.CaderaCentro] = new Vector3(0f, 1.0f, 0f);
            pose[(int)Articulacion.Columna] = new Vector3(0f, 1.2f, 0f);
            pose[(int)Articulacion.HombroCentro] = new Vector3(0f, 1.45f, 0f);
            pose[(int)Articulacion.Cabeza] = new Vector3(0f, 1.65f, 0f);
            pose[(int)Articulacion.HombroIzquierdo] = new Vector3(-0.2f, 1.42f, 0f);
            pose[(int)Articulacion.HombroDerecho] = new Vector3(0.2f, 1.42f, 0f);
            pose[(int)Articulacion.CaderaIzquierda] = new Vector3(-0.1f, 0.95f, 0f);
            pose[(int)Articulacion.CaderaDerecha] = new Vector3(0.1f, 0.95f, 0f);
            pose[(int)Articulacion.RodillaIzquierda] = new Vector3(-0.1f, 0.5f, 0f);
            pose[(int)Articulacion.RodillaDerecha] = new Vector3(0.1f, 0.5f, 0f);
            pose[(int)Articulacion.TobilloIzquierdo] = new Vector3(-0.1f, 0.08f, 0f);
            pose[(int)Articulacion.TobilloDerecho] = new Vector3(0.1f, 0.08f, 0f);
            pose[(int)Articulacion.PieIzquierdo] = new Vector3(-0.1f, 0.03f, 0.1f);
            pose[(int)Articulacion.PieDerecho] = new Vector3(0.1f, 0.03f, 0.1f);
            return pose;
        }
    }
}
