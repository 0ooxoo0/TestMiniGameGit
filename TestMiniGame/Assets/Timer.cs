using UnityEngine;
using Cysharp.Threading.Tasks;   // ��� UniTask
using DG.Tweening;
using TMPro;              // ��� DoTween (���� ����������� ��������)

public class TimerController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText; // ���� ����������� TextMeshPro, �� TMP_Text

    private float elapsedTime;
    private bool isTimerRunning;

    private void Start()
    {
        // ��������� ������ ��� ������
        StartTimer();
    }

    /// <summary>
    /// ������ ������� (��� ������� ����� �������� �� ������� �����)
    /// </summary>
    public void StartTimer()
    {
        if (isTimerRunning) return;  // ����� �� ��������� ������

        isTimerRunning = true;
        TimerRoutine().Forget();     // ��������� ����������� ������ ��� await
    }

    /// <summary>
    /// ��������� ������� (���� ����� �����������)
    /// </summary>
    public void StopTimer()
    {
        isTimerRunning = false;
    }

    /// <summary>
    /// �������� �������� (����� UniTask), ������� ������� ����� � ��������� �����
    /// </summary>
    private async UniTaskVoid TimerRoutine()
    {
        while (isTimerRunning)
        {
            // ���������� ��������� �����
            elapsedTime += Time.deltaTime;

            // ����������� �����
            var minutes = Mathf.FloorToInt(elapsedTime / 60f);
            var seconds = Mathf.FloorToInt(elapsedTime % 60f);

            // ��������� ����� �� UI
            timerText.text = $"{minutes:00}:{seconds:00}";

            // ���� ������ �������� �������� � ��������, ����� ��������� ������:
            // AnimateTimerText();

            // ��� ��������� ����
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }

    /// <summary>
    /// ������ �������� ������ ����� DoTween
    /// </summary>
    private void AnimateTimerText()
    {
        // ���������� ��������, ����� �� ������������ ���� �� �����
        timerText.transform.DOKill();

        // ��������� ����������� ������: ����������� � ���������� �������
        timerText.transform.DOScale(1.2f, 0.15f).SetLoops(2, LoopType.Yoyo);
    }
}
