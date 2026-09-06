using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Intent;

namespace ShotGame.Gameplay.Character
{
    public sealed class EquipmentComponent : EntityComponent
    {
        public int CurrentWeaponSlot { get; private set; } = 1;
        public int PreviousWeaponSlot { get; private set; } = 1;

        public void ApplyIntent(in PawnIntent intent)
        {
            if (intent.SwitchWeaponSlot > 0) SelectSlot(intent.SwitchWeaponSlot);
            else if (intent.SwitchWeaponStep != 0) SelectSlot(System.Math.Max(1, CurrentWeaponSlot + intent.SwitchWeaponStep));
            else if (intent.QuickSwapPressed) SelectSlot(PreviousWeaponSlot);
        }

        private void SelectSlot(int slot)
        {
            if (slot == CurrentWeaponSlot) return;
            PreviousWeaponSlot = CurrentWeaponSlot;
            CurrentWeaponSlot = slot;
        }
    }
}
