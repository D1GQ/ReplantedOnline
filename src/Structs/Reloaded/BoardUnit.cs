using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Structs.Reloaded;

/// <summary>
/// Represents a board unit in the X dimension, storing both grid index and board position.
/// </summary>
internal readonly struct BoardUnitX
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoardUnitX"/> struct from a grid index and board position.
    /// </summary>
    /// <param name="gridX">The grid X index.</param>
    /// <param name="posX">The board X position.</param>
    internal BoardUnitX(int gridX, float posX)
    {
        Grid = gridX;
        Pos = posX;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoardUnitX"/> struct from a grid index.
    /// </summary>
    /// <param name="gridX">The grid X index.</param>
    internal BoardUnitX(int gridX)
    {
        Grid = gridX;
        Pos = PvZRUtils.GridXToReloadedObjectX(gridX);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoardUnitX"/> struct from a board position.
    /// </summary>
    /// <param name="posX">The board X position.</param>
    internal BoardUnitX(float posX)
    {
        Grid = PvZRUtils.ReloadedObjectXToGridX(posX);
        Pos = posX;
    }

    /// <summary>
    /// Gets the grid X index.
    /// </summary>
    internal readonly int Grid;

    /// <summary>
    /// Gets the board X position.
    /// </summary>
    internal readonly float Pos;

    /// <summary>
    /// Returns the grid X index.
    /// </summary>
    /// <param name="unit">The board unit.</param>
    public static implicit operator int(BoardUnitX unit) => unit.Grid;

    /// <summary>
    /// Returns the board X position.
    /// </summary>
    /// <param name="unit">The board unit.</param>
    public static implicit operator float(BoardUnitX unit) => unit.Pos;

    /// <summary>
    /// Creates a <see cref="BoardUnitX"/> from a grid index.
    /// </summary>
    /// <param name="gridX">The grid X index.</param>
    public static implicit operator BoardUnitX(int gridX) => new(gridX);

    /// <summary>
    /// Creates a <see cref="BoardUnitX"/> from a board position.
    /// </summary>
    /// <param name="posX">The board X position.</param>
    public static implicit operator BoardUnitX(float posX) => new(posX);

    /// <summary>
    /// Determines whether two <see cref="BoardUnitX"/> instances are equal.
    /// </summary>
    /// <param name="left">The left instance.</param>
    /// <param name="right">The right instance.</param>
    /// <returns><see langword="true"/> if equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(BoardUnitX left, BoardUnitX right) => left.Grid == right.Grid;

    /// <summary>
    /// Determines whether two <see cref="BoardUnitX"/> instances are not equal.
    /// </summary>
    /// <param name="left">The left instance.</param>
    /// <param name="right">The right instance.</param>
    /// <returns><see langword="true"/> if not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(BoardUnitX left, BoardUnitX right) => !(left == right);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is BoardUnitX other && this == other;

    /// <inheritdoc/>
    public override int GetHashCode() => Grid.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => $"GridX: {Grid}, PosX: {Pos}";

    /// <summary>
    /// Serializes the <see cref="BoardUnitX"/> to a packet writer.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write to.</param>
    internal void Serialize(PacketWriter packetWriter)
    {
        packetWriter.WriteInt(Grid);
        packetWriter.WriteFloat(Pos);
    }

    /// <summary>
    /// Deserializes a <see cref="BoardUnitX"/> from a packet reader.
    /// </summary>
    /// <param name="packetReader">The packet reader to read from.</param>
    /// <returns>A new <see cref="BoardUnitX"/> instance deserialized from the packet.</returns>
    internal static BoardUnitX Deserialize(PacketReader packetReader)
    {
        var grid = packetReader.ReadInt();
        var pos = packetReader.ReadFloat();
        return new BoardUnitX(grid, pos);
    }
}

/// <summary>
/// Represents a board unit in the Y dimension, storing both grid index and board position.
/// </summary>
internal readonly struct BoardUnitY
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoardUnitY"/> struct from a grid index and board position.
    /// </summary>
    /// <param name="gridY">The grid Y index.</param>
    /// <param name="posY">The board Y position.</param>
    internal BoardUnitY(int gridY, float posY)
    {
        Grid = gridY;
        Pos = posY;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoardUnitY"/> struct from a grid index.
    /// </summary>
    /// <param name="gridY">The grid Y index.</param>
    internal BoardUnitY(int gridY)
    {
        Grid = gridY;
        Pos = PvZRUtils.GridYToReloadedObjectY(gridY);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoardUnitY"/> struct from a board position.
    /// </summary>
    /// <param name="posY">The board Y position.</param>
    internal BoardUnitY(float posY)
    {
        Grid = PvZRUtils.ReloadedObjectYToGridY(posY);
        Pos = posY;
    }

    /// <summary>
    /// Gets the grid Y index.
    /// </summary>
    internal readonly int Grid;

    /// <summary>
    /// Gets the board Y position.
    /// </summary>
    internal readonly float Pos;

    /// <summary>
    /// Returns the grid Y index.
    /// </summary>
    /// <param name="unit">The board unit.</param>
    public static implicit operator int(BoardUnitY unit) => unit.Grid;

    /// <summary>
    /// Returns the board Y position.
    /// </summary>
    /// <param name="unit">The board unit.</param>
    public static implicit operator float(BoardUnitY unit) => unit.Pos;

    /// <summary>
    /// Creates a <see cref="BoardUnitY"/> from a grid index.
    /// </summary>
    /// <param name="gridY">The grid Y index.</param>
    public static implicit operator BoardUnitY(int gridY) => new(gridY);

    /// <summary>
    /// Creates a <see cref="BoardUnitY"/> from a board position.
    /// </summary>
    /// <param name="posY">The board Y position.</param>
    public static implicit operator BoardUnitY(float posY) => new(posY);

    /// <summary>
    /// Determines whether two <see cref="BoardUnitY"/> instances are equal.
    /// </summary>
    /// <param name="left">The left instance.</param>
    /// <param name="right">The right instance.</param>
    /// <returns><see langword="true"/> if equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(BoardUnitY left, BoardUnitY right) => left.Grid == right.Grid;

    /// <summary>
    /// Determines whether two <see cref="BoardUnitY"/> instances are not equal.
    /// </summary>
    /// <param name="left">The left instance.</param>
    /// <param name="right">The right instance.</param>
    /// <returns><see langword="true"/> if not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(BoardUnitY left, BoardUnitY right) => !(left == right);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is BoardUnitY other && this == other;

    /// <inheritdoc/>
    public override int GetHashCode() => Grid.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => $"GridY: {Grid}, PosY: {Pos}";

    /// <summary>
    /// Serializes the <see cref="BoardUnitY"/> to a packet writer.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write to.</param>
    internal void Serialize(PacketWriter packetWriter)
    {
        packetWriter.WriteInt(Grid);
        packetWriter.WriteFloat(Pos);
    }

    /// <summary>
    /// Deserializes a <see cref="BoardUnitY"/> from a packet reader.
    /// </summary>
    /// <param name="packetReader">The packet reader to read from.</param>
    /// <returns>A new <see cref="BoardUnitY"/> instance deserialized from the packet.</returns>
    internal static BoardUnitY Deserialize(PacketReader packetReader)
    {
        var grid = packetReader.ReadInt();
        var pos = packetReader.ReadFloat();
        return new BoardUnitY(grid, pos);
    }
}