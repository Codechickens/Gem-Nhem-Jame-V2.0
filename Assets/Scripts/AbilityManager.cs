using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    public AbilityBase[] abilitySlots = new AbilityBase[3];
    bool[] slotStates = new bool[3];

    public void ProcessInput(int slotIndex, bool isPressed)
    {
        if (slotIndex >= abilitySlots.Length || abilitySlots[slotIndex] == null) return;
        if (isPressed && !slotStates[slotIndex])
        {
            abilitySlots[slotIndex].OnButtonDown();
        } else if (isPressed && slotStates[slotIndex])
        {
            abilitySlots[slotIndex].OnButtonHeld();
        } else if (!isPressed && slotStates[slotIndex])
        {
            abilitySlots[slotIndex].OnButtonUp();
        }
        slotStates[slotIndex] = isPressed;
    }

    public void SwapAbilities(int indexA, int indexB)
    {
        AbilityBase temp = abilitySlots[indexA];
        abilitySlots[indexA] = abilitySlots[indexB];
        abilitySlots[indexB] = temp;
    }
}
