using UnityEngine;
using Cysharp.Threading.Tasks;   // Для UniTask
using DG.Tweening;
using TMPro;              // Для DoTween (если потребуется анимация)

public class TimerController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText; // Если используете TextMeshPro, то TMP_Text

    private float elapsedTime;
    private bool isTimerRunning;

    private void Start()
    {
        // Запускаем таймер при старте
        StartTimer();
    }

    /// <summary>
    /// Запуск таймера (при желании можно вызывать из другого места)
    /// </summary>
    public void StartTimer()
    {
        if (isTimerRunning) return;  // Чтобы не запустить дважды

        isTimerRunning = true;
        TimerRoutine().Forget();     // Запускаем асинхронную рутину без await
    }

    /// <summary>
    /// Остановка таймера (если вдруг понадобится)
    /// </summary>
    public void StopTimer()
    {
        isTimerRunning = false;
    }

    /// <summary>
    /// Основная корутина (через UniTask), которая считает время и обновляет текст
    /// </summary>
    private async UniTaskVoid TimerRoutine()
    {
        while (isTimerRunning)
        {
            // Прибавляем прошедшее время
            elapsedTime += Time.deltaTime;

            // Форматируем время
            var minutes = Mathf.FloorToInt(elapsedTime / 60f);
            var seconds = Mathf.FloorToInt(elapsedTime % 60f);

            // Обновляем текст на UI
            timerText.text = $"{minutes:00}:{seconds:00}";

            // Если хотите добавить анимацию — например, лёгкую пульсацию текста:
            // AnimateTimerText();

            // Ждём следующий кадр
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }

    /// <summary>
    /// Пример анимации текста через DoTween
    /// </summary>
    private void AnimateTimerText()
    {
        // Сбрасываем анимации, чтобы не наслаивались друг на друга
        timerText.transform.DOKill();

        // Небольшая “пульсация” текста: увеличиваем и возвращаем обратно
        timerText.transform.DOScale(1.2f, 0.15f).SetLoops(2, LoopType.Yoyo);
    }
}
