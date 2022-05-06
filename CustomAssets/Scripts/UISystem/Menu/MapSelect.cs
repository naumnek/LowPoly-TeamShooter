using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapSelect : MonoBehaviour
{
    public string SceneName;
    public Sprite PressedSprite;
    private Sprite m_DefaultSprite;
    private Image m_MapButtonImage;
    public bool buttonPressed { get; private set; }

    private void Start()
    {
        m_MapButtonImage = GetComponent<Image>();
        m_DefaultSprite = m_MapButtonImage.sprite;
    }

    public void SetStatePressed(bool state)
    {
        buttonPressed = state;
        m_MapButtonImage.sprite = state ? PressedSprite : m_DefaultSprite;
    }
}
