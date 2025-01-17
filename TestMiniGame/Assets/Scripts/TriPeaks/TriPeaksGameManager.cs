using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class TriPeaksGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform stockPosition;   // �����, ��� ����� ������ (Stock)
    [SerializeField] private Transform wastePosition;   // �����, ���� ���������� ����� (Waste)
    [SerializeField] private Transform[] peakPositions; // ������ ������� ��� 3 ����� (��� ������������ GameObject���)
    [SerializeField] private CardView cardPrefab;       // ������ �������� (UI ��� 3D, � ����������� �� �������)

    private List<CardModel> _deck;
    private Stack<CardModel> _stockModels = new Stack<CardModel>();
    private List<CardView> _stockViews = new List<CardView>();

    private Stack<CardModel> _wasteModels = new Stack<CardModel>();
    private CardView _topWasteCardView;

    // �������� (������ ����� � ���� �������)
    // ��� �������� �����������, ��� � ��� 3 * 10 = 30 ���� �� ���� (�� ������������ TriPeaks ������� 28 ����;
    // �� ������� �� ������ ���������).  
    private List<CardModel> _peakModels = new List<CardModel>();
    private List<CardView> _peakViews = new List<CardView>();

    private bool _isGameActive;
    private float _timeElapsed;
    private int _movesCount;

    [Header("Gameplay Settings")]
    [SerializeField] private float cardOffset = 0.3f; // �������� ���� ���� ������������ �����

    // ������ �� UI (������, ����) ����� ������� � TriPeaksUI.cs
    [SerializeField] private TriPeaksUI triPeaksUI;

    private void Start()
    {
        StartGame().Forget();
    }

    private void Update()
    {
        if (_isGameActive)
        {
            _timeElapsed += Time.deltaTime;
            triPeaksUI.UpdateTimer(_timeElapsed);
        }
    }

    private async UniTaskVoid StartGame()
    {
        _timeElapsed = 0f;
        _movesCount = 0;
        _isGameActive = true;

        // 1. ������� � ���������� ������
        _deck = DeckGenerator.CreateShuffledDeck();

        // 2. ������� ����� � ����
        // ��������, ����� ������ 28 ���� ��� ��� ���� (��� 30, �������� �� ����� ���������)
        int peakCardCount = 28; // ��������
        for (int i = 0; i < peakCardCount; i++)
        {
            _peakModels.Add(_deck[i]);
        }

        // ��������� � � Stock
        for (int i = peakCardCount; i < _deck.Count; i++)
        {
            _stockModels.Push(_deck[i]);
        }

        // 3. ���������� ����� Stock (� ���� ������)
        // (������������� ���������� ��� ����� ������, ����� ���� ���������� �������)
        await CreateStockViews();

        // 4. ��������� ���� (�������� TriPeaks)
        await LayoutPeaks();

        // 5. ���������� Waste ����
        _wasteModels.Clear();
        _topWasteCardView = null;

        triPeaksUI.UpdateMoves(_movesCount);
    }

    // �������� ���������� ���� � ������ (Stock)
    private async UniTask CreateStockViews()
    {
        // ����� ������� ���� "��������" ����� � ��������, ����� ����������, ��� ���� ������
        // ��� ������� ��� �����, �� �������� �� ���� �� �����
        foreach (var cardModel in _stockModels)
        {
            CardView view = Instantiate(cardPrefab, stockPosition.position, Quaternion.identity, stockPosition);
            view.transform.localPosition = Vector3.zero; // ��� � ����� �����
            await view.Initialize(cardModel);
            // ����� ��������� �������� �����:
            if (cardModel.IsFaceUp)
                await view.FlipCard(false, 0f); // ��� �������� (������������� � �������)

            _stockViews.Add(view);
        }
    }

    // ��������� 3 ���� (���������� ������)
    private async UniTask LayoutPeaks()
    {
        // ��� ����������� �������� 28 ����: 10 � ������ ���, 10 �� ������, 8 � ������ (��� �� �������� 3*10 = 30).
        // ������������ ����� TriPeaks ������������� ������������� �����. 
        // ���� � ���� ���������������� �������.

        int index = 0;
        for (int p = 0; p < peakPositions.Length; p++)
        {
            // ������� ���� ����� � �������� p?
            int cardsInThisPeak = (p < 2) ? 10 : 8; // ������
            for (int c = 0; c < cardsInThisPeak; c++)
            {
                var model = _peakModels[index];
                var view = Instantiate(cardPrefab, peakPositions[p].position, Quaternion.identity, peakPositions[p]);
                await view.Initialize(model);

                // ������� �� ����� � ������ ���� ��� ������ ����� ������ ���?
                // �����������, ������ ������ ��� ������
                bool isBottomRow = (c >= cardsInThisPeak - 4); // ��������� 4 ����� � ������ ���
                model.IsFaceUp = isBottomRow;
                if (!isBottomRow)
                {
                    await view.FlipCard(false, 0f);
                }

                // ������� ������ ����� ���� ������/����/����
                // ��� ����� ���������� ������
                view.transform.localPosition += new Vector3(c * cardOffset, -c * cardOffset, 0f);

                _peakViews.Add(view);
                index++;
            }
        }
    }

    // ���������� ��� ������� �� ����� � ����� ��� �� ������� ����� WASTE,
    // ����� ���������, ����� �� �����������
    public async UniTask TryMoveCardFromPeak(CardView cardView)
    {
        // ���� ����� �� �������, ����������
        if (!cardView.CardModel.IsFaceUp) return;

        // ���� Waste ����, ����� �������� ����� �������� �����
        if (_wasteModels.Count == 0)
        {
            await MoveCardToWaste(cardView);
        }
        else
        {
            // �������� �� ������� � Waste
            CardModel topWasteCard = _wasteModels.Peek();
            if (topWasteCard.CanPlaceOnTop(cardView.CardModel))
            {
                await MoveCardToWaste(cardView);
            }
        }
    }

    private async UniTask MoveCardToWaste(CardView cardView)
    {
        _movesCount++;
        triPeaksUI.UpdateMoves(_movesCount);

        // ������� �� �������� (��� �������)
        _peakViews.Remove(cardView);
        _peakModels.Remove(cardView.CardModel);

        // ��������� � Waste
        _wasteModels.Push(cardView.CardModel);
        _topWasteCardView = cardView;

        // ��������� ����������� � ������� Waste
        await cardView.MoveTo(wastePosition.position, 0.5f, Ease.OutCubic);

        // ��������� ��� Waste � ��������, ����� ����� "������" �� ����� ���������
        cardView.transform.SetParent(wastePosition);

        // ��������� ������ �����, ������� ������ ����� ����������
        CheckAndFlipNewlyOpenedCards();

        // ��������� ������
        if (_peakModels.Count == 0)
        {
            // ������
            _isGameActive = false;
            triPeaksUI.ShowWinMessage();
        }
    }

    // ���������� �������� � �������, � ����� ���� �� �������� "�����������" ����
    // ���������� ������ � ������� �� ����, ��� ������ ��������� ����
    private void CheckAndFlipNewlyOpenedCards()
    {
        // � �������� TriPeaks ���� ��������� ����� �����.  
        // ��������, ����� ��������� ���������, ���� ������ �� (� ��������) ��� ������ ����.
        // ���� � ������ "������������" ������: ���� � p��� ��� child'��, ������� �����.

        // � ����-������� ��������� ��������� ������.
    }

    // ����� ����� �� Stock, ���� ��� �����
    public async UniTask TakeCardFromStock()
    {
        if (_stockModels.Count == 0) return; // ��� ������ ����

        _movesCount++;
        triPeaksUI.UpdateMoves(_movesCount);

        // ���� ������� (��������� �����������) �����
        CardModel topCard = _stockModels.Pop();
        // ������� � View
        CardView topView = _stockViews[_stockViews.Count - 1];
        _stockViews.Remove(topView);

        // �������������� �����
        if (!topCard.IsFaceUp)
        {
            await topView.FlipCard(true, 0.2f);
        }

        // ����� � Waste
        _wasteModels.Push(topCard);
        _topWasteCardView = topView;

        // �������� �����������
        await topView.MoveTo(wastePosition.position, 0.5f, Ease.OutCubic);
        topView.transform.SetParent(wastePosition);
    }

    #region UI Buttons

    // �������� �� Stock (���� ����� ����� �����)
    public async void OnClickStock()
    {
        await TakeCardFromStock();
    }

    public async void OnClickBackToMenu()
    {
        _isGameActive = false;
        // �������� ����� "MainMenu" (��� ����� ������) � ������� UniTask
        await SceneManager.LoadSceneAsync("MainMenu");
    }

    public async void OnClickGoToClicker()
    {
        _isGameActive = false;
        await SceneManager.LoadSceneAsync("Clicker");
    }

    #endregion
}
