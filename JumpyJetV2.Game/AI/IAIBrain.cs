using System.Threading.Tasks;

namespace JumpyJetV2.AI
{
    public interface IAIBrain
    {
        PipesScript Pipes { get; set; }
        UIScript UI { get; set; }
        bool HasPredicted { get; }
        bool Train { get; set; }

        /// <summary>
        /// Get statistics from the AI brain.
        /// </summary>
        /// <returns></returns>
        (uint generation, uint seqNum, uint highscore, uint genhigh) GetStats();

        /// <summary>
        /// Provide feedback on the prediction.
        /// </summary>
        /// <param name="result"></param>
        void Inform(AIInput.CharacterMoveResult result, uint id = 0);

        /// <summary>
        /// Given a gameState predict wether Jumpy should jump.
        /// </summary>
        /// <param name="gameState"></param>
        /// <returns></returns>
        Task<bool> Predict(AIInput.GameState gameState, uint id = 0);

        /// <summary>
        /// Initialize AI brain.
        /// </summary>
        void Start();
    }
}