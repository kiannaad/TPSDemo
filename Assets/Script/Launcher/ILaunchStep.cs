namespace CGame
{
    public interface ILaunchStep 
    {
        public void Enter();
        public bool Update();
        public void Exit();
    }
}