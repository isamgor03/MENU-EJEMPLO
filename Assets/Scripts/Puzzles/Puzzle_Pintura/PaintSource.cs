using UnityEngine;

//Los colores del puzzle. Los numeros estan puestos a proposito para poder mezclar sumando bits:
//Rojo (1) + Amarillo (2) = Naranja (3)
//Rojo (1) + Azul (4) = Morado (5)
//Amarillo (2) + Azul (4) = Verde (6)
//Cualquier otra mezcla acaba en Marron (7)
public enum PaintColor
{

    None = 0, Red = 1, Yellow = 2, Orange = 3, Blue = 4, Purple = 5, Green = 6, Brown = 7

}

//Un bote de pintura. El jugador lo mira y pulsa E para coger su color
//Necesita un Collider para que el rayo del jugador lo detecte
public class PaintSource : MonoBehaviour
{

    public PaintColor color;

    //Color real de Unity de cada PaintColor (en el mismo orden que los numeros del enum)
    public static readonly Color[] realColors =
    {
        Color.gray,                     //None
        Color.red,                      //Red
        Color.yellow,                   //Yellow
        new Color(1f, 0.5f, 0f),        //Orange
        Color.blue,                     //Blue
        new Color(0.6f, 0.2f, 0.8f),    //Purple
        Color.green,                    //Green
        new Color(0.4f, 0.25f, 0.1f)    //Brown
    };

    void Start()
    {
        //Pinta el bote de su color para que se reconozca
        GetComponentInChildren<Renderer>().material.color = realColors[(int)color];
    }

}
