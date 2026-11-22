
using Xunit;
using RussianCheckers.Core.Models;
using RussianCheckers.Data.Serialization;
using System.Text.Json.Nodes;

namespace RussianCheckers.Tests
{
    public class DrawStateTests
    {
        [Fact]
        public void NewGame_HasEmptyDrawState()
        {
            var state = new GameState();

            Assert.Equal(0,
                state.DrawState.NoCapturePlies);

            Assert.Equal(0,
                state.DrawState.KingOnlyPlies);

            Assert.Equal(0,
                state.DrawState.WhiteMovesSinceMaterialChange);

            Assert.Equal(0,
                state.DrawState.BlackMovesSinceMaterialChange);
        }

        [Fact]
        public void Clone_CopiesDrawStateIndependently()
        {
            var state = new GameState();

            state.DrawState.KingOnlyPlies = 10;

            var copy = state.Clone();

            copy.DrawState.KingOnlyPlies = 15;

            Assert.Equal(10,
                state.DrawState.KingOnlyPlies);

            Assert.Equal(15,
                copy.DrawState.KingOnlyPlies);
        }

        [Fact]
        public void DrawState_SurvivesSerialization()
        {
            var state = new GameState();

            state.DrawState.NoCapturePlies = 8;
            state.DrawState.KingOnlyPlies = 6;

            state.DrawState.WhiteMovesSinceMaterialChange = 4;
            state.DrawState.BlackMovesSinceMaterialChange = 3;

            state.DrawState.MaterialSignature = "1:2:0:1";

            string json =
                GameStateSerializer.Serialize(state);

            GameState restored =
                GameStateSerializer.Deserialize(json);

            Assert.Equal(8,
                restored.DrawState.NoCapturePlies);

            Assert.Equal(6,
                restored.DrawState.KingOnlyPlies);

            Assert.Equal(4,
                restored.DrawState.WhiteMovesSinceMaterialChange);

            Assert.Equal(3,
                restored.DrawState.BlackMovesSinceMaterialChange);

            Assert.Equal("1:2:0:1",
                restored.DrawState.MaterialSignature);
        }

        [Fact]
        public void OldSaveWithoutDrawState_CanBeLoaded()
        {
            var state = new GameState();

            string json =
                GameStateSerializer.Serialize(state);

            var jsonObject =
                JsonNode.Parse(json)!.AsObject();

            jsonObject.Remove("DrawState");

            GameState restored =
                GameStateSerializer.Deserialize(
                    jsonObject.ToJsonString());

            Assert.NotNull(restored.DrawState);

            Assert.Equal(0,
                restored.DrawState.KingOnlyPlies);
        }
    }
}
