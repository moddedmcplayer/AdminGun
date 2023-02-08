namespace AdminGun
{
    using System.ComponentModel;

    public class Config
    {
        public bool AllowDropping { get; set; } = true;
        public bool RestrictPickingUp { get; set; } = true;
        [Description("Why?")]
        public bool FlashLightEnabledExplosiveAmmo { get; set; } = true;

        public string AuthorizedObtainMessage { get; set; } = "Keep yourself within limits";
        public string UnauthorizedObtainMessage { get; set; } = "You probably shouldn't have this";
    }
}