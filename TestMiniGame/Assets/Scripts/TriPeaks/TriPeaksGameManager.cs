using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class TriPeaksGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform stockPosition;   // Точка, где лежит колода (Stock)
    [SerializeField] private Transform wastePosition;   // Точка, куда сбрасываем карту (Waste)
    [SerializeField] private Transform[] peakPositions; // Массив позиций для 3 пиков (или родительских GameObject’ов)
    [SerializeField] private CardView cardPrefab;       // Префаб карточки (UI или 3D, в зависимости от проекта)

    private List<CardModel> _deck;
    private Stack<CardModel> _stockModels = new Stack<CardModel>();
    private List<CardView> _stockViews = new List<CardView>();

    private Stack<CardModel> _wasteModels = new Stack<CardModel>();
    private CardView _topWasteCardView;

    // Пирамиды (каждая карта — своя позиция)
    // Для простоты предположим, что у нас 3 * 10 = 30 карт на поле (но классический TriPeaks требует 28 карт;
    // всё зависит от точной раскладки).  
    private List<CardModel> _peakModels = new List<CardModel>();
    private List<CardView> _peakViews = new List<CardView>();

    private bool _isGameActive;
    private float _timeElapsed;
    private int _movesCount;

    [Header("Gameplay Settings")]
    [SerializeField] private float cardOffset = 0.3f; // смещение карт друг относительно друга

    // Ссылки на UI (таймер, ходы) можно вынести в TriPeaksUI.cs
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

        // 1. Создать и перемешать колоду
        _deck = DeckGenerator.CreateShuffledDeck();

        // 2. Раздать карты в пики
        // Допустим, берем первые 28 карт для три пика (или 30, смотрите по вашей раскладке)
        int peakCardCount = 28; // классика
        for (int i = 0; i < peakCardCount; i++)
        {
            _peakModels.Add(_deck[i]);
        }

        // Остальные — в Stock
        for (int i = peakCardCount; i < _deck.Count; i++)
        {
            _stockModels.Push(_deck[i]);
        }

        // 3. Отобразить карты Stock (в виде стопки)
        // (Необязательно отображать все карты стопки, можно лишь показывать рубашку)
        await CreateStockViews();

        // 4. Разложить пики (согласно TriPeaks)
        await LayoutPeaks();

        // 5. Изначально Waste пуст
        _wasteModels.Clear();
        _topWasteCardView = null;

        triPeaksUI.UpdateMoves(_movesCount);
    }

    // Создание визуальных карт в стопке (Stock)
    private async UniTask CreateStockViews()
    {
        // Можно создать одну "фейковую" карту с рубашкой, чтобы показывать, что есть колода
        // Или создать все сразу, но наложить их друг на друга
        foreach (var cardModel in _stockModels)
        {
            CardView view = Instantiate(cardPrefab, stockPosition.position, Quaternion.identity, stockPosition);
            view.transform.localPosition = Vector3.zero; // все в одной точке
            await view.Initialize(cardModel);
            // Сразу перевернём рубашкой вверх:
            if (cardModel.IsFaceUp)
                await view.FlipCard(false, 0f); // без анимации (принудительно в рубашку)

            _stockViews.Add(view);
        }
    }

    // Разложить 3 пика (упрощённый пример)
    private async UniTask LayoutPeaks()
    {
        // Для наглядности разобьём 28 карт: 10 в первый пик, 10 во второй, 8 в третий (или по классике 3*10 = 30).
        // Классическая схема TriPeaks подразумевает специфическую форму. 
        // Ниже — лишь демонстрационный вариант.

        int index = 0;
        for (int p = 0; p < peakPositions.Length; p++)
        {
            // Сколько карт кладём в пирамиду p?
            int cardsInThisPeak = (p < 2) ? 10 : 8; // пример
            for (int c = 0; c < cardsInThisPeak; c++)
            {
                var model = _peakModels[index];
                var view = Instantiate(cardPrefab, peakPositions[p].position, Quaternion.identity, peakPositions[p]);
                await view.Initialize(model);

                // Открыты ли карты в нижнем ряду или только самый нижний ряд?
                // Предположим, только нижний ряд открыт
                bool isBottomRow = (c >= cardsInThisPeak - 4); // последние 4 карты — нижний ряд
                model.IsFaceUp = isBottomRow;
                if (!isBottomRow)
                {
                    await view.FlipCard(false, 0f);
                }

                // Смещаем каждую карту чуть правее/выше/ниже
                // Это чисто визуальный эффект
                view.transform.localPosition += new Vector3(c * cardOffset, -c * cardOffset, 0f);

                _peakViews.Add(view);
                index++;
            }
        }
    }

    // Вызывается при нажатии на карту в пиках или на верхнюю карту WASTE,
    // чтобы проверить, можно ли переместить
    public async UniTask TryMoveCardFromPeak(CardView cardView)
    {
        // Если карта не открыта, пропускаем
        if (!cardView.CardModel.IsFaceUp) return;

        // Если Waste пуст, можно положить любую открытую карту
        if (_wasteModels.Count == 0)
        {
            await MoveCardToWaste(cardView);
        }
        else
        {
            // Сравнить со старшей в Waste
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

        // Удаляем из пирамиды (или массива)
        _peakViews.Remove(cardView);
        _peakModels.Remove(cardView.CardModel);

        // Добавляем в Waste
        _wasteModels.Push(cardView.CardModel);
        _topWasteCardView = cardView;

        // Анимируем перемещение к позиции Waste
        await cardView.MoveTo(wastePosition.position, 0.5f, Ease.OutCubic);

        // Переводим под Waste в иерархии, чтобы карта "лежала" на верху визуально
        cardView.transform.SetParent(wastePosition);

        // Открываем другие карты, которые теперь стали доступными
        CheckAndFlipNewlyOpenedCards();

        // Проверить победу
        if (_peakModels.Count == 0)
        {
            // Победа
            _isGameActive = false;
            triPeaksUI.ShowWinMessage();
        }
    }

    // Перебираем пирамиду и смотрим, у каких карт не осталось "покрывающих" карт
    // Упрощённая логика — зависит от того, как именно выстроены пики
    private void CheckAndFlipNewlyOpenedCards()
    {
        // В реальном TriPeaks надо учитывать форму пиков.  
        // Например, карта считается доступной, если сверху неё (в иерархии) нет других карт.
        // Ниже — пример "сферического" метода: если в pике нет child'ов, открыть карту.

        // В демо-примере пропустим детальную логику.
    }

    // Взять карту из Stock, если нет ходов
    public async UniTask TakeCardFromStock()
    {
        if (_stockModels.Count == 0) return; // нет больше карт

        _movesCount++;
        triPeaksUI.UpdateMoves(_movesCount);

        // Берём верхнюю (последнюю добавленную) карту
        CardModel topCard = _stockModels.Pop();
        // Находим её View
        CardView topView = _stockViews[_stockViews.Count - 1];
        _stockViews.Remove(topView);

        // Переворачиваем карту
        if (!topCard.IsFaceUp)
        {
            await topView.FlipCard(true, 0.2f);
        }

        // Кладём в Waste
        _wasteModels.Push(topCard);
        _topWasteCardView = topView;

        // Анимация перемещения
        await topView.MoveTo(wastePosition.position, 0.5f, Ease.OutCubic);
        topView.transform.SetParent(wastePosition);
    }

    #region UI Buttons

    // Нажимаем на Stock (если хотим брать карту)
    public async void OnClickStock()
    {
        await TakeCardFromStock();
    }

    public async void OnClickBackToMenu()
    {
        _isGameActive = false;
        // Загрузка сцены "MainMenu" (или любая другая) с помощью UniTask
        await SceneManager.LoadSceneAsync("MainMenu");
    }

    public async void OnClickGoToClicker()
    {
        _isGameActive = false;
        await SceneManager.LoadSceneAsync("Clicker");
    }

    #endregion
}
