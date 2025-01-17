using Svelto.ECS.Hybrid;

namespace Svelto.ECS.Example.Survive.HUD
{
    public struct HUDEntityViewComponent : IEntityViewComponent
    {
        public IAnimationComponent          HUDAnimator;
        public IDamageHUDComponent          damageImageComponent;
        public IHealthSliderComponent       healthSliderComponent;
        public IScoreComponent              scoreComponent;
        public IWaveProgressionComponent    waveProgressionComponent;
        public IWaveComponent               waveComponent;
        public IAmmoComponent               ammoComponent;
        public EGID                         ID { get; set; }
    }
}