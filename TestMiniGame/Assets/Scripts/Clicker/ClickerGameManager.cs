public class ClickerGameManager : MonoBehaviour
{
    [SerializeField] private Image clickerImage;
    [SerializeField] private Text clickerCountText;

    private int clickCount;
    private const string CLICK_COUNT_KEY = "ClickCount";

    private async void Start()
    {
        // Загрузка спрайта из Addressables
        // Допустим, "ClickerSprite" — это имя Addressable-ресурса
        Sprite sprite = await LoadingManager.LoadSpriteAsync("ClickerSprite");
        clickerImage.sprite = sprite;

        // Восстанавливаем из PlayerPrefs (или через свой SaveManager)
        clickCount = PlayerPrefs.GetInt(CLICK_COUNT_KEY, 0);
        UpdateClickCountText();
    }

    public void OnClickerPressed()
    {
        clickCount++;
        UpdateClickCountText();
        // Сохраняем
        PlayerPrefs.SetInt(CLICK_COUNT_KEY, clickCount);
        PlayerPrefs.Save();
    }

    private void UpdateClickCountText()
    {
        clickerCountText.text = $"Clicks: {clickCount}";
    }

    public async void OnBackToMenuPressed()
    {
        await GameFlowManager.LoadScene(GameFlowManager.GameScene.MainMenu);
    }

    public async void OnGoToTriPeaksPressed()
    {
        await GameFlowManager.LoadScene(GameFlowManager.GameScene.TriPeaks);
    }
}
