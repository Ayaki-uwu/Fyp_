using UnityEngine;
using UnityEngine.UI;

public class DigitButton : MonoBehaviour
{
    public int digitValue;
    public RectTransform rectTransform;

    private MiniGame gameController;
    public Image digitImage;         // Reference to the image
    public Sprite[] digitSprites;    // 0-9 digit sprites (set in Inspector)
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        gameController = FindObjectOfType<MiniGame>();
    }
    void Update()
    {
        if (gameController.IsBarOverlapping(rectTransform))
        {
            button.interactable = true;
        }
        else
        {
            button.interactable = false;
        }
    }

    public void SetDigit(int value)
    {
        digitValue = value;
        digitImage.sprite = digitSprites[digitValue]; // Update the image
    }

    public void OnClick()
    {
        gameController.TryInputDigit(digitValue);
    }
}
