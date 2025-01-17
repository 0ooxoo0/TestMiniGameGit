using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button clickerButton;
    [SerializeField] private Button triPeaksButton;

    private void Start()
    {
        clickerButton.onClick.AddListener(() => OnClickerButtonPressed().Forget());
        triPeaksButton.onClick.AddListener(() => OnTriPeaksButtonPressed().Forget());
    }

    private async UniTaskVoid OnClickerButtonPressed()
    {
        await GameFlowManager.LoadScene(GameFlowManager.GameScene.Clicker);
    }

    private async UniTaskVoid OnTriPeaksButtonPressed()
    {
        await GameFlowManager.LoadScene(GameFlowManager.GameScene.TriPeaks);
    }
}
