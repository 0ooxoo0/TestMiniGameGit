using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CardView : MonoBehaviour
{
    [SerializeField] private Image cardImage;      // UI-Image дл€ отображени€ текущей стороны
    [SerializeField] private Image cardBackImage;  // UI-Image дл€ рубашки (можно скрывать/показывать)

    public CardModel CardModel { get; private set; }

    // »нициализаци€ вида
    public async UniTask Initialize(CardModel model)
    {
        CardModel = model;

        // «агрузка спрайта лицевой стороны из Addressables
        // AddressKey, например, "Card_AceHearts" (или любой другой)
        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(model.AddressKey);
        Sprite frontSprite = await handle.ToUniTask();

        cardImage.sprite = frontSprite;

        // ќбновить состо€ние (лицева€/рубашка)
        UpdateFaceState();
    }

    // ќбновл€ет, кака€ сторона показана
    private void UpdateFaceState()
    {
        bool isFaceUp = CardModel.IsFaceUp;
        cardImage.gameObject.SetActive(isFaceUp);
        cardBackImage.gameObject.SetActive(!isFaceUp);
    }

    // ѕеревернуть карту с анимацией
    public async UniTask FlipCard(bool faceUp, float duration = 0.2f)
    {
        // јнимаци€ "складывани€" по X
        await transform
            .DOScaleX(0f, duration)
            .SetEase(Ease.Linear)
            .AsyncWaitForCompletion();

        CardModel.IsFaceUp = faceUp;
        UpdateFaceState();

        // ќбратна€ анимаци€ "раскрыти€"
        await transform
            .DOScaleX(1f, duration)
            .SetEase(Ease.Linear)
            .AsyncWaitForCompletion();
    }

    // ѕеремещение к указанной позиции с анимацией
    public async UniTask MoveTo(Vector3 targetPos, float duration = 0.5f, Ease easeType = Ease.OutCubic)
    {
        await transform.DOMove(targetPos, duration).SetEase(easeType).AsyncWaitForCompletion();
    }
}
