using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Network.Reloaded.Serialization;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Gargantuar)]
internal sealed class GargantuarNetworkComponent : ZombieNetworkComponent
{
    private int GetPoleSkinIndex()
    {
        if (Net.Zombie == null)
            return 0;

        string codeName = Net.Zombie.mController.GetImageOverride(Animations.GARGANTUAR_POLE_OBJECT.Slot);
        var skinData = Net.Zombie.mController.m_skinController.m_skinSets.First();

        foreach (var skin in skinData.Skins)
        {
            if (skin.SlotTrackName != Animations.GARGANTUAR_POLE_OBJECT.Slot)
                continue;

            for (int i = 0; i < skin.Entries.Count; i++)
            {
                if (skin.Entries[i].CodeName == codeName)
                {
                    return i;
                }
            }

            break;
        }

        return 0;
    }

    private void SetPoleSkinByIndex(int index)
    {
        if (Net.Zombie == null)
            return;

        var skinData = Net.Zombie.mController.m_skinController.m_skinSets.First();

        foreach (var skin in skinData.Skins)
        {
            if (skin.SlotTrackName != Animations.GARGANTUAR_POLE_OBJECT.Slot)
                continue;

            if (index >= skin.Entries.Count)
                return;

            var entry = skin.Entries[index];
            if (entry != null)
            {
                Net.Zombie.mController.SetImageOverride(Animations.GARGANTUAR_POLE_OBJECT.Slot, entry.CodeName);
            }
            break;
        }
    }

    public sealed override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
        {
            // Sync pole type used
            var poleSkinIndex = GetPoleSkinIndex();
            packetWriter.WritePackedInt(poleSkinIndex);
        }

        base.Serialize(packetWriter, init);
    }

    public sealed override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init)
        {
            // Sync pole type used
            var poleSkinIndex = packetReader.ReadPackedInt();
            SetPoleSkinByIndex(poleSkinIndex);
        }

        base.Deserialize(packetReader, init);
    }
}
