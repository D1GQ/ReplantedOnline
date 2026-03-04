using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Object.Game;
using UnityEngine;

namespace ReplantedOnline.Monos;

/// <summary>
/// Debugger component for visualizing and debugging networked objects in the game.
/// </summary>
internal sealed class NetworkedDebugger : MonoBehaviour
{
    private NetworkObject _instance;
    private const float BASE_WIDTH = 1920f;
    private const float BASE_HEIGHT = 1080f;

    private static Vector2 ScreenScale => new(
        Screen.width / BASE_WIDTH,
        Screen.height / BASE_HEIGHT
    );

    /// <summary>
    /// Initializes the debugger with a networked object instance.
    /// </summary>
    /// <param name="networkObj">The networked object instance to debug.</param>
    [HideFromIl2Cpp]
    internal void Initialize(NetworkObject networkObj)
    {
        _instance = networkObj;
    }

    private Vector3 _cachedControllerPosition;
    private Vector3 _cachedWPos;
    private string[] _cachedTexts;

    public void OnGUI()
    {
        if (!InfoDisplay.DebugEnabled) return;
        if (_instance == null) return;
        if (!_instance.IsOnNetwork) return;

        if (_instance is ZombieNetworked zombieNetworked)
        {
            DebugZombie(zombieNetworked);
        }
        else if (_instance is PlantNetworked plantNetworked)
        {
            DebugPlant(plantNetworked);
        }
    }

    [HideFromIl2Cpp]
    private void DebugZombie(ZombieNetworked zombieNetworked)
    {
        var zombie = zombieNetworked._Zombie;
        if (zombie != null)
        {
            if (zombie.mDead) return;
            if (zombieNetworked.ZombieType is ZombieType.Target or ZombieType.Gravestone) return;

            _cachedControllerPosition = zombie.mController.transform.position;
            _cachedWPos = GetWorldPos(_cachedControllerPosition);

            // Scale offsets based on screen resolution
            Vector2 scaledOffset = new Vector2(85f, 175f) * ScreenScale.y; // Scale based on height for consistency
            _cachedWPos += new Vector3(scaledOffset.x, scaledOffset.y, 0f);

            _cachedTexts =
            [
                $"{Enum.GetName(zombieNetworked.ZombieType)} Zombie",
                $"{Enum.GetName(zombie.mZombiePhase)}: {zombie.mPhaseCounter}"
            ];

            // Scale box size based on resolution
            Vector2 boxSize = new Vector2(100f, 150f) * ScreenScale.y;
            Vector2 boxPosition = new(_cachedWPos.x, _cachedWPos.y - (75f * ScreenScale.y));

            DebugRenderHelper.Strings(_cachedWPos.x, _cachedWPos.y + (15f * ScreenScale.y),
                1f, 1f, _cachedTexts, Color.white,
                new Vector2(0f, 15f * ScreenScale.y));
            DebugRenderHelper.Box(boxPosition, boxSize, 1f * ScreenScale.y, Color.white);

            if (zombieNetworked.lastSyncPosX != null)
            {
                var syncWorldPos = new Vector3(
                    PvZRHelper.GetGridOffsetXPosFromBoardXPos(zombieNetworked.lastSyncPosX.Value),
                    _cachedControllerPosition.y
                );
                var syncPos = GetWorldPos(syncWorldPos);

                // Scale sync position offset
                Vector2 syncOffset = new Vector2(75f, 125f) * ScreenScale.y;
                syncPos += new Vector3(syncOffset.x, syncOffset.y, 0f);

                DebugRenderHelper.Line(_cachedWPos, syncPos, 1 * ScreenScale.y, Color.magenta);

                Vector2 syncBoxSize = new Vector2(50f, 50f) * ScreenScale.y;
                DebugRenderHelper.Box(new Vector2(syncPos.x, syncPos.y), syncBoxSize,
                    1f * ScreenScale.y, Color.magenta);
            }
        }
        else
        {
            if (_cachedTexts != null && _cachedTexts.Length > 0)
            {
                DebugRenderHelper.Strings(_cachedWPos.x, _cachedWPos.y + (15f * ScreenScale.y),
                    1f, 1f, _cachedTexts, Color.red,
                    new Vector2(0f, 15f * ScreenScale.y));

                Vector2 boxSize = new Vector2(100f, 150f) * ScreenScale.y;
                Vector2 boxPosition = new(_cachedWPos.x, _cachedWPos.y - (75f * ScreenScale.y));
                DebugRenderHelper.Box(boxPosition, boxSize, 1f * ScreenScale.y, Color.red);
            }
        }
    }

    [HideFromIl2Cpp]
    private void DebugPlant(PlantNetworked plantNetworked)
    {
        var plant = plantNetworked._Plant;
        if (plant != null)
        {
            if (plant.mDead) return;

            _cachedControllerPosition = plant.mController.transform.position;
            _cachedWPos = GetWorldPos(_cachedControllerPosition);

            // Scale offsets based on screen resolution
            Vector2 scaledOffset = new Vector2(55f, 90f) * ScreenScale.y;
            _cachedWPos += new Vector3(scaledOffset.x, scaledOffset.y, 0f);

            _cachedTexts =
            [
                $"{Enum.GetName(plant.mSeedType)} Plant",
                $"{Enum.GetName(plant.mState)}: {plant.mStateCountdown}"
            ];

            // Scale box size based on resolution
            Vector2 boxSize = new Vector2(100f, 100f) * ScreenScale.y;
            Vector2 boxPosition = new(_cachedWPos.x, _cachedWPos.y - (25f * ScreenScale.y));

            DebugRenderHelper.Strings(_cachedWPos.x, _cachedWPos.y + (35f * ScreenScale.y),
                1f, 1f, _cachedTexts, Color.white,
                new Vector2(0f, 15f * ScreenScale.y));
            DebugRenderHelper.Box(boxPosition, boxSize, 1f * ScreenScale.y, Color.white);
        }
        else
        {
            if (_cachedTexts != null && _cachedTexts.Length > 0)
            {
                DebugRenderHelper.Strings(_cachedWPos.x, _cachedWPos.y + (35f * ScreenScale.y),
                    1f, 1f, _cachedTexts, Color.red,
                    new Vector2(0f, 15f * ScreenScale.y));

                Vector2 boxSize = new Vector2(100f, 100f) * ScreenScale.y;
                Vector2 boxPosition = new(_cachedWPos.x, _cachedWPos.y - (25f * ScreenScale.y));
                DebugRenderHelper.Box(boxPosition, boxSize, 1f * ScreenScale.y, Color.red);
            }
        }
    }

    private static Vector3 GetWorldPos(Vector3 worldPos)
    {
        var cam = Camera.main;
        Vector3 viewportPos = cam.WorldToViewportPoint(worldPos);

        float distance = Mathf.Round(Vector3.Distance(worldPos, cam.transform.parent.position));
        float size = Mathf.Clamp(1000f / distance, 10f, 50f);

        // Scale size based on screen resolution to maintain consistent appearance
        float screenScale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        size *= screenScale;

        Vector3 screenPos = new(
            viewportPos.x * Screen.width,
            (1 - viewportPos.y) * Screen.height,
            size
        );

        return screenPos;
    }
}