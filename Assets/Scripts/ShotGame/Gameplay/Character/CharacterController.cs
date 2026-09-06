using System;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Intent;

namespace ShotGame.Gameplay.Character
{
    public sealed class CharacterController : EntityComponent, IEntityTickable
    {
        private IPawnIntentSource _intentSource;
        private MovementComponent _movement;
        private WeaponUseComponent _weaponUse;
        private EquipmentComponent _equipment;
        private GrazeComponent _graze;

        public override void Initialize()
        {
            _intentSource = Owner.GetComponent<IPawnIntentSource>()
                ?? throw new InvalidOperationException($"角色 {Owner.Id} 缺少意图源。");
            _movement = Owner.GetComponent<MovementComponent>();
            _weaponUse = Owner.GetComponent<WeaponUseComponent>();
            _equipment = Owner.GetComponent<EquipmentComponent>();
            _graze = Owner.GetComponent<GrazeComponent>();
        }

        public void Tick(float deltaTime)
        {
            var intent = _intentSource.GetIntent();
            _movement?.SetMoveDirection(intent.MoveDirection);
            _equipment?.ApplyIntent(intent);
            _weaponUse?.ApplyIntent(intent);
            _graze?.ApplyIntent(intent.GrazePressed);
            _intentSource.ClearFrameIntent();
        }
    }
}
