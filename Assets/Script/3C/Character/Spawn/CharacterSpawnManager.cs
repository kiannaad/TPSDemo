using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGame
{
    public sealed class CharacterSpawnManager : IManager
    {
        public const int TerminalRequestCapacity = 128;

        private readonly Dictionary<CharacterSpawnRequestId, CharacterSpawnOperation> operations = new Dictionary<CharacterSpawnRequestId, CharacterSpawnOperation>();
        private readonly Dictionary<CharacterSpawnOperation, ICharacterDefinitionResolveOperation> resolveOperations = new Dictionary<CharacterSpawnOperation, ICharacterDefinitionResolveOperation>();
        private readonly Dictionary<CharacterSpawnOperation, ResolvedCharacterDefinitionLease> definitionLeases = new Dictionary<CharacterSpawnOperation, ResolvedCharacterDefinitionLease>();
        private readonly Dictionary<CharacterSpawnOperation, CharacterDefinition> definitions = new Dictionary<CharacterSpawnOperation, CharacterDefinition>();
        private readonly Dictionary<CharacterSpawnOperation, CharacterAssembly> assemblies = new Dictionary<CharacterSpawnOperation, CharacterAssembly>();
        private readonly Dictionary<CharacterSpawnOperation, IPawnRegistration> pawnRegistrations = new Dictionary<CharacterSpawnOperation, IPawnRegistration>();
        private readonly Dictionary<CharacterRuntimeId, OwnedCharacterRuntime> runtimes = new Dictionary<CharacterRuntimeId, OwnedCharacterRuntime>();
        private readonly Dictionary<CharacterRuntimeId, CharacterView> views = new Dictionary<CharacterRuntimeId, CharacterView>();
        private readonly Dictionary<CharacterRuntimeId, CharacterSpawnOperation> runtimeOperations = new Dictionary<CharacterRuntimeId, CharacterSpawnOperation>();
        private readonly Queue<CharacterSpawnRequestId> terminalRequestIds = new Queue<CharacterSpawnRequestId>();
        private readonly Queue<DespawnCommand> pendingDespawns = new Queue<DespawnCommand>();
        private readonly HashSet<CharacterRuntimeId> queuedRuntimeIds = new HashSet<CharacterRuntimeId>();
        private ICharacterDefinitionProvider definitionProvider;
        private CharacterAssembler assembler;
        private LocalPlayerControllerBinder localPlayerBinder;
        private PawnManager pawnManager;
        private ControllerManager controllerManager;
        private Transform runtimeRoot;

        public override int Priority => 60;
        public event Action<CharacterRuntimeId> CharacterReady;
        public event Action<CharacterRuntimeId, CharacterDespawnReason> CharacterReleased;

        public CharacterSpawnOperation BeginSpawn(CharacterSpawnRequest request)
        {
            if (!request.RequestId.IsValid)
            {
                throw new ArgumentException("A valid spawn request ID is required.", nameof(request));
            }

            if (operations.TryGetValue(request.RequestId, out CharacterSpawnOperation existing))
            {
                if (!IsTerminal(existing.State) || existing.State == CharacterSpawnState.CharacterReady)
                {
                    return existing;
                }

                return CharacterSpawnOperation.CreateDuplicate(request);
            }

            var operation = new CharacterSpawnOperation(request);
            operations.Add(request.RequestId, operation);
            return operation;
        }

        public bool CancelSpawn(CharacterSpawnRequestId requestId)
        {
            if (!operations.TryGetValue(requestId, out CharacterSpawnOperation operation))
            {
                return false;
            }

            if (operation.State == CharacterSpawnState.CancelRequested)
            {
                return true;
            }

            if (operation.IsComplete)
            {
                return false;
            }

            operation.State = CharacterSpawnState.CancelRequested;
            return true;
        }

        public bool TryGetCharacterView(CharacterRuntimeId runtimeId, out ICharacterView view)
        {
            if (views.TryGetValue(runtimeId, out CharacterView characterView) && characterView.IsValid)
            {
                view = characterView;
                return true;
            }

            view = null;
            return false;
        }

        public bool Despawn(CharacterRuntimeId runtimeId, CharacterDespawnReason reason = CharacterDespawnReason.Requested)
        {
            if (!runtimes.ContainsKey(runtimeId) || !queuedRuntimeIds.Add(runtimeId))
            {
                return false;
            }

            pendingDespawns.Enqueue(new DespawnCommand(runtimeId, reason));
            return true;
        }

        public override void Init()
        {
            InputManager inputManager = GameManager.GetManager<InputManager>();
            pawnManager = GameManager.GetManager<PawnManager>();
            controllerManager = GameManager.GetManager<ControllerManager>();
            GameManager.GetManager<PhysicsManager>();
            CharacterDefinition definition = Resources.Load<CharacterDefinition>("CharacterDefinition");
            definitionProvider = new InMemoryCharacterDefinitionProvider(new[] { definition });
            assembler = new CharacterAssembler();
            localPlayerBinder = new LocalPlayerControllerBinder(inputManager, controllerManager);
            runtimeRoot = new GameObject("[CharacterRuntimeRoot]").transform;
        }

        public override void Update(float elapseSeconds)
        {
            ProcessPendingDespawns();
            var currentOperations = new List<CharacterSpawnOperation>(operations.Values);
            foreach (CharacterSpawnOperation operation in currentOperations)
            {
                if (!operation.IsComplete)
                {
                    Advance(operation);
                }
            }
        }

        public override void Shutdown()
        {
            var runtimeIds = new List<CharacterRuntimeId>(runtimes.Keys);
            foreach (CharacterRuntimeId runtimeId in runtimeIds)
            {
                ReleaseRuntime(runtimeId, CharacterDespawnReason.ManagerShutdown);
            }

            foreach (CharacterAssembly assembly in assemblies.Values)
            {
                assembly.Dispose();
            }

            runtimes.Clear();
            views.Clear();
            runtimeOperations.Clear();
            assemblies.Clear();
            foreach (IPawnRegistration registration in pawnRegistrations.Values)
            {
                registration.Dispose();
            }

            pawnRegistrations.Clear();
            definitions.Clear();
            foreach (ResolvedCharacterDefinitionLease lease in definitionLeases.Values)
            {
                lease.Dispose();
            }

            definitionLeases.Clear();
            foreach (ICharacterDefinitionResolveOperation resolveOperation in resolveOperations.Values)
            {
                if (resolveOperation.IsCompleted)
                {
                    resolveOperation.Result.Lease?.Dispose();
                }
            }

            resolveOperations.Clear();
            operations.Clear();
            terminalRequestIds.Clear();
            pendingDespawns.Clear();
            queuedRuntimeIds.Clear();
            if (runtimeRoot != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(runtimeRoot.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(runtimeRoot.gameObject);
                }

                runtimeRoot = null;
            }
        }

        private void Advance(CharacterSpawnOperation operation)
        {
            try
            {
                switch (operation.State)
                {
                    case CharacterSpawnState.Requested:
                        if (!operation.Request.Placement.IsValid)
                        {
                            Fail(operation, CharacterSpawnError.InvalidPlacement);
                            break;
                        }

                        if (operation.Request.ControlKind != CharacterControlKind.LocalPlayer)
                        {
                            Fail(operation, CharacterSpawnError.UnsupportedControlKind);
                            break;
                        }

                        resolveOperations.Add(operation, definitionProvider.BeginResolve(operation.Request.DefinitionId));
                        operation.State = CharacterSpawnState.ResolvingDefinition;
                        break;
                    case CharacterSpawnState.ResolvingDefinition:
                        ICharacterDefinitionResolveOperation resolveOperation = resolveOperations[operation];
                        if (!resolveOperation.IsCompleted)
                        {
                            break;
                        }

                        resolveOperations.Remove(operation);
                        CharacterDefinitionResolveResult result = resolveOperation.Result;
                        if (!result.IsSuccess)
                        {
                            result.Lease?.Dispose();
                            Fail(operation, MapResolveError(result.Error));
                            break;
                        }

                        if (!result.Definition.Supports(operation.Request.ControlKind))
                        {
                            result.Lease.Dispose();
                            Fail(operation, CharacterSpawnError.ControlKindNotSupportedByDefinition);
                            break;
                        }

                        definitionLeases.Add(operation, result.Lease);
                        definitions.Add(operation, result.Definition);
                        operation.State = CharacterSpawnState.Assembling;
                        break;
                    case CharacterSpawnState.Assembling:
                        CharacterDefinition definition = definitions[operation];
                        CharacterAssembly assembly = assembler.Assemble(definition, runtimeRoot, operation.Request.Placement.Position, operation.Request.Placement.Rotation, operation.Request.DisplayName);
                        assemblies.Add(operation, assembly);
                        operation.State = CharacterSpawnState.Registering;
                        break;
                    case CharacterSpawnState.Registering:
                        IPawnRegistration pawnRegistration = pawnManager.RegisterPawn(assemblies[operation].Character);
                        if (pawnRegistration == null || !pawnRegistration.IsActive)
                        {
                            throw new InvalidOperationException("Pawn registration could not be acquired.");
                        }

                        pawnRegistrations.Add(operation, pawnRegistration);
                        operation.State = CharacterSpawnState.Possessing;
                        break;
                    case CharacterSpawnState.Possessing:
                        CharacterAssembly readyAssembly = assemblies[operation];
                        LocalPlayerControllerBinding binding = localPlayerBinder.Bind(readyAssembly.Character, operation.Request.InputType);
                        try
                        {
                            readyAssembly.Root.SetActive(true);
                            var runtimeId = new CharacterRuntimeId(Guid.NewGuid().ToString("N"));
                            IPawnRegistration readyPawnRegistration = pawnRegistrations[operation];
                            ResolvedCharacterDefinitionLease readyDefinitionLease = definitionLeases[operation];
                            readyAssembly.TransferRuntimeOwnership();
                            var runtime = new OwnedCharacterRuntime(
                                readyAssembly.Root,
                                readyAssembly.PawnHost,
                                readyAssembly.Motor,
                                binding,
                                readyPawnRegistration,
                                readyDefinitionLease);
                            var view = new CharacterView(runtimeId, runtime.Transform);
                            runtimes.Add(runtimeId, runtime);
                            views.Add(runtimeId, view);
                            runtimeOperations.Add(runtimeId, operation);
                            assemblies.Remove(operation);
                            pawnRegistrations.Remove(operation);
                            definitionLeases.Remove(operation);
                            operation.RuntimeId = runtimeId;
                            operation.Result = new CharacterSpawnResult(runtimeId);
                            operation.State = CharacterSpawnState.CharacterReady;
                            PublishReady(runtimeId);
                        }
                        catch
                        {
                            binding.Dispose();
                            throw;
                        }

                        break;
                    case CharacterSpawnState.CancelRequested:
                        AdvanceCancellation(operation);
                        break;
                }
            }
            catch
            {
                Fail(operation, CharacterSpawnError.CommitFailed);
            }
        }

        private void Fail(CharacterSpawnOperation operation, CharacterSpawnError error)
        {
            if (resolveOperations.TryGetValue(operation, out ICharacterDefinitionResolveOperation resolveOperation))
            {
                resolveOperations.Remove(operation);
                if (resolveOperation.IsCompleted)
                {
                    resolveOperation.Result.Lease?.Dispose();
                }
            }

            CleanupPendingOperation(operation);

            operation.Error = error;
            operation.State = CharacterSpawnState.Failed;
            MarkTerminal(operation);
        }

        private void AdvanceCancellation(CharacterSpawnOperation operation)
        {
            if (resolveOperations.TryGetValue(operation, out ICharacterDefinitionResolveOperation resolveOperation))
            {
                if (!resolveOperation.IsCompleted)
                {
                    return;
                }

                resolveOperations.Remove(operation);
                resolveOperation.Result.Lease?.Dispose();
            }

            CleanupPendingOperation(operation);
            operation.State = CharacterSpawnState.Cancelled;
            MarkTerminal(operation);
        }

        private void CleanupPendingOperation(CharacterSpawnOperation operation)
        {
            if (pawnRegistrations.TryGetValue(operation, out IPawnRegistration registration))
            {
                registration.Dispose();
                pawnRegistrations.Remove(operation);
            }

            if (assemblies.TryGetValue(operation, out CharacterAssembly assembly))
            {
                assembly.Dispose();
                assemblies.Remove(operation);
            }

            if (definitionLeases.TryGetValue(operation, out ResolvedCharacterDefinitionLease lease))
            {
                lease.Dispose();
                definitionLeases.Remove(operation);
            }

            definitions.Remove(operation);
        }

        private void ProcessPendingDespawns()
        {
            int commandCount = pendingDespawns.Count;
            for (int i = 0; i < commandCount; i++)
            {
                DespawnCommand command = pendingDespawns.Dequeue();
                queuedRuntimeIds.Remove(command.RuntimeId);
                ReleaseRuntime(command.RuntimeId, command.Reason);
            }
        }

        private void ReleaseRuntime(CharacterRuntimeId runtimeId, CharacterDespawnReason reason)
        {
            if (!runtimes.TryGetValue(runtimeId, out OwnedCharacterRuntime runtime))
            {
                return;
            }

            runtime.Dispose();
            if (views.TryGetValue(runtimeId, out CharacterView view))
            {
                view.Release();
                views.Remove(runtimeId);
            }

            runtimes.Remove(runtimeId);
            if (runtimeOperations.TryGetValue(runtimeId, out CharacterSpawnOperation operation))
            {
                runtimeOperations.Remove(runtimeId);
                operation.State = CharacterSpawnState.Released;
                MarkTerminal(operation);
            }

            PublishReleased(runtimeId, reason);
        }

        private void MarkTerminal(CharacterSpawnOperation operation)
        {
            terminalRequestIds.Enqueue(operation.Request.RequestId);
            while (terminalRequestIds.Count > TerminalRequestCapacity)
            {
                CharacterSpawnRequestId expiredId = terminalRequestIds.Dequeue();
                if (operations.TryGetValue(expiredId, out CharacterSpawnOperation expiredOperation) && IsTerminal(expiredOperation.State))
                {
                    operations.Remove(expiredId);
                }
            }
        }

        private static bool IsTerminal(CharacterSpawnState state)
        {
            return state == CharacterSpawnState.Cancelled
                || state == CharacterSpawnState.Released
                || state == CharacterSpawnState.Failed;
        }

        private void PublishReady(CharacterRuntimeId runtimeId)
        {
            try
            {
                CharacterReady?.Invoke(runtimeId);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void PublishReleased(CharacterRuntimeId runtimeId, CharacterDespawnReason reason)
        {
            try
            {
                CharacterReleased?.Invoke(runtimeId, reason);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static CharacterSpawnError MapResolveError(CharacterDefinitionResolveError error)
        {
            switch (error)
            {
                case CharacterDefinitionResolveError.InvalidDefinitionId:
                    return CharacterSpawnError.InvalidDefinitionId;
                case CharacterDefinitionResolveError.DefinitionNotFound:
                    return CharacterSpawnError.DefinitionNotFound;
                default:
                    return CharacterSpawnError.InvalidDefinition;
            }
        }

        private readonly struct DespawnCommand
        {
            public DespawnCommand(CharacterRuntimeId runtimeId, CharacterDespawnReason reason)
            {
                RuntimeId = runtimeId;
                Reason = reason;
            }

            public CharacterRuntimeId RuntimeId { get; }
            public CharacterDespawnReason Reason { get; }
        }
    }
}
