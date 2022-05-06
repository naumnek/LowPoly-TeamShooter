using UnityEngine;

namespace Photon.Pun.Demo.Asteroids
{
    public class AsteroidsGame
    {
        public const int TEAM_SCORE_FOR_KILL = 1;
        public const int NUMBER_KILLS_TO_TEAM_WIN = 30;
        public const int MAX_TIMES_MATCH_WIN = 300;

        public const float LOOTS_RESPAWN_TIME = 15.0f;

        public const int ASTEROIDS_MAX_COUNT_ON_SCENE = 5;
        public const float ASTEROIDS_MIN_SPAWN_TIME = 5.0f;
        public const float ASTEROIDS_MAX_SPAWN_TIME = 10.0f;

        public const float DISTANCE_OPEN_SELECT_WEAPON = 15f;
        public const int MAX_PAID_WEAPONS = 1;
        public const float TIME_TO_BLOCK_SELECTION = 15f;
        public const float PLAYER_PASSIVE_REGENERATION_AMOUNT = 10.0f;
        public const float PLAYER_PASSIVE_REGENERATION_INTERVAL = 10.0f;
        public const float PLAYER_INVULNERABLE_TIME = 5.0f;
        public const float PLAYER_RESPAWN_TIME = 4.0f;

        public const int PLAYER_SCORE_FOR_EXIT = 0;
        public const int PLAYER_SCORE_FOR_LOSE = 30;
        public const int PLAYER_SCORE_FOR_WIN = 100;
        public const int PLAYER_SCORE_FOR_KILL = 5;
        public const int PLAYER_MAX_LIVES = 3;

        public const string PLAYER_LIVES = "PlayerLives";
        public const string PLAYER_READY = "IsPlayerReady";
        public const string PLAYER_LOADED_LEVEL = "PlayerLoadedLevel";

        public static Color GetColor(int colorChoice)
        {
            switch (colorChoice)
            {
                case 0: return Color.red;
                case 1: return Color.green;
                case 2: return Color.blue;
                case 3: return Color.yellow;
                case 4: return Color.cyan;
                case 5: return Color.grey;
                case 6: return Color.magenta;
                case 7: return Color.white;
            }

            return Color.black;
        }
    }
}