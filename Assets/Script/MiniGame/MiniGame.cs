using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniGame : MonoBehaviour
{
    public Text[] targetDigits; // Assign these in the inspector (4 Texts for the 4-digit code)
    private string targetCode;
    public RectTransform bar;
    public float moveSpeed;
    private float startX, endX;
    private bool movingRight = true;
    public Button[] numberButtons;
    public Sprite[] numberSprites;
    public List<DigitButton> digitButtons;
    public List<Image> answerImages; 
    private List<int> currentAnswerDigits;
    public Sprite[] digitSprites;
    private int currentAnswerIndex = 0;
    public MiniGameActivate tubeReference;
    private player playerScript;
    private GameObject player;

    [Header("Timer Settings")]
    public Slider timeSlider;
    public float maxTime; // total time to complete the mini-game
    private float remainingTime;
    private bool isGameActive = true;

    // Start is called before the first frame update
    void Start()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerScript = playerObject.GetComponent<player>();
        }
        if (playerScript != null)
        {
            Debug.Log("hv script");
        }
        startX = -550;
        endX = 490;
        RandomizeDigits();
        RandomizeAnswerField();
        Debug.Log(playerScript.ReturnCrystal());
        if (playerScript.ReturnCrystal() != null)
        {
            Debug.Log("TEST2");
            GameObject crystal = playerScript.ReturnCrystal();
            Debug.Log("crystal" + crystal.name);
            tubeReference = crystal.GetComponent<MiniGameActivate>();
        }
        else 
        {
            Debug.Log("have bug");
        }

        remainingTime = maxTime;
        timeSlider.maxValue = maxTime;
        timeSlider.value = maxTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (isGameActive)
        {
            Vector2 pos = bar.anchoredPosition;

            if (movingRight)
            {
                pos.x += moveSpeed * Time.deltaTime;
                if (pos.x >= endX) movingRight = false;
            }
            else
            {
                pos.x -= moveSpeed * Time.deltaTime;
                if (pos.x <= startX) movingRight = true;
            }

            bar.anchoredPosition = pos;

            remainingTime -= Time.deltaTime;
            timeSlider.value = remainingTime;

            if (remainingTime <= 0)
            {
                Debug.Log("⏰ Time's up! Mini-game failed.");
                isGameActive = false;
                OnMiniGameFailed();
            }
        }
    }

    void RandomizeDigits()
    {
        // Create a list of digits 0–9
        List<int> digits = new List<int>();
        for (int i = 0; i < 10; i++) digits.Add(i);

        // Shuffle the digits
        for (int i = 0; i < digits.Count; i++)
        {
            int randIndex = Random.Range(i, digits.Count);
            int temp = digits[i];
            digits[i] = digits[randIndex];
            digits[randIndex] = temp;
        }

        // Assign digits to each button
        for (int i = 0; i < digitButtons.Count; i++)
        {
            digitButtons[i].SetDigit(digits[i]);
        }
    }

    void RandomizeAnswerField()
    {
        currentAnswerDigits = new List<int>();

        for (int i = 0; i < answerImages.Count; i++)
        {
            int randDigit = Random.Range(0, 10);
            currentAnswerDigits.Add(randDigit);
            answerImages[i].sprite = digitSprites[randDigit];
        }
        currentAnswerIndex = 0;

        Debug.Log("Answer sequence: " + string.Join("", currentAnswerDigits));
    }

    public bool IsBarOverlapping(RectTransform buttonRect)
    {
        Rect barRect = GetWorldRect(bar);
        Rect buttonWorldRect = GetWorldRect(buttonRect);
        return barRect.Overlaps(buttonWorldRect);
    }
    private Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector2 size = corners[2] - corners[0];
        return new Rect(corners[0], size);
    }

    public void TryInputDigit(int inputDigit)
    {
        if (currentAnswerIndex >= currentAnswerDigits.Count) return;

        int expected = currentAnswerDigits[currentAnswerIndex];
        if (inputDigit == expected)
        {
            Debug.Log("✅ Correct! " + inputDigit);
            answerImages[currentAnswerIndex].color = new Color32(115,164,126,225);
            currentAnswerIndex++;

            if (currentAnswerIndex >= currentAnswerDigits.Count)
            {
                Debug.Log("🎉 Mini-game Completed!");
                // TODO: trigger success effect
                OnMiniGameSuccess();
            }
        }
        else
        {
            Debug.Log("❌ Wrong digit! Try again.");
            StartCoroutine(ShakeAnswerSlot(answerImages[currentAnswerIndex]));
            // Optionally reset: currentAnswerIndex = 0;
        }
    }

    public void OnMiniGameSuccess()
    {
        isGameActive = false;
        tubeReference.CompleteMinigame();
        // Re-enable player control if needed
        playerScript.SetControl();
        playerScript.SetMiniGame();
    }

    public void OnMiniGameFailed()
    {
        isGameActive = false;
        tubeReference.FailedMinigame();
        playerScript.SetControl();
        playerScript.SetMiniGame();
    }

    IEnumerator ShakeAnswerSlot(Image image, float duration = 0.3f, float strength = 10f)
    {
        RectTransform rt = image.GetComponent<RectTransform>();
        Vector3 originalPosition = rt.anchoredPosition;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * strength;
            rt.anchoredPosition = originalPosition + new Vector3(x, 0, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rt.anchoredPosition = originalPosition;
    }
}
