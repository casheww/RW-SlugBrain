
namespace SlugBrain.GameClasses
{
    abstract class SlugcatAIModule : AIModule
    {
        public SlugcatAIModule(ArtificialIntelligence AI) : base(AI) { }

        abstract public void UpdateRoomRepresentation(RoomRepresentation rRep);

    }
}
