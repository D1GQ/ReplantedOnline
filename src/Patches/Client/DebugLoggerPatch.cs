using HarmonyLib;
using Il2CppReloaded.Characters;
using ReplantedOnline.Monos;
using System.Reflection;
using System.Text;

namespace ReplantedOnline.Patches.Client;

internal static class DebugLoggerPatch
{
    private static readonly MethodBase[] Targets =
    [
        AccessTools.Method(typeof(CharacterAnimationController), nameof(CharacterAnimationController.PlayAnimation)),
    ];

    internal static void Patch()
    {
        foreach (var method in Targets)
        {
            if (method != null)
            {
                ReplantedOnlineMod.harmony.Patch(method, postfix: new HarmonyMethod(typeof(DebugLoggerPatch), nameof(Postfix)));
            }
        }
    }

    private static void LogInfo(string className, string methodName, object[] args, ParameterInfo[] parameters)
    {
        if (!InfoDisplay.DebugLoggerEnabled) return;

        var sb = new StringBuilder();
        sb.Append($"{className}.{methodName}: ");

        if (args != null && args.Length > 0)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (i < parameters.Length)
                    sb.Append($"{parameters[i].Name}=");

                if (args[i] != null)
                {
                    var type = args[i].GetType();
                    if (type.IsEnum)
                        sb.Append($"{type.Name}.{args[i]} ({(int)args[i]})");
                    else if (type.IsArray)
                    {
                        var array = (Array)args[i];
                        sb.Append("[");
                        for (int j = 0; j < array.Length; j++)
                        {
                            sb.Append(array.GetValue(j));
                            if (j < array.Length - 1) sb.Append(", ");
                        }
                        sb.Append(']');
                    }
                    else
                        sb.Append(args[i]);
                }
                else
                    sb.Append("null");

                if (i < args.Length - 1)
                    sb.Append(", ");
            }
        }

        ReplantedOnlineMod.DebugLogger.Msg(sb.ToString());
    }

    private static void Postfix(object[] __args, MethodBase __originalMethod)
    {
        var parameters = __originalMethod.GetParameters();
        LogInfo(__originalMethod.DeclaringType.Name, __originalMethod.Name, __args, parameters);
    }
}