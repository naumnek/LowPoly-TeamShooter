using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorController : MonoBehaviour
{

    [Tooltip("Sound played for footsteps")]
    public AudioClip FootstepSfx;

    [Tooltip("Sound played when jumping")]
    public AudioClip JumpSfx;

    [Tooltip("Sound played when landing")]
    public AudioClip LandSfx;

    [Tooltip("Sound played when taking damage froma fall")]
    public AudioClip FallDamageSfx;

    [Tooltip("Audio source for footsteps, jump, etc...")]
    private AudioSource AudioSource;
    // Start is called before the first frame update
    void Start()
    {
        AudioSource = GetComponent<AudioSource>();
    }

    public void FootstepSfxPlay()
    {

        AudioSource.PlayOneShot(FootstepSfx);
    }

    public void JumpSfxPlay()
    {

        AudioSource.PlayOneShot(JumpSfx);
    }

    public void LandSfxPlay()
    {

        AudioSource.PlayOneShot(LandSfx);
    }

    public void FallDamageSfxPlay()
    {

        AudioSource.PlayOneShot(FallDamageSfx);
    }
}
