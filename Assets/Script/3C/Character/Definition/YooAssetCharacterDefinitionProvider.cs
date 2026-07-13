using System;
using System.Collections.Generic;

namespace CGame
{
    public sealed class YooAssetCharacterDefinitionProvider : ICharacterDefinitionProvider
    {
        private const string LocalPlayerDefinitionLocation = "CharacterDefinition";
        private readonly ICharacterDefinitionAssetLoader assetLoader;
        private readonly Dictionary<CharacterDefinitionId, string> locations;

        public YooAssetCharacterDefinitionProvider(ICharacterDefinitionAssetLoader assetLoader)
            : this(
                assetLoader,
                new[]
                {
                    new KeyValuePair<CharacterDefinitionId, string>(
                        new CharacterDefinitionId("local-player"),
                        LocalPlayerDefinitionLocation),
                })
        {
        }

        public YooAssetCharacterDefinitionProvider(
            ICharacterDefinitionAssetLoader assetLoader,
            IEnumerable<KeyValuePair<CharacterDefinitionId, string>> locations)
        {
            this.assetLoader = assetLoader ?? throw new ArgumentNullException(nameof(assetLoader));
            this.locations = new Dictionary<CharacterDefinitionId, string>();
            if (locations == null)
            {
                throw new ArgumentNullException(nameof(locations));
            }

            foreach (KeyValuePair<CharacterDefinitionId, string> location in locations)
            {
                if (!location.Key.IsValid || string.IsNullOrWhiteSpace(location.Value))
                {
                    throw new ArgumentException("Definition locations require a valid ID and location.", nameof(locations));
                }

                this.locations.Add(location.Key, location.Value);
            }
        }

        public ICharacterDefinitionResolveOperation BeginResolve(CharacterDefinitionId definitionId)
        {
            if (!definitionId.IsValid)
            {
                return CharacterDefinitionResolveOperation.Completed(
                    new CharacterDefinitionResolveResult((CharacterDefinition)null, CharacterDefinitionResolveError.InvalidDefinitionId));
            }

            if (!locations.TryGetValue(definitionId, out string location))
            {
                return CharacterDefinitionResolveOperation.Completed(
                    new CharacterDefinitionResolveResult((CharacterDefinition)null, CharacterDefinitionResolveError.DefinitionNotFound));
            }

            try
            {
                return new YooAssetCharacterDefinitionResolveOperation(
                    definitionId,
                    assetLoader.BeginLoad(location));
            }
            catch
            {
                return CharacterDefinitionResolveOperation.Completed(
                    new CharacterDefinitionResolveResult((CharacterDefinition)null, CharacterDefinitionResolveError.AssetLoadFailed));
            }
        }

        public CharacterDefinitionResolveResult Resolve(CharacterDefinitionId definitionId)
        {
            throw new NotSupportedException("YooAsset definition loading is asynchronous. Use BeginResolve instead.");
        }

        private sealed class YooAssetCharacterDefinitionResolveOperation : ICharacterDefinitionResolveOperation
        {
            private readonly CharacterDefinitionId expectedId;
            private ICharacterDefinitionAssetLoadOperation loadOperation;
            private CharacterDefinitionResolveResult result;
            private bool isCompleted;
            private bool isDisposed;

            public YooAssetCharacterDefinitionResolveOperation(
                CharacterDefinitionId expectedId,
                ICharacterDefinitionAssetLoadOperation loadOperation)
            {
                this.expectedId = expectedId;
                this.loadOperation = loadOperation ?? throw new InvalidOperationException("Asset loader returned no operation.");
            }

            public bool IsCompleted
            {
                get
                {
                    TryComplete();
                    return isCompleted;
                }
            }

            public CharacterDefinitionResolveResult Result
            {
                get
                {
                    TryComplete();
                    if (!isCompleted)
                    {
                        throw new InvalidOperationException("Definition resolve has not completed.");
                    }

                    return result;
                }
            }

            public void Dispose()
            {
                if (isDisposed)
                {
                    return;
                }

                isDisposed = true;
                if (isCompleted)
                {
                    result.Lease?.Dispose();
                }
                else
                {
                    loadOperation?.Dispose();
                    loadOperation = null;
                }
            }

            private void TryComplete()
            {
                if (isCompleted || isDisposed || loadOperation == null || !loadOperation.IsCompleted)
                {
                    return;
                }

                if (!loadOperation.IsSuccessful || loadOperation.Asset == null)
                {
                    loadOperation.Dispose();
                    loadOperation = null;
                    result = new CharacterDefinitionResolveResult(
                        (CharacterDefinition)null,
                        CharacterDefinitionResolveError.AssetLoadFailed);
                    isCompleted = true;
                    return;
                }

                CharacterDefinition definition = loadOperation.Asset;
                CharacterDefinitionResolveError validationError = definition.Validate(expectedId);
                if (validationError != CharacterDefinitionResolveError.None)
                {
                    loadOperation.Dispose();
                    loadOperation = null;
                    result = new CharacterDefinitionResolveResult((CharacterDefinition)null, validationError);
                    isCompleted = true;
                    return;
                }

                ICharacterDefinitionAssetLoadOperation ownedLoadOperation = loadOperation;
                loadOperation = null;
                result = new CharacterDefinitionResolveResult(
                    new ResolvedCharacterDefinitionLease(definition, ownedLoadOperation.Dispose),
                    CharacterDefinitionResolveError.None);
                isCompleted = true;
            }
        }
    }
}
