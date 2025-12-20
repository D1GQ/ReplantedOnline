using Microsoft.VisualBasic;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;

namespace ReplantedOnline.Managers;

/// <summary>
/// Manages content loading and seasonal content activation for the game.
/// </summary>
internal static class ContentManager
{
    /// <summary>
    /// Initializes the content manager and performs initial content checks.
    /// </summary>
    internal static void Initialize()
    {
        CheckDecemberContent();
    }

    /// <summary>
    /// Checks and activates/deactivates December seasonal content based on the current month.
    /// </summary>
    private static void CheckDecemberContent()
    {
        bool isDecember = DateAndTime.Now.Month is 12;

        foreach (var plant in Instances.DataServiceActivity.Service.PlantDefinitions.EnumerateIl2CppReadonlyList())
        {
            if (isDecember)
            {
                if (plant.m_decemberGameObject != null)
                {
                    plant.m_decemberChance100 = 100;
                }
            }
            else
            {
                plant.m_decemberChance100 = 0;
            }
        }

        foreach (var zombie in Instances.DataServiceActivity.Service.ZombieDefinitions.EnumerateIl2CppReadonlyList())
        {
            if (isDecember)
            {
                if (zombie.m_decemberGameObject != null)
                {
                    zombie.m_decemberChance100 = 100;
                }
            }
            else
            {
                zombie.m_decemberChance100 = 0;
            }
        }
    }
}