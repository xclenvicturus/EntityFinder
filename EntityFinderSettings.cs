using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;

namespace EntityFinder
{
    public class EntityFinderSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);
    }
}
