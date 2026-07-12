using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGame
{
    public sealed class CharacterSpawnManager : IManager
    {
        private readonly Dictionary<CharacterSpawnRequestId, CharacterSpawnOperation> operations = new Dictionary<CharacterSpawnRequestId, CharacterSpawnOperation>();
        private readonly Dictionary<CharacterSpawnOperation, CharacterDefinition> definitions = new Dictionary<CharacterSpawnOperation, CharacterDefinition>();
        private readonly Dictionary<CharacterSpawnOperation, CharacterAssembly> assemblies = new Dictionary<CharacterSpawnOperation, CharacterAssembly>();
        private readonly Dictionary<CharacterSpawnOperation, CharacterRuntime> runtimes = new Dictionary<CharacterSpawnOperation, CharacterRuntime>();
        private ICharacterDefinitionProvider definitionProvider;
        private PawnManager pawnManager;
        private ControllerManager controllerManager;
        private Transform runtimeRoot;

        public override int Priority => 60;

        public CharacterSpawnOperation BeginSpawn(CharacterSpawnRequest request)
        {
            if (!request.RequestId.IsValid)
            {
                throw new ArgumentException("A valid spawn request ID is required.", nameof(request));
            }

            if (operations.TryGetValue(request.RequestId, out CharacterSpawnOperation existing))
            {
                return existing;
            }

            var operation = new CharacterSpawnOperation(request);
            operations.Add(request.RequestId, operation);
            return operation;
        }

        public override void Init()
        {
            pawnManager = GameManager.GetManager<PawnManager>();
            controllerManager = GameManager.GetManager<ControllerManager>();
            CharacterDefinition definition = Resources.Load<CharacterDefinition>("CharacterDefinition");
            definitionProvider = new InMemoryCharacterDefinitionProvider(new[] { definition });
            runtimeRoot = new GameObject("[CharacterRuntimeRoot]").transform;
        }

        public override void Update(float elapseSeconds)
        {
            foreach (CharacterSpawnOperation operation in operations.Values)
            {
                if (!operation.IsComplete)
                {
                    Advance(operation);
                }
            }
        }

        public override void Shutdown()
        {
            foreach (CharacterRuntime runtime in runtimes.Values)
            {
                runtime.Dispose();
            }

            foreach (CharacterAssembly assembly in assemblies.Values)
            {
                assembly.Dispose();
            }

            runtimes.Clear();
            assemblies.Clear();
            definitions.Clear();
            operations.Clear();
            if (runtimeRoot != null)
            {
                UnityEngine.Object.Destroy(runtimeRoot.gameObject);
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
                        operation.State = CharacterSpawnState.ResolvingDefinition;
                        break;
                    case CharacterSpawnState.ResolvingDefinition:
                        CharacterDefinitionResolveResult result = definitionProvider.Resolve(operation.Request.DefinitionId);
                        if (!result.IsSuccess)
                        {
                            Fail(operation, result.Error);
                            break;
                        }

                        definitions.Add(operation, result.Definition);
                        operation.State = CharacterSpawnState.Assembling;
                        break;
                    case CharacterSpawnState.Assembling:
                        CharacterDefinition definition = definitions[operation];
                        CharacterAssembly assembly = new CharacterAssembler().Assemble(definition, runtimeRoot, operation.Request.Placement.Position, operation.Request.Placement.Rotation, operation.Request.DisplayName);
                        assemblies.Add(operation, assembly);
                        operation.State = CharacterSpawnState.Registering;
                        break;
                    case CharacterSpawnState.Registering:
                        pawnManager.RegisteringPawn(assemblies[operation].Character);
                        operation.State = CharacterSpawnState.Possessing;
                        break;
                    case CharacterSpawnState.Possessing:
                        if (operation.Request.ControlKind != CharacterControlKind.LocalPlayer || operation.Request.Input == null)
                        {
                            Fail(operation, CharacterDefinitionResolveError.MissingSupportedControlKind);
                            break;
                        }

                        CharacterAssembly readyAssembly = assemblies[operation];
                        PlayerController controller = controllerManager.CreatingController<PlayerController>();
                        controller.SettingInputHandle(operation.Request.Input);
                        controller.PossessingPawn(readyAssembly.Character);
                        readyAssembly.Root.SetActive(true);
                        readyAssembly.TransferRuntimeOwnership();
                        runtimes.Add(operation, new CharacterRuntime(
                            readyAssembly.Root,
                            readyAssembly.Character,
                            readyAssembly.PawnHost,
                            readyAssembly.Motor,
                            controller,
                            pawnManager,
                            controllerManager));
                        assemblies.Remove(operation);
                        operation.RuntimeId = new CharacterRuntimeId(operation.Request.RequestId.Value);
                        operation.State = CharacterSpawnState.CharacterReady;
                        break;
                }
            }
            catch
            {
                Fail(operation, CharacterDefinitionResolveError.InvalidAnimationConfig);
            }
        }

        private void Fail(CharacterSpawnOperation operation, CharacterDefinitionResolveError error)
        {
            if (assemblies.TryGetValue(operation, out CharacterAssembly assembly))
            {
                pawnManager.UnregisteringPawn(assembly.Character);
                assembly.Dispose();
                assemblies.Remove(operation);
            }

            operation.Error = error;
            operation.State = CharacterSpawnState.Failed;
        }
    }
}
