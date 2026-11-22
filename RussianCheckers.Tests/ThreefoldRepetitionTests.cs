
using Xunit;
using RussianCheckers.Core.Models;
using RussianCheckers.Core.Services;
using RussianCheckers.Data.Serialization;

namespace RussianCheckers.Tests
{
    public class ThreefoldRepetitionTests
    {
        [Fact]
        public void PositionRepeatedThreeTimes_IsDetected()
        {
            var state = new GameState();
            var engine = new GameEngine();

            string position =
                PositionKeyGenerator.Generate(state);

            state.PositionHistory = new()
            {
                position,
                position,
                position
            };

            Assert.True(
                engine.IsThreefoldRepetition(state));
        }

        [Fact]
        public void PositionRepeatedTwice_IsNotDraw()
        {
            var state = new GameState();
            var engine = new GameEngine();

            string position =
                PositionKeyGenerator.Generate(state);

            state.PositionHistory = new()
            {
                position,
                position
            };

            Assert.False(
                engine.IsThreefoldRepetition(state));
        }

        [Fact]
        public void PositionHistory_IsCopiedIndependently()
        {
            var state = new GameState();

            var copy = state.Clone();

            copy.PositionHistory.Add("test");

            Assert.Single(state.PositionHistory);
            Assert.Equal(2, copy.PositionHistory.Count);
        }

        [Fact]
        public void PositionHistory_SurvivesSerialization()
        {
            var state = new GameState();

            string position =
                PositionKeyGenerator.Generate(state);

            state.PositionHistory.Add(position);

            string json =
                GameStateSerializer.Serialize(state);

            GameState restored =
                GameStateSerializer.Deserialize(json);

            Assert.Equal(
                state.PositionHistory,
                restored.PositionHistory);
        }

        [Fact]
        public void OldSaveWithoutHistory_CanBeLoaded()
        {
            var state = new GameState();

            string json = GameStateSerializer.Serialize(state);

            var jsonObject =
                System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();

            jsonObject.Remove("PositionHistory");

            GameState restored =
                GameStateSerializer.Deserialize(
                    jsonObject.ToJsonString());

            Assert.Single(restored.PositionHistory);
        }
    }
}
