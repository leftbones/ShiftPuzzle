using Raylib_cs;
using static Raylib_cs.Raylib;

namespace ShiftPuzzle;

class Game {
    public Vector2i WindowSize { get; private set; }
    public Vector2i FieldSize { get; private set; }

    private int BlockSize = 32;

    public Vector2i CursorPos { get; private set; }

    private Vector2i WindowCenter { get { return WindowSize / 2; } }
    private Vector2i FieldOrigin { get { return WindowCenter - ((FieldSize * BlockSize) / 2); } }

    private Block[,] Field;
    private List<Block> FakeBlocks = new List<Block>();

    private float SpawnTimer = 0.0f;
    private float SpawnRate = 0.2f;

    private Color[] Colors = new Color[] {
        new Color(239, 71, 111, 255),       // Red
        // new Color(7, 197, 102, 255),        // Green
        new Color(1, 151, 244, 255),        // Blue
        new Color(255, 206, 92, 255),       // Yellow
        new Color(159, 89, 197, 255)        // Purple
    };

    private Dictionary<string, KeyboardKey> Keys = new Dictionary<string, KeyboardKey>() {
        { "MoveUp", KeyboardKey.KEY_W },
        { "MoveDown", KeyboardKey.KEY_S },
        { "ShiftLeft", KeyboardKey.KEY_A },
        { "ShiftRight", KeyboardKey.KEY_D },
    };

    private Random RNG = new Random();

    public Game() {
        // Setup
        WindowSize = new Vector2i(800, 600); 
        FieldSize = new Vector2i(6, 13);

        CursorPos = new Vector2i(0, (FieldSize.Y / 2) - 1);

        // Populate Field
        int i = 0;
        Field = new Block[FieldSize.X, FieldSize.Y];
        for (int x = 0; x < FieldSize.X; x++) {
            for (int y = 0; y < FieldSize.Y - 1; y++) {
                Field[x, y] = new Block(new Vector2i(FieldOrigin.X + (x * BlockSize), FieldOrigin.Y + (y * BlockSize)), RNG.Next(Colors.Length));
                i++;
            }
        }
    }

    public void Update() {
        ////
        // Input
        Input();


        ////
        // Timers
        SpawnTimer += SpawnRate;
        if (SpawnTimer >= 100.0f) {
            SpawnTimer = 0.0f;
        }


        ////
        // Field
        for (int y = FieldSize.Y - 1; y >= 0; y--) {
            for (int x = 0; x < FieldSize.X; x++) {
                Block B = Field[x, y];
                if (B is null || B.Type == -1)
                    continue;

                B.Update();
            }
        }
    }

    public void Draw() {
        ////
        // Field

        // Timer
        DrawRectangleLines(FieldOrigin.X - 3, FieldOrigin.Y - 30, FieldSize.X * BlockSize + 6, 16, Color.WHITE);
        DrawRectangleLines(FieldOrigin.X - 4, FieldOrigin.Y - 31, FieldSize.X * BlockSize + 8, 18, Color.WHITE);

        int Length = (FieldSize.X * BlockSize + 2) * Convert.ToInt32(SpawnTimer) / 100;
        DrawRectangle(FieldOrigin.X - 1, FieldOrigin.Y - 28, Length, 12, Color.WHITE);

        // Background
        DrawRectangle(FieldOrigin.X - 3, FieldOrigin.Y - 3, FieldSize.X * BlockSize + 6, (FieldSize.Y - 1) * BlockSize + 6, new Color(30, 30, 30, 255));

        // Border
        DrawRectangleLines(FieldOrigin.X - 3, FieldOrigin.Y - 3, FieldSize.X * BlockSize + 6, (FieldSize.Y - 1) * BlockSize + 6, Color.WHITE);
        DrawRectangleLines(FieldOrigin.X - 4, FieldOrigin.Y - 4, FieldSize.X * BlockSize + 8, (FieldSize.Y - 1) * BlockSize + 8, Color.WHITE);


        ////
        // Blocks
        foreach (Block B in Field) {
            if (B is null || B.Type == -1)
                continue;

            Color Base = Colors[B.Type];
            Color Accent = new Color(
                (byte)Math.Clamp(Base.r - 50, 0, 255),
                (byte)Math.Clamp(Base.g - 50, 0, 255),
                (byte)Math.Clamp(Base.b - 50, 0, 255),
                (byte)Base.a
            );

            DrawRectangle(B.Position.X + 1, B.Position.Y + 1, BlockSize - 2, BlockSize - 2, Base);
            DrawRectangle(B.Position.X + 2, B.Position.Y + 2, BlockSize - 4, BlockSize - 4, Accent);
        }

        // Fake Blocks
        foreach (Block B in FakeBlocks) {
            Block.Update();
        }


        ////
        // Cursor
        DrawRectangleLines(FieldOrigin.X + CursorPos.X * BlockSize - 1, FieldOrigin.Y + CursorPos.Y * BlockSize - 1, BlockSize * FieldSize.X + 2, BlockSize + 2, Color.WHITE);
        DrawRectangleLines(FieldOrigin.X + CursorPos.X * BlockSize, FieldOrigin.Y + CursorPos.Y * BlockSize, BlockSize * FieldSize.X, BlockSize, Color.WHITE);
    }

    public void Input() {
        if (IsKeyPressed(Keys["MoveUp"])) MoveCursor(-1);
        if (IsKeyPressed(Keys["MoveDown"])) MoveCursor(1);
        if (IsKeyPressed(Keys["ShiftLeft"])) ShiftRow(-1);
        if (IsKeyPressed(Keys["ShiftRight"])) ShiftRow(1);
    }

    public void MoveCursor(int dir)  {
        if (CursorPos.Y + dir >= 0 && CursorPos.Y + dir < FieldSize.Y - 1)
            CursorPos = new Vector2i(CursorPos.X, CursorPos.Y + dir);
    }

    public void ShiftRow(int dir) {
        // Get all blocks in the cursor row
        Block[] Row = new Block[FieldSize.X];
        for (int x = 0; x < FieldSize.X; x++) {
            Row[x] = Field[x, CursorPos.Y];
        }

        // Shift all the blocks
        foreach (Block Block in Row) {
            int Dest = Block.TargetPos.X + (BlockSize * dir);

            if (Dest == FieldOrigin.X + (FieldSize.X * BlockSize)) {
                Dest = FieldOrigin.X;
                FakeBlocks.Add(new Block(Block.Position, Block.Type));
                Block.Position = new Vector2i(FieldOrigin.X - BlockSize, Block.Position.Y);
            } else if (Dest == FieldOrigin.X - BlockSize) {
                Dest = FieldOrigin.X + (FieldSize.X * BlockSize) - BlockSize;
                FakeBlocks.Add(new Block(Block.Position, Block.Type));
                Block.Position = new Vector2i(FieldOrigin.X + (FieldSize.X * BlockSize), Block.Position.Y);
            }

            Block.TargetPos = new Vector2i(Dest, Block.TargetPos.Y);
        }
    }
}