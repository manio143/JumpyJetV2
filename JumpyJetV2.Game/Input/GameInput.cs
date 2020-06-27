using System.Threading.Tasks;

namespace JumpyJetV2.Input
{
    public interface IGameInput
    {
        void Initialize();
        Task<bool> HasJumped();
    }
}
