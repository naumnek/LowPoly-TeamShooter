using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Unity.FPS.Game
{
    // The Game Events used across the Game.
    // Anytime there is a need for a new event, it should be added here.

    public static class Events
    {
        public static ObjectiveUpdateEvent ObjectiveUpdateEvent = new ObjectiveUpdateEvent();
        public static AllObjectivesCompletedEvent AllObjectivesCompletedEvent = new AllObjectivesCompletedEvent();
        public static EndLoadGameSceneEvent EndLoadGameSceneEvent = new EndLoadGameSceneEvent();
        public static PlayerSpawnEvent PlayerSpawnEvent = new PlayerSpawnEvent();
        public static GamePauseEvent GamePauseEvent = new GamePauseEvent();
        public static SwitchMusicEvent SwitchMusicEvent = new SwitchMusicEvent();
        public static WaveCompletedEvent WaveCompletedEvent = new WaveCompletedEvent();
        public static AllWaveCompletedEvent AllWaveCompletedEvent = new AllWaveCompletedEvent();
        public static ExitEvent ExitEvent = new ExitEvent();
        public static EndGameEvent EndGameEvent = new EndGameEvent();
        public static GameOverEvent GameOverEvent = new GameOverEvent();
        public static EnemyKillEvent EnemyKillEvent = new EnemyKillEvent();
        public static PlayerDeathEvent PlayerDeathEvent = new PlayerDeathEvent();
        public static KillEvent KillEvent = new KillEvent();
        public static PickupEvent PickupEvent = new PickupEvent();
        public static AmmoPickupEvent AmmoPickupEvent = new AmmoPickupEvent();
        public static DamageEvent DamageEvent = new DamageEvent();
        public static DisplayMessageEvent DisplayMessageEvent = new DisplayMessageEvent();
    }

    public class ObjectiveUpdateEvent : GameEvent
    {
        public Objective Objective;
        public string DescriptionText;
        public string CounterText;
        public bool IsComplete;
        public string NotificationText;
    }

    public class AllObjectivesCompletedEvent : GameEvent { }

    public class GameOverEvent : GameEvent
    {
        public bool Win;
    }

    public class SwitchMusicEvent : GameEvent
    {
        public AudioClip Music;
        public string SwitchMusic;
    }

    public class KillEvent : GameEvent
    {
        public Actor killed;
        public Actor killer;
    }

    public class PlayerSpawnEvent : GameEvent
    {
        public PlayerController player;
    }

    public class GamePauseEvent : GameEvent
    {
        public bool ServerPause;
        public bool MenuPause;
    }

    public class EndGameEvent : GameEvent
    {
        public bool win;
    }

    public class ExitEvent : GameEvent { }


    public class AllWaveCompletedEvent : GameEvent { }

    public class WaveCompletedEvent : GameEvent
    {
        public int BossKillCount = 0;
        public int WaveLevel = 0;
        public int CountActiveEnemySpawner = 0;
    }

    public class EndLoadGameSceneEvent : GameEvent { }

    public class PlayerDeathEvent : GameEvent
    {
        public bool Die;
    }

    public class EnemyKillEvent : GameEvent
    {
        public GameObject Enemy;
        public int RemainingEnemyCount;
    }

    public class PickupEvent : GameEvent
    {
        public GameObject Pickup;
    }

    public class AmmoPickupEvent : GameEvent
    {
        public WeaponController Weapon;
    }

    public class DamageEvent : GameEvent
    {
        public GameObject Sender;
        public float DamageValue;
    }

    public class DisplayMessageEvent : GameEvent
    {
        public string Message;
        public float DelayBeforeDisplay;
    }
}
