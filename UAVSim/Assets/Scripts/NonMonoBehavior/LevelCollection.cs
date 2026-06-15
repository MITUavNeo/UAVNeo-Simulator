using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Information about a collection of related levels.
/// </summary>
public class LevelCollection
{
    #region Constants
    /// <summary>
    /// The build index of the main menu level.
    /// </summary>
    public const int MainMenuBuildIndex = 0;
    #endregion

    /// <summary>
    /// The name of the collection displayed to users.
    /// </summary>
    public string DisplayName;

    /// <summary>
    /// A shorter name for the collection which can be prepended to the names of the levels in the collection.
    /// </summary>
    public string ShortName;

    /// <summary>
    /// The levels in the collection.
    /// </summary>
    public LevelInfo[] Levels;

    /// <summary>
    /// The display names of the levels in the collection.
    /// </summary>
    public List<string> LevelNames
    {
        get
        {
            List<string> levelNames = new List<string>(this.Levels.Length);
            foreach (LevelInfo level in this.Levels)
            {
                levelNames.Add(level.DisplayName);
            }
            return levelNames;
        }
    }

    #region All Level Info
    public static readonly LevelCollection[] LevelCollections =
    {
        
        new LevelCollection()
        {
            DisplayName = "Drone Environments",
            ShortName = "Drone",
            Levels = new LevelInfo[]
            {
                new LevelInfo()
                {
                    DisplayName = "Drone World",
                    BuildIndex = 4,
                    HelpMessage = "Press A (1 key) to arm the drone, then fly through gates and obstacles!"
                },
                new LevelInfo()
                {
                    DisplayName = "Flight Demo",
                    BuildIndex = 2,
                },
                new LevelInfo()
                {
                    DisplayName = "Color Camera Sandbox",
                    BuildIndex = 5,
                },
                new LevelInfo()
                {
                    DisplayName = "Depth Camera Sandbox",
                    BuildIndex = 6,
                },
                new LevelInfo()
                {
                    DisplayName = "AR Marker Sandbox",
                    BuildIndex = 7,
                    HelpMessage = "Click a block to select it, then: left-click it to change it's tag, right-click it to change it's color, or scroll to rotate"
                },
                new LevelInfo()
                {
                    DisplayName = "Long Hallway Sandbox",
                    BuildIndex = 8,
                },
            }
        },

        new LevelCollection()
        {
            DisplayName = "UAV Neo Labs",
            ShortName = "UAV",
            Levels = new LevelInfo[]
            {
                new LevelInfo()
                {
                    DisplayName = "Module 1 - Hello Drone",
                    BuildIndex = 9,
                    HelpMessage = "Take off, hover, and explore! Use the colored cones to orient yourself - Red=North, Blue=East, Green=South, Yellow=West."
                },
                new LevelInfo()
                {
                    DisplayName = "Module 2 - Drone Control",
                    BuildIndex = 10,
                    HelpMessage = "Fly through all 4 gates in order! Gates are color-coded: Blue (1), Green (2), Yellow (3), Red (4). Each gate is at a different altitude.",
                    AutograderBuildIndex = 11,
                    AutograderLevelCode = "mod2gates",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo()
                        {
                            Title = "Module 2 - Gate 1",
                            Description = "Fly through Gate 1 (Blue).",
                            MaxPoints = 1,
                            TimeLimit = 120
                        },
                        new AutograderLevelInfo()
                        {
                            Title = "Module 2 - Gate 2",
                            Description = "Fly through Gate 2 (Green).",
                            MaxPoints = 1,
                            TimeLimit = 120
                        },
                        new AutograderLevelInfo()
                        {
                            Title = "Module 2 - Gate 3",
                            Description = "Fly through Gate 3 (Yellow).",
                            MaxPoints = 1,
                            TimeLimit = 120
                        },
                        new AutograderLevelInfo()
                        {
                            Title = "Module 2 - Gate 4",
                            Description = "Fly through Gate 4 (Red).",
                            MaxPoints = 1,
                            TimeLimit = 120
                        },
                        new AutograderLevelInfo()
                        {
                            Title = "Module 2 - Gate 5",
                            Description = "Fly through Gate 5.",
                            MaxPoints = 1,
                            TimeLimit = 120
                        },
                        new AutograderLevelInfo()
                        {
                            Title = "Module 2 - Gate 6",
                            Description = "Fly through Gate 6.",
                            MaxPoints = 1,
                            TimeLimit = 120
                        },
                        new AutograderLevelInfo()
                        {
                            Title = "Module 2 - Gate 7",
                            Description = "Fly through Gate 7.",
                            MaxPoints = 1,
                            TimeLimit = 120
                        },
                        new AutograderLevelInfo()
                        {
                            Title = "Module 2 - Gate 8",
                            Description = "Fly through Gate 8.",
                            MaxPoints = 1,
                            TimeLimit = 120
                        },
                        new AutograderLevelInfo()
                        {
                            Title = "Module 2 - Gate 9",
                            Description = "Fly through Gate 9.",
                            MaxPoints = 1,
                            TimeLimit = 120
                        },
                        new AutograderLevelInfo()
                        {
                            Title = "Module 2 - Gate 10",
                            Description = "Fly through Gate 10.",
                            MaxPoints = 1,
                            TimeLimit = 120
                        },
                        new AutograderLevelInfo()
                        {
                            Title = "Module 2 - Gate 11",
                            Description = "Fly through Gate 11.",
                            MaxPoints = 1,
                            TimeLimit = 120
                        },
                        new AutograderLevelInfo()
                        {
                            Title = "Module 2 - Gate 12",
                            Description = "Fly through Gate 12.",
                            MaxPoints = 1,
                            TimeLimit = 120
                        },
                    }
                },
                new LevelInfo()
                {
                    DisplayName = "Module 3a - Object Detection",
                    BuildIndex = 12,
                    HelpMessage = "Use your downward camera to detect objects by name (Pineapple, Vase, Hourglass, Cactus, Book). Fly within 6m of your detected target to win!",
                    AutograderBuildIndex = 13,
                    AutograderLevelCode = "mod3aobj",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo()
                        {
                            Title = "Module 3a - Pineapple",
                            Description = "Locate the Pineapple (BP_PineappleSculpture02_2) and fly within 6m of it.",
                            MaxPoints = 2,
                            TimeLimit = 90
                        },
                        new AutograderLevelInfo()
                        {
                            Title = "Module 3a - Vase",
                            Description = "Locate the Vase (BP_Vase09_2) and fly within 6m of it.",
                            MaxPoints = 2,
                            TimeLimit = 90
                        },
                        new AutograderLevelInfo()
                        {
                            Title = "Module 3a - Hourglass",
                            Description = "Locate the Hourglass (BP_Hourglass01_2) and fly within 6m of it.",
                            MaxPoints = 2,
                            TimeLimit = 90
                        },
                        new AutograderLevelInfo()
                        {
                            Title = "Module 3a - Cactus",
                            Description = "Locate the Cactus (BP_CactusSculpture03_2) and fly within 6m of it.",
                            MaxPoints = 2,
                            TimeLimit = 90
                        },
                        new AutograderLevelInfo()
                        {
                            Title = "Module 3a - Book",
                            Description = "Locate the Book (BP_BookGroup08_2) and fly within 6m of it.",
                            MaxPoints = 2,
                            TimeLimit = 90
                        },
                    }
                },
                new LevelInfo()
                {
                    DisplayName = "Module 3b - ArUco Maze",
                    BuildIndex = 18,
                    HelpMessage = "Navigate the false-wall maze using ArUco marker IDs. ID 0 = FAKE (can fly through), ID 1 = REAL (solid wall). Reach the blue room to win!",
                    AutograderBuildIndex = 19,
                    AutograderLevelCode = "mod3baruco",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo()
                        {
                            Title = "Module 3b - ArUco Maze",
                            Description = "Read ArUco marker IDs to navigate through the false-wall maze. Reach the red room (z > 38) to win.",
                            MaxPoints = 10,
                            TimeLimit = 120
                        }
                    }
                },
                new LevelInfo()
                {
                    DisplayName = "Module 4 - Search and Rescue",
                    BuildIndex = 20,
                    HelpMessage = "Search the 60x60m area using spiral or lawnmower patterns to locate the orange SAR target. Fly within 3m of the red landing pad to win!",
                    AutograderBuildIndex = 21,
                    AutograderLevelCode = "mod4sar",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo()
                        {
                            Title = "Module 4 - Search and Rescue",
                            Description = "Locate the orange SAR target and fly within 3m of the red landing pad.",
                            MaxPoints = 10,
                            TimeLimit = 120
                        }
                    }
                },
                new LevelInfo()
                {
                    DisplayName = "Module 5 - Maze Navigation",
                    BuildIndex = 22,
                    HelpMessage = "Navigate the 3-corridor brick maze using distance sensors. Spawn at (-68, 0, 0), use the right-hand rule to reach the green exit marker.",
                    AutograderBuildIndex = 23,
                    AutograderLevelCode = "mod5maze",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo()
                        {
                            Title = "Module 5 - Maze Navigation",
                            Description = "Navigate the brick maze from spawn (-68, 0, 0) using the right-hand rule and reach the exit at x >= 16.",
                            MaxPoints = 10,
                            TimeLimit = 180
                        }
                    }
                },
                new LevelInfo()
                {
                    DisplayName = "Module 6 - Line Following",
                    BuildIndex = 24,
                    HelpMessage = "Fly at 1.2 m and use the downward camera to follow the red line. Implement proportional control to stay centered on the F1 circuit!",
                    AutograderBuildIndex = 25,
                    AutograderLevelCode = "mod6line",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo()
                        {
                            Title = "Module 6 - Line Following",
                            Description = "Follow the red line around the F1 circuit. " +
                                          "Pass T1 (z≥45) → reach east side (x≥55) → reach back straight (z≤-25).",
                            MaxPoints = 10,
                            TimeLimit = 120
                        }
                    }
                },
            }
        },

        new LevelCollection()
        {
            DisplayName = "Final Challenge",
            ShortName = "Final",
            Levels = new LevelInfo[]
            {
                new LevelInfo()
                {
                    DisplayName = "2026 Final UAV",
                    BuildIndex = 26,
                    HelpMessage = "The Beaver Warrior Challenge! Complete all 9 tasks in order: Figure-8 → Gate Run → ArUco Maze → Aerial Slalom → Tunnel → Speed Corridor → SAR Detect → SAR Land → Line Following.",
                    AutograderBuildIndex = 27,
                    AutograderLevelCode = "grandprix",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo()
                        {
                            Title = "Grand Prix",
                            Description = "Complete all 9 tasks in order: Figure-8 → Gate Run → ArUco Maze → Aerial Slalom → Tunnel → Speed Corridor → SAR Detect → SAR Land → Line Following. 5 pts per task.",
                            MaxPoints = 45,
                            TimeLimit = 720,
                            TimeBonuses = new Vector2[]
                            {
                                new Vector2(300, 3),
                                new Vector2(360, 2),
                                new Vector2(480, 1),
                                new Vector2(600, 0),
                                new Vector2(660, -1),
                                new Vector2(float.PositiveInfinity, -3)
                            }
                        }
                    }
                },
                new LevelInfo()
                {
                    DisplayName = "demo_additive_autograder",
                    BuildIndex = 26,
                    HelpMessage = "POC: Additive scene loading — exploration environment + autograder task layer as separate scenes.",
                    AutograderBuildIndex = 28,
                    AutograderLevelCode = "grandprix_additive",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo()
                        {
                            Title = "Grand Prix (Additive Demo)",
                            Description = "Complete all 9 tasks in order. Environment loaded additively from GrandPrix exploration scene.",
                            MaxPoints = 45,
                            TimeLimit = 720,
                            TimeBonuses = new Vector2[]
                            {
                                new Vector2(300, 3),
                                new Vector2(360, 2),
                                new Vector2(480, 1),
                                new Vector2(600, 0),
                                new Vector2(660, -1),
                                new Vector2(float.PositiveInfinity, -3)
                            }
                        }
                    }
                },
            }
        },
    };
    #endregion

    /// <summary>
    /// Initializes static level-related fields.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by Unity")]
    private static void Initialize()
    {
        foreach (LevelCollection collection in LevelCollection.LevelCollections)
        {
            foreach (LevelInfo level in collection.Levels)
            {
                level.CollectionName = collection.ShortName;
                if (level.IsRaceable)
                {
                    level.WinableIndex = LevelInfo.WinableLevels.Count;
                    LevelInfo.WinableLevels.Add(level);
                }
            }
        }
    }
}
