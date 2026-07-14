using System;

namespace CGame
{
    public sealed class LocalPlayerCameraTargetBinding : IDisposable
    {
        private readonly CharacterSpawnManager spawnManager;
        private readonly FirstPersonCameraBinding binding;
        private readonly LocalOwnerWorldBodyVisibility ownerWorldBodyVisibility;
        private CharacterRuntimeId boundRuntimeId;
        private bool hasBoundRuntime;
        private bool isDisposed;

        public LocalPlayerCameraTargetBinding(
            CharacterSpawnManager spawnManager,
            FirstPersonCameraBinding binding)
            : this(spawnManager, binding, null)
        {
        }

        public LocalPlayerCameraTargetBinding(
            CharacterSpawnManager spawnManager,
            FirstPersonCameraBinding binding,
            LocalOwnerWorldBodyVisibility ownerWorldBodyVisibility)
        {
            this.spawnManager = spawnManager ?? throw new ArgumentNullException(nameof(spawnManager));
            this.binding = binding ?? throw new ArgumentNullException(nameof(binding));
            this.ownerWorldBodyVisibility = ownerWorldBodyVisibility;
            this.spawnManager.CharacterReady += HandleCharacterReady;
            this.spawnManager.CharacterReleasing += HandleCharacterReleasing;
        }

        public FirstPersonCameraBindResult LastResult { get; private set; } = FirstPersonCameraBindResult.InvalidCharacter;

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            spawnManager.CharacterReady -= HandleCharacterReady;
            spawnManager.CharacterReleasing -= HandleCharacterReleasing;
            ownerWorldBodyVisibility?.Unbind();
            binding.Unbind();
            hasBoundRuntime = false;
            boundRuntimeId = default;
        }

        private void HandleCharacterReady(CharacterRuntimeId runtimeId)
        {
            if (!spawnManager.TryGetCharacterView(runtimeId, out ICharacterView view))
            {
                LastResult = FirstPersonCameraBindResult.InvalidCharacter;
                return;
            }

            LastResult = binding.BindCharacter(view.Transform);
            if (LastResult != FirstPersonCameraBindResult.Bound)
            {
                return;
            }

            if (ownerWorldBodyVisibility != null && !ownerWorldBodyVisibility.BindCharacter(view.Transform))
            {
                binding.Unbind();
                LastResult = FirstPersonCameraBindResult.InvalidCharacter;
                return;
            }

            boundRuntimeId = runtimeId;
            hasBoundRuntime = true;
        }

        private void HandleCharacterReleasing(CharacterRuntimeId runtimeId, CharacterDespawnReason reason)
        {
            if (!hasBoundRuntime || boundRuntimeId != runtimeId)
            {
                return;
            }

            ownerWorldBodyVisibility?.Unbind();
            binding.Unbind();
            hasBoundRuntime = false;
            boundRuntimeId = default;
        }
    }
}
