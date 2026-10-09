using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class LeerValores : MonoBehaviour
{

    //Ejemplo Togle

    public Toggle miToggle;


    void Start()
    {
        OnSliderChange();

    }

    public void OnToggleChange()// Esto solo se ejecuta cuando cambie el valor del toggle
    {
        if (miToggle.isOn == true)
        {
            Debug.Log("Mi Toggle ha cambiado a VERDADERO");
        }
        else
        {
            Debug.Log("Mi Toggle ha cambiado a FALSO");
        }
    }


    //Ejemplo Slider  

    public Slider miSlider;
    public Text textSlider;

    public void OnSliderChange() 
    {
       
        textSlider.text = (miSlider.value*100).ToString("F1") + " % ";

    }



}