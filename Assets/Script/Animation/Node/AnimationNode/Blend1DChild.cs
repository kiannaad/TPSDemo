namespace CGame.Animation
{
    public readonly struct Blend1DChild
    {
        public Blend1DChild(IAnimationPlayableNode node, float threshold)
        {
            Node = node;
            Threshold = threshold;
        }

        public IAnimationPlayableNode Node { get; }
        public float Threshold { get; }
    }
}
