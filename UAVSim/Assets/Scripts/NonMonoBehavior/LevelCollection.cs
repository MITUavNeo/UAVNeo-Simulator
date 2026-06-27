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
                    HelpMessage = "Press L to launch the drone, then fly through gates and obstacles!"
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
                new LevelInfo()
                {
                    DisplayName = "Optical Flow Sandbox",
                    BuildIndex = 62,
                    HelpMessage = "Press L to launch, then through the scattered gates, using the colored floor to develop and test optical-flow navigation. Press E to rotate the spawn 90 degrees."
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
                    DisplayName = "Lab A - Drone Control",
                    BuildIndex = 9,
                    HelpMessage = "Use the L key to launch/land, and the WASD/Arrow Keys to fly around. Press the E key to rotate 90 degrees to spawn at the next course.",
                    AutograderBuildIndex = 10,
                    AutograderLevelCode = "laba",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo() { Title = "A - 1 Gate", Description = "Fly through one gate", MaxPoints = 5, TimeLimit = 30 },
                        new AutograderLevelInfo() { Title = "B - 3 Gates", Description = "Fly through three gates", MaxPoints = 5, TimeLimit = 30 },
                        new AutograderLevelInfo() { Title = "X - Walls", Description = "Navigate around the walls", MaxPoints = 5, TimeLimit = 30 },
                        new AutograderLevelInfo() { Title = "Y - Gates and Walls", Description = "Fly through a course of gates and walls", MaxPoints = 5, TimeLimit = 30 },
                    }
                },
                new LevelInfo()
                {
                    DisplayName = "Lab B - Vent Challenge",
                    BuildIndex = 14,
                    HelpMessage = "Navigate through the vents by reading the colored cube at each intersection (Blue = right, Orange = left, Green = straight, Red = stop). Left-click a stoplight to select it, right-click to change its color.",
                    AutograderBuildIndex = 15,
                    AutograderLevelCode = "labb",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo() { Title = "Right Turn", Description = "Detect the cue and turn right.", MaxPoints = 2, TimeLimit = 30 },
                        new AutograderLevelInfo() { Title = "Left Turn", Description = "Detect the cue and turn left.", MaxPoints = 2, TimeLimit = 30 },
                        new AutograderLevelInfo() { Title = "Go Straight", Description = "Detect the cue and continue straight.", MaxPoints = 2, TimeLimit = 30 },
                        new AutograderLevelInfo() { Title = "3 Intersections", Description = "Navigate 3 intersections by color.", MaxPoints = 3, TimeLimit = 60 },
                        new AutograderLevelInfo() { Title = "5 Intersections", Description = "Navigate 5 intersections by color.", MaxPoints = 5, TimeLimit = 60 },
                        new AutograderLevelInfo() { Title = "10 Intersections", Description = "Navigate 10 intersections by color.", MaxPoints = 6, TimeLimit = 120 },
                    }
                },
                new LevelInfo()
                {
                    DisplayName = "Lab C - Line Follower",
                    BuildIndex = 21,
                    HelpMessage = "Follow the colored lines on the floor using RED > GREEN > BLUE color priority. Press the E key to rotate 90 degrees to spawn at the next course.",
                    AutograderBuildIndex = 22,
                    AutograderLevelCode = "labc",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo() { Title = "Straight Line", Description = "Follow a straight line to the end.", MaxPoints = 1, TimeLimit = 20 },
                        new AutograderLevelInfo() { Title = "Curved Line", Description = "Follow a curved line to the end.", MaxPoints = 2, TimeLimit = 20 },
                        new AutograderLevelInfo() { Title = "Perpendicular Line", Description = "Handle a perpendicular junction.", MaxPoints = 2, TimeLimit = 20 },
                        new AutograderLevelInfo() { Title = "Color Priority I", Description = "Choose the correct path by color priority.", MaxPoints = 5, TimeLimit = 30 },
                        new AutograderLevelInfo() { Title = "Color Priority II", Description = "Choose the correct path by color priority (advanced).", MaxPoints = 5, TimeLimit = 30 },
                        new AutograderLevelInfo() { Title = "Floor Maze", Description = "Solve the line-based floor maze.", MaxPoints = 5, TimeLimit = 45 },
                    }
                },
                new LevelInfo()
                {
                    DisplayName = "Lab D - Gate Navigation",
                    BuildIndex = 28,
                    HelpMessage = "Use the AR Tags to autonomously detect and fly through the nearest gates. Use RED > GREEN > BLUE color priority for multiple gates.",
                    AutograderBuildIndex = 29,
                    AutograderLevelCode = "labd",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo() { Title = "One Gate", Description = "Center on and fly through a single AR-tagged gate.", MaxPoints = 3, TimeLimit = 20 },
                        new AutograderLevelInfo() { Title = "Two Gates", Description = "Fly through two gates in order.", MaxPoints = 3, TimeLimit = 30 },
                        new AutograderLevelInfo() { Title = "Three Gates", Description = "Fly through three gates in order.", MaxPoints = 4, TimeLimit = 40 },
                        new AutograderLevelInfo() { Title = "Two Rows of Gates", Description = "Use color priority to fly two rows of gates.", MaxPoints = 5, TimeLimit = 30 },
                        new AutograderLevelInfo() { Title = "Three Rows of Gates", Description = "Use color priority to fly three rows of gates.", MaxPoints = 5, TimeLimit = 40 },
                    }
                },
                new LevelInfo()
                {
                    DisplayName = "Lab E - Gates and Lines",
                    BuildIndex = 34,
                    HelpMessage = "Use the lines on the floor to navigate through the course, and use the gates to control your altitude. Use RED > GREEN > BLUE color priority for lines.",
                    AutograderBuildIndex = 35,
                    AutograderLevelCode = "labe",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo() { Title = "Simple Track, 1 Gate", Description = "Follow a simple track through one altitude gate.", MaxPoints = 3, TimeLimit = 30 },
                        new AutograderLevelInfo() { Title = "Complex Track, 3 Gates", Description = "Follow a complex track through three altitude gates.", MaxPoints = 3, TimeLimit = 30 },
                        new AutograderLevelInfo() { Title = "Color Priority, 1 Gate", Description = "Use color priority on the line with one gate.", MaxPoints = 4, TimeLimit = 30 },
                        new AutograderLevelInfo() { Title = "Color Priority, 3 Gates", Description = "Use color priority on the line with three gates.", MaxPoints = 4, TimeLimit = 30 },
                        new AutograderLevelInfo() { Title = "All-Out Adventure", Description = "Complete the full combined course.", MaxPoints = 6, TimeLimit = 60 },
                    }
                },
                /* --- Hidden for the v1.0.0 release: Labs F–I are not being shipped yet. ---
                new LevelInfo()
                {
                    DisplayName = "Lab F - Drone Slalom",
                    BuildIndex = 40,
                    HelpMessage = "State machines: fly left of red, right of blue, above green, and below yellow balls.",
                    AutograderBuildIndex = 41,
                    AutograderLevelCode = "labf",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo() { Title = "5 Balls", Description = "Pass 5 balls on the correct side/level by color.", MaxPoints = 5, TimeLimit = 30 },
                        new AutograderLevelInfo() { Title = "10 Balls", Description = "Pass 10 balls on the correct side/level by color.", MaxPoints = 5, TimeLimit = 60 },
                        new AutograderLevelInfo() { Title = "5 Balls Curved", Description = "Pass 5 balls along a curved path.", MaxPoints = 5, TimeLimit = 30 },
                        new AutograderLevelInfo() { Title = "10 Balls Curved", Description = "Pass 10 balls along a curved path.", MaxPoints = 5, TimeLimit = 60 },
                    }
                },
                new LevelInfo()
                {
                    DisplayName = "Lab G - Dynamic Walls",
                    BuildIndex = 45,
                    HelpMessage = "AR reasoning: tag orientation points to a wall; the tag ID says whether it tells the truth or lies (0 truth, 1 lies in y, 2 lies in x, 3 lies diagonal).",
                    AutograderBuildIndex = 46,
                    AutograderLevelCode = "labg",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo() { Title = "1 Gate", Description = "Choose the correct wall through 1 gate set.", MaxPoints = 5, TimeLimit = 150 },
                        new AutograderLevelInfo() { Title = "2 Gates", Description = "Choose the correct wall through 2 gate sets.", MaxPoints = 5, TimeLimit = 180 },
                        new AutograderLevelInfo() { Title = "3 Gates", Description = "Choose the correct wall through 3 gate sets.", MaxPoints = 5, TimeLimit = 210 },
                        new AutograderLevelInfo() { Title = "5 Gates", Description = "Choose the correct wall through 5 gate sets.", MaxPoints = 5, TimeLimit = 240 },
                    }
                },
                new LevelInfo()
                {
                    DisplayName = "Lab H - Search and Rescue",
                    BuildIndex = 50,
                    HelpMessage = "Search algorithms: find the randomly placed object, then land on the pad within the range and time limit.",
                    AutograderBuildIndex = 51,
                    AutograderLevelCode = "labh",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo() { Title = "Relaxed", Description = "Find the target and land on the pad (relaxed limits).", MaxPoints = 5, TimeLimit = 240 },
                        new AutograderLevelInfo() { Title = "Normal", Description = "Find the target and land on the pad (normal limits).", MaxPoints = 5, TimeLimit = 180 },
                        new AutograderLevelInfo() { Title = "Challenging", Description = "Find the target and land on the pad (challenging limits).", MaxPoints = 5, TimeLimit = 150 },
                        new AutograderLevelInfo() { Title = "I Am Speed", Description = "Find the target and land on the pad (tight time limit).", MaxPoints = 5, TimeLimit = 90 },
                    }
                },
                new LevelInfo()
                {
                    DisplayName = "Lab I - Target Follower",
                    BuildIndex = 55,
                    HelpMessage = "Continuous tracking: follow a moving target on the floor along a (possibly random) path.",
                    AutograderBuildIndex = 56,
                    AutograderLevelCode = "labi",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo() { Title = "Relaxed", Description = "Track the moving target (relaxed speed).", MaxPoints = 5, TimeLimit = 180 },
                        new AutograderLevelInfo() { Title = "Normal", Description = "Track the moving target (normal speed).", MaxPoints = 5, TimeLimit = 180 },
                        new AutograderLevelInfo() { Title = "Challenging", Description = "Track the moving target (challenging speed/path).", MaxPoints = 5, TimeLimit = 180 },
                        new AutograderLevelInfo() { Title = "I Am Speed", Description = "Track the fastest moving target.", MaxPoints = 5, TimeLimit = 180 },
                    }
                },
                --- end hidden Labs F–I --- */
            }
        },

        /* --- Hidden for the v1.0.0 release: the Grand Prix capstone draws on Labs F–I, which are not being shipped yet. ---
        new LevelCollection()
        {
            DisplayName = "Grand Prix",
            ShortName = "GP",
            Levels = new LevelInfo[]
            {
                new LevelInfo()
                {
                    DisplayName = "Grand Prix 2026",
                    BuildIndex = 60,
                    IsRaceable = false,
                    HelpMessage = "The capstone: gates and lines, slalom, target following, dynamic walls, and mazes combined.",
                    AutograderBuildIndex = 61,
                    AutograderLevelCode = "grandprix",
                    AutograderLevels = new AutograderLevelInfo[]
                    {
                        new AutograderLevelInfo() { Title = "Grand Prix 2026", Description = "Complete the full Grand Prix circuit.", MaxPoints = 25, TimeLimit = 300 },
                    }
                },
            }
        },
        --- end hidden Grand Prix --- */
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
