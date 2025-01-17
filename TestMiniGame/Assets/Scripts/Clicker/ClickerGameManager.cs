public class ClickerGameManager : MonoBehaviour
{
    [SerializeField] private Image clickerImage;
    [SerializeField] private Text clickerCountText;

    private int clickCount;
    private const string CLICK_COUNT_KEY = "ClickCount";

    private async void Start()
    {
        // �������� ������� �� Addressables
        // ��������, "ClickerSprite" � ��� ��� Addressable-�������
        Sprite sprite = await LoadingManager.LoadSpriteAsync("ClickerSprite");
        clickerImage.sprite = sprite;

        // ��������������� �� PlayerPrefs (��� ����� ���� SaveManager)
        clickCount = PlayerPrefs.GetInt(CLICK_COUNT_KEY, 0);
        UpdateClickCountText();
    }

    public void OnClickerPressed()
    {
        clickCount++;
        UpdateClickCountText();
        // ���������
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
