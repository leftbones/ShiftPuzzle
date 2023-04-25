using Raylib_cs;
using static Raylib_cs.Raylib;

namespace ShiftPuzzle;

enum GameState {
    Normal,
    ShiftBlocks,
    DropBlocks,
    DestroyMatches
};

class Game {
    public Vector2i WindowSize { get; private set; }
    public Vector2i FieldSize { get; private set; }
    public Vector2i CursorPos { get; private set; }
    public GameState State { get; private set; }

    private int BlockSize = 32;

    private Vector2i WindowCenter { get { return WindowSize / 2; } }
    private Vector2i FieldOrigin { get { return new Vector2i(WindowCenter.X - ((FieldSize.X * BlockSize) / 2), WindowCenter.Y - ((FieldSize.Y - 1) * BlockSize) / 2); } }

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

    private Color MaskColor = new Color(40, 40, 40, 255);

    private Random RNG = new Random();

    public Game() {
        // Setup
        WindowSize = new Vector2i(800, 600); 
        FieldSize = new Vector2i(6, 13);
        CursorPos = new Vector2i(0, (FieldSize.Y / 2) - 1);
        State = GameState.Normal;

        // Populate Field
        int i = 0;
        Field = new Block[FieldSize.X, FieldSize.Y];
        for (int x = 0; x < FieldSize.X; x++) {
            for (int y = 0; y < FieldSize.Y - 1; y++) {
                NewBlock(x, y, RNG.Next(Colors.Length));
                i++;
            }
        }
    }

    // Update all the things
    public void Update() {
        // Accept input in any state
        Input();

        ////
        // Normal State

        if (State == GameState.Normal) {
            ////
            // Timers
            SpawnTimer += SpawnRate;
            if (SpawnTimer >= 100.0f) {
                SpawnTimer = 0.0f;
            }


            ////
            // Field
            
            // Blocks
            for (int y = FieldSize.Y - 1; y >= 0; y--) {
                for (int x = 0; x < FieldSize.X; x++) {
                    Block B = Field[x, y];
                    if (B is null || B.Type == -1 || B.IsMoving)
                        continue;

                    // Check if block should drop
                    if (ShouldDrop(x, y))
                        DropBlock(x, y);

                    // Match Vertical
                    List<Block> Matches = new List<Block>() { B };

                    if (IsMatch(x, y-1, B.Type)) {
                        Matches.Add(Field[x, y-1]);
                        if (IsMatch(x, y-2, B.Type))
                            Matches.Add(Field[x, y-2]);
                    }

                    if (IsMatch(x, y+1, B.Type)) {
                        Matches.Add(Field[x, y+1]);
                        if (IsMatch(x, y+2, B.Type))
                            Matches.Add(Field[x, y+2]);
                    }

                    if (Matches.Count >= 3) {
                        foreach (Block M in Matches) {
                            B.Matched = true;
                            State = GameState.DestroyMatches;
                        }
                    }

                    // Match Horizontal
                    Matches = new List<Block>() { B };

                    if (IsMatch(x-1, y, B.Type)) {
                        Matches.Add(Field[x-1, y]);
                        if (IsMatch(x-2, y, B.Type))
                            Matches.Add(Field[x-2, y]);
                    }

                    if (IsMatch(x+1, y, B.Type)) {
                        Matches.Add(Field[x+1, y]);
                        if (IsMatch(x+2, y, B.Type))
                            Matches.Add(Field[x+2, y]);
                    }

                    if (Matches.Count >= 3) {
                        foreach (Block M in Matches) {
                            B.Matched = true;
                            State = GameState.DestroyMatches;
                        }
                    }
                }
            }
        }


        //// 
        // Shift Blocks State

        else if (State == GameState.ShiftBlocks) {
            bool GoToDropBlocks = true;

            // Blocks
            foreach (Block B in Field) {
                if (B is null || B.Type == -1)
                    continue;

                B.Update();
                if (B.IsMoving)
                    GoToDropBlocks = false;
            }

            // Fake Blocks
            for (int i = FakeBlocks.Count - 1; i >= 0; i--) {
                Block B = FakeBlocks[i];
                B.Update();
                
                if (!B.IsMoving)
                    FakeBlocks.Remove(B);
            }

            if (GoToDropBlocks)
                State = GameState.DropBlocks;
        }


        ////
        // Destroy Matches State

        else if (State == GameState.DestroyMatches) {
            bool GoToDropBlocks = true;

            for (int y = FieldSize.Y - 2; y >= 0; y--) {
                for (int x = FieldSize.X - 1; x >= 0; x--) {
                    Block B = Field[x, y];
                    if (B.Type == -1)
                        continue;

                    if (B.Matched) {
                        GoToDropBlocks = false;
                        NewBlock(x, y, -1);
                        return;
                    }
                }
            }

            if (GoToDropBlocks) {
                State = GameState.DropBlocks;
            }
        }


        ////
        // Drop Blocks State

        else if (State == GameState.DropBlocks) {
            bool GoToNormal = true;

            for (int y = FieldSize.Y - 2; y >= 0; y--) {
                for (int x = FieldSize.X - 1; x >= 0; x--) {
                    Block B = Field[x, y];
                    B.Update();
                    if (B.IsMoving)
                        GoToNormal = false;
                }
            }

            if (GoToNormal) {
                bool DropAgain = false;

                for (int y = FieldSize.Y - 2; y >= 0; y--) {
                    for (int x = FieldSize.X - 1; x >= 0; x--) {
                        Block B = Field[x, y];
                        if (ShouldDrop(x, y)) {
                            DropBlock(x, y);
                            DropAgain = true;
                        }
                    }
                }
                
                if (!DropAgain)
                    State = GameState.Normal;
            }
        }
    }

    // Draw all the things
    public void Draw() {
        ////
        // Debug
        DrawText(String.Format("State: {0}", State.ToString()), 5, 5, 20, Color.WHITE);

        ////
        // Field

        // Timer
        DrawRectangleLines(FieldOrigin.X - 1, FieldOrigin.Y - 28, FieldSize.X * BlockSize + 6, 16, Color.BLACK); // Shadow
        DrawRectangleLines(FieldOrigin.X - 2, FieldOrigin.Y - 29, FieldSize.X * BlockSize + 8, 18, Color.BLACK); // Shadow
        DrawRectangleLines(FieldOrigin.X - 3, FieldOrigin.Y - 30, FieldSize.X * BlockSize + 6, 16, Color.WHITE);
        DrawRectangleLines(FieldOrigin.X - 4, FieldOrigin.Y - 31, FieldSize.X * BlockSize + 8, 18, Color.WHITE);

        int Length = (FieldSize.X * BlockSize + 2) * Convert.ToInt32(SpawnTimer) / 100;
        DrawRectangle(FieldOrigin.X - 1, FieldOrigin.Y - 28, Length, 12, Color.WHITE);

        // Background
        DrawRectangle(FieldOrigin.X - 3, FieldOrigin.Y - 3, FieldSize.X * BlockSize + 6, (FieldSize.Y - 1) * BlockSize + 6, new Color(30, 30, 30, 255));

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

            DrawRectangle(B.Position.X + 3, B.Position.Y + 3, BlockSize - 2, BlockSize - 2, Color.BLACK); // Shadow
            DrawRectangle(B.Position.X + 1, B.Position.Y + 1, BlockSize - 2, BlockSize - 2, Base);
            DrawRectangle(B.Position.X + 2, B.Position.Y + 2, BlockSize - 4, BlockSize - 4, Accent);
        }

        // Fake Blocks
        foreach (Block B in FakeBlocks) {
            Color Base = Colors[B.Type];
            Color Accent = new Color(
                (byte)Math.Clamp(Base.r - 50, 0, 255),
                (byte)Math.Clamp(Base.g - 50, 0, 255),
                (byte)Math.Clamp(Base.b - 50, 0, 255),
                (byte)Base.a
            );

            DrawRectangle(B.Position.X + 3, B.Position.Y + 3, BlockSize - 2, BlockSize - 2, Color.BLACK); // Shadow
            DrawRectangle(B.Position.X + 1, B.Position.Y + 1, BlockSize - 2, BlockSize - 2, Base);
            DrawRectangle(B.Position.X + 2, B.Position.Y + 2, BlockSize - 4, BlockSize - 4, Accent);
        }

        // Mask
        DrawRectangle(FieldOrigin.X - BlockSize - 4, FieldOrigin.Y, BlockSize + 4, (FieldSize.Y - 1) * BlockSize, MaskColor);
        DrawRectangle(FieldOrigin.X + (FieldSize.X * BlockSize), FieldOrigin.Y, BlockSize + 4, (FieldSize.Y - 1) * BlockSize, MaskColor);

        // Cursor Shadow
        DrawRectangleLines(FieldOrigin.X + CursorPos.X * BlockSize + 1, FieldOrigin.Y + CursorPos.Y * BlockSize + 1, BlockSize * FieldSize.X + 2, BlockSize + 2, Color.BLACK);  // Shadow
        DrawRectangleLines(FieldOrigin.X + CursorPos.X * BlockSize + 2, FieldOrigin.Y + CursorPos.Y * BlockSize + 2, BlockSize * FieldSize.X, BlockSize, Color.BLACK);          // Shadow

        // Border
        DrawRectangleLines(FieldOrigin.X - 1, FieldOrigin.Y - 1, FieldSize.X * BlockSize + 6, (FieldSize.Y - 1) * BlockSize + 6, Color.BLACK); // Shadow
        DrawRectangleLines(FieldOrigin.X - 2, FieldOrigin.Y - 2, FieldSize.X * BlockSize + 8, (FieldSize.Y - 1) * BlockSize + 8, Color.BLACK); // Shadow
        DrawRectangleLines(FieldOrigin.X - 3, FieldOrigin.Y - 3, FieldSize.X * BlockSize + 6, (FieldSize.Y - 1) * BlockSize + 6, Color.WHITE);
        DrawRectangleLines(FieldOrigin.X - 4, FieldOrigin.Y - 4, FieldSize.X * BlockSize + 8, (FieldSize.Y - 1) * BlockSize + 8, Color.WHITE);

        // Cursor
        DrawRectangleLines(FieldOrigin.X + CursorPos.X * BlockSize - 1, FieldOrigin.Y + CursorPos.Y * BlockSize - 1, BlockSize * FieldSize.X + 2, BlockSize + 2, Color.WHITE);
        DrawRectangleLines(FieldOrigin.X + CursorPos.X * BlockSize, FieldOrigin.Y + CursorPos.Y * BlockSize, BlockSize * FieldSize.X, BlockSize, Color.WHITE);
    }

    // Get and handle user input depending on state
    public void Input() {
        if (IsKeyPressed(Keys["MoveUp"])) MoveCursor(-1);
        if (IsKeyPressed(Keys["MoveDown"])) MoveCursor(1);

        // Normal state only
        if (State == GameState.Normal || State == GameState.DropBlocks) {
            if (IsKeyPressed(Keys["ShiftLeft"])) ShiftRow(-1);
            if (IsKeyPressed(Keys["ShiftRight"])) ShiftRow(1);
        }
    }

    // Add a new block to the field
    public void NewBlock(int x, int y, int type) {
        if (!InBounds(x, y))
            return;

        Field[x, y] = new Block(new Vector2i(FieldOrigin.X + (x * BlockSize), FieldOrigin.Y + (y * BlockSize)), type);
    }

    // Check if a block should drop (has an empty space below it)
    public bool ShouldDrop(int x, int y) {
        Block B = Field[x, y];
        return B.Type != -1 && !B.Matched && !B.IsMoving && y < FieldSize.Y - 1 && IsMatch(x, y+1, -1);
    }

    // Check if the block at the position given matches the type given
    public bool IsMatch(int x, int y, int type) {
        if (!InBounds(x, y))
            return false;

        Block B = Field[x, y];
        if (B.Position == B.TargetPos && B.Type == type)
            return true;

        return false;
    }

    // Check if the position given is within the game field
    public bool InBounds(int x, int y) {
        return x >= 0 && x < FieldSize.X && y >= 0 && y < FieldSize.Y - 1;
    }

    // Move the cursor up or down
    public void MoveCursor(int dir)  {
        if (CursorPos.Y + dir >= 0 && CursorPos.Y + dir < FieldSize.Y - 1)
            CursorPos = new Vector2i(CursorPos.X, CursorPos.Y + dir);
    }

    // Drop a block (swap it with the block below it)
    public void DropBlock(int x, int y) {
        Block Upper = Field[x, y];
        Block Lower = Field[x, y+1];
        Upper.TargetPos = new Vector2i(Upper.TargetPos.X, Upper.TargetPos.Y + BlockSize);
        Lower.TargetPos = new Vector2i(Lower.TargetPos.X, Lower.TargetPos.Y - BlockSize);
        Field[x, y] = Lower;
        Field[x, y+1] = Upper;
    }

    // Shift a row of blocks left or right
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
                if (Block.Type != -1) {
                    Block FakeBlock = new Block(Block.Position, Block.Type);
                    FakeBlock.TargetPos = new Vector2i(FieldOrigin.X + (FieldSize.X * BlockSize), FakeBlock.TargetPos.Y);
                    FakeBlocks.Add(FakeBlock);
                }

                Dest = FieldOrigin.X;
                Block.Position = new Vector2i(FieldOrigin.X - BlockSize, Block.Position.Y);
            } else if (Dest == FieldOrigin.X - BlockSize) {
                if (Block.Type != -1) {
                    Block FakeBlock = new Block(Block.Position, Block.Type);
                    FakeBlock.TargetPos = new Vector2i(FieldOrigin.X - BlockSize, FakeBlock.TargetPos.Y);
                    FakeBlocks.Add(FakeBlock);
                }

                Dest = FieldOrigin.X + (FieldSize.X * BlockSize) - BlockSize;
                Block.Position = new Vector2i(FieldOrigin.X + (FieldSize.X * BlockSize), Block.Position.Y);
            }

            Block.TargetPos = new Vector2i(Dest, Block.TargetPos.Y);
        }

        // Shift field array to match visual field
        Block[] Temp = new Block[Row.Length];
        if (dir < 0) {
            for (int i = 0; i < Row.Length - 1; i++) {
                Temp[i] = Row[i + 1];
            }
            Temp[Temp.Length - 1] = Row[0];
        } else if (dir > 0) {
            for (int i = 1; i < Row.Length; i++) {
                Temp[i] = Row[i - 1];
            }
            Temp[0] = Row[Temp.Length - 1];
        }

        for (int i = 0; i < Temp.Length; i++) {
            Field[i, CursorPos.Y] = Temp[i];
        }

        State = GameState.ShiftBlocks;
    }
}