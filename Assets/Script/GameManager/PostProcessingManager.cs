using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingManager : MonoBehaviour
{
    public static PostProcessingManager Instance;
    private Volume volume;
    private Vignette vignette;
    bool isEffect;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }
    void Start()
    {
        volume = GetComponent<Volume>();

        if (volume.profile.TryGet<Vignette>(out vignette))
        {
            Debug.Log("Bloom component found.");
        }
        else
        {
            Debug.LogWarning("Bloom component not found in the Volume Profile.");
        }

    }

    public void HurtEffect()
    {
        if(isEffect)
        {
            return;
        }
        StartCoroutine(ChangeVignetteIntensity(0.5f, 0.3f, 0.2f)); 
    }

    public void UpdateEffect(int currHealth, int maxHealth)
    {
        Debug.Log((1- currHealth / maxHealth) * 0.5f);
        ChangeVignetteIntensity( (1- (float) currHealth / maxHealth) * 0.5f);
    }

    void ChangeVignetteIntensity(float val)
    {
        if(isEffect)    return;
        vignette.intensity.Override(val);
    }

    IEnumerator ChangeVignetteIntensity(float targetIntensity, float duration, float holdDuration)
    {
        isEffect = true;
        
        if (vignette != null)
        {
            float initialIntensity = vignette.intensity.value;
            float elapsedTime = 0f;
            targetIntensity = 0.5f + initialIntensity;

            // Increase intensity to target
            while (elapsedTime < duration)
            {
                vignette.intensity.Override(Mathf.Lerp(initialIntensity, targetIntensity, elapsedTime / duration));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure it ends exactly at the target value
            vignette.intensity.Override(targetIntensity);

            // Hold the target intensity
            yield return new WaitForSeconds(holdDuration);

            // Decrease intensity back to 0
            elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                vignette.intensity.Override(Mathf.Lerp(targetIntensity, initialIntensity, elapsedTime / duration));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure it ends exactly at 0
            vignette.intensity.Override(initialIntensity);
            isEffect = false;
        }
    }

}