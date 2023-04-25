using Raylib_cs;

namespace ShiftPuzzle;

class Block {
    public Vector2i Position { get; set; }
    public Vector2i TargetPos { get; set; }
    public int Type { get; private set; }

    public Block(Vector2i pos, int type) {
        Position = pos;
        TargetPos = Position;
        Type = type;
    }

    public void Update() {
        if (Position != TargetPos) {
            int MX = 0;
            int MY = 0;

            if (Position.X < TargetPos.X) MX = 4;
            if (Position.X > TargetPos.X) MX = -4;
            if (Position.Y < TargetPos.Y) MY = 4;
            if (Position.Y > TargetPos.Y) MY = -4;

            Move(MX, MY);
        }
    }

    public void Shift(int dir) {
    }

    private void Move(int x, int y) {
        Position = new Vector2i(Position.X + x, Position.Y + y);
    }
}