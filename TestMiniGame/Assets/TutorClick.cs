using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class TutorClick : MonoBehaviour
{
    private CancellationTokenSource _cts;
    Animator anim;
    Image image;

    public async void Start()
    {
        image = GetComponent<Image>();
        anim = GetComponent<Animator>();
        // Создаем токен-источник для отмены
        _cts = new CancellationTokenSource();
        await WaitTenSeconds(_cts.Token);
    }

    public async UniTask WaitTenSeconds(CancellationToken token)
    {
        await UniTask.Delay(10_000, cancellationToken: token);

        image.color = new Vector4(image.color.r, image.color.b, image.color.g, 0.7f);
        anim.SetBool("Tutor", true);
    }
    public async void StopWaiting()
    {
        image.color = new Vector4(image.color.r, image.color.b, image.color.g, 0);
        anim.SetBool("Tutor", false);
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
        _cts = new CancellationTokenSource();
        await WaitTenSeconds(_cts.Token);
    }

    private void OnDestroy()
    {
        anim.SetBool("Tutor", false);
        // При уничтожении объекта тоже отменяем, если ещё не отменили
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
        _cts.Dispose();
    }
}
