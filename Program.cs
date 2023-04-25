using Raylib_cs;
using static Raylib_cs.Raylib;

namespace ShiftPuzzle;

class Program {
    static void Main(string[] args) {
        ////
        // Init
        var Game = new Game();
        InitWindow(Game.WindowSize.X, Game.WindowSize.Y, "Shift");
        SetTargetFPS(60);
        

        ////
        // Loop
        while (!WindowShouldClose()) {
            ////
            // Update
            Game.Update();

            ////
            // Draw
            BeginDrawing();
            ClearBackground(new Color(40, 40, 40, 255));

            Game.Draw();

            EndDrawing();
        }

        ////
        // Exit
        CloseWindow();
    }
}
