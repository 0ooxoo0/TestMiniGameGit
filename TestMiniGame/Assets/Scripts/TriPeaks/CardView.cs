using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CardView : MonoBehaviour
{
    [SerializeField] private Image cardImage;      // UI-Image ��� ����������� ������� �������
    [SerializeField] private Image cardBackImage;  // UI-Image ��� ������� (����� ��������/����������)

    public CardModel CardModel { get; private set; }

    // ������������� ����
    public async UniTask Initialize(CardModel model)
    {
        CardModel = model;

        // �������� ������� ������� ������� �� Addressables
        // AddressKey, ��������, "Card_AceHearts" (��� ����� ������)
        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(model.AddressKey);
        Sprite frontSprite = await handle.ToUniTask();

        cardImage.sprite = frontSprite;

        // �������� ��������� (�������/�������)
        UpdateFaceState();
    }

    // ���������, ����� ������� ��������
    private void UpdateFaceState()
    {
        bool isFaceUp = CardModel.IsFaceUp;
        cardImage.gameObject.SetActive(isFaceUp);
        cardBackImage.gameObject.SetActive(!isFaceUp);
    }

    // ����������� ����� � ���������
    public async UniTask FlipCard(bool faceUp, float duration = 0.2f)
    {
        // �������� "�����������" �� X
        await transform
            .DOScaleX(0f, duration)
            .SetEase(Ease.Linear)
            .AsyncWaitForCompletion();

        CardModel.IsFaceUp = faceUp;
        UpdateFaceState();

        // �������� �������� "���������"
        await transform
            .DOScaleX(1f, duration)
            .SetEase(Ease.Linear)
            .AsyncWaitForCompletion();
    }

    // ����������� � ��������� ������� � ���������
    public async UniTask MoveTo(Vector3 targetPos, float duration = 0.5f, Ease easeType = Ease.OutCubic)
    {
        await transform.DOMove(targetPos, duration).SetEase(easeType).AsyncWaitForCompletion();
    }
}
