using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TriPeaksUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI movesText;
    [SerializeField] private GameObject winPanel;


    public void UpdateMoves(int moves)
    {
        movesText.text = $"|| Moves: {moves}";
    }

    public void ShowWinMessage()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }
    }
}
