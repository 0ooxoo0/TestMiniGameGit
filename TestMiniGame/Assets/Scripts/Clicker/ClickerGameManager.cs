using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ClickerGameManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image clickerImage;
    [SerializeField] private TextMeshProUGUI clickerCountText;

    [Header("Plus Text Prefab")]
    [SerializeField] private GameObject plusTextPrefab;
    // Должен содержать TextMeshProUGUI + CanvasGroup

    private int clickCount;
    private const string CLICK_COUNT_KEY = "ClickCount";

    [Header("Increment Settings")]
    [SerializeField] private int clickIncrement = 1;

    private void Start()
    {
        clickCount = PlayerPrefs.GetInt(CLICK_COUNT_KEY, 0);
        UpdateClickCountText();
    }

    public void OnClickerPressed()
    {
        clickCount += clickIncrement;
        UpdateClickCountText();

        PlayerPrefs.SetInt(CLICK_COUNT_KEY, clickCount);
        PlayerPrefs.Save();

        ShowPlusText(Input.mousePosition, clickIncrement);
    }

    private void UpdateClickCountText()
    {
        clickerCountText.text = $"{clickCount}";
    }

    private void ShowPlusText(Vector3 screenPosition, int increment)
    {
        // Создаём
        var instance = Instantiate(plusTextPrefab, clickerImage.transform.parent);
        var rect = instance.GetComponent<RectTransform>();

        rect.position = screenPosition;

        Destroy(instance, 0.5f);
    }

}
