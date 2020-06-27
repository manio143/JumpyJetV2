using OpenTK.Graphics.ES20;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Engine.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JumpyJetV2.AIv2
{
    public class AIController : AsyncScript
    {
        private EventReceiver newGameEvent = new EventReceiver(GlobalEvents.NewGame);

        public UrlReference<Prefab> CharacterPrefabUrl { get; set; }
        public PipesScript PipesScript { get; set; }
        public UIScript UIScript { get; set; }

        private NeuralNetworkEvolution neat;
        private List<NeuralNetworkEvolution.Network> ai;
        private Entity[] characters;
        private CharacterScript[] characterScripts;
        private Prefab characterPrefab;

        private int generation;
        private int score;
        private int highscore;
        private bool[] dead;

        public void Start()
        {
            characterPrefab = Content.Load(CharacterPrefabUrl);
            
            UIScript.UserControlled = false;
            
            neat = new NeuralNetworkEvolution();
            
            generation = 1;
            highscore = 0;

            characters = new Entity[neat.options.Population];
            characterScripts = new CharacterScript[neat.options.Population];

            for (int i = 0; i < characters.Length; i++)
            {
                characters[i] = characterPrefab.Instantiate()[0];
                characterScripts[i] = characters[i].Get<CharacterScript>();
                characterScripts[i].CharacterId = (uint)i + 1;
                characterScripts[i].Broadcast = false;

                Entity.Scene.Entities.Add(characters[i]);
            }

            ResetRound();
        }

        public void ResetRound()
        {
            ai = neat.NextGeneration();

            dead = characterScripts.Select(_ => false).ToArray();
            score = 0;
            generation = 1;
        }

        public override async Task Execute()
        {
            await Task.Yield(); // don't execute initialization on call

            Start();

            // Wait for scripts to get initialized????
            await Script.NextFrame();

            await newGameEvent.ReceiveAsync();

            while(true)
            {
                await Script.NextFrame();

                DebugText.Print($"Generation: {generation}\nAlive: {dead.Count(d => !d)}/{ai.Count}\nScore: {score}\nHighscore: {highscore}", new Int2(20, 300));

                // get current state
                float dist = 0, height = 0;
                PipesScript.ProvideAiInformation(ref dist, ref height);

                for (int i = 0; i < characterScripts.Length; i++)
                {
                    if (dead[i]) continue;

                    var character = characterScripts[i];

                    if (!character.isRunning || character.isDying)
                    {
                        dead[i] = true;
                        neat.AddWithScore(ai[i], score);
                        continue;
                    }

                    var position = character.Movement.Position.Y;

                    // we try to find a function that given to positions
                    // tries to jump so that they come close together
                    var aiResult = ai[i].Compute(neat.options, new double[] { position, height });
                    if (aiResult[0] > 0.5)
                        character.Jump();
                }

                score++;

                highscore = score > highscore ? score : highscore;

                if (dead.All(d => d))
                {
                    GlobalEvents.GameOver.Broadcast();
                    ResetRound();
                }
            }

        }
    }
}
