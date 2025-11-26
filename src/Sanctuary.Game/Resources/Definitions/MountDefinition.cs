namespace Sanctuary.Game.Resources.Definitions;

public class MountDefinition
{
    public int Id { get; set; }

    public int NameId { get; set; }

    public int ImageSetId { get; set; }

    public string TextureAlias { get; set; } = null!;
    public string TintAlias { get; set; } = null!;

    public int TintId { get; set; }

    public bool MembersOnly { get; set; }

    public bool IsUpgradable { get; set; }

    public int ModelId { get; set; }

    public float NameVerticalOffset { get; set; }

    public MountStats Stats { get; set; } = new();

    public class MountStats
    {
        public float MaxMovementSpeed { get; set; }

        public float GlideDefaultForwardSpeed { get; set; }
        public float GlideMinForwardSpeed { get; set; }
        public float GlideMaxForwardSpeed { get; set; }
        public float GlideFallTime { get; set; }
        public float GlideFallSpeed { get; set; }
        public int GlideEnabled { get; set; }
        public float GlideAccel { get; set; }

        public float JumpHeight { get; set; }
    }
}