// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Bubbles;

internal sealed class EntityMover
{
    internal const int Up = 0;
    internal const int Right = 1;
    internal const int Down = 2;
    internal const int Left = 3;

    private const byte BottomRow = 0x1A;
    private const byte RowCount = 0x1D;

    private const byte WrapBelow = 0x18;
    private const byte WrapType = 0x38;

    private const byte SubXCount = 0x04;

    private readonly ObjectTable _objects;

    internal EntityMover(ObjectTable objects)
    {
        ArgumentNullException.ThrowIfNull(objects);

        _objects = objects;
    }

    internal void Step(int slot, int direction)
    {
        switch (direction & 0x03)
        {
            case Up:
                MoveUp(slot);
                break;
            case Right:
                MoveRight(slot);
                break;
            case Down:
                MoveDown(slot);
                break;
            default:
                MoveLeft(slot);
                break;
        }
    }

    private void MoveUp(int slot)
    {
        _objects.SubY[slot] -= 2;
        _objects.Y[slot] -= 2;

        _objects.SubY[slot] &= 0x07;

        if (_objects.SubY[slot] != 0)
        {
            return;
        }

        _objects.Row[slot]--;

        if ((_objects.Row[slot] & 0x80) == 0)
        {
            return;
        }

        _objects.Row[slot] = BottomRow;
        Wrapped(slot);
    }

    private void MoveDown(int slot)
    {
        _objects.SubY[slot] += 2;
        _objects.Y[slot] += 2;

        if (_objects.SubY[slot] == 0x08)
        {
            _objects.SubY[slot] = 0;
            return;
        }

        if (_objects.SubY[slot] != 0x02)
        {
            return;
        }

        _objects.Row[slot]++;

        if (_objects.Row[slot] != RowCount)
        {
            return;
        }

        _objects.Row[slot] = 0;
        Wrapped(slot);
    }

    private void MoveRight(int slot)
    {
        _objects.X[slot] += 2;
        _objects.SubX[slot]++;

        if (_objects.SubX[slot] != SubXCount)
        {
            return;
        }

        _objects.SubX[slot] = 0;
        _objects.Column[slot]++;
    }

    private void MoveLeft(int slot)
    {
        _objects.X[slot] -= 2;
        _objects.SubX[slot]--;

        if ((_objects.SubX[slot] & 0x80) == 0)
        {
            return;
        }

        _objects.SubX[slot] = SubXCount - 1;
        _objects.Column[slot]--;
    }

    private void Wrapped(int slot)
    {
        if (_objects.Type[slot] >= WrapBelow)
        {
            return;
        }

        _objects.Type[slot] = WrapType;
    }
}
