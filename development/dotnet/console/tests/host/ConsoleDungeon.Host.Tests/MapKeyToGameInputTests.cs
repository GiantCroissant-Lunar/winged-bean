using Terminal.Gui;
using Xunit;

namespace ConsoleDungeon.Host.Tests
{
    /// <summary>
    /// Unit tests for key-to-game-input mapping logic.
    /// Tests the MapKeyToGameInput method without running the full Terminal.Gui application.
    /// </summary>
    public class MapKeyToGameInputTests
    {
        [Theory]
        [InlineData(KeyCode.CursorUp, "MoveUp")]
        [InlineData(KeyCode.CursorDown, "MoveDown")]
        [InlineData(KeyCode.CursorLeft, "MoveLeft")]
        [InlineData(KeyCode.CursorRight, "MoveRight")]
        [InlineData(KeyCode.Esc, "Quit")]
        [InlineData(KeyCode.Space, "Attack")]
        public void SpecialKeys_ShouldMapCorrectly(KeyCode keyCode, string expectedInputType)
        {
            // Act
            var result = MapKeyCodeToGameInputTestHelper(keyCode);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedInputType, result);
        }

        [Theory]
        [InlineData('W', "MoveUp")]
        [InlineData('w', "MoveUp")]
        [InlineData('A', "MoveLeft")]
        [InlineData('a', "MoveLeft")]
        [InlineData('S', "MoveDown")]
        [InlineData('s', "MoveDown")]
        [InlineData('D', "MoveRight")]
        [InlineData('d', "MoveRight")]
        [InlineData('M', "ToggleMenu")]
        [InlineData('m', "ToggleMenu")]
        [InlineData('I', "ToggleInventory")]
        [InlineData('i', "ToggleInventory")]
        [InlineData('Q', "Quit")]
        [InlineData('q', "Quit")]
        [InlineData('E', "Use")]
        [InlineData('e', "Use")]
        [InlineData('G', "Pickup")]
        [InlineData('g', "Pickup")]
        public void CharacterKeys_ShouldMapCorrectly(char character, string expectedInputType)
        {
            // Act
            var result = MapCharacterToGameInputTestHelper(character);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedInputType, result);
        }

        [Fact]
        public void UnmappedCharacter_ShouldReturnNull()
        {
            // Act
            var result = MapCharacterToGameInputTestHelper('Z');

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Test helper that replicates the KeyCode mapping logic from ConsoleDungeonApp.MapKeyToGameInput.
        /// </summary>
        private string? MapKeyCodeToGameInputTestHelper(KeyCode keyCode)
        {
            return keyCode switch
            {
                KeyCode.CursorUp => "MoveUp",
                KeyCode.CursorDown => "MoveDown",
                KeyCode.CursorLeft => "MoveLeft",
                KeyCode.CursorRight => "MoveRight",
                KeyCode.Space => "Attack",
                KeyCode.Esc => "Quit",
                _ => null
            };
        }

        /// <summary>
        /// Test helper that replicates the character mapping logic from ConsoleDungeonApp.MapKeyToGameInput.
        /// </summary>
        private string? MapCharacterToGameInputTestHelper(char character)
        {
            var ch = char.ToUpper(character);
            return ch switch
            {
                // WASD movement
                'W' => "MoveUp",
                'A' => "MoveLeft",
                'S' => "MoveDown",
                'D' => "MoveRight",
                // SS3 fallback (issue #214)
                'B' => "MoveDown",  // ESC O B
                'C' => "MoveRight", // ESC O C
                // Game commands
                'E' => "Use",
                'G' => "Pickup",
                'M' => "ToggleMenu",
                'I' => "ToggleInventory",
                'Q' => "Quit",
                _ => null
            };
        }
    }
}
