using System;
using System.Collections.Generic;
using System.Windows.Forms; // For Clipboard functionality
using ExileCore2;
using ExileCore2.PoEMemory.MemoryObjects;
using ImGuiNET;
using System.Numerics;

namespace EntityFinder
{
    public class EntityFinder : BaseSettingsPlugin<EntityFinderSettings>
    {
        private readonly List<Entity> _matchingEntities = new();
        private readonly LinkedList<string> _lastTwentyResults = new(); // Store last 20 results
        private string _searchText = string.Empty; // Search term entered by the user
        private bool _searchNearMouse = false; // Option to search near the mouse cursor
        private float _distanceFromCharacter = 50f; // Default distance from character in "distance units"
        private float _distanceFromMouse = 2.5f; // Default distance from mouse cursor in "distance units"

        public override bool Initialise()
        {
            LogMessage($"{GetType().Name} initialized.", 5);
            return true;
        }

        public override void Tick()
        {
            _matchingEntities.Clear();

            // Automatically add wildcards to the search text
            var searchText = string.IsNullOrWhiteSpace(_searchText) ? "*" : $"*{_searchText}*";

            foreach (var entity in GameController.Entities)
            {
                if (IsMatch(entity.Path, searchText))
                {
                    if (_searchNearMouse)
                    {
                        // Check if entity is near the mouse cursor
                        if (IsNearMouse(entity)) _matchingEntities.Add(entity);
                    }
                    else
                    {
                        // Check if entity is within distance from the character
                        if (IsNearCharacter(entity)) _matchingEntities.Add(entity);
                    }
                }
            }

            LogMessage($"Search complete. Matches found: {_matchingEntities.Count}.", 5);
        }

        public override void Render()
        {
            RenderSearchInterface();
            RenderSearchResults();
        }

        private void RenderSearchInterface()
        {
            // Search text input
            ImGui.Text("Enter search text:");
            if (ImGui.InputText("##SearchText", ref _searchText, 256))
            {
                LogMessage("Search text updated.", 5);
            }

            ImGui.SameLine();

            // Search near mouse toggle
            ImGui.Checkbox("Search Near Mouse", ref _searchNearMouse);

            // Show appropriate slider based on the search option
            if (_searchNearMouse)
            {
                ImGui.Text("Distance from Mouse Cursor:");
                ImGui.SliderFloat("##DistanceFromMouse", ref _distanceFromMouse, 0.5f, 25f, "%.1f distance");
            }
            else
            {
                ImGui.Text("Distance from Character:");
                ImGui.SliderFloat("##DistanceFromCharacter", ref _distanceFromCharacter, 5f, 100f, "%.1f distance");
            }

            ImGui.SameLine();

        }

        private void SafeSetClipboardText(string text)
        {
            var thread = new System.Threading.Thread(() => Clipboard.SetText(text));
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        private void RenderSearchResults()
        {
            ImGui.Text("Search Results:");

            if (_matchingEntities.Count == 0)
            {
                ImGui.Text("No matches found.");
            }
            else
            {
                foreach (var entity in _matchingEntities)
                {
                    ImGui.Text(entity.Path);

                    ImGui.SameLine();
                    if (ImGui.Button($"Copy##{entity.Path}")) // Unique button ID for each entity
                    {
                        SafeSetClipboardText(entity.Path); // Use the helper method here
                        LogMessage($"Copied to clipboard: {entity.Path}");
                    }
                }
            }
        }

        private bool IsMatch(string entityPath, string searchText)
        {
            return entityPath.Contains(searchText.Trim('*'), StringComparison.OrdinalIgnoreCase);
        }

        private bool IsNearMouse(Entity entity)
        {
            // Get the raw screen-space mouse position from ImGui
            var mousePosition = ImGui.GetMousePos(); // This gives screen coordinates

            // Convert entity world position to screen-space coordinates
            var entityScreenPosition = GameController.Game.IngameState.Camera.WorldToScreen(entity.Pos);

            // Calculate the distance between mouse position and the entity
            var distance = Vector2.Distance(mousePosition, entityScreenPosition);

            // Check if within user-defined threshold (converted to pixels)
            return distance < (_distanceFromMouse * 20f);
        }

        private bool IsNearCharacter(Entity entity)
        {
            // Get the character's position
            var characterPosition = GameController.Player.Pos;

            // Calculate the distance between character and the entity
            var distance = Vector2.Distance(new Vector2(characterPosition.X, characterPosition.Y),
                                             new Vector2(entity.Pos.X, entity.Pos.Y));

            // Check if within user-defined threshold
            return distance < (_distanceFromCharacter * 20f);
        }
    }
}
