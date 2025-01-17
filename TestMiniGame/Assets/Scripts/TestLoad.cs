// Пример использования в любом вашем MonoBehaviour
public class TestLoad : MonoBehaviour
{
    [SerializeField] private string spriteAddress = "MySpriteKey";
    [SerializeField] private string prefabAddress = "MyPrefabKey";
    [SerializeField] private Vector3 spawnPosition;

    private GameObject _spawnedPrefab;

    private async void Start()
    {
        // Загрузка спрайта
        Sprite sprite = await LoadingManager.LoadSpriteAsync(spriteAddress);
        // Применим куда-нибудь, например, в UI Image
        GetComponent<UnityEngine.UI.Image>().sprite = sprite;

        // Инстанцируем префаб
        _spawnedPrefab = await LoadingManager.InstantiateAsync(prefabAddress, spawnPosition, Quaternion.identity);
    }

    private void OnDestroy()
    {
        // Освобождаем экземпляр
        if (_spawnedPrefab != null)
        {
            LoadingManager.ReleaseInstance(_spawnedPrefab);
        }
    }
}
