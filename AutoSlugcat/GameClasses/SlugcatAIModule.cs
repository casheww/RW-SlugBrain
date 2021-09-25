
namespace SlugBrain.GameClasses
{
    public abstract class SlugcatAIModule : AIModule
    {
        protected SlugcatAIModule(ArtificialIntelligence ai) : base(ai) { }

        public abstract void UpdateRoomRepresentation(RoomRepresentation rRep);

    }
}
