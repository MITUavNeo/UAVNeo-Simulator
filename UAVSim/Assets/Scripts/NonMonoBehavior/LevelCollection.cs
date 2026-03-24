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
                    BuildIndex = 141,
                    HelpMessage = "Press E to arm the drone, then fly through gates and obstacles!"
                },
                new LevelInfo()
                {
                    DisplayName = "Flight Demo",
                    BuildIndex = 2,
                },
                new LevelInfo()
                {
                    DisplayName = "Color Camera Sandbox",
                    BuildIndex = 4,
                },
                new LevelInfo()
                {
                    DisplayName = "Depth Camera Sandbox",
                    BuildIndex = 7,
                },
                new LevelInfo()
                {
                    DisplayName = "AR Marker Sandbox",
                    BuildIndex = 15,
                    HelpMessage = "Click a block to select it, then: left-click it to change it's tag, right-click it to change it's color, or scroll to rotate"
                },
                new LevelInfo()
                {
                    DisplayName = "Long Hallway Sandbox",
                    BuildIndex = 132,
                }
            }
        },

        new LevelCollection()
        {
            DisplayName = "UAV GRG Labs",
            ShortName = "GRG",
            Levels = new LevelInfo[]
            {
                new LevelInfo()
                {
                    DisplayName = "Module 1 - Hello Drone",
                    BuildIndex = 142,
                    HelpMessage = "Take off, hover, and explore! Use the colored cones to orient yourself - Red=North, Blue=East, Green=South, Yellow=West."
                },
                new LevelInfo()
                {
                    DisplayName = "Module 2 - Drone Control",
                    BuildIndex = 143,
                    HelpMessage = "Fly through all 4 gates in order! Gates are color-coded: Blue (1), Green (2), Yellow (3), Red (4). Each gate is at a different altitude."
                },
                new LevelInfo()
                {
                    DisplayName = "Module 3 Pt1 - Object Detection",
                    BuildIndex = 144,
                    HelpMessage = "Use your downward camera to detect objects by name (Pineapple, Vase, Hourglass, Cactus, Book). Fly within 6m of your detected target to win!"
                },
                new LevelInfo()
                {
                    DisplayName = "Module 3 Pt2 - ArUco Maze",
                    BuildIndex = 145,
                    HelpMessage = "Navigate the false-wall maze using ArUco marker IDs. ID 0 = FAKE (can fly through), ID 1 = REAL (solid wall). Reach the blue room to win!"
                },
                new LevelInfo()
                {
                    DisplayName = "Module 4 - Search and Rescue",
                    BuildIndex = 146,
                    HelpMessage = "Search the 60x60m area using spiral or lawnmower patterns to locate the orange SAR target. Fly within 3m of the red landing pad to win!"
                },
                new LevelInfo()
                {
                    DisplayName = "Module 5 - Maze Navigation",
                    BuildIndex = 147,
                    HelpMessage = "Navigate the 3-corridor brick maze using distance sensors. Spawn at (-68, 0, 0), use the right-hand rule to reach the green exit marker."
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
                    DisplayName = "Mini Grand Prix: Fall 2025",
                    BuildIndex = 134,
                    HasRandomMaps = true,
                    RandomSceneBuildIndices = new int[] {135, 136, 137, 138, 139, 140},
                    IsRaceable = true,
                    NumCheckpoints = 0,
                    MaxCars = 2,
                    AutograderBuildIndex = 135,
                    AutograderLevelCode = "mgp2025fa",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo()
                        {
                            Title = "Mini Grand Prix Fall 2025 Autograder",
                            Description = "Navigate through === THE METROPOLIS ===",
                            MaxPoints = 20,
                            TimeLimit = 120
                        }
                    }
                },
                new LevelInfo()
                {
                    DisplayName = "Grand Prix 2025",
                    BuildIndex = 133,
                    IsRaceable = true,
                    NumCheckpoints = 4,
                    MaxCars = 4,
                    AutograderBuildIndex = 133,
                    AutograderLevelCode = "gp2025",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo()
                        {
                            Title = "Grand Prix 2025 Autograder",
                            Description = "Navigate through the course.",
                            MaxPoints = 25,
                            TimeLimit = 200
                        }
                    }
                },
                new LevelInfo()
                {
                    DisplayName = "Mini Grand Prix: Spring 2025",
                    BuildIndex = 131,
                    IsRaceable = true,
                    NumCheckpoints = 4,
                    MaxCars = 2,
                },
                new LevelInfo()
                {
                    DisplayName = "Micro Grand Prix 2025",
                    BuildIndex = 130,
                    IsRaceable = true,
                    NumCheckpoints = 0,
                    MaxCars = 2,
                },
                new LevelInfo()
                {
                    DisplayName = "Mini Grand Prix: Fall 2024",
                    BuildIndex = 126,
                    IsRaceable = true,
                    NumCheckpoints = 1,
                    MaxCars = 2,
                },
                new LevelInfo()
                {
                    DisplayName = "Grand Prix 2024",
                    BuildIndex = 114,
                    IsRaceable = true,
                    NumCheckpoints = 5,
                    MaxCars = 4,
                },
                new LevelInfo()
                {
                    DisplayName = "Mini Grand Prix: Spring 2024",
                    BuildIndex = 113,
                    IsRaceable = true,
                    NumCheckpoints = 0,
                    MaxCars = 2,
                },
                new LevelInfo()
                {
                    DisplayName = "Mini Grand Prix: Fall 2023",
                    BuildIndex = 104,
                    IsRaceable = true,
                    NumCheckpoints = 0,
                    MaxCars = 4,
                },
                new LevelInfo()
                {
                    DisplayName = "Grand Prix 2022",
                    BuildIndex = 96,
                    IsRaceable = true,
                    NumCheckpoints = 7,
                    MaxCars = 4,
                },
                new LevelInfo()
                {
                    DisplayName = "Grand Prix 2021",
                    BuildIndex = 95,
                    IsRaceable = true,
                    NumCheckpoints = 8,
                    MaxCars = 4,
                },
                new LevelInfo()
                {
                    DisplayName = "Grand Prix 2020",
                    BuildIndex = 18,
                    IsRaceable = true,
                    NumCheckpoints = 5,
                    MaxCars = 4,
                    AutograderBuildIndex = 92,
                    AutograderLevelCode = "final",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo()
                        {
                            Title = "Grad Prix 2020",
                            Description = "Navigate through the course.",
                            MaxPoints = 25,
                            TimeLimit = 360,
                            TimeBonuses = new Vector2[]{ new Vector2(105, 3), new Vector2(120, 2), new Vector2(150, 1), new Vector2(180, 0), new Vector2(240, -1), new Vector2(300, -3), new Vector2(float.PositiveInfinity, -5) }
                        }
                    }
                },
                new LevelInfo()
                {
                    DisplayName = "Time Trial 2020",
                    BuildIndex = 17,
                    IsRaceable = true,
                    NumCheckpoints = 3,
                    AutograderBuildIndex = 91,
                    AutograderLevelCode = "final",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo()
                        {
                            Title = "Time Trial",
                            Description = "Navigate through the course.",
                            MaxPoints = 25,
                            TimeLimit = 300,
                            TimeBonuses = new Vector2[]{ new Vector2(75, 3), new Vector2(90, 2), new Vector2(120, 1), new Vector2(150, 0), new Vector2(180, -1), new Vector2(240, -3), new Vector2(float.PositiveInfinity, -5) }
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
