using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;

public class CardClickHandler : MonoBehaviour, IPointerClickHandler
{
    private CardView _cardView;
    private TriPeaksGameManager _gameManager;

    private void Awake()
    {
        _cardView = GetComponent<CardView>();
        // Можно найти TriPeaksGameManager по сцене
        _gameManager = FindObjectOfType<TriPeaksGameManager>();
    }

    public async void OnPointerClick(PointerEventData eventData)
    {
        // Если это действительно карта из пика
        // В реальном проекте как-то определяем, что эта карта именно на поле, а не в Waste
        if (_gameManager != null && _cardView != null)
        {
            await _gameManager.TryMoveCardFromPeak(_cardView);
        }
    }
}
