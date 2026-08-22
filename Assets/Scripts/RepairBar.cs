using UnityEngine;
using UnityEngine.UI;

public class RepairBar : MonoBehaviour
{
    [SerializeField] Slider slider;

    public void SetMaxRepair(int repair){
        slider.maxValue = repair;
        slider.value = repair;
    }
    public void SetRepair(int repair){
        slider.value = repair;
    }
}
