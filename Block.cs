using Raylib_cs;

namespace ShiftPuzzle;

class Block {
    public Vector2i Position { get; set; }
    public Vector2i TargetPos { get; set; }
    public int Type { get; set; }
    public bool Matched { get; set; }

    public bool IsMoving { get { return Position != TargetPos; } }

    public Block(Vector2i pos, int type) {
        Position = pos;
        TargetPos = Position;
        Type = type;
    }

    public void Update() {
        if (IsMoving) {
            int MX = 0;
            int MY = 0;

            if (Position.X < TargetPos.X) MX = 8;
            if (Position.X > TargetPos.X) MX = -8;
            if (Position.Y < TargetPos.Y) MY = 8;
            if (Position.Y > TargetPos.Y) MY = -8;

            Move(MX, MY);
        }
    }

    private void Move(int x, int y) {
        Position = new Vector2i(Position.X + x, Position.Y + y);
    }
}