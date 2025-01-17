using UnityEngine;
using UnityEngine.UI;

public class TriPeaksUI : MonoBehaviour
{
    [SerializeField] private Text timerText;
    [SerializeField] private Text movesText;
    [SerializeField] private GameObject winPanel;

    public void UpdateTimer(float time)
    {
        timerText.text = $"Time: {time:F2}";
    }

    public void UpdateMoves(int moves)
    {
        movesText.text = $"Moves: {moves}";
    }

    public void ShowWinMessage()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }
    }
}
