using System.Threading.Tasks;

namespace JumpyJetV2
{
    public interface IGameInput
    {
        void Initialize();
        Task<bool> HasJumped();
    }
}
