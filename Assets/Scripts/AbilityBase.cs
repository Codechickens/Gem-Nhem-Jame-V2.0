using UnityEngine;

public abstract class AbilityBase : MonoBehaviour
{
    public virtual void OnButtonDown(){}
    public virtual void OnButtonHeld(){}
    public virtual void OnButtonUp(){}
}
