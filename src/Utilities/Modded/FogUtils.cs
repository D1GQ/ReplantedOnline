using Il2CppInterop.Runtime;
using Il2CppReloaded.Gameplay;

namespace ReplantedOnline.Utilities.Modded;

/// <summary>
/// Provides utility methods for manipulating stage fog in the game.
/// </summary>
internal static class FogUtils
{
    /// <summary>
    /// Clears fog around a plant with the specified radius.
    /// </summary>
    /// <param name="board">The board instance containing the fog data.</param>
    /// <param name="plant">The plant that is clearing the fog.</param>
    /// <param name="radius">The clearing radius. Use 4 for Plantern, 1 for Torchwood.</param>
    internal static void ClearFogAroundPlant(Board board, Plant plant, int radius)
    {
        if (board == null || plant == null)
            return;

        int fogReduction = 6;

        if (board.mFogBlownCountDown > 0)
        {
            fogReduction = 40;
            if (board.mFogBlownCountDown < 2000)
                fogReduction = 2;
        }

        int leftFogCol = board.LeftFogColumn();

        int plantGridX = PvZRUtils.ReloadedObjectXToGridX(plant.mX);
        int plantGridY = PvZRUtils.ReloadedObjectYToGridY(plant.mY) + 1;

        int minX = plantGridX - radius;
        int maxX = plantGridX + radius;
        int minY = plantGridY - radius;
        int maxY = plantGridY + radius;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (x >= leftFogCol && x < 12 && y >= 0 && y < 7)
                {
                    int distX = Math.Abs(x - plantGridX);
                    int distY = Math.Abs(y - plantGridY);

                    bool shouldClear;
                    if (radius == 1)
                    {
                        shouldClear = distX == 0 && distY == 0;
                    }
                    else if (radius == 3)
                    {
                        shouldClear = distX < 4 && distY < 3 && (distX + distY) != 5;
                    }
                    else
                    {
                        shouldClear = distX + distY <= radius;
                    }

                    if (shouldClear)
                    {
                        int currentFog = GetFogAt(board, x, y);
                        int newFog = currentFog - fogReduction;
                        if (newFog < 0)
                            newFog = 0;
                        SetFogAt(board, x, y, newFog);
                    }
                }
            }
        }
    }

    private const int COLS = 12;
    private const int ROWS = 7;
    private static uint? _fogFieldOffset;

    /// <summary>
    /// Gets the memory offset of the mGridCelFog field.
    /// </summary>
    /// <returns>The field offset in bytes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the field cannot be found.</exception>
    private static unsafe uint GetFogFieldOffset()
    {
        if (_fogFieldOffset.HasValue)
            return _fogFieldOffset.Value;

        // Get class and field pointer
        IntPtr klassPtr = Il2CppClassPointerStore<Board>.NativeClassPtr;
        IntPtr fieldPtr = IL2CPP.GetIl2CppField(klassPtr, nameof(Board.mGridCelFog));

        // Return 0 if field not found
        if (fieldPtr == IntPtr.Zero)
        {
            return 0;
        }

        // Get the offset of this field within the Board object's memory layout
        _fogFieldOffset = IL2CPP.il2cpp_field_get_offset(fieldPtr);
        return _fogFieldOffset.Value;
    }

    /// <summary>
    /// Gets the fog value at the specified grid position.
    /// </summary>
    /// <param name="board">The board instance containing the fog data.</param>
    /// <param name="x">The column index (0-11).</param>
    /// <param name="y">The row index (0-6).</param>
    /// <returns>The fog value (0 = clear, 255 = full fog). Returns 255 on error or out of bounds.</returns>
    internal static unsafe int GetFogAt(Board board, int x, int y)
    {
        if (board == null || board.Pointer == IntPtr.Zero)
            return 255;

        if (x < 0 || x >= COLS || y < 0 || y >= ROWS)
            return 255;

        try
        {
            // Get the offset where the fog array pointer is stored
            uint offset = GetFogFieldOffset();

            // Get the native Board pointer
            byte* boardPtr = (byte*)board.Pointer.ToPointer();

            // Read the fog array pointer from the Board object at the calculated offset
            // This is a double pointer: boardPtr + offset points to a pointer to the array
            int** fogArrayPtrPtr = (int**)(boardPtr + offset);
            int* fogArrayPtr = *fogArrayPtrPtr;

            // NULL check, array might not exist
            if (fogArrayPtr == null)
                return 255;

            // Read the number of rows from the array metadata
            // In Il2Cpp arrays, the metadata is stored before the actual data:
            // At offset 0x10: Array header
            // At offset 0x14: Number of rows (for multi dimensional arrays)
            int numRows = *(int*)((byte*)fogArrayPtr + 0x14);
            if (numRows != ROWS)
                numRows = ROWS;  // Fallback to expected value if metadata is wrong

            // Calculate flattened index for the 2D array: [x, y] -> index = x * rows + y
            int index = x * numRows + y;

            // Array data starts at offset 0x20 in Il2CppArray
            // Each element is 4 bytes (int)
            int* dataStart = (int*)((byte*)fogArrayPtr + 0x20);

            // Return the fog value at the calculated index
            return dataStart[index];
        }
        catch
        {
            return 255;
        }
    }

    /// <summary>
    /// Sets the fog value at the specified grid position.
    /// </summary>
    /// <param name="board">The board instance containing the fog data.</param>
    /// <param name="x">The column index (0-11).</param>
    /// <param name="y">The row index (0-6).</param>
    /// <param name="value">The fog value to set (0 = clear, 255 = full fog). Will be clamped to 0-255.</param>
    internal static unsafe void SetFogAt(Board board, int x, int y, int value)
    {
        if (board == null || board.Pointer == IntPtr.Zero)
            return;

        if (x < 0 || x >= COLS || y < 0 || y >= ROWS)
            return;

        try
        {
            // Clamp the value to valid fog range
            int clamped = Math.Clamp(value, 0, 255);

            // Get the offset where the fog array pointer is stored
            uint offset = GetFogFieldOffset();

            // Get the native Board pointer and fog array pointer
            byte* boardPtr = (byte*)board.Pointer.ToPointer();
            int** fogArrayPtrPtr = (int**)(boardPtr + offset);
            int* fogArrayPtr = *fogArrayPtrPtr;

            if (fogArrayPtr == null)
                return;

            // Read the number of rows from the array metadata
            int numRows = *(int*)((byte*)fogArrayPtr + 0x14);
            if (numRows != ROWS)
                numRows = ROWS;

            // Calculate flattened index and set the fog value
            int index = x * numRows + y;
            int* dataStart = (int*)((byte*)fogArrayPtr + 0x20);
            dataStart[index] = clamped;
        }
        catch { }
    }

    /// <summary>
    /// Checks if the board has a valid fog array.
    /// </summary>
    /// <param name="board">The board instance to check.</param>
    /// <returns>True if the fog array exists, false otherwise.</returns>
    internal static unsafe bool HasFogArray(Board board)
    {
        // Safety checks
        if (board == null || board.Pointer == IntPtr.Zero)
            return false;

        try
        {
            // Try to get and validate the fog array pointer
            uint offset = GetFogFieldOffset();
            byte* boardPtr = (byte*)board.Pointer.ToPointer();
            int** fogArrayPtrPtr = (int**)(boardPtr + offset);
            int* fogArrayPtr = *fogArrayPtrPtr;

            // Return true only if the pointer is not null
            return fogArrayPtr != null;
        }
        catch
        {
            return false;
        }
    }
}