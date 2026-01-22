using System.Diagnostics;

namespace FlappyBird;

internal static class Program
{
    private const int ScreenWidth = 60;
    private const int ScreenHeight = 20;
    private const int GroundRow = ScreenHeight - 1;
    private const char BirdChar = '@';
    private const char PipeChar = '#';
    private const char GroundChar = '=';
    private const char EmptyChar = ' ';

    private const float Gravity = 18f;
    private const float FlapVelocity = -6.5f;
    private const float PipeSpeed = 18f;
    private const float SpawnInterval = 1.8f;
    private const int PipeGap = 6;

    private sealed record Pipe(float X, int GapY, int GapHeight);

    private static readonly Random Random = new();

    private static void Main()
    {
        Console.CursorVisible = false;
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        ShowIntro();

        while (true)
        {
            RunGame();
            Console.SetCursorPosition(0, ScreenHeight + 2);
            Console.Write("Press R to restart or Q to quit. ");

            while (true)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.R)
                {
                    Console.Clear();
                    break;
                }

                if (key == ConsoleKey.Q || key == ConsoleKey.Escape)
                {
                    return;
                }
            }
        }
    }

    private static void ShowIntro()
    {
        Console.Clear();
        Console.WriteLine("Flappy Bird (ASCII) - C#");
        Console.WriteLine();
        Console.WriteLine("Controls:");
        Console.WriteLine("  Space / Up Arrow - flap");
        Console.WriteLine("  Q - quit");
        Console.WriteLine();
        Console.WriteLine("Press any key to start...");
        Console.ReadKey(true);
        Console.Clear();
    }

    private static void RunGame()
    {
        var pipes = new List<Pipe>();
        var birdY = ScreenHeight / 2f;
        var birdVelocity = 0f;
        var birdX = 12;
        var score = 0;
        var lastSpawn = 0f;
        var stopwatch = Stopwatch.StartNew();
        var lastFrame = stopwatch.ElapsedMilliseconds;
        var gameOver = false;

        while (!gameOver)
        {
            var now = stopwatch.ElapsedMilliseconds;
            var deltaTime = (now - lastFrame) / 1000f;
            if (deltaTime < 0.001f)
            {
                continue;
            }

            lastFrame = now;

            HandleInput(ref birdVelocity, ref gameOver);
            UpdateBird(ref birdY, ref birdVelocity, deltaTime);
            UpdatePipes(pipes, deltaTime, ref lastSpawn, ref score);

            gameOver = CheckCollision(birdY, birdX, pipes);
            RenderFrame(birdY, birdX, pipes, score);

            const int targetFrameMs = 33; // ~30 FPS
            var frameElapsed = stopwatch.ElapsedMilliseconds - now;
            if (frameElapsed < targetFrameMs)
            {
                Thread.Sleep((int)(targetFrameMs - frameElapsed));
            }
        }

        Console.SetCursorPosition(0, ScreenHeight + 1);
        Console.WriteLine("Game Over! Final Score: {0}", score);
    }

    private static void HandleInput(ref float birdVelocity, ref bool gameOver)
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.Spacebar || key == ConsoleKey.UpArrow)
            {
                birdVelocity = FlapVelocity;
            }
            else if (key == ConsoleKey.Q || key == ConsoleKey.Escape)
            {
                gameOver = true;
            }
        }
    }

    private static void UpdateBird(ref float birdY, ref float birdVelocity, float deltaTime)
    {
        birdVelocity += Gravity * deltaTime;
        birdY += birdVelocity * deltaTime;

        if (birdY < 0)
        {
            birdY = 0;
            birdVelocity = 0;
        }
    }

    private static void UpdatePipes(List<Pipe> pipes, float deltaTime, ref float lastSpawn, ref int score)
    {
        for (var i = 0; i < pipes.Count; i++)
        {
            var pipe = pipes[i];
            var newPipe = pipe with { X = pipe.X - PipeSpeed * deltaTime };
            pipes[i] = newPipe;
        }

        pipes.RemoveAll(pipe => pipe.X < -2);

        lastSpawn += deltaTime;
        if (lastSpawn >= SpawnInterval)
        {
            lastSpawn = 0f;
            pipes.Add(CreatePipe());
        }

        foreach (var pipe in pipes)
        {
            if (!pipe.X.Equals(birdX) && pipe.X < birdX && pipe.X > birdX - 1.5f)
            {
                score += 1;
            }
        }
    }

    private static Pipe CreatePipe()
    {
        var gapHeight = PipeGap;
        var gapStart = Random.Next(2, ScreenHeight - gapHeight - 2);
        return new Pipe(ScreenWidth - 2, gapStart, gapHeight);
    }

    private static bool CheckCollision(float birdY, int birdX, List<Pipe> pipes)
    {
        if (birdY >= GroundRow)
        {
            return true;
        }

        foreach (var pipe in pipes)
        {
            var pipeX = (int)MathF.Round(pipe.X);
            if (pipeX != birdX)
            {
                continue;
            }

            var gapStart = pipe.GapY;
            var gapEnd = pipe.GapY + pipe.GapHeight;
            if (birdY < gapStart || birdY > gapEnd)
            {
                return true;
            }
        }

        return false;
    }

    private static void RenderFrame(float birdY, int birdX, List<Pipe> pipes, int score)
    {
        var buffer = new char[ScreenHeight, ScreenWidth];

        for (var row = 0; row < ScreenHeight; row++)
        {
            for (var col = 0; col < ScreenWidth; col++)
            {
                buffer[row, col] = row == GroundRow ? GroundChar : EmptyChar;
            }
        }

        foreach (var pipe in pipes)
        {
            var pipeX = (int)MathF.Round(pipe.X);
            if (pipeX < 0 || pipeX >= ScreenWidth)
            {
                continue;
            }

            for (var row = 0; row < ScreenHeight - 1; row++)
            {
                var inGap = row >= pipe.GapY && row <= pipe.GapY + pipe.GapHeight;
                if (!inGap)
                {
                    buffer[row, pipeX] = PipeChar;
                }
            }
        }

        var birdRow = (int)MathF.Round(birdY);
        if (birdRow >= 0 && birdRow < ScreenHeight - 1)
        {
            buffer[birdRow, birdX] = BirdChar;
        }

        Console.SetCursorPosition(0, 0);
        Console.WriteLine($"Score: {score}");
        for (var row = 0; row < ScreenHeight; row++)
        {
            for (var col = 0; col < ScreenWidth; col++)
            {
                Console.Write(buffer[row, col]);
            }

            Console.WriteLine();
        }
    }
}
