namespace AdminGun
{
    using System.ComponentModel;
    using UnityEngine;
    using YamlDotNet.Serialization;

    public class Config
    {
        public bool AllowDropping { get; set; } = true;
        public bool RestrictPickingUp { get; set; } = true;
        [Description("Why?")]
        public bool FlashLightEnabledExplosiveAmmo { get; set; } = true;
        public bool EnableVisualGun { get; set; } = true;
        [Description("Visual gun flashlight = freddy schem ammo")]
        public bool EnableFreddy { get; set; } = true;
        [Description("Requires MER and EXILED!!!!!!")]
        public string MERSchematicName { get; set; } = "Freddy";
        public float SchematicScale { get; set; } = -0.5f;
        public float Force { get; set; } = 1000f;

        public string AuthorizedObtainMessage { get; set; } = "Keep yourself within limits";
        public string UnauthorizedObtainMessage { get; set; } = "You probably definitely shouldn't have this";
        public string AuthorizedObtainMessageFreddy { get; set; } = "Harmless, but not to your frames";
        public string UnauthorizedObtainMessageFreddy { get; set; } = "You probably shouldn't have this";
    }
}