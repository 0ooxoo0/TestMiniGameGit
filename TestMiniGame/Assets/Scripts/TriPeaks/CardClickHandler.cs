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
        // ����� ����� TriPeaksGameManager �� �����
        _gameManager = FindObjectOfType<TriPeaksGameManager>();
    }

    public async void OnPointerClick(PointerEventData eventData)
    {
        // ���� ��� ������������� ����� �� ����
        // � �������� ������� ���-�� ����������, ��� ��� ����� ������ �� ����, � �� � Waste
        if (_gameManager != null && _cardView != null)
        {
            await _gameManager.TryMoveCardFromPeak(_cardView);
        }
    }
}
