using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using Photon.Pun.Demo.Asteroids;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using StarterAssets;
using naumnek.Settings;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;

public class PlayerController : MonoBehaviour
{
    [Header("General")]
    public LayerMask EnemyLayer;
    public SkinnedMeshRenderer CharacterSkinned;
    public WeaponController[] StartingWeapons;
    public Transform CameraTarget;
    public Transform AimPoint;
    public Collider playerCollider;
    public Renderer playerRenderer;
    [Tooltip("Height at which the player dies instantly when falling off the map")]
    public float KillHeight = -50f;
    [Tooltip("Parent transform where all weapon will be added in the hierarchy")]
    public Transform WeaponParentSocket;
    [Tooltip("Position for weapons when active but not actively aiming")]
    public Transform DefaultWeaponPosition;
    [Tooltip("Position for weapons when aiming")]
    public Transform AimingWeaponPosition;
    [Tooltip("Position for innactive weapons")]
    public Transform DownWeaponPosition;

    public PhotonView PhotonView { get; private set; }
    public Actor Actor { get; private set; }
    public Health Health { get; private set; }
    public Damageable Damageable { get; private set; }
    public Animator Animator { get; private set; }
    public PlayerWeaponsManager PlayerWeaponsManager { get; private set; }
    public PlayerCharacterController PlayerCharacterController { get; private set; }
    public ThirdPersonController ThirdPersonController { get; private set; }
    public SettingsManager SettingsManager { get; private set; }
    public GameFlowManager GameFlowManager { get; private set; }
    public PlayerInputHandler PlayerInputHandler { get; private set; }
    public Player PhotonPlayer { get; private set; }
    public ActorsManager ActorsManager { get; private set; }
    public AudioSource AudioSource { get; private set; }
    public CharacterController CharacterController { get; private set; }
    public CustomizationInfo Customization { get; private set; }

    public bool controllable { get; private set; } = true;

    public Transform Spawnpoint { get; private set; }
    private WeaponController[] m_Weapons;

    public void SetSpawn(Transform spawnpoint)
    {
        Spawnpoint = spawnpoint;
    }

    // Start is called before the first frame update
    void Start()
    {
        Actor = GetComponent<Actor>();
        Health = GetComponent<Health>();
        Damageable = GetComponent<Damageable>();
        Animator = GetComponent<Animator>();
        AudioSource = GetComponent<AudioSource>();
        CharacterController = GetComponent<CharacterController>();
        PhotonView = GetComponent<PhotonView>();
        PhotonPlayer = PhotonView.Owner;
        ActorsManager = ActorsManager.GetInstance();

        if (PhotonView.IsMine)
        {
            PlayerWeaponsManager = PlayerWeaponsManager.GetInstance();
            PlayerCharacterController = PlayerCharacterController.GetInstance();
            ThirdPersonController = ThirdPersonController.GetInstance();
            SettingsManager = SettingsManager.GetInstance();
            Customization = SettingsManager.Customization;
            GameFlowManager = GameFlowManager.GetInstance();
            PlayerInputHandler = PlayerInputHandler.GetInstance();

            PhotonView.RPC("SetPlayerSettings", RpcTarget.AllViaServer, 
                Customization.GetCurrentIndexModel(), Customization.GetCurrentIndexMaterial(), Customization.Type);

            Health.OnDie += OnDie;

            Actor.SetAffiliation(this);

            PlayerSpawnEvent evt = Events.PlayerSpawnEvent;
            evt.player = this;
            EventManager.Broadcast(evt);
        }
        else
        {
            //gameObject.layer = LayerMask.NameToLayer("Enemy");
        }
    }

    private void Update()
    {
        if (!PhotonView.AmOwner || !controllable)
        {
            return;
        }

        // we don't want the master client to apply input to remote ships while the remote player is inactive
        if (this.PhotonView.CreatorActorNr != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            return;
        }

        // check for Y kill
        if (transform.position.y < KillHeight)
        {
            OnDie();
        }
    }

    public void OnPlayerShoot(int weaponIndex, Vector3 mouseWorldPosition)
    {
        PhotonView.RPC("Fire", RpcTarget.AllViaServer, mouseWorldPosition);
    }

    [PunRPC]
    public void AddPlayerWeapon(string weaponName)
    {
        GameObject weapon = SettingsManager.GetInstance().RequredWeaponsList.Where(w => w.WeaponName == weaponName).FirstOrDefault().WeaponPrefab;
        // spawn the weapon prefab as child of the weapon socket
        GameObject weaponObject = Instantiate(weapon, WeaponParentSocket.position, WeaponParentSocket.rotation);
        if (PlayerWeaponsManager != null) PlayerWeaponsManager.SetWeapon(weaponObject);
    }

    [PunRPC]
    private void SetPlayerSettings(int indexModel, int indexMaterial, SkinType type)
    {
        CharacterSkinned.sharedMesh = SettingsManager.GetInstance().Customization.GetModel(type, indexModel);
        CharacterSkinned.material = SettingsManager.GetInstance().Customization.GetMaterial(type, indexMaterial);
    }

    [PunRPC]
    public void Fire(Vector3 mouseWorldPosition)
    {
        WeaponController weapon = PlayerWeaponsManager.GetActiveWeapon();
        weapon.HandleShoot(mouseWorldPosition);
    }

    [PunRPC]
    private void OnDie()
    {
        PhotonView.RPC("DiePlayer", RpcTarget.AllViaServer);
    }

    #region COROUTINES

    private IEnumerator WaitForRespawn()
    {
        yield return new WaitForSeconds(AsteroidsGame.PLAYER_RESPAWN_TIME);

        PhotonView.RPC("RespawnPlayer", RpcTarget.AllViaServer);
    }

    private IEnumerator WaitForDisableInvulnerable()
    {
        yield return new WaitForSeconds(AsteroidsGame.PLAYER_INVULNERABLE_TIME);
        Health.DisableInvulnerable();
    }

    #endregion

    [PunRPC]
    public void DiePlayer()
    {

        PlayerDeathEvent evt = Events.PlayerDeathEvent;
        evt.Die = true;
        EventManager.Broadcast(evt);

        playerCollider.enabled = false;
        playerRenderer.enabled = false;

        PlayerWeaponsManager.SetWeaponRenderers(false);

        controllable = false;

        transform.position = Spawnpoint.position;
        transform.rotation = Spawnpoint.rotation;

        if (PhotonView.IsMine)
        {
            StartCoroutine(WaitForRespawn());
        }
    }


    [PunRPC]
    public void RespawnPlayer()
    {
        playerCollider.enabled = true;
        playerRenderer.enabled = true;

        PlayerWeaponsManager.SetWeaponRenderers(true);

        controllable = true;

        Health.Heal(Health.MaxHealth);
        Health.IsDead = false;

        PlayerDeathEvent evt = Events.PlayerDeathEvent;
        evt.Die = false;
        EventManager.Broadcast(evt);

        StartCoroutine(WaitForDisableInvulnerable());
    }

}
