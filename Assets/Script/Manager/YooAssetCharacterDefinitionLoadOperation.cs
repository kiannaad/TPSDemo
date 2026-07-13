using System;
using YooAsset;

internal sealed class YooAssetCharacterDefinitionLoadOperation : CGame.ICharacterDefinitionAssetLoadOperation
{
    private AssetHandle handle;

    public YooAssetCharacterDefinitionLoadOperation(AssetHandle handle)
    {
        this.handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    public bool IsCompleted => handle == null || handle.IsDone;
    public bool IsSuccessful => handle != null && handle.Status == EOperationStatus.Succeed;
    public CGame.CharacterDefinition Asset => handle?.GetAssetObject<CGame.CharacterDefinition>();
    public string Error => handle?.LastError ?? string.Empty;

    public void Dispose()
    {
        if (handle == null)
        {
            return;
        }

        handle.Release();
        handle = null;
    }
}
