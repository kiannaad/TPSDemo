using UnityEngine;
using YooAsset;
using System.Threading.Tasks;

/// <summary>
/// 业务接口代理单例（对外统一入口，极简职责）
/// </summary>
public class AssetManager : Singleton<AssetManager>
{
    // 快捷访问默认包裹
    private ResourcePackage Package => ResourceManager.Instance.GetPackage();

    /// <summary>
    /// 加载资源句柄（需要手动释放）
    /// </summary>
    public AssetHandle LoadAsset<T>(string location) where T : Object
    {
        return Package.LoadAssetAsync<T>(location);
    }

    /// <summary>
    /// 同步实例化资源（确保资源已加载或内部等待异步）
    /// </summary>
    public async Task<GameObject> InstantiateAsync(string location, Transform parent = null)
    {
        var handle = LoadAsset<GameObject>(location);
        await handle.Task;
        return handle.InstantiateSync(parent);
    }

    /// <summary>
    /// 加载并返回资源对象
    /// </summary>
    public async Task<T> LoadAssetAsync<T>(string location) where T : Object
    {
        var handle = LoadAsset<T>(location);
        await handle.Task;
        return handle.AssetObject as T;
    }

    /// <summary>
    /// 卸载所有引用计数为0的资源
    /// </summary>
    public void UnloadUnusedAssets()
    {
        Package.UnloadUnusedAssetsAsync();
    }

    /// <summary>
    /// 检查资源定位地址是否有效
    /// </summary>
    public bool CheckLocation(string location)
    {
        return Package.CheckLocationValid(location);
    }
}
