using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

public static class LoadingManager
{
    public static async UniTask<Sprite> LoadSpriteAsync(string address)
    {
        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(address);
        Sprite loadedSprite = await handle.ToUniTask();
        return loadedSprite;
    }
    // Загрузка и инстанцирование префаба
    public static async UniTask<GameObject> InstantiateAsync(string address, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, position, rotation, parent);
        GameObject obj = await handle.ToUniTask();
        return obj;
    }

    // Освобождение префаба
    public static void ReleaseInstance(GameObject obj)
    {
        Addressables.ReleaseInstance(obj);
    }
}
