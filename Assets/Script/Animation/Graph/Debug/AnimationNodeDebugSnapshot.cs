namespace CGame.Animation
{
    public readonly struct AnimationNodeDebugSnapshot
    {
        public AnimationNodeDebugSnapshot(string nodeName, bool isValid, float weight, int inputCount)
        {
            NodeName = nodeName;
            IsValid = isValid;
            Weight = weight;
            InputCount = inputCount;
        }

        public string NodeName { get; }
        public bool IsValid { get; }
        public float Weight { get; }
        public int InputCount { get; }

        public static AnimationNodeDebugSnapshot Invalid(string nodeName)
        {
            return new AnimationNodeDebugSnapshot(nodeName, false, 0f, 0);
        }
    }
}
