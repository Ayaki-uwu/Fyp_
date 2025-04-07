using UnityEngine;
using UnityEngine.EventSystems;

public class ChipUI : MonoBehaviour, IPointerClickHandler
{
    public ChipUIManager chipUIManager;
    void Awake()
    {
        chipUIManager = FindObjectOfType<ChipUIManager>();
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        Debug.Log("OnPointerClick");
        if (pointerEventData.button == PointerEventData.InputButton.Left) {
            chipUIManager.EquipChip(gameObject.name);
        }
        else if (pointerEventData.button == PointerEventData.InputButton.Right) {
            chipUIManager.UnequipChip(gameObject.name);
        }
    }
}

