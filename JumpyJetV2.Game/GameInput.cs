using Stride.Engine;

namespace JumpyJetV2
{
    public abstract class GameInput : SyncScript
    {
        public virtual bool Enabled { get; set; }
        public virtual bool IsJumping { get; }
    }
}
