// ������ ������������� � ����� ����� MonoBehaviour
public class TestLoad : MonoBehaviour
{
    [SerializeField] private string spriteAddress = "MySpriteKey";
    [SerializeField] private string prefabAddress = "MyPrefabKey";
    [SerializeField] private Vector3 spawnPosition;

    private GameObject _spawnedPrefab;

    private async void Start()
    {
        // �������� �������
        Sprite sprite = await LoadingManager.LoadSpriteAsync(spriteAddress);
        // �������� ����-������, ��������, � UI Image
        GetComponent<UnityEngine.UI.Image>().sprite = sprite;

        // ������������ ������
        _spawnedPrefab = await LoadingManager.InstantiateAsync(prefabAddress, spawnPosition, Quaternion.identity);
    }

    private void OnDestroy()
    {
        // ����������� ���������
        if (_spawnedPrefab != null)
        {
            LoadingManager.ReleaseInstance(_spawnedPrefab);
        }
    }
}
